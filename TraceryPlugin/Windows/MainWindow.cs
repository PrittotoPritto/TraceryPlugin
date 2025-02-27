using System;
using System.IO;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using Lumina.Data.Files.Excel;
using TraceryPlugin.GrammarBuilder;
using TraceryPlugin.GrammarBuilder.Commands;
using Newtonsoft.Json;

namespace TraceryPlugin.Windows
{
    public class MainWindow : Window, IDisposable
    {
        public MainWindow(Plugin plugin)
            : base("Prittoto Pritto's Tracery Plugin##TraceryPlugin", ImGuiWindowFlags.AlwaysVerticalScrollbar)
        {
            this.plugin = plugin;
            this.configuration = plugin.Configuration;
            this.windowState = new WindowState();
            this.frameState = new FrameState();

            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(450, 450),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };
        }

        public void Dispose() { } //Is there anything I need to clean up here?

        public override void Draw()
        {
            frameState = new();

            this.windowState.fileDialogManager.Draw();

            //Header, undo/redo/save
            ImGui.BeginDisabled(!plugin.UndoStack.CanUndo());
            if (ImGui.Button("Undo"))
            {
                windowState.Clear();
                plugin.UndoStack.Undo();

            }
            ImGui.EndDisabled();
            ImGui.SameLine();
            ImGui.BeginDisabled(!plugin.UndoStack.CanRedo());
            if (ImGui.Button("Redo"))
            {
                windowState.Clear();
                plugin.UndoStack.Redo();

            }
            ImGui.EndDisabled();
            ImGui.SameLine();
            if (ImGui.Button("Save Config"))
            {
                windowState.Clear();
                configuration.Save();
            }
            //rulesets
            for (int i = 0; i < configuration.RuleSets.Count; i++)
            {
                DrawRuleSet(i);
            }
            //new ruleset... do this before extraitems?

            //Do any action that's been queued
            foreach (Action action in frameState.Actions)
            {
                action();
            }

            if (ImGui.Button("Save All Rulesets"))
            {
                windowState.BeginSaveAll(plugin, configuration);
            }
            if(ImGui.Button("Load All Rulesets"))
            {
                windowState.BeginLoadAll(plugin, configuration);
            }

            //Debug messages
            if (frameState.Message != null)
            {
                ImGui.Text("Message:" + frameState.Message!);
            }
        }

