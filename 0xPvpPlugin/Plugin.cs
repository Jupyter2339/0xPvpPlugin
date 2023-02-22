using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using OPP.Config;
using OPP.Services;
using OPP.Hook;
using System;
using Dalamud.Game;
using OPP.Select;
using OPP.Window;

namespace OPP
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "0xPvpPlugin";
        private const string CommandName = "/0x";

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            Service.PluginInterface = pluginInterface;
            Service.CommandManager = commandManager;
            Service.chatGui.Print("0xPvpPlugin Initialize.");
            Service.Configuration = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            //Service.Configuration.Initialize(Service.PluginInterface);
            Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "_ → x0\\"
            }) ;

            Selector.Init();
            IconHook.Init();

            Service.Framework.Update += this.OnFrameworkUpdate;
            Service.PluginInterface.UiBuilder.Draw += DrawUI;
            Service.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            IconHook.Dispose();
            Service.CommandManager.RemoveHandler(CommandName);
            Service.Framework.Update -= this.OnFrameworkUpdate;
        }

        private void OnCommand(string command, string args)
        {
            if (command == CommandName && args == "on")
            {
                Service.Configuration.AutoSelect = true;
            }
            if (command == CommandName && args == "off")
            {
                Service.Configuration.AutoSelect = false;
            }
            if (command == CommandName && args == "setting") {
                Service.ConfigWindow.visible = true;
            }
        }
        private void OnFrameworkUpdate(Framework framework)
        {
            try
            {
                Service.Configuration.LocalPlayer = Service.clientState.LocalPlayer;
                if (Service.Configuration.LocalPlayer != null && Service.Configuration.LocalPlayer.CurrentHp != 0 && Service.Configuration.AutoSelect && Service.clientState.IsPvP) {
                    if (!IconHook.IsHooking()) {
                        IconHook.DoingHook();
                    }
                    Selector.DoingSelect();
                }
                else {
                    if (IconHook.IsHooking()) {
                        IconHook.StopDoingHook();
                    }
                }
            }catch(Exception ex) { }
        }

        private void DrawUI()
        {
            Service.ConfigWindow.Draw();
        }

        public void DrawConfigUI()
        {
            Service.ConfigWindow.visible = true;
        }
    }
}
