using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState;
using Dalamud.Game;
using Dalamud.Data;
using Dalamud.Game.Gui;
using OPP.Config;
using ImGuiNET;
using System.Drawing;
using System.Numerics;
using System;
using OPP.Services;

namespace OPP.Window
{
    public sealed class ConfigWindow : Dalamud.Interface.Windowing.Window, IDisposable {
        private bool visible = false;
        public bool Visible {
            get => visible;
            set => visible = value;
        }

        public ConfigWindow() : base("OOP Config", ImGuiWindowFlags.AlwaysAutoResize) {
            RespectCloseHotkey = true;

            SizeCondition = ImGuiCond.FirstUseEver;
            Size = new Vector2(740, 490);
        }

        public override void Draw() {
            DrawConfig();
        }

        public void DrawConfig() {
            if (!Visible) {
                return;
            }

            if (ImGui.Begin("OOP Config", ref visible)) {

                bool AutoSelect = Service.Configuration.AutoSelect;
                if (ImGui.Checkbox("自动选择", ref AutoSelect)) {
                    Service.Configuration.AutoSelect = AutoSelect;
                    Service.Configuration.Save();
                }

                float SelectDistance = Service.Configuration.SelectDistance;
                if (ImGui.SliderFloat("选择范围", ref SelectDistance, 5f, 25f, "%.1f")) {
                    Service.Configuration.SelectDistance = SelectDistance;
                    Service.Configuration.Save();
                }

                bool noPaladin = Service.Configuration.noPaladin;
                if (ImGui.Checkbox("不选择骑士", ref noPaladin)) {
                    Service.Configuration.noPaladin = noPaladin;
                    Service.Configuration.Save();
                }

                bool noDarknight = Service.Configuration.noDarknight;
                if (ImGui.Checkbox("不选择DK", ref noDarknight)) {
                    Service.Configuration.noDarknight = noDarknight;
                    Service.Configuration.Save();
                }

                bool noPretected = Service.Configuration.noPretected;
                if (ImGui.Checkbox("不选择被保护的敌人", ref noPretected)) {
                    Service.Configuration.noPretected = noPretected;
                    Service.Configuration.Save();
                }

                bool noSamuraiWithDT = Service.Configuration.noSamuraiWithDT;
                if (ImGui.Checkbox("不打地天武士", ref noSamuraiWithDT)) {
                    Service.Configuration.noSamuraiWithDT = noSamuraiWithDT;
                    Service.Configuration.Save();
                }

                bool KeepSD = Service.Configuration.KeepSD;
                if (ImGui.Checkbox("保留缩地", ref KeepSD)) {
                    Service.Configuration.KeepSD = KeepSD;
                    Service.Configuration.Save();
                }

                bool noMS = Service.Configuration.noMS;
                if (ImGui.Checkbox("屏蔽命水", ref noMS)) {
                    Service.Configuration.noMS = noMS;
                    Service.Configuration.Save();
                }

                bool KT = Service.Configuration.KT;
                if (ImGui.Checkbox("自动星遁天诛", ref KT)) {
                    Service.Configuration.KT = KT;
                    Service.Configuration.Save();
                }
            }
        }


        public void Dispose() {

        }
    }
}
