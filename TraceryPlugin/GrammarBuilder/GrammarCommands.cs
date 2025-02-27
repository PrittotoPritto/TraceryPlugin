using static System.Net.Mime.MediaTypeNames;

namespace TraceryPlugin.GrammarBuilder.Commands
{
    //RULESET

    //Adding a ruleset
    public class InsertRulesetCommand : IDoableCommand<RuleSetCollection>
    {
        public InsertRulesetCommand(int index, RuleSet addedRuleset)
        {
            this.index = index;
            this.addedRuleset = addedRuleset;
        }

        public IDoableCommand<RuleSetCollection> Do(RuleSetCollection target)
        {
            target.Insert(index, addedRuleset);
            return new DeleteRulesetCommand(index);
        }

        private int index;
        private RuleSet addedRuleset;
    }
    //Deleting a ruleset
    public class DeleteRulesetCommand : IDoableCommand<RuleSetCollection>
    {
        public DeleteRulesetCommand(int index)
        {
            this.index = index;
        }
        public IDoableCommand<RuleSetCollection> Do(RuleSetCollection target) 
        {
            RuleSet removed = target[index];
            target.RemoveAt(index);
            return new InsertRulesetCommand(index, removed);
        }

        private int index;
    }
    //Replacing a ruleset
    public class ReplaceRulesetCommand : IDoableCommand<RuleSetCollection>
    {
        public ReplaceRulesetCommand(int index, RuleSet newRuleset)
        {
            this.index = index;
            this.newRuleset = newRuleset;
        }
        public IDoableCommand<RuleSetCollection> Do(RuleSetCollection target)
        {
            RuleSet replaced = target[index];
            target[index] = newRuleset;
            return new ReplaceRulesetCommand(index, replaced);
        }

        private int index;
        private RuleSet newRuleset;
    }
    //Renaming a ruleset
    public class ChangeRulesetNameCommand : IDoableCommand<RuleSetCollection>
    {
        public ChangeRulesetNameCommand(int index, string newName)
        {
            this.index = index;
            this.newName = newName;
        }
        public IDoableCommand<RuleSetCollection> Do(RuleSetCollection target)
        {
            string oldName = target[index].Name;
            target[index].Name = newName;
            return new ChangeRulesetNameCommand(index, oldName);
        }

        private int index;
        private string newName;
    }
    //Changing a merge strategy
    public class ChangeRulesetStrategyCommand : IDoableCommand<RuleSetCollection>
    {
        public ChangeRulesetStrategyCommand(int index, MergeStrategy newStrategy)
        {
            this.index = index;
            this.newStrategy = newStrategy;
        }
        public IDoableCommand<RuleSetCollection> Do(RuleSetCollection target)
        {
            MergeStrategy oldStrategy = target[index].Strategy;
            target[index].Strategy = newStrategy;
            return new ChangeRulesetStrategyCommand(index, oldStrategy);
        }

        private int index;
        private MergeStrategy newStrategy;
    }
    //Changing the source path
    public class ChangeRulesetPathCommand : IDoableCommand<RuleSetCollection>
    {
        public ChangeRulesetPathCommand(int index, string? newPath)
        {
            this.index = index;
            this.newPath = newPath;
        }
        public IDoableCommand<RuleSetCollection> Do(RuleSetCollection target)
        {
            string? oldPath = target[index].SourcePath;
            target[index].SourcePath = newPath;
            return new ChangeRulesetPathCommand(index, oldPath);
        }

        private int index;
        private string? newPath;
    }

    //RULES

    //Adding a rule
    public class AddRuleCommand : IDoableCommand<RuleSetCollection>
    {
        public AddRuleCommand(int setIndex, Rule addedRule)
        {
            this.setIndex = setIndex;
            this.addedRule = addedRule;
        }

        public IDoableCommand<RuleSetCollection> Do(RuleSetCollection target)
        {
            int ruleIndex = target[setIndex].Rules.Count;
            target[setIndex].Rules.Insert(ruleIndex, addedRule);
            return new DeleteRuleCommand(setIndex, ruleIndex);
        }

        private int setIndex;
        private Rule addedRule;
    }
    public class InsertRuleCommand : IDoableCommand<RuleSetCollection>
    {
        public InsertRuleCommand(int setIndex, int ruleIndex, Rule addedRule)
        {
            this.setIndex = setIndex;
            this.ruleIndex = ruleIndex;
            this.addedRule = addedRule;
        }

        public IDoableCommand<RuleSetCollection> Do(RuleSetCollection target)
        {
            target[setIndex].Rules.Insert(ruleIndex, addedRule);
            return new DeleteRuleCommand(setIndex, ruleIndex);
        }

        private int setIndex;
        private int ruleIndex;
        private Rule addedRule;
    }
    //Deleting a rule
    public class DeleteRuleCommand : IDoableCommand<RuleSetCollection>
    {
        public DeleteRuleCommand(int setIndex, int ruleIndex)
        {
            this.setIndex = setIndex;
            this.ruleIndex = ruleIndex;
        }

