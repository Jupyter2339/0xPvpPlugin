using Dalamud.Game.ClientState.Objects.SubKinds;
using System;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Hooking;
using System.Collections.Generic;
using OPP.Select;
using OPP.Services;

namespace OPP.Hook
{
    public sealed class IconHook {

        private static IntPtr actionManager = IntPtr.Zero;
        private delegate uint GetIconDelegate(IntPtr actionManager, uint actionID);
        private static Hook<GetIconDelegate> getIconHook;
        private static IntPtr GetAdjustedActionId;
        private static ushort sybuff = (ushort)1317; //三印
        private static ushort dtbuff = (ushort)1240; //必杀剑·地天

        public static void Init() {
            GetAdjustedActionId = Service.Scanner.ScanText("E8 ?? ?? ?? ?? 8B F8 3B DF");
            getIconHook = Hook<GetIconDelegate>.FromAddress(GetAdjustedActionId, GetIconDetour);
        }

        public static void DoingHook() { 
            getIconHook.Enable();
        }

        public static void StopDoingHook() {
            getIconHook.Disable();
        }
        public static bool IsHooking() {
            return getIconHook.IsEnabled;
        }
        public static void Dispose() {
            getIconHook?.Dispose();
        }

        internal static uint OriginalHook(uint actionID) => getIconHook.Original(actionManager, actionID);
        private static unsafe uint GetIconDetour(IntPtr actionManager, uint actionID) {
            try {
                if (Service.Configuration.KeepSD && actionID == 29513)  //缩地不受三印影响
                {
                    return actionID;
                }
                if (Service.Configuration.noMS && Service.Configuration.LocalPlayer != null && actionID == 29507 && HasEffect(sybuff, Service.Configuration.LocalPlayer))  //防止重复点三印点出命水
                {
                    return 29657;
                }
                if (Service.Configuration.LocalPlayer != null && Service.Configuration.LocalPlayer.TargetObject != null) {
                    PlayerCharacter actor = Service.Configuration.LocalPlayer.TargetObject as PlayerCharacter;
                    if (Service.Configuration.LocalPlayer.CurrentHp != 0 && actor.CurrentHp != 0 && actor.CurrentHp >= ((actor.MaxHp / 2) - 2) && actionID == 29515) {
                        return 29657;
                    }
                    else if (Service.Configuration.noSamuraiWithDT && HasEffect(dtbuff, actor)) 
                    {
                        if (actionID == 29515 || actionID == 29506 || actionID == 29054 || actionID == 29054 || actionID == 29055 || actionID == 29056 || actionID == 29057 || actionID == 29711) {
                            return OriginalHook(actionID);
                        }
                        else {
                            return 29657;
                        }
                    }
                    else {
                        return OriginalHook(actionID);
                    }
                }
                else {
                    return OriginalHook(actionID);
                }
            }
            catch (Exception e) {
                return OriginalHook(actionID);
            }
        }

        public static bool HasEffect(ushort effectID, GameObject? obj) => GetStatus(effectID, obj, null) is not null;
        internal static Dalamud.Game.ClientState.Statuses.Status? GetStatus(uint statusID, GameObject? obj, uint? sourceID) {
            Dictionary<(uint StatusID, uint? TargetID, uint? SourceID), Dalamud.Game.ClientState.Statuses.Status?> statusCache = new();
            uint InvalidObjectID = 0xE000_0000;
            var key = (statusID, obj?.ObjectId, sourceID);
            if (statusCache.TryGetValue(key, out Dalamud.Game.ClientState.Statuses.Status? found))
                return found;

            if (obj is null)
                return statusCache[key] = null;

            if (obj is not BattleChara chara)
                return statusCache[key] = null;
            foreach (Dalamud.Game.ClientState.Statuses.Status? status in chara.StatusList) {
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
    }
}
