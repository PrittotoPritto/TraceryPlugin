
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
                                    if (RuleItemAddition.ContainsKey(Symbol))
                                    {
                                        newRule = RuleItemAddition[Symbol];
                                    }
                                    if (ImGui.InputText($"New Rule###nr{Symbol}", ref newRule, RuleLength))
                                    {
                                        RuleItemAddition[Symbol] = newRule;
                                    }
                                    ImGui.SameLine();
                                    if(ImGui.Button($"+###ar{Symbol}"))
                                    {
                                        asArray.Add(newRule);
                                        RuleItemAddition.Remove(Symbol);
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
