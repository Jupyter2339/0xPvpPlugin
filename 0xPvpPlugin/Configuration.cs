using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Numerics;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState;

namespace NKPlugin.Windows;
[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool ConfigWindowVisible = false;
    public Vector2 ConfigWindowPos = new(20, 20);
    public Vector2 ConfigWindowSize = new(300, 300);
    public float ConfigWindowBgAlpha = 1;

    public float SelectDistance { get; set; } = 20;
    public float SelectInterval = 50;
    public bool Includ_NPC = false;
    public bool AutoSelect { get; set; } = false;
    public bool K { get; set; } = false;

    [NonSerialized]
    public DalamudPluginInterface? pluginInterface;
    public ObjectTable nearObjects;
    public List<PlayerCharacter> EnermyActors = new List<PlayerCharacter>();
    public PlayerCharacter LocalPlayer;

    public void Initialize(DalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Save()
    {
        this.pluginInterface!.SavePluginConfig(this);
    }
}
