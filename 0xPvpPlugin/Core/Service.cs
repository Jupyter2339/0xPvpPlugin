using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState;
using Dalamud.Game;
using Dalamud.Data;
using Dalamud.Game.Gui;
using OPP.Config;
using OPP.Window;

namespace OPP.Services
{
    public sealed class Service
    {
        internal static DalamudPluginInterface PluginInterface { get; set; }
        internal static CommandManager CommandManager { get; set; }
        internal static Configuration Configuration { get; set; }
        internal static ConfigWindow ConfigWindow { get; set; }
        [PluginService] internal static TargetManager TargetManaget { get; private set; } = null!;
        [PluginService] internal static ClientState clientState { get; private set; } = null!;
        [PluginService] internal static ChatGui chatGui { get; private set; } = null!;
        [PluginService] internal static ObjectTable objectTable { get; private set; } = null!;
        [PluginService] internal static Framework Framework { get; private set; } = null!;
        [PluginService] internal static SigScanner Scanner { get; private set; }
        [PluginService] internal static DataManager DataManager { get; private set; } = null!;
    }
}