        public IDoableCommand<RuleSetCollection> Do(RuleSetCollection target)
        {
            Rule removedRule = target[setIndex].Rules[ruleIndex];
            target[setIndex].Rules.RemoveAt(ruleIndex);
            return new InsertRuleCommand(setIndex, ruleIndex, removedRule);
        }

        private int setIndex;
        private int ruleIndex;
    }
    //Renaming a rule
    public class ChangeRuleNameCommand : IDoableCommand<RuleSetCollection>
    {
        public ChangeRuleNameCommand(int setIndex, int ruleIndex, string newName)
        {
            this.setIndex = setIndex;
            this.ruleIndex = ruleIndex;
            this.newName = newName;
        }
        public IDoableCommand<RuleSetCollection> Do(RuleSetCollection target)
        {
            string oldName = target[setIndex].Rules[ruleIndex].Name;
            target[setIndex].Rules[ruleIndex].Name = newName;
            return new ChangeRuleNameCommand(setIndex, ruleIndex, oldName);
        }

        private int setIndex;
        private int ruleIndex;
        private string newName;
    }
    //Changing a merge strategy
    public class ChangeRuleStrategyCommand : IDoableCommand<RuleSetCollection>
    {
        public ChangeRuleStrategyCommand(int setIndex, int ruleIndex, MergeStrategy? strategy)
        {
            this.setIndex = setIndex;
            this.ruleIndex = ruleIndex;
            this.strategy = strategy;
        }
        public IDoableCommand<RuleSetCollection> Do(RuleSetCollection target)
        {
            MergeStrategy? oldStrategy = target[setIndex].Rules[ruleIndex].Strategy;
            target[setIndex].Rules[ruleIndex].Strategy = strategy;
            return new ChangeRuleStrategyCommand(setIndex, ruleIndex, oldStrategy);
        }

        private int setIndex;
        private int ruleIndex;
        private MergeStrategy? strategy;
    }

    //Adding an item
    public class AddItemCommand : IDoableCommand<RuleSetCollection>
    {
        public AddItemCommand(int setIndex, int ruleIndex, string text = "")
        {
            this.setIndex = setIndex;
            this.ruleIndex = ruleIndex;
            this.text = text;
        }
        public IDoableCommand<RuleSetCollection> Do(RuleSetCollection target)
        {
            int index = target[setIndex].Rules[ruleIndex].Children.Count;
            target[setIndex].Rules[ruleIndex].Children.Add(text);
            return new DeleteItemCommand(setIndex, ruleIndex, index);
        }

        private int setIndex;
        private int ruleIndex;
        private string text;
    }
    //Deleting an item
    public class DeleteItemCommand : IDoableCommand<RuleSetCollection>
    {
        public DeleteItemCommand(int setIndex, int ruleIndex, int itemIndex)
        {
            this.setIndex = setIndex;
            this.ruleIndex = ruleIndex;
            this.itemIndex = itemIndex;
        }
        public IDoableCommand<RuleSetCollection> Do(RuleSetCollection target)
        {
            string oldText = target[setIndex].Rules[ruleIndex].Children[itemIndex];
            target[setIndex].Rules[ruleIndex].Children.RemoveAt(itemIndex);
            return new InsertItemCommand(setIndex, ruleIndex, itemIndex, oldText);
        }

        private int setIndex;
        private int ruleIndex;
        private int itemIndex;
    }
    //Inserting an item. Note: This is primarily an inverse of delete
    public class InsertItemCommand : IDoableCommand<RuleSetCollection>
    {
        public InsertItemCommand(int setIndex, int ruleIndex, int itemIndex, string text)
        {
            this.setIndex = setIndex;
            this.ruleIndex = ruleIndex;
            this.itemIndex = itemIndex;
            this.text = text;
        }
        public IDoableCommand<RuleSetCollection> Do(RuleSetCollection target)
        {
            target[setIndex].Rules[ruleIndex].Children.Insert(itemIndex, text);
            return new DeleteItemCommand(setIndex, ruleIndex, itemIndex);
        }

        private int setIndex;
        private int ruleIndex;
        private int itemIndex;
        private string text;
    }
    //Modifying an item
    public class ModifyItemCommand : IDoableCommand<RuleSetCollection>
    {
        public ModifyItemCommand(int setIndex, int ruleIndex, int itemIndex, string text)
        {
            this.setIndex = setIndex;
            this.ruleIndex = ruleIndex;
            this.itemIndex = itemIndex;
            this.text = text;
        }
        public IDoableCommand<RuleSetCollection> Do(RuleSetCollection target)
        {
            string oldText = target[setIndex].Rules[ruleIndex].Children[itemIndex];
            target[setIndex].Rules[ruleIndex].Children[itemIndex] = text;
            return new ModifyItemCommand(setIndex, ruleIndex, itemIndex, oldText);
        }

        private int setIndex;
        private int ruleIndex;
        private int itemIndex;
        private string text;
    }

}