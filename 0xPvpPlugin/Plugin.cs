using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Reflection;
using Dalamud.Interface.Windowing;
using OPP.Windows;
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
using System.Linq;
using System.Collections;
using Dalamud.Plugin.Services;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.Interop;
using Lumina.Excel.GeneratedSheets2;

namespace OPP
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "0xPvpPlugin";
        private const string CommandName = "/0x";

        private DalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        [PluginService] public static ITargetManager TargetManaget { get; private set; } = null!;
        public Configuration Configuration { get; init; }
        [PluginService] public static IClientState clientState { get; private set; } = null!;
        [PluginService] public static IGameInteropProvider gameInteropProvider { get; private set; }
        [PluginService] public static IChatGui chatGui { get; private set; } = null!;
        public WindowSystem WindowSystem = new("0xPvpPlugin");
        [PluginService] public static IObjectTable objectTable { get; private set; }
        [PluginService] internal static IFramework Framework { get; private set; } = null!;
        [PluginService] internal static ISigScanner Scanner { get; set; }
        //internal PluginAddressResolver Address{get; set; } = null!;
        [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
        internal static ActionManager ActionManager { get; private set; }
        [PluginService]
        internal static IPluginLog pluginLog { get; private set; }

        DateTime LastSelectTime;

        uint xdtz = (uint)29515; //星遁天诛

        private bool drawConfigWindow;
        Action _action => DataManager.GetExcelSheet<Action>().GetRow(xdtz);
        uint ID => _action.RowId;
        //uint ID = 11;

        ushort bhbuff = (ushort)1301; //被保护
        ushort sybuff = (ushort)1317; //三印
        ushort dtbuff = (ushort)1240; //必杀剑·地天

        private IntPtr actionManager = IntPtr.Zero;
        private delegate uint GetIconDelegate(IntPtr actionManager, uint actionID);
        //private readonly Hook<GetIconDelegate> getIconHook;

        [Signature("E8 ?? ?? ?? ?? 8B F8 3B DF", DetourName = nameof(GetIconDelegate))]
        private Hook<GetIconDelegate> getIconHook { get; init; }

        #region 更新地址
        private IntPtr GetAdjustedActionId;
        private CanAttackDelegate CanAttack;
        private delegate int CanAttackDelegate(int arg, IntPtr objectAddress);
        //private const int CanAttackOffset = 0x802840;//Struct121_IntPtr_17
        #endregion

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            //chatGui.Print("0xPvpPlugin Initialize.");
            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);
            this.CommandManager.AddHandler("/0x", new CommandInfo(OnCommand)
            {
                HelpMessage = "/0x settings"
            }) ;

            //CanAttack = Marshal.GetDelegateForFunctionPointer<CanAttackDelegate>(Process.GetCurrentProcess().MainModule.BaseAddress + CanAttackOffset);
            CanAttack = Marshal.GetDelegateForFunctionPointer<CanAttackDelegate>(Scanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B DA 8B F9 E8 ?? ?? ?? ?? 4C 8B C3"));

            GetAdjustedActionId = Scanner.ScanText("E8 ?? ?? ?? ?? 8B F8 3B DF");

            if(gameInteropProvider is null) pluginLog.Debug(Convert.ToString("1234"));
            pluginLog.Debug(Convert.ToString(gameInteropProvider));

            getIconHook = gameInteropProvider.HookFromAddress<GetIconDelegate>(GetAdjustedActionId, GetIconDetour);
            getIconHook.Enable();

            Framework.Update += this.OnFrameworkUpdate;
            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += OnOpenConfig;
        }


        
        internal uint OriginalHook(uint actionID) => getIconHook.Original(actionManager, actionID);
        private unsafe uint GetIconDetour(IntPtr actionManager, uint actionID) {
            try {
                if (clientState.IsPvP && clientState.LocalPlayer != null)
                {
                    //忍者
                    if (clientState.LocalPlayer.ClassJob.Id == 30)
                    {

                        if (actionID == 29513)  //缩地不受三印影响
                        {
                            return actionID;
                        }

                        if (clientState.LocalPlayer != null && actionID == 29507 && HasEffect(sybuff, clientState.LocalPlayer))  //防止重复点三印点出命水
                        {
                            return 29657;
                        }

                        if (clientState.LocalPlayer.TargetObject != null)
                        {
                            PlayerCharacter actor = clientState.LocalPlayer.TargetObject as PlayerCharacter;
                            if (clientState.LocalPlayer.CurrentHp != 0 && actor.CurrentHp != 0 && actor.CurrentHp >= ((actor.MaxHp / 2) - 2) && actionID == 29515) //星遁天诛
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
                        else if (clientState.LocalPlayer.CurrentHp != 0 && actionID == 29515)
                        {
                            return 29657;
                        }
                        else
                        {
                            return OriginalHook(actionID);
                        }
                    }
                    //武士
                    else if (clientState.LocalPlayer.ClassJob.Id == 34)
                    {
                        if (Configuration.SLBX && actionID == 29524)  //冰雪
                        {
                            return 29523;
                        }
                        else {
                            return OriginalHook(actionID);
                        }
                    }
                    //其他职业
                    else {
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
                if (actionID == 29515)
                {
                    return 29657;
                }
                else { 
                    return OriginalHook(actionID);
                }
            }
        }



        public bool HasEffect(ushort effectID, GameObject? obj) => GetStatus(effectID, obj, null) is not null;

        internal Dalamud.Game.ClientState.Statuses.Status? GetStatus(uint statusID, GameObject? obj, uint? sourceID)
        {
            if (obj is null || obj is not BattleChara chara) return null;

            //Dictionary<(uint StatusID, uint? TargetID, uint? SourceID), Dalamud.Game.ClientState.Statuses.Status?> statusCache = new();
            uint InvalidObjectID = 0xE000_0000;
            //var key = (statusID, obj?.ObjectId, sourceID);

            BattleChara charaTemp = (BattleChara) obj;

            foreach (Dalamud.Game.ClientState.Statuses.Status? status in charaTemp.StatusList)
            {
                if (status.StatusId == statusID && (!sourceID.HasValue || status.SourceId == 0 || status.SourceId == InvalidObjectID || status.SourceId == sourceID))
                {
                    return status;
                }
            }
            return null;
        }

        internal Dalamud.Game.ClientState.Statuses.Status? GetStatusFromMe(uint statusID, GameObject? obj)
        {
            if (obj is null || obj is not BattleChara chara) return null;
            BattleChara charaTemp = (BattleChara)obj;
            //chatGui.Print(charaTemp.StatusList.Count().ToString());
            foreach (Dalamud.Game.ClientState.Statuses.Status status in charaTemp.StatusList)
            {
                //chatGui.Print(status.StatusId.ToString());
                //chatGui.Print(status.SourceId.ToString());
                //chatGui.Print(clientState.LocalPlayer.ObjectId.ToString());
                //chatGui.Print(status.SourceObject?.OwnerId.ToString());
                if (status.StatusId == statusID && (status.SourceId == clientState.LocalPlayer.ObjectId || status.SourceObject?.OwnerId == clientState.LocalPlayer.ObjectId))
                {
                    return status;
                }
            }
            return null;
        }

        public void Dispose()
        {
            getIconHook?.Dispose();
            this.CommandManager.RemoveHandler("/0x");
            Framework.Update -= this.OnFrameworkUpdate;
            this.WindowSystem.RemoveAllWindows();
            this.CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            if (command == "/0x" && args == "settings")
            {
                drawConfigWindow = !drawConfigWindow;
            }
            if (command == "/0x" && args == "autoON")
            {
                chatGui.Print("———AUTO ON———");
                Configuration.AutoSelect = true;

                //TargetManaget.Target = null;

                //var distance2D = Math.Sqrt(Math.Pow(clientState.LocalPlayer.TargetObject.YalmDistanceX, 2) + Math.Pow(clientState.LocalPlayer.TargetObject.YalmDistanceZ, 2)) - 6;
                //distance2D = Math.Sqrt(Math.Pow(clientState.LocalPlayer.Position.X - clientState.LocalPlayer.TargetObject.Position.X, 2) + Math.Pow(clientState.LocalPlayer.Position.Y - clientState.LocalPlayer.TargetObject.Position.Y, 2)) - 1;
                //pluginLog.Error(distance2D.ToString());
            }
            if (command == "/0x" && args == "autoOFF")
            {
                chatGui.Print("———AUTO OFF———");
                Configuration.AutoSelect = false;
            }
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
            if (command == "/0x" && args == "SLBXON")
            {
                chatGui.Print("——SLBX: ON——");
                Configuration.SLBX = true;
            }
            if (command == "/0x" && args == "SLBXOFF")
            {
                chatGui.Print("——SLBX: OFF——");
                Configuration.SLBX = false;
            }
        }
        private void OnFrameworkUpdate(IFramework framework)
        {
            try
            {
                if (clientState.LocalPlayer != null && clientState.IsPvP && clientState.LocalPlayer.CurrentHp != 0 && Configuration.AutoSelect)
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
                        //if (obj != null && (obj.ObjectId != clientState.LocalPlayer.ObjectId) & obj.Address.ToInt64() != 0 )
                        if (obj != null && (obj.ObjectId != clientState.LocalPlayer.ObjectId) & obj.Address.ToInt64() != 0 && CanAttack(142, obj.Address) == 1)
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
            if (clientState.LocalPlayer == null || Configuration.EnermyActors == null)
            {
                return;
            }
            PlayerCharacter selectActor = null;
            if (clientState.LocalPlayer.ClassJob.Id == 30)
            {
                if (clientState.LocalPlayer.TargetObject != null && clientState.LocalPlayer.TargetObject is PlayerCharacter)
                {
                    PlayerCharacter temp = clientState.LocalPlayer.TargetObject as PlayerCharacter;
                    if (temp.CurrentHp != 0 && temp.ClassJob.Id != 19 && temp.ClassJob.Id != 32)
                    {
                        Configuration.EnermyActors.Insert(0, temp);
                    }
                }

                foreach (PlayerCharacter actor in Configuration.EnermyActors)
                {
                    try
                    {
                        //pluginLog.Error(Convert.ToString(actor.Name));

                        //var distance2D = Math.Sqrt(Math.Pow(clientState.LocalPlayer.Position.X - actor.Position.X, 2) + Math.Pow(clientState.LocalPlayer.Position.Y - actor.Position.Y, 2)) - 1;
                        var distance2D = Math.Sqrt(Math.Pow(actor.YalmDistanceX, 2) + Math.Pow(actor.YalmDistanceZ, 2)) - 6;
                        //var distance2D = 30;

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
            }
            if (selectActor != null)
            {
                TargetManaget.Target = selectActor;
            }
        }
            private void DrawUI()
        {
            //this.WindowSystem.Draw();
            drawConfigWindow = drawConfigWindow && Configuration.DrawConfigUI();
        }

        public void OnOpenConfig()
        {
            drawConfigWindow = true;
            //WindowSystem.GetWindow("NKP设置").IsOpen = true;
            //gui.IsOpen = true;
        }
    }
}
