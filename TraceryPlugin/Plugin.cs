using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

using TraceryNet;
using TraceryPlugin.GrammarBuilder;
using Dalamud.Interface.Windowing;
using TraceryPlugin.Windows;

namespace TraceryPlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "TraceryPlugin";

        private const string commandName = "/trace";

        [PluginService]
        public static IDalamudPluginInterface PluginInterface { get; private set; }
        [PluginService]
        public static IClientState ClientState { get; private set; }
        [PluginService]
        public static ICommandManager CommandManager { get; private set; }
        [PluginService]
        public static IChatGui ChatGui { get; private set; }
        [PluginService]
        public static IFramework Framework { get; private set; }

        public Configuration Configuration { get; init; }
        //private PluginUI PluginUi { get; init; }

        public readonly WindowSystem WindowSystem = new("PPTraceryPlugin");
        private Windows.MainWindow MainWindow { get; init; }
        //PromptWindow?

        public IUndoStack<RuleSetCollection> UndoStack; //The main thing the UI should be interfacing with.

        //The updated version of the grammar
        private GrammarBuilder.MergedRules MergedRules;
        private TraceryNet.Grammar Grammar;

        //Outputting messages
        private bool ChatAvailable = true; //TODO: true for dev reload testing, set to false!
        private Channel<string> MessageSink;
        private struct DelayedReader
        {
            const uint MinDelayMS = 150;
            public DelayedReader(ChannelReader<string> Source)
            {
                RawReader = Source;
                NextAvailable = DateTime.Now;
            }
            private ChannelReader<string> RawReader;
            private DateTime NextAvailable;
            public bool TryRead(out string Out)
            {
                Out = "";
                if (DateTime.Now < NextAvailable)
                {
                    return false;
                }
                else
                {
                    if (RawReader.TryRead(out Out!))
                    {
                        NextAvailable = DateTime.Now + TimeSpan.FromMilliseconds(MinDelayMS);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
        private DelayedReader MessageSource;

        public Plugin()
        {
            this.Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            if (this.Configuration.RuleSets.Count == 0)
            {
                this.Configuration.RuleSets = Configuration.GetDefaultRules();
            }
            this.MergedRules = new GrammarBuilder.MergedRules(this.Configuration.RuleSets);
            this.Grammar = new Grammar(this.MergedRules.GetResult().ForGrammar());
            // you might normally want to embed resources and load them from the manifest stream
            //this.PluginUi = new PluginUI(this.Configuration, this);

            CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the Tracery Plugin GUI, executes a subcommand, or with another text string flattens it."
            });

            this.MessageSink = Channel.CreateUnbounded<string>();
            this.MessageSource = new DelayedReader(this.MessageSink.Reader);

            Framework.Update += this.OnFrameworkUpdate;
            ClientState.Login += this.OnLogin;
            ClientState.Logout += this.OnLogout;

            UndoStack = new(Configuration.RuleSets, (RuleSetCollection Collection) => UpdateGrammar());

            MainWindow = new MainWindow(this);
            WindowSystem.AddWindow(MainWindow);

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUI;
            PluginInterface.UiBuilder.OpenMainUi += ToggleMainUI;
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();
            MainWindow.Dispose();

            Framework.Update -= this.OnFrameworkUpdate;
            ClientState.Login -= this.OnLogin;
            ClientState.Logout -= this.OnLogout;

            CommandManager.RemoveHandler(commandName);
            //this.PluginUi.Dispose();
        }

        public void UpdateGrammar()
        {
            this.MergedRules = new GrammarBuilder.MergedRules(Configuration.RuleSets);
            this.Grammar = new Grammar(MergedRules.GetResult().ForGrammar());
        }

        public void ClearSavedItems()
        {
            this.Grammar.SaveData.Clear();
        }

        private void OnCommand(string command, string args)
        {
            // check if we've got nothing in args and show the UI
            string trimmed = args.Trim().ToLower();
            if (trimmed.Length == 0)
            {
                ToggleMainUI();
            }
            // check for special subcommands
            else if (trimmed == "reset")
            {
                ClearSavedItems();
            }
            // otherwise, flatten.
            else
            {
                try
                {
                    string flattened = Grammar.Flatten(args.TrimStart());
                    this.MessageSink.Writer.WriteAsync(flattened);
                }
                catch (Exception ex)
                {
                    string errorOutput = "/echo" + ex.ToString();
                    this.MessageSink.Writer.WriteAsync(errorOutput);
                }
            }
        }
        public void OnFrameworkUpdate(IFramework framework1)
        {
            if (!this.MessageSource.TryRead(out var message) || !this.ChatAvailable)
            //Check availability afterwards, to clear unsendable messages
            {
                return;
            }
            ChatSender.SendMessage(message);
        }

        private void OnLogin()
        {
            this.ChatAvailable = true;
        }

        private void OnLogout(int type, int code)
        {
            this.ChatAvailable = false;
        }

        private void DrawUI() => WindowSystem.Draw();
        public void ToggleConfigUI() => MainWindow.Toggle();
        public void ToggleMainUI() => MainWindow.Toggle();
    }
}