        private void DrawRuleSet(int index)
        {
            RuleSet ruleSet = configuration.RuleSets[index];
            string rulesetId = $"ruleset{index}";
            bool isExtraItems = ruleSet.IsExtraItems();
            bool expand = false;
            if (isExtraItems)
            {
                //These buttons go before the extra items, so this seems like a good place to do this.
                if (ImGui.Button("New Rule Set"))
                {
                    RuleSet newRuleSet = new();
                    newRuleSet.Name = "New Rule Set";
                    newRuleSet.Strategy = MergeStrategy.Combine_Or_Replace;
                    frameState.Actions.Add(() => { plugin.UndoStack.AddCommand(new InsertRulesetCommand(index, newRuleSet)); });
                }
                ImGui.SameLine();
                if (ImGui.Button("Load Rule Set"))
                {
                    windowState.BeginLoadNew(plugin, configuration, configuration.RuleSets);
                }
                ImGui.SameLine();
                if (ImGui.Button("Load Raw Grammar"))
                {
                    windowState.BeginLoadRaw(plugin, configuration, configuration.RuleSets);
                }
                //

                expand = ImGui.TreeNode($"{ruleSet.Name}###ruleset{index}node");
            }
            else
            {
                float outerColumnWidth = ImGui.GetColumnWidth();

                //Do a newname, a sameline, and some buttons!
                expand = ImGui.TreeNode($"###{rulesetId}node");
                ImGui.SameLine();
                string varName = $"###{rulesetId}name";
                string? edited = CreateTextInput(varName, ruleSet.Name);
                if (edited != null)
                {
                    frameState.Actions.Add(() =>
                    {
                        plugin.UndoStack.AddCommand(new ChangeRulesetNameCommand(index, edited!));
                    });
                }

                float buttonOffset = 160;
                //Merge status indicator
                {
                    ImGui.SameLine(outerColumnWidth - buttonOffset);
                    ResultType resultType = ruleSet.ResultType();
                    CreateMergeStatusIndicator(resultType);
                }
                //Strategy popup
                {
                    ImGui.SameLine();
                    MergeStrategy? selectedStrategy = CreateMergeStrategySelector(rulesetId, ruleSet.Strategy, false);
                    if (selectedStrategy != ruleSet.Strategy)
                    {
                        frameState.Actions.Add(() => { plugin.UndoStack.AddCommand(new ChangeRulesetStrategyCommand(index, selectedStrategy!.Value)); });
                    }
                }
                //Save            
                {
                    ImGui.SameLine();
                    if (ImGuiComponents.IconButton($"{rulesetId}_save", FontAwesomeIcon.Download))
                    {
                        windowState.BeginSave(plugin, configuration, configuration.RuleSets, index);
                    }
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    {
                        ImGui.SetTooltip("Save ruleset to file");
                    }
                }
                //Load
                {
                    ImGui.SameLine();
                    if (ImGuiComponents.IconButton($"{rulesetId}_load", FontAwesomeIcon.FileUpload))
                    {
                        windowState.BeginLoad(plugin, configuration, configuration.RuleSets, index);
                    }
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    {
                        ImGui.SetTooltip("Load ruleset from file");
                    }
                }
                //Delete
                {
                    ImGui.SameLine();
                    if (ImGuiComponents.IconButton($"{rulesetId}_delete", FontAwesomeIcon.TrashAlt))
                    {
                        frameState.Actions.Add(() => { plugin.UndoStack.AddCommand(new DeleteRulesetCommand(index)); });
                    }
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    {
                        ImGui.SetTooltip("Delete this ruleset");
                    }
                }
            }
            if (expand)
            {//Assumption: Don't need to treepush, the node handles that.
                for (int i = 0; i < ruleSet.Rules.Count; ++i)
                {
                    DrawRule(index, i);
                }
                if (ImGui.Button($"New Rule###{rulesetId}newrule"))
                {
                    Rule newRule = new();
                    newRule.Name = "new_rule";
                    frameState.Actions.Add(() => { plugin.UndoStack.AddCommand(new AddRuleCommand(index, newRule)); });
                }
                ImGui.TreePop();
            }
        }
        private void DrawRule(int setIndex, int ruleIndex)
        {
            float outerColumnWidth = ImGui.GetColumnWidth();
            Rule rule = configuration.RuleSets[setIndex].Rules[ruleIndex];
            string ruleId = $"rule_{setIndex}_{ruleIndex}";

            bool expand = ImGui.TreeNode($"###{ruleId}_node");

            //Rule Name
            ImGui.SameLine();
            string varName = $"###{ruleId}_name";
            string? edited = CreateTextInput(varName, rule.Name);
            if (edited != null)
            {
                frameState.Actions.Add(() =>
                {
                    plugin.UndoStack.AddCommand(new ChangeRuleNameCommand(setIndex, ruleIndex, edited!));
                });
            }

            float buttonOffset = 74;
            //Merge status indicator
            {
                ImGui.SameLine(outerColumnWidth - buttonOffset);
                ResultType resultType = rule.ResultType();
                CreateMergeStatusIndicator(resultType, rule.MergeResult.ToString().Replace("_", " "));
            }
            //Strategy popup
            {
                ImGui.SameLine();
                MergeStrategy? selectedStrategy = CreateMergeStrategySelector(ruleId, rule.Strategy, false);
                if (selectedStrategy != rule.Strategy)
                {
                    frameState.Actions.Add(() => { plugin.UndoStack.AddCommand(new ChangeRuleStrategyCommand(setIndex, ruleIndex, selectedStrategy)); });
                }
            }
            //Delete
            {
                ImGui.SameLine();
                if (ImGuiComponents.IconButton($"{ruleId}_delete", FontAwesomeIcon.TrashAlt))
                {
                    frameState.Actions.Add(() => { plugin.UndoStack.AddCommand(new DeleteRuleCommand(setIndex, ruleIndex)); });
                }
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                {
                    ImGui.SetTooltip("Delete this rule");
                }
            }

            if (expand)
            {//Assumption: Don't need to treepush, the node handles that.
                for (int i = 0; i < rule.Children.Count; ++i)
                {
                    DrawItem(setIndex, ruleIndex, i);
                }
                ImGui.Text("     ");
                ImGui.SameLine();
                if (ImGui.Button($"New Item###{ruleId}_newitem"))
                {
                    frameState.Actions.Add(() => { plugin.UndoStack.AddCommand(new AddItemCommand(setIndex, ruleIndex, "text")); });
                }
                ImGui.TreePop();
            }
        }
        private void DrawItem(int setIndex, int ruleIndex, int itemIndex)
        {
            float outerColumnWidth = ImGui.GetColumnWidth();
            string item = configuration.RuleSets[setIndex].Rules[ruleIndex].Children[itemIndex];
            string itemId = $"item_{setIndex}_{ruleIndex}_{itemIndex}";

            //Spacer
            ImGui.Text("     ");
            ImGui.SameLine();
            //Rule Name
            string varName = $"###{itemId}_text";
            string? edited = CreateTextInput(varName, item, RuleSet.ItemLength);
            if (edited != null)
            {
                frameState.Actions.Add(() =>
                {
                    plugin.UndoStack.AddCommand(new ModifyItemCommand(setIndex, ruleIndex, itemIndex, edited!));
                });
            }

            float buttonOffset = 2;
            //Delete
            {
                ImGui.SameLine(outerColumnWidth - buttonOffset);
                if (ImGuiComponents.IconButton($"{itemId}_delete", FontAwesomeIcon.TrashAlt))
                {
                    frameState.Actions.Add(() => { plugin.UndoStack.AddCommand(new DeleteItemCommand(setIndex, ruleIndex, itemIndex)); });
                }
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                {
                    ImGui.SetTooltip("Delete this item");
                }
            }
        }
        private string? CreateTextInput(string id, string baseValue, uint length = RuleSet.RuleNameLength)
        {
            //We want to return a value when:
            //  The user presses enter when editing this text box
            //  The user deselects the item, meaning we were in the editedString slot, but are not selected now
            //  The user has already selected another item, pushing this into the lastEditedString slot

            bool beginEdited = windowState.editedString != null && windowState.editedString.Value.Item1 == id;
            bool lastEdited = windowState.lastEditedString != null && windowState.lastEditedString.Value.Item1 == id;

            bool commitChange = lastEdited;
            string newValue = beginEdited ? windowState.editedString!.Value.Item2 : (lastEdited ? windowState.lastEditedString!.Value.Item2 : baseValue);

            if (lastEdited) //This has been processed now, so we can clear it.
            {
                windowState.lastEditedString = null;
            }

            if (ImGui.InputText(id, ref newValue, length, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                commitChange = true;
            }
            if (ImGui.IsItemActive())
            {
                if (!beginEdited)
                {
                    windowState.lastEditedString = windowState.editedString;
                }
                windowState.editedString = (id, newValue);
            }
            else if (beginEdited) //Item was selected, but isn't any more
            {
                commitChange = true;
                windowState.editedString = null;
            }

            if (commitChange && newValue != baseValue)
            {
                return newValue;
            }
            return null;
        }
        private void CreateMergeStatusIndicator(ResultType resultType, string? description = null)
        {
            if (description == null)
            {
                description = resultType.ToString();
            }
            switch (resultType)
            {
                case ResultType.OK:
                    uint okColor = ImGui.GetColorU32(new Vector4(0.3f, 1.0f, 0.3f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.Text, okColor);
                    ImGui.Text("OK");
                    ImGui.PopStyleColor();
                    break;
                case ResultType.Warning:
                    uint warningColor = ImGui.GetColorU32(new Vector4(0.7f, 0.7f, 0.0f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.Text, warningColor);
                    ImGui.Text("Wrn");
                    ImGui.PopStyleColor();
                    break;
                case ResultType.Error:
                case ResultType.Unmerged:
                    uint errorColor = ImGui.GetColorU32(new Vector4(1.0f, 0.0f, 0.0f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.Text, errorColor);
                    ImGui.Text("Err");
                    ImGui.PopStyleColor();
                    break;
            }
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.SetTooltip($"Merge Result: {description}");
            }
        }
        private MergeStrategy? CreateMergeStrategySelector(string itemId, MergeStrategy? currentStrategy, bool allowUnset)
        {
            MergeStrategy? selectedStrategy = currentStrategy;

            string popupId = $"{itemId}mergepopup";
            if (ImGui.Button($"M###{itemId}mergebutton"))
            {
                ImGui.OpenPopup(popupId);
            }

            if (ImGui.BeginPopup(popupId))//Note: These don't have a title bar, so "Text###id" does not work
            {
                //frameState.Message = popupId;
                if (allowUnset)
                {
                    if (ImGui.Button($"Unset###{popupId}_unset"))
                    {
                        selectedStrategy = null;
                        ImGui.CloseCurrentPopup();
                    }
                }
                foreach (MergeStrategy strategy in Enum.GetValues(typeof(MergeStrategy)))
                {
                    if (ImGui.Button($"{strategy.ToString().Replace("_", " ")}###{popupId}_{strategy.ToString()}"))
                    {
                        selectedStrategy = strategy;
                        ImGui.CloseCurrentPopup();
                    }
                }
                ImGui.EndPopup();
            }
            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                string strategyName = selectedStrategy != null ? selectedStrategy.ToString()!.Replace("_", " ") : "Unset";
                ImGui.SetTooltip($"Merge strategy: {strategyName}");
            }
            return selectedStrategy;
        }

        private class WindowState
        {
            public FileDialogManager fileDialogManager = new();
            public (string, string)? editedString;
            public (string, string)? lastEditedString;

            public void BeginLoadNew(Plugin plugin, Configuration config, RuleSetCollection collection)
            {
                fileDialogManager.OpenFileDialog("Load ruleset", "*.json", (bool chosen, string path) =>
                {
                    if (chosen)
                    {
                        try
                        {
                            RuleSet? ruleSet = JsonConvert.DeserializeObject<RuleSet>(File.ReadAllText(path));
                            if (ruleSet != null)
                            {
                                int index = collection.Count - 1;
                                plugin.UndoStack.AddCommand(new InsertRulesetCommand(index, ruleSet));
                            }
                        }
                        catch (Exception E)
                        {
                            //TODO: Report the error
                        }
                    }
                });
            }

            public void BeginLoad(Plugin plugin, Configuration config, RuleSetCollection collection, int index)
            {
                fileDialogManager.OpenFileDialog("Load ruleset", ".json", (bool chosen, string path) =>
                {
                    if (chosen)
                    {
                        try
                        {
                            RuleSet? ruleSet = JsonConvert.DeserializeObject<RuleSet>(File.ReadAllText(path));
                            if (ruleSet != null)
                            {
                                plugin.UndoStack.AddCommand(new ReplaceRulesetCommand(index, ruleSet));
                            }
                        }
                        catch (Exception E)
                        {
                            //TODO: Report the error
                        }
                    }
                });
            }

            public void BeginLoadRaw(Plugin plugin, Configuration config, RuleSetCollection collection)
            {
                fileDialogManager.OpenFileDialog("Load raw grammar", ".json", (bool chosen, string path) =>
                {
                    if (chosen)
                    {
                        try
                        {
                            Dictionary<string, List<string>>? RawGrammar = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(File.ReadAllText(path));
                            if (RawGrammar != null)
                            {
                                RuleSet ruleSet = new();
                                ruleSet.Name = Path.GetFileNameWithoutExtension(path);
                                foreach (string key in RawGrammar.Keys)
                                {
                                    Rule rule = new();
                                    rule.Name = key;
                                    rule.Children = RawGrammar[key];
                                    ruleSet.Rules.Add(rule);
                                }
                                int index = collection.Count - 1;
                                plugin.UndoStack.AddCommand(new InsertRulesetCommand(index, ruleSet));
                            }
                        }
                        catch (Exception E)
                        {
                            //TODO: Report the error
                        }
                    }
                });
            }

            public void BeginSave(Plugin plugin, Configuration config, RuleSetCollection collection, int index)
            {
                RuleSet ruleSet = collection[index];
                string defaultPath = string.IsNullOrEmpty(ruleSet.SourcePath) ? config.SavePath + ruleSet.Name + ".json" : ruleSet.SourcePath!;
                fileDialogManager.SaveFileDialog("Save ruleset", ".json", defaultPath, ".json", (bool chosen, string path) =>
                {
                    if (chosen)
                    {
                        try
                        {
                            File.WriteAllText(path, JsonConvert.SerializeObject(ruleSet, Formatting.Indented));
                        }
                        catch (Exception E)
                        {
                            //TODO: Report the error
                        }

                        if (path != ruleSet.SourcePath)
                        {
                            plugin.UndoStack.AddCommand(new ChangeRulesetPathCommand(index, path));
                        }
                    }
                });
            }

            public void BeginSaveAll(Plugin plugin, Configuration config)
            {
                fileDialogManager.SaveFileDialog("Save all rulesets", ".json", config.GeneralSavePath, ".json", (bool chosen, string path) =>
                {
                    if (chosen)
                    {
                        try
                        {
                            File.WriteAllText(path, JsonConvert.SerializeObject(config.RuleSets, Formatting.Indented));
                        }
                        catch (Exception E)
                        {
                            //TODO: Report the error
                        }
                        config.GeneralSavePath = path;
                    }
                });
            }
            public void BeginLoadAll(Plugin plugin, Configuration config)
            {
                fileDialogManager.OpenFileDialog("Load all rulesets", ".json", (bool chosen, string path) =>
                {
                    if (chosen)
                    {
                        try
                        {
                            RuleSetCollection? ruleSetCollection = JsonConvert.DeserializeObject<RuleSetCollection>(File.ReadAllText(path));
                            if (ruleSetCollection != null)
                            {
                                config.RuleSets = ruleSetCollection;
                                plugin.UndoStack.Target = ruleSetCollection;
                                plugin.UpdateGrammar();
                            }

                        }
                        catch (Exception E)
                        {
                            //TODO: Report the error
                        }
                        config.GeneralSavePath = path;
                    }
                });
            }

            public void Clear()
            {
                editedString = null;
                lastEditedString = null;
                fileDialogManager.Reset();
            } //Remove any editing state, something has been comitted
        }
        private WindowState windowState;
        private class FrameState
        {
            public List<Action> Actions = new();
            public string? Message = null;
        }
        private FrameState frameState;
        private Plugin plugin;
        private Configuration configuration;
        private const float buttonSep = 3;
    }
}