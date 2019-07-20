using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Stickman
{
    public class DiscordBot
    {
        public bool Online { get; set; } = false;
        public string Name { get; private set; }

        public event AsyncEventHandler<GuildMemberAddEventArgs> GuildMemberAdded;
        public event AsyncEventHandler<MessageCreateEventArgs> MessageCreated;

        public DiscordBot(string name, string token)
        {
            this.Name = name;

            m_discord = new DiscordClient(new DiscordConfiguration
            {
                Token = token,
                TokenType = TokenType.Bot,
#if DEBUG
                LogLevel = LogLevel.Debug,
                UseInternalLogHandler = true,
#endif
            });


            Init();
        }

        public void RegisterCommand<T>() where T : class
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
            m_discord.Ready += async (arg) =>
            {
                Console.WriteLine("Bot ready!");

                await Task.CompletedTask;
            };

            m_discord.GuildMemberAdded += async (args) =>
            {
                if (GuildMemberAdded != null)
                {
                    await GuildMemberAdded(args);
                }
            };

            m_discord.MessageCreated += async (args) =>
            {
                if (MessageCreated != null)
                {
                    await MessageCreated(args);
                }
            };
            m_discord.MessageCreated += OnMessageCreated;


            m_commands = m_discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefix = "?",
                CaseSensitive = false,
            });

            m_commands.CommandExecuted += Commands_CommandExecuted;
            m_commands.CommandErrored += Commands_CommandErrored;


            m_interactivity = m_discord.UseInteractivity(new InteractivityConfiguration());
        }

        private async Task OnMessageCreated(MessageCreateEventArgs e)
        {
            await GlobalMessenger.PushMessage(this.Name, "NewMessage", e);
        }

        private async Task Commands_CommandErrored(CommandErrorEventArgs e)
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

        private async Task Commands_CommandExecuted(CommandExecutionEventArgs e)
        {
            Console.WriteLine("{0}: {1}", e.Context.User.Username, e.Context.Message.Content);
            await Task.CompletedTask;
        }

        private DiscordClient m_discord = null;
        private CommandsNextModule m_commands = null;
        private InteractivityModule m_interactivity = null;

        private List<IBotService> m_services = new List<IBotService>();
    }
}
