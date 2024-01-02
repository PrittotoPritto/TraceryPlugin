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
using XivCommon;

using TraceryNet;

namespace TraceryPlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "TraceryPlugin";

        private const string commandName = "/trace";

        [PluginService]
        public static DalamudPluginInterface DalamudPluginInterface { get; private set; }
        [PluginService]
        public static IClientState ClientState { get; private set; }
        [PluginService]
        public static ICommandManager CommandManager { get; private set; }
        [PluginService]
        public static IChatGui ChatGui { get; private set; }
        [PluginService]
        public static IFramework Framework { get; private set; }

        public XivCommonBase Common { get; }

        public Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }

        //Running the game
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
                    if (RawReader.TryRead(out Out))
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

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface
        )
        {
            this.Common = new XivCommonBase(pluginInterface);
            this.Configuration = DalamudPluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(DalamudPluginInterface);
            this.Grammar = new TraceryNet.Grammar(this.Configuration.BaseGrammar);
            // you might normally want to embed resources and load them from the manifest stream
            this.PluginUi = new PluginUI(this.Configuration, this);

            CommandManager.AddHandler(commandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Opens the Tracery Plugin GUI, executes a subcommand, or with another text string flattens it."
            });

            DalamudPluginInterface.UiBuilder.Draw += DrawUI;
            DalamudPluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            this.MessageSink = Channel.CreateUnbounded<string>();
            this.MessageSource = new DelayedReader(this.MessageSink.Reader);

            Framework.Update += this.OnFrameworkUpdate;
            ClientState.Login += this.OnLogin;
            ClientState.Logout += this.OnLogout;

        }

        public void Dispose()
        {
            Framework.Update -= this.OnFrameworkUpdate;
            ClientState.Login -= this.OnLogin;
            ClientState.Logout -= this.OnLogout;
            
            CommandManager.RemoveHandler(commandName);
            this.PluginUi.Dispose();
        }

        public void ResetGrammar()
        {
            this.Grammar = new TraceryNet.Grammar(this.Configuration.BaseGrammar);
            this.Grammar.SaveData = new Dictionary<string, string>(this.Configuration.SavedItems);
        }

        private void OnCommand(string command, string args)
        {
            // check if we've got nothing in args and show the UI
            string trimmed = args.Trim().ToLower();
            if (trimmed.Length == 0)
            {
                this.PluginUi.Visible = true;
            }
            // check for special subcommands
            else if (trimmed == "reset")
            {
                ResetGrammar();
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
            if (!this.MessageSource.TryRead(out var Message) || !this.ChatAvailable) 
            //Check availability afterwards, to clear unsendable messages
            {
                return;
            }
            this.Common.Functions.Chat.SendMessage(Message);
        }

        private void OnLogin()
        {
            this.ChatAvailable = true;
        }

        private void OnLogout()
        {
            this.ChatAvailable = false;
        }
        private void DrawUI()
        {
            this.PluginUi.Draw();
        }
        
        private void DrawConfigUI()
        {
            this.PluginUi.Visible = true;
        }
    }
}
