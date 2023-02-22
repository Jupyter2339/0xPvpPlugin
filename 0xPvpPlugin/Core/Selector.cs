using Dalamud.Game.ClientState.Objects.SubKinds;
using System;
using FFXIVClientStructs.FFXIV.Client.Game;
using Action = Lumina.Excel.GeneratedSheets.Action;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using OPP.Services;
using OPP.Config;

namespace OPP.Select
{
    public sealed class Selector
    {
        static DateTime LastSelectTime;

        static uint xdtz => Service.DataManager.GetExcelSheet<Action>().GetRow(29515).RowId;
        //uint ID = 11;

        static ushort bhbuff = 1301; //被保护

        private static CanAttackDelegate CanAttack;
        private delegate int CanAttackDelegate(int arg, IntPtr objectAddress);
        //private const int CanAttackOffset = 0x802840;//Struct121_IntPtr_17

        internal static void Init()
        {
            //CanAttack = Marshal.GetDelegateForFunctionPointer<CanAttackDelegate>(Process.GetCurrentProcess().MainModule.BaseAddress + CanAttackOffset);
            CanAttack = Marshal.GetDelegateForFunctionPointer<CanAttackDelegate>(Service.Scanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B DA 8B F9 E8 ?? ?? ?? ?? 4C 8B C3"));
        }

        internal static void DoingSelect()
        {
            if (Service.Configuration.LocalPlayer == null) {
                return;
            }
            lock (Service.Configuration.EnermyActors)
            {
                Service.Configuration.EnermyActors.Clear();
                if (Service.objectTable == null)
                {
                    return;
                }
                foreach (var obj in Service.objectTable)
                {
                    try
                    {
                        if (obj != null && obj.ObjectId != Service.Configuration.LocalPlayer.ObjectId & obj.Address.ToInt64() != 0 && CanAttack(142, obj.Address) == 1)
                        {
                            PlayerCharacter rcTemp = obj as PlayerCharacter;
                            if (rcTemp != null)
                            {
                                if (Service.Configuration.noPaladin && rcTemp.ClassJob.Id == 19) {
                                    continue;
                                }
                                else if (Service.Configuration.noDarknight && rcTemp.ClassJob.Id == 32) {
                                    continue;
                                }
                                else 
                                { 
                                    Service.Configuration.EnermyActors.Add(rcTemp);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }
            if (Service.Configuration.AutoSelect)
            {
                DateTime now = DateTime.Now;
                if (LastSelectTime == null || (now - LastSelectTime).TotalMilliseconds > 50) //每50ms选一次
                {
                    Select();
                    LastSelectTime = now;
                }
            }
        }

        private unsafe static void Select()
        {
            if (Service.Configuration.LocalPlayer == null || Service.Configuration.EnermyActors == null)
            {
                return;
            }
            PlayerCharacter selectActor = null;
            foreach (PlayerCharacter actor in Service.Configuration.EnermyActors)
            {
                try
                {
                    //var distance2D = Math.Sqrt(Math.Pow(clientState.LocalPlayer.Position.X - actor.Position.X, 2) + Math.Pow(clientState.LocalPlayer.Position.Y - actor.Position.Y, 2)) - 1;
                    var distance2D = Math.Sqrt(Math.Pow(actor.YalmDistanceX, 2) + Math.Pow(actor.YalmDistanceZ, 2)) - 6;

                    //if (distance2D <= Configuration.SelectDistance && actor.CurrentHp != 0 && (selectActor == null || actor.CurrentHp < selectActor.CurrentHp))
                    if (distance2D <= Service.Configuration.SelectDistance && actor.CurrentHp != 0 && actor.CurrentHp <= actor.MaxHp / 2)
                    {
                        if (Service.Configuration.noPretected && HasEffect(bhbuff, actor)) {
                            continue;
                        }
                        else 
                        {
                            selectActor = actor;
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }
            if (selectActor != null)
            {
                Service.TargetManaget.SetTarget(selectActor);

                if (Service.Configuration.KT && selectActor.CurrentHp <= ((selectActor.MaxHp / 2) - 2)) {
                    try {
                        ActionManager.Instance()->UseAction(ActionType.Spell, xdtz, Service.Configuration.LocalPlayer.TargetObjectId);
                    }
                    catch (Exception) { }
                }
            }
        }

        public static bool HasEffect(ushort effectID, GameObject? obj) => GetStatus(effectID, obj, null) is not null;
        internal static Dalamud.Game.ClientState.Statuses.Status? GetStatus(uint statusID, GameObject? obj, uint? sourceID)
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
    }
}
