using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using Dalamud.Utility;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace TraceryPlugin.GrammarBuilder
{
    [Serializable]
    public class Rule
    {
        public Rule(string InName = "", IEnumerable<string>? InitialChildren = null)
        {
            if (InitialChildren != null)
            {
                foreach (string Child in InitialChildren)
                {
                    Children.Add(Child);
                }
            }
            Name = InName;
        }

        public string Name = "";
        public List<string> Children = new List<string>();
        public MergeStrategy? Strategy;
        [NonSerialized]
        public MergeResult MergeResult = MergeResult.Unmerged;
        public ResultType ResultType() { return Merge.GetResultType(MergeResult); }
    }

    [Serializable]
    public class RuleSet //Not calling this a Grammar to avoid confusion with Tracery
    {
        public const uint RuleNameLength = 32;
        public const uint ItemLength = 256;
        public static readonly string ExtraItemsPathMarker = "__EXTRA_ITEMS__";

        public string Name = "";
        public List<Rule> Rules = new List<Rule>();
        public MergeStrategy Strategy;

        public string? SourcePath;

        public ResultType ResultType()
        {
            if (Rules.Count == 0)
            {
                return GrammarBuilder.ResultType.OK;
            }
            ResultType Result = GrammarBuilder.ResultType.Unmerged;
            foreach (Rule Rule in Rules)
            {
                GrammarBuilder.ResultType RuleResult = Rule.ResultType();
                if (RuleResult > Result)
                {
                    Result = RuleResult;
                }
            }
            return Result;
        }

        public JObject ForGrammar()
        {
            JObject Ret = new JObject();
            foreach (Rule Rule in Rules)
            {
                Ret.Add(Rule.Name, JArray.FromObject(Rule.Children));
            }
            return Ret;
        }

        public bool IsExtraItems() { return SourcePath == ExtraItemsPathMarker; } 
    }

    public class RuleSetCollection : List<RuleSet> {}
}
