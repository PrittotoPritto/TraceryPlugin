using Lumina.Data.Parsing.Scd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using static FFXIVClientStructs.FFXIV.Client.UI.Agent.AgentSatisfactionSupply;

namespace TraceryPlugin.GrammarBuilder
{

    public enum MergeStrategy
    {
        Combine_Or_Replace, //Create a union, or allow replacement. Default value.
        Combine, //Create a union of similarly named rules' children.
        Combine_Or_Replace_Optional, //Combine or replace if the existing value isn't exclusive
        Combine_Optional, //Combine if the existing value isn't exclusive
        Override_Then_Combine, //Replace an existing Combine_And_Replace rule, then allow combination
        Exclusive, //Replacing this rule is an error, as is replacing a rule with it.
        Override_Then_Exclusive, //Replace an existing rule, then allow no more modifications.
        Must_Override, //This rule needs to be overridden by another.
    }
    public enum MergeResult
    {
        Unmerged,
        //Expected Results
        Overidden,
        Combined,
        Replace,
        //Warnings
        Ignored_Replace,
        //Errors
        Exclusive_Clash,
        Bad_Override,
        Not_Overridden,

        Unknown_Error

    }
    public enum ResultType
    {
        Unmerged,
        OK,
        Warning,
        Error,
    }
    public class MergedRules
    {
        public RuleSet GetResult()
        {
            return Result; //Not faffing around with copies here as it's getting turned into json anyway
        }
        public MergedRules(IEnumerable<RuleSet> RuleSets)
        {
            MergeChains = new Dictionary<string, MergeChain>();
            CreateMergeChains(RuleSets);
            Result = new RuleSet();
            foreach (MergeChain chain in MergeChains.Values)
            {
                chain.Process();
                Result.Rules.Add(chain.Result);
            }
        }

        //Internals
        RuleSet Result = new RuleSet();

