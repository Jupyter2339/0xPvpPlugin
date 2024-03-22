using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Numerics;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState;
using ImGuiNET;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin.Services;

namespace OPP.Windows;
[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool ConfigWindowVisible = false;
    public Vector2 ConfigWindowPos = new(20, 20);
    public Vector2 ConfigWindowSize = new(300, 300);
    public float ConfigWindowBgAlpha = 1;

    public int SelectDistance = 20;
    public int SelectInterval = 50;
    public bool AutoSelect = false;
    public bool SLBX = false;
    public bool MS = true;
    public bool TD = true;
    public bool XDTZ = true;

    [NonSerialized]
    public DalamudPluginInterface? pluginInterface;
    [NonSerialized]
    private Plugin plugin;
    [PluginService]
    public static IChatGui chatGui { get; private set; } = null!;
    public IObjectTable nearObjects;
    public List<PlayerCharacter> EnermyActors = new List<PlayerCharacter>();
    public PlayerCharacter LocalPlayer;

    public void Initialize(DalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Init(Plugin plugin, DalamudPluginInterface pluginInterface)
    {
        this.plugin = plugin;
        this.pluginInterface = pluginInterface;
    }

    public bool DrawConfigUI()
    {
        var drawConfig = true;

        var scale = ImGui.GetIO().FontGlobalScale;

        var modified = false;

        ImGui.SetNextWindowSize(new Vector2(560 * scale, 200), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(new Vector2(560 * scale, 200), new Vector2(560 * scale, 200));
        ImGui.Begin("设置", ref drawConfig, ImGuiWindowFlags.NoCollapse);

        modified |= ImGui.SliderInt("选择范围(m)", ref SelectDistance, 5, 30);
        modified |= ImGui.SliderInt("选择间隔(ms)", ref SelectInterval, 50, 100);
        if (ImGui.Checkbox("自动选择", ref AutoSelect))
        {
            AutoSelect = AutoSelect;
            Save();
        }
        ImGui.End();


        if (modified)
        {
            Save();
        }

        return drawConfig;
    }

    public void Save()
    {
        pluginInterface.SavePluginConfig(this);
    }
}
