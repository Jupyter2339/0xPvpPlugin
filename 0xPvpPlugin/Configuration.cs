using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using OPP.Services;

namespace OPP.Config {
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public float SelectDistance { get; set; } = 20;
        public bool AutoSelect { get; set; } = false;
        public bool KT { get; set; } = false;
        public bool noPaladin { get; set; } = true;
        public bool noDarknight { get; set; } = true;
        public bool noPretected { get; set; } = true;
        public bool noSamuraiWithDT { get; set; } = false;
        public bool KeepSD { get; set; } = true;
        public bool noMS { get; set; } = true;

        [NonSerialized]
        public List<PlayerCharacter> EnermyActors = new List<PlayerCharacter>();
        public PlayerCharacter LocalPlayer;

        public void Save()
        {
            Service.PluginInterface.SavePluginConfig(this);
        }
    }
}

