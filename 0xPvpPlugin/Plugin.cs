using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Reflection;
using Dalamud.Interface.Windowing;
using NKPlugin.Windows;
using Dalamud.Game.ClientState.Objects.SubKinds;
using System.Runtime.InteropServices;
using System;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState;
using Dalamud.Game;
using Dalamud.Data;
using Action = Lumina.Excel.GeneratedSheets.Action;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using System.Diagnostics;
using System.ComponentModel;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Game.ClientState.Statuses;
using Lumina.Excel.GeneratedSheets;
using FFXIVClientStructs.STD;
using System.Collections.Generic;
using System.Numerics;

namespace NKPlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Ninja KT Plugin";
        private const string CommandName = "/nkp";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        [PluginService] public TargetManager TargetManaget { get; init; } = null!;
        public Configuration Configuration { get; init; }
        [PluginService] public ClientState clientState { get; init; } = null!;
        [PluginService] public static ChatGui chatGui { get; private set; } = null!;
        public WindowSystem WindowSystem = new("NKPlugin");
        [PluginService]
        internal ObjectTable objectTable { get; init; } = null!;
        [PluginService]
        internal Framework Framework { get; init; } = null!;
        [PluginService]
        internal static SigScanner Scanner{get; private set;}
        //internal PluginAddressResolver Address{get; set; } = null!;
        [PluginService]
        internal static DataManager DataManager { get; private set; } = null!;
        internal static ActionManager ActionManager { get; private set; }

        DateTime LastSelectTime;

        uint xdtz = (uint)29515; //星遁天诛
        //uint xdtz = (uint)29067; //圣盾阵
        Action _action => DataManager.GetExcelSheet<Action>().GetRow(xdtz);
        uint ID => _action.RowId;
        //uint ID = 11;

        ushort bhbuff = (ushort)1301; //被保护
        ushort sybuff = (ushort)1317; //三印
        ushort dtbuff = (ushort)1240; //必杀剑·地天

        private IntPtr actionManager = IntPtr.Zero;
        private delegate uint GetIconDelegate(IntPtr actionManager, uint actionID);
        private readonly Hook<GetIconDelegate> getIconHook;

        #region 更新地址
        private IntPtr GetAdjustedActionId;
        private CanAttackDelegate CanAttack;
        private delegate int CanAttackDelegate(int arg, IntPtr objectAddress);
        //private const int CanAttackOffset = 0x802840;//Struct121_IntPtr_17
        #endregion

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            chatGui.Print("0xsPvpPlugin Initialize.");
            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);
            this.CommandManager.AddHandler("/0x", new CommandInfo(OnCommand)
            {
                HelpMessage = "_ → x0\\"
            }) ;

            //CanAttack = Marshal.GetDelegateForFunctionPointer<CanAttackDelegate>(Process.GetCurrentProcess().MainModule.BaseAddress + CanAttackOffset);
            CanAttack = Marshal.GetDelegateForFunctionPointer<CanAttackDelegate>(Scanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B DA 8B F9 E8 ?? ?? ?? ?? 4C 8B C3"));

            GetAdjustedActionId = Scanner.ScanText("E8 ?? ?? ?? ?? 8B F8 3B DF");
            getIconHook = Hook<GetIconDelegate>.FromAddress(GetAdjustedActionId, GetIconDetour);
            getIconHook.Enable();

            this.Framework.Update += this.OnFrameworkUpdate;
            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }


        
        internal uint OriginalHook(uint actionID) => getIconHook.Original(actionManager, actionID);
        private unsafe uint GetIconDetour(IntPtr actionManager, uint actionID) {
            try {
                if (Configuration.AutoSelect)
                {
                    if (actionID == 29513)  //缩地不受三印影响
                    {
                        return actionID;
                    }
                    if (clientState.LocalPlayer != null && actionID == 29507 && HasEffect(sybuff, clientState.LocalPlayer))  //防止重复点三印点出命水
                    {
                        return 29657;
                    }
                    if (clientState.LocalPlayer != null && clientState.LocalPlayer.TargetObject != null)
                    {
                        PlayerCharacter actor = clientState.LocalPlayer.TargetObject as PlayerCharacter;
                        if (clientState.LocalPlayer.CurrentHp != 0 && actor.CurrentHp != 0 && actor.CurrentHp >= ((actor.MaxHp / 2) - 2) && actionID == 29515)
                        {
                            return 29657;
                        }
                        //else if (HasEffect(dtbuff, actor))
                        //{
                        //    if (actionID == 29515 || actionID == 29506 || actionID == 29054 || actionID == 29054 || actionID == 29055 || actionID == 29056 || actionID == 29057 || actionID == 29711)
                        //    {
                        //        return OriginalHook(actionID);
                        //    }
                        //    else
                        //    {
                        //        return 29657;
                        //    }
                        //}
                        else
                        {
                            return OriginalHook(actionID);
                        }
                    }
                    else
                    {
                        return OriginalHook(actionID);
                    }
                }
                else
                {
                    return OriginalHook(actionID);
                }
            }
            catch(Exception e)
            {
                return OriginalHook(actionID);
            }
        }



        public bool HasEffect(ushort effectID, GameObject? obj) => GetStatus(effectID, obj, null) is not null;
        internal Dalamud.Game.ClientState.Statuses.Status? GetStatus(uint statusID, GameObject? obj, uint? sourceID)
        {
            Dictionary<(uint StatusID, uint? TargetID, uint? SourceID), Dalamud.Game.ClientState.Statuses.Status?> statusCache = new();
            uint InvalidObjectID = 0xE000_0000;
            var key = (statusID, obj?.ObjectId, sourceID);
            if (statusCache.TryGetValue(key, out Dalamud.Game.ClientState.Statuses.Status? found))
                return found;

            if (obj is null)
                return statusCache[key] = null;

            if (obj is not BattleChara chara)
                return statusCache[key] = null;
            foreach (Dalamud.Game.ClientState.Statuses.Status? status in chara.StatusList)
            {
                if (status.StatusId == statusID && (!sourceID.HasValue || status.SourceId == 0 || status.SourceId == InvalidObjectID || status.SourceId == sourceID))
                    return statusCache[key] = status;
            }
            return statusCache[key] = null;
        }

        //3190: ["星遁天诛", 0],
        //3192: ["星遁天诛预备", 14945],
        //3194: ["无法发动冰晶乱流之术", 14947],
        //public bool IsOnCooldown(uint actionID) => GetCooldown(actionID).IsCooldown;
        //internal unsafe CooldownData GetCooldown(uint actionID)
        //{
        //    if (cooldownCache.TryGetValue(actionID, out CooldownData found))
        //        return found;

        //    ActionManager* actionManager = ActionManager.Instance();
        //    if (actionManager == null)
        //        return cooldownCache[actionID] = default;

        //    byte cooldownGroup = GetCooldownGroup(actionID);

        //    RecastDetail* cooldownPtr = actionManager->GetRecastGroupDetail(cooldownGroup - 1);
        //    if (cooldownPtr is null)
        //    {
        //        CooldownData data = new();
        //        data.CooldownTotal = -1;

        //        return cooldownCache[actionID] = data;
        //    }

        //    cooldownPtr->ActionID = actionID;

        //    return cooldownCache[actionID] = *(CooldownData*)cooldownPtr;
        //}




        public void Dispose()
        {
            getIconHook?.Dispose();
            this.CommandManager.RemoveHandler("/0x");
            this.Framework.Update -= this.OnFrameworkUpdate;
            this.WindowSystem.RemoveAllWindows();
            this.CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            if (command == "/0x" && args == "autoON")
            {
                chatGui.Print("———AUTO ON———");
                Configuration.AutoSelect = true;
            }
            if (command == "/0x" && args == "autoOFF")
            {
                chatGui.Print("———AUTO OFF———");
                Configuration.AutoSelect = false;
            }
            //if (command == "/0x" && args == "KON")
            //{
            //    chatGui.Print("———K ON———");
            //    Configuration.K = true;
            //}
            //if (command == "/0x" && args == "KOFF")
            //{
            //    chatGui.Print("———K OFF———");
            //    Configuration.K = false;
            //}
            if (command == "/0x" && args == "20")
            {
                chatGui.Print("——Distance: 20——");
                Configuration.SelectDistance = 20;
            }
            if (command == "/0x" && args == "25")
            {
                chatGui.Print("——Distance: 25——");
                Configuration.SelectDistance = 25;
            }
        }
        private void OnFrameworkUpdate(Framework framework)
        {
            try
            {
                if (clientState.LocalPlayer != null && clientState.LocalPlayer.CurrentHp != 0 && Configuration.AutoSelect && clientState.IsPvP)
                {
                    this.ReFreshEnermyActors_And_AutoSelect();
                }
            }catch(Exception ex) { }
        }

        private void ReFreshEnermyActors_And_AutoSelect()
        {
            if (clientState.LocalPlayer == null)
            {
                return;
            }
            Configuration.LocalPlayer = clientState.LocalPlayer;
            lock (Configuration.EnermyActors)
            {
                Configuration.EnermyActors.Clear();
                if (objectTable == null)
                {
                    return;
                }
                foreach (var obj in objectTable)
                {
                    try
                    {
                        if (obj != null && (obj.ObjectId != Configuration.LocalPlayer.ObjectId) & obj.Address.ToInt64() != 0 && CanAttack(142, obj.Address) == 1)
                        {
                            PlayerCharacter rcTemp = obj as PlayerCharacter;
                            //19 骑士   32 DK
                            if (rcTemp != null && rcTemp.ClassJob.Id != 19 && rcTemp.ClassJob.Id != 32)
                            {
                                Configuration.EnermyActors.Add(rcTemp);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }
            if (Configuration.AutoSelect)
            {
                DateTime now = DateTime.Now;
                if (LastSelectTime == null || (now - LastSelectTime).TotalMilliseconds > Configuration.SelectInterval)
                {
                    SelectEnermyOnce();
                    LastSelectTime = now;
                }
            }
        }

        private void SelectEnermyOnce()
        {
            if (Configuration.LocalPlayer == null || Configuration.EnermyActors == null)
            {
                return;
            }
            PlayerCharacter selectActor = null;
            foreach (PlayerCharacter actor in Configuration.EnermyActors)
            {
                try
                {
                    //var distance2D = Math.Sqrt(Math.Pow(clientState.LocalPlayer.Position.X - actor.Position.X, 2) + Math.Pow(clientState.LocalPlayer.Position.Y - actor.Position.Y, 2)) - 1;
                    var distance2D = Math.Sqrt(Math.Pow(actor.YalmDistanceX, 2) + Math.Pow(actor.YalmDistanceZ, 2)) - 6;

                    //if (distance2D <= Configuration.SelectDistance && actor.CurrentHp != 0 && (selectActor == null || actor.CurrentHp < selectActor.CurrentHp))
                    if (distance2D <= Configuration.SelectDistance && actor.CurrentHp != 0 && actor.CurrentHp <= ((actor.MaxHp / 2)) && !HasEffect(bhbuff, actor))
                    {
                         selectActor = actor;
                        break;
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }
            if (selectActor != null)
            {
                TargetManaget.SetTarget(selectActor);

                //bool KFlag = false;
                //if (selectActor.CurrentHp < ((selectActor.MaxHp / 2) - 2000))
                //{
                //    KFlag = true;
                //}
                //if (KFlag && Configuration.K)
                //{
                //    try
                //    {
                //        K();
                //    }
                //    catch (Exception) { }
                //}
            }
        }

        unsafe private void K()
        {
            ActionManager.Instance()->UseAction(ActionType.Spell, ID, clientState.LocalPlayer.TargetObjectId);
        }

            private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            //WindowSystem.GetWindow("NKP设置").IsOpen = true;
        }
    }
}
