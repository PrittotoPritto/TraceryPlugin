using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Newtonsoft.Json.Linq;
using TraceryPlugin.GrammarBuilder;
using Lumina.Data.Parsing.Scd;

namespace TraceryPlugin
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 2;
        public bool IsConfigWindowMovable { get; set; } = true;

        public RuleSetCollection RuleSets = new();

        public string SavePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal)+"\\";
        public string GeneralSavePath = Environment.GetFolderPath(Environment.SpecialFolder.Personal) + "\\TraceryPlugin.json";

        public void Save()
        {
            Plugin.PluginInterface.SavePluginConfig(this);
        }
        public static RuleSetCollection GetDefaultRules()
        {
            RuleSetCollection RuleSets = new RuleSetCollection();
            RuleSet WelcomeRules = new RuleSet();
            WelcomeRules.Name = "Welcome";
            //This is hideous, I know.
            WelcomeRules.Rules.Add(new Rule("welcome", new List<string> { "#greet# <t> to #venue#!" }));
            WelcomeRules.Rules.Add(new Rule("venue", new List<string> { "the party", "the club", "the shindig", "the festivities" }));
            WelcomeRules.Rules.Add(new Rule("greet", new List<string> { "welcomes", "#adverb# welcomes" }));
            WelcomeRules.Rules.Add(new Rule("adverb", new List<string> { "warmly", "cheerfully", "happily" }));
            WelcomeRules.Rules.Add(new Rule("welcomeemote", new List<string> { "joy", "wave", "laliho" }));
            RuleSets.Add(WelcomeRules);
            RuleSet ExtraItems = new RuleSet();
            ExtraItems.Name = "Extra Items";
            ExtraItems.SourcePath = RuleSet.ExtraItemsPathMarker;
            RuleSets.Add(ExtraItems);
            return RuleSets;
        }
    }
}
