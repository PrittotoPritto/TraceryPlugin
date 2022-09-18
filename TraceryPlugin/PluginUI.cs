using ImGuiNET;
using System;
using System.Numerics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Tracery.Net;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace TraceryPlugin
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    class PluginUI : IDisposable
    {
        private Configuration Configuration;
        private Plugin Plugin;

        // this extra bool exists for ImGui, since you can't ref a property
        private bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        private HashSet<string> SymbolItemsOpen = new HashSet<string>();
        private Dictionary<string, string> SymbolItemAddition = new Dictionary<string, string>();
        private string NewGrammarSymbol = "";
        private bool SavedItemsOpen = true;
        private string NewSavedItemSymbol = "";
        private string NewSavedItemRule = "";
        //Todo - wrap these in a state struct?

        private string Error = "";
        private DateTime ErrorDisplay = DateTime.MinValue;

        // passing in the image here just for simplicity
        public PluginUI(Configuration configuration, Plugin plugin)
        {
            Configuration = configuration;
            this.Plugin = plugin;
        }

        public void Dispose()
        {
        }

        public void Draw()
        {
            // This is our only draw handler attached to UIBuilder, so it needs to be
            // able to draw any windows we might have open.
            // Each method checks its own visibility/state to ensure it only draws when
            // it actually makes sense.
            // There are other ways to do this, but it is generally best to keep the number of
            // draw delegates as low as possible.

            DrawMainWindow();
        }

        public const int SymbolLength = 20;
        public const int RuleLength = 200;
        public void DrawMainWindow()
        {
            if (!Visible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(400, 400), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(300, 300), new Vector2(float.MaxValue, float.MaxValue));

            if (ImGui.Begin("! Prittoto Pritto's Tracery Plugin !", ref this.visible, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
            {
                bool updatePlugin = false;
                ImGui.Text("Base Grammar");
                {
                    int ruleNumber = 0;
                    ref var BaseGrammar = ref Configuration.BaseGrammar;
                    foreach (var Production in BaseGrammar)
                    {
                        string Symbol = Production.Key;
                        var Rules = Production.Value;
                        if (Rules == null) //Not sure if this can happen, but just in case.
                        {
                            ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Symbol {Symbol} has no value");
                            ImGui.SameLine();
                            if (ImGui.Button($"X###rm{Symbol}"))
                            {
                                BaseGrammar.Remove(Symbol); //Note: Need to update any state tracking
                                updatePlugin = true;
                            }
                            ImGui.SameLine();
                            if (ImGui.Button($"Convert###cv{Symbol}"))
                            {
                                BaseGrammar[Symbol] = JToken.Parse("[]");
                                updatePlugin = true;
                            }
                        }
                        if (Rules.Type == JTokenType.Array)
                        {
                            bool view = SymbolItemsOpen.Contains(Symbol);
                            if (view)
                            {
                                if (ImGui.Button($"<###s{Symbol}"))
                                {
                                    SymbolItemsOpen.Remove(Symbol);
                                    view = false;
                                }
                            }
                            else
                            {
                                if (ImGui.Button($">###h{Symbol}"))
                                {
                                    SymbolItemsOpen.Add(Symbol);
                                    view = true;
                                }
                            }
                            ImGui.SameLine();
                            ImGui.Text(Symbol);
                            ImGui.SameLine();
                            if (ImGui.Button($"X###rm{Symbol}"))
                            {
                                BaseGrammar.Remove(Symbol);
                                updatePlugin = true;
                                break;
                            }
                            if (view)
                            {
                                ImGui.Indent();
                                bool updateArray = false;
                                var asArray = (JArray)Rules;
                                foreach (var Child in asArray)
                                {
                                    if (Child == null)
                                    {
                                        //Not sure *how* Child can be null, but whatever, the compiler moans
                                        continue;
                                    }
                                    if (Child.Type == JTokenType.String)
                                    {
                                        string rule = Child.ToString();
                                        if (ImGui.InputText($"###rule{ruleNumber}", ref rule, RuleLength))
                                        {
                                            asArray[asArray.IndexOf(Child)] = rule;//Not the cleanest...
                                            updateArray = true;
                                            break;
                                        }
                                        ++ruleNumber;
                                        ImGui.SameLine();
                                        if (ImGui.Button($"X###rmr{ruleNumber}"))
                                        {
                                            asArray.Remove(Child);
                                            updateArray = true;
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Unusable value");
                                        ImGui.SameLine();
                                        if (ImGui.Button($"X###rmr{ruleNumber}"))
                                        {
                                            asArray.Remove(Child);
                                            updateArray = true;
                                            break;
                                        }
                                    }
                                }

                                //Add item
                                {
                                    string newRule = "";
                                    if (SymbolItemAddition.ContainsKey(Symbol))
                                    {
                                        newRule = SymbolItemAddition[Symbol];
                                    }
                                    if (ImGui.InputText($"New Rule###nr{Symbol}", ref newRule, RuleLength))
                                    {
                                        SymbolItemAddition[Symbol] = newRule;
                                    }
                                    ImGui.SameLine();
                                    if(ImGui.Button($"+###ar{Symbol}"))
                                    {
                                        asArray.Add(newRule);
                                        SymbolItemAddition.Remove(Symbol);
                                        updateArray = true;
                                    }
                                }

                                ImGui.Unindent();
                                if (updateArray)
                                {
                                    BaseGrammar[Symbol] = asArray;
                                    updatePlugin = true;
                                    break;
                                }
                            }
                        }
                        else if (Rules.Type == JTokenType.String)
                        {
                            //This is gonna be a copy/paste of Array for now. Need to abstract this out I think.
                            //If we add, we need to convert to an array
                        }
                        else
                        {
                            ImGui.TextColored(new Vector4(1, 0, 0, 0), $"Symbol {Symbol} has rules of an unusable type");
                            ImGui.SameLine();
                            if (ImGui.Button($"X###rm{Symbol}"))
                            {
                                BaseGrammar.Remove(Symbol); //Note: Need to update any state tracking
                                updatePlugin = true;
                                break;
                            }
                            ImGui.SameLine();
                            if (ImGui.Button($"Convert###cv{Symbol}"))
                            {
                                BaseGrammar[Symbol] = JToken.Parse("[]");
                                updatePlugin = true;
                                break;
                            }
                            continue;
                        }
                    }
                    ImGui.InputText($"New Symbol###ngsymbol", ref NewGrammarSymbol, SymbolLength);
                    ImGui.SameLine();
                    bool disableAdd = (NewGrammarSymbol == "") || BaseGrammar.ContainsKey(NewGrammarSymbol);
                    if (disableAdd)
                    {
                        ImGui.BeginDisabled();
                    }
                    if (ImGui.Button("+###as"))
                    {
                        BaseGrammar.Add(NewGrammarSymbol, JToken.Parse("[]"));//Neater way to do this?
                        SymbolItemsOpen.Add(NewGrammarSymbol);
                        NewGrammarSymbol = "";
                        updatePlugin = true;
                    }
                    if (disableAdd)
                    {
                        ImGui.EndDisabled();
                    }
                }

                ImGui.Separator();
                {
                    ImGui.Text("Extra Items");
                    ImGui.SameLine();
                    if (!SavedItemsOpen)
                    { 
                        if(ImGui.Button("+###eis"))
                        {
                            SavedItemsOpen = true;
                        }
                    }
                    else
                    {
                        if (ImGui.Button("-###eih"))
                        {
                            SavedItemsOpen = false;
                        }

                    }
                    ImGui.Text("(not part of the saved/loaded grammar)");
                    if (SavedItemsOpen)
                    {
                        ref var SavedItems = ref Configuration.SavedItems;
                        //Modify/Remove
                        for (var i = 0; i < SavedItems.Count; ++i)
                        {
                            KeyValuePair<string, string> SavedItem = SavedItems[i];
                            string Symbol = SavedItem.Key;
                            ImGui.SetNextItemWidth(120);
                            if (ImGui.InputText($"Symbol###ssymbol{i}", ref Symbol, SymbolLength, ImGuiInputTextFlags.ReadOnly))
                            {
                                //Don't allow symbols to be set for now, worried it'll lead to symbol conflicts
                            }
                            ImGui.SameLine();
                            string Rule = SavedItem.Value;
                            ImGui.SetNextItemWidth(120);
                            if (ImGui.InputText($"Rule###srule{i}", ref Rule, RuleLength))
                            {
                                SavedItems[i] = new KeyValuePair<string, string>(Symbol, Rule);
                                updatePlugin = true;
                            }
                            ImGui.SameLine();
                            if (ImGui.Button($"-###rsi{i}"))
                            {
                                SavedItems.RemoveAt(i);
                                updatePlugin = true;
                                break;
                            }
                        }
                        //Add
                        {
                            ImGui.SetNextItemWidth(120);
                            ImGui.InputText($"###nssymbol", ref NewSavedItemSymbol, SymbolLength);
                            ImGui.SameLine();
                            ImGui.SetNextItemWidth(120);
                            ImGui.InputText($"###nsval", ref NewSavedItemRule, RuleLength);
                            ImGui.SameLine();
                            bool disableAdd = (NewSavedItemSymbol == "") || SavedItems.Any( ( KeyValuePair<string, string> kv ) => kv.Key == NewSavedItemSymbol);
                            if (disableAdd)
                            {
                                ImGui.BeginDisabled();
                            }
                            if (ImGui.Button("+###asi"))
                            {
                                SavedItems.Add(new KeyValuePair<string, string>(NewSavedItemSymbol, NewSavedItemRule));
                                NewSavedItemSymbol = "";
                                NewSavedItemRule = "";
                                updatePlugin = true;
                            }
                            if (disableAdd)
                            {
                                ImGui.EndDisabled();
                            }
                        }
                        if(ImGui.Button("Reset Live Saved Items"))
                        {
                            updatePlugin = true;
                        }
                    }
                }

                if (updatePlugin)
                {
                    this.Plugin.ResetGrammar();
                    //Error = "Updated Plugin";
                    //ErrorDisplay = DateTime.Now + TimeSpan.FromSeconds(3);
                }

                ImGui.Separator();
                if (ErrorDisplay > DateTime.Now)
                {
                    ImGui.Text(Error);
                }
                else
                {
                    string Path = Configuration.SavePath;
                    if (ImGui.InputText("File Path", ref Path, 300, ImGuiInputTextFlags.None))
                    {
                        Configuration.SavePath = Path;
                    }
                    if (ImGui.Button("Save"))
                    {
                        try
                        {
                            File.WriteAllText(Configuration.SavePath, JsonConvert.SerializeObject(Configuration.BaseGrammar));
                            throw new Exception("Save Successful");
                        }
                        catch (Exception e)
                        {
                            Error = e.Message;
                            ErrorDisplay = DateTime.Now + TimeSpan.FromSeconds(3);
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Load"))
                    {
                        try
                        {
                            string source = File.ReadAllText(Configuration.SavePath);
                            JObject? NewBaseGrammar = null;
                            if (InputValidators.IsValidJson(source))
                            {
                                NewBaseGrammar = JsonConvert.DeserializeObject<dynamic>(source);
                            }
                            if (NewBaseGrammar != null)
                            {
                                Configuration.BaseGrammar = NewBaseGrammar;
                                Plugin.ResetGrammar();
                                throw new Exception("Load Successful");
                            }
                            else
                            {
                                throw new Exception("Could not load grammar");
                            }
                        }
                        catch (Exception e)
                        {
                            Error = e.Message;
                            ErrorDisplay = DateTime.Now + TimeSpan.FromSeconds(3);
                        }
                    }
                }
            }

            ImGui.Separator();
            if (ImGui.Button("Save Plugin Config"))
            {
                Configuration.Save();
            }

            ImGui.End();
        }
    }
}