        class MergeChain
        {
            class MergeInfo
            {
                public MergeInfo(Rule InSource, MergeStrategy InStrategy)
                {
                    Source = InSource;
                    Strategy = InStrategy;
                }
                public MergeStrategy Strategy;
                public MergeResult Result = MergeResult.Unmerged;
                public Rule Source;
                public void Confirm() //Finalize causes a warning
                {
                    Source.MergeResult = Result;
                }
            }
            public MergeChain(String InName)
            {
                Name = InName;
                Result = new Rule();
                InfoSet = new List<MergeInfo>();
                MergeResults = new Dictionary<Rule, MergeResult>();
            }
            public void Add(Rule InRule, MergeStrategy strategy)
            {
                InfoSet.Add(new MergeInfo(InRule, strategy));
            }
            public void Process()
            {
                Result = new Rule();
                MergeResults = new Dictionary<Rule, MergeResult>();
                Result.Name = Name;
                //Forward pass
                MergeInfo PrevInfo = new MergeInfo(new Rule(), MergeStrategy.Must_Override);
                MergeStrategy CurrentStrategy = MergeStrategy.Must_Override;
                foreach (MergeInfo StepInfo in InfoSet)
                {
                    switch (CurrentStrategy)
                    {
                        case MergeStrategy.Combine:
                            //case MergeStrategy.Combine_Optional:
                            switch (StepInfo.Strategy)
                            {
                                case MergeStrategy.Combine_Or_Replace:
                                case MergeStrategy.Combine_Or_Replace_Optional:
                                case MergeStrategy.Combine:
                                case MergeStrategy.Combine_Optional:
                                    StepInfo.Source.Children.ForEach(
                                        (string Child) => { Result.Children.Add(Child); }
                                    );
                                    StepInfo.Result = MergeResult.Combined;
                                    //Keep our strategy the same
                                    break;
                                case MergeStrategy.Override_Then_Combine:
                                case MergeStrategy.Override_Then_Exclusive:
                                    StepInfo.Result = MergeResult.Bad_Override;
                                    break;
                                case MergeStrategy.Exclusive:
                                    StepInfo.Result = MergeResult.Exclusive_Clash;
                                    break;
                            }
                            break;
                        case MergeStrategy.Combine_Or_Replace:
                            //case MergeStrategy.Combine_Or_Replace_Optional:
                            switch (StepInfo.Strategy)
                            {
                                case MergeStrategy.Combine_Or_Replace:
                                case MergeStrategy.Combine_Or_Replace_Optional:
                                case MergeStrategy.Combine:
                                case MergeStrategy.Combine_Optional:
                                    StepInfo.Source.Children.ForEach(
                                        (string Child) => { Result.Children.Add(Child); }
                                    );
                                    CurrentStrategy = OnwardStrategy(StepInfo.Strategy);
                                    StepInfo.Result = MergeResult.Combined;
                                    break;
                                case MergeStrategy.Override_Then_Combine:
                                case MergeStrategy.Override_Then_Exclusive:
                                    Result.Children = new List<string>(StepInfo.Source.Children);
                                    CurrentStrategy = OnwardStrategy(StepInfo.Strategy);
                                    StepInfo.Result = MergeResult.Replace;
                                    PrevInfo.Result = MergeResult.Overidden;
                                    break;
                                case MergeStrategy.Exclusive:
                                    StepInfo.Result = MergeResult.Exclusive_Clash;
                                    break;
                            }
                            break;
                        case MergeStrategy.Exclusive:
                            //case MergeStrategy.Override_Then_Exclusive:
                            switch (StepInfo.Strategy)
                            {
                                case MergeStrategy.Must_Override:
                                case MergeStrategy.Combine_Optional:
                                case MergeStrategy.Combine_Or_Replace_Optional:
                                    StepInfo.Result = MergeResult.Overidden;
                                    break;
                                default:
                                    StepInfo.Result = MergeResult.Exclusive_Clash;
                                    break;
                            }
                            break;
                        case MergeStrategy.Must_Override:
                            Result.Children = new List<string>(StepInfo.Source.Children);
                            CurrentStrategy = OnwardStrategy(StepInfo.Strategy);
                            PrevInfo.Result = MergeResult.Overidden;
                            StepInfo.Result = MergeResult.Replace;
                            break;
                        default:
                            break;
                    }

                    PrevInfo = StepInfo;
                }
                //Backward pass to update states
                bool HasOverridden = false;
                foreach (MergeInfo StepInfo in ((IEnumerable<MergeInfo>)InfoSet).Reverse())
                {
                    if (HasOverridden)
                    {
                        ResultType PrevType = Merge.GetResultType(StepInfo.Result);
                        if (PrevType <= ResultType.OK)
                        {
                            StepInfo.Result = MergeResult.Overidden;
                        }
                    }
                    else if (StepInfo.Strategy == MergeStrategy.Must_Override)
                    {
                        StepInfo.Result = MergeResult.Not_Overridden;
                    }

                    if (StepInfo.Result == MergeResult.Replace)
                    {
                        HasOverridden = true;
                    }

                    MergeResults[StepInfo.Source] = StepInfo.Result;
                }

                foreach (MergeInfo StepInfo in InfoSet)
                {
                    StepInfo.Confirm();
                }
            }
            static MergeStrategy OnwardStrategy(MergeStrategy CurrentStrategy)
            {
                switch (CurrentStrategy)
                {
                    case MergeStrategy.Combine:
                    case MergeStrategy.Override_Then_Combine:
                    case MergeStrategy.Combine_Optional:
                        return MergeStrategy.Combine;
                    case MergeStrategy.Exclusive:
                    case MergeStrategy.Override_Then_Exclusive:
                        return MergeStrategy.Exclusive;
                    case MergeStrategy.Combine_Or_Replace:
                    case MergeStrategy.Combine_Or_Replace_Optional:
                        return MergeStrategy.Combine_Or_Replace;
                    case MergeStrategy.Must_Override:
                    default:
                        return CurrentStrategy;
                }
            }

            public String Name;
            public Rule Result;
            public Dictionary<Rule, MergeResult> MergeResults;
            List<MergeInfo> InfoSet;
        }

        Dictionary<string, MergeChain> MergeChains;
        void CreateMergeChains(IEnumerable<RuleSet> RuleSets)
        {
            foreach (RuleSet set in RuleSets)
            {
                foreach (Rule rule in set.Rules)
                {
                    if (!MergeChains.ContainsKey(rule.Name))
                    {
                        MergeChains.Add(rule.Name, new MergeChain(rule.Name));
                    }
                    MergeStrategy strategy = (rule.Strategy == null ? set.Strategy : rule.Strategy).Value;
                    MergeChains[rule.Name].Add(rule, strategy);
                }
            }
        }
    }

    static class Merge
    {
        public static ResultType GetResultType(MergeResult Result)
        {
            switch (Result)
            {
                case MergeResult.Unmerged:
                    return ResultType.Unmerged;
                case MergeResult.Overidden:
                case MergeResult.Combined:
                case MergeResult.Replace:
                    return ResultType.OK;
                case MergeResult.Ignored_Replace:
                    return ResultType.Warning;
                case MergeResult.Exclusive_Clash:
                case MergeResult.Bad_Override:
                case MergeResult.Not_Overridden:
                case MergeResult.Unknown_Error:
                default: //Very annoying that the default is needed here
                    return ResultType.Error;
            }
        }
    }

}