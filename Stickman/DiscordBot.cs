using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Emzi0767.Utilities;

namespace Stickman
{
    public class DiscordBot
    {
        public bool Online { get; set; } = false;
        public string Name { get; private set; }

        public event AsyncEventHandler<DiscordClient, GuildMemberAddEventArgs> GuildMemberAdded;
        public event AsyncEventHandler<DiscordClient, MessageCreateEventArgs> MessageCreated;

        public DiscordBot(string name, string token)
        {
            this.Name = name;

            m_discord = new DiscordClient(new DiscordConfiguration
            {
                Token = token,
                TokenType = TokenType.Bot,
            });


            Init();
        }

        public void RegisterCommand<T>() where T : BaseCommandModule
        {
            m_commands.RegisterCommands<T>();
        }

        public void Start()
        {
            m_discord.ConnectAsync().Wait();

            foreach (var service in m_services)
            {
                service.InitService(m_discord);
            }

            this.Online = true;

            while (this.Online)
            {
                Task.Delay(1000).Wait();

                foreach (var service in m_services)
                {
                    try
                    {
                        service.UpdateService(m_discord);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                    }
                }
            }

            foreach (var service in m_services)
            {
                try
                {
                    service.DisposeService(m_discord);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);
                }
            }
        }

        public void AddService(IBotService service)
        {
            if (this.Online)
            {
                throw new InvalidOperationException();
            }

            m_services.Add(service);
        }

        public void RemoveService(IBotService service)
        {
            if (this.Online)
            {
                throw new InvalidOperationException();
            }

            m_services.Remove(service);
        }

        public DiscordEmoji CreateEmoji(string name)
        {
            return DiscordEmoji.FromName(m_discord, name);
        }

        private void Init()
        {
            m_discord.Ready += async (client, arg) =>
            {
                Console.WriteLine("Bot ready!");

                await Task.CompletedTask;
            };

            m_discord.GuildMemberAdded += async (client, args) =>
            {
                if (GuildMemberAdded != null)
                {
                    await GuildMemberAdded(client, args);
                }
            };

            m_discord.MessageCreated += async (client, args) =>
            {
                if (MessageCreated != null)
                {
                    await MessageCreated(client, args);
                }
            };
            m_discord.MessageCreated += OnMessageCreated;
            m_discord.MessageReactionAdded += OnMessageReactionAdded;
            m_discord.MessageReactionRemoved += OnMessageReactionRemoved;


            m_commands = m_discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new[] { "?" },
                CaseSensitive = false,
            });

            m_commands.CommandExecuted += Commands_CommandExecuted;
            m_commands.CommandErrored += Commands_CommandErrored;


            m_interactivity = m_discord.UseInteractivity();


            GlobalMessenger.RegisterReceiver(this.Name, (type, param) =>
            {
                if (type == "Shutdown")
                {
                    this.Online = false;
                }
            });
        }

        private async Task OnMessageCreated(DiscordClient client, MessageCreateEventArgs e)
        {
            await GlobalMessenger.PushMessage(this.Name, "NewMessage", e);
        }

        private async Task OnMessageReactionAdded(DiscordClient client, MessageReactionAddEventArgs e)
        {
            await GlobalMessenger.PushMessage(this.Name, "AddReaction", e);
        }

        private async Task OnMessageReactionRemoved(DiscordClient client, MessageReactionRemoveEventArgs e)
        {
            await GlobalMessenger.PushMessage(this.Name, "RemoveReaction", e);
        }

        private async Task Commands_CommandErrored(CommandsNextExtension ext, CommandErrorEventArgs e)
        {
            if (e.Exception is ChecksFailedException)
            {
                var emoji = DiscordEmoji.FromName(e.Context.Client, ":no_entry:");
                await e.Context.RespondAsync($"{emoji} Access denied!");
            }
            else if (e.Exception is CommandNotFoundException)
            {
                Console.WriteLine(e.Exception.Message);
            }
            else
            {
                Console.WriteLine(e.Exception.Message);
                Console.WriteLine(e.Exception.StackTrace);

                await e.Context.RespondAsync(e.Exception.Message);
            }
        }

        private async Task Commands_CommandExecuted(CommandsNextExtension ext, CommandExecutionEventArgs e)
        {
            Console.WriteLine("{0}: {1}", e.Context.User.Username, e.Context.Message.Content);
            await Task.CompletedTask;
        }

        private DiscordClient m_discord = null;
        private CommandsNextExtension m_commands = null;
        private InteractivityExtension m_interactivity = null;

        private List<IBotService> m_services = new List<IBotService>();
    }
}
