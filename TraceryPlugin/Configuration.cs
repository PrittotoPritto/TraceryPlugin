using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace TraceryPlugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public bool IsAlliance { get; set; } = false;

        public string SavePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
}
