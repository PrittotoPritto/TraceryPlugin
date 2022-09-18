using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Newtonsoft.Json.Linq;

namespace TraceryPlugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 1;

        public JObject BaseGrammar = new JObject();

        public List<KeyValuePair<string, string>> SavedItems = new List<KeyValuePair<string, string>>();

        //public bool Stage;
        //Hopefully I can give the option to just stage the results instead of posting them immediately

        public string SavePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);

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
