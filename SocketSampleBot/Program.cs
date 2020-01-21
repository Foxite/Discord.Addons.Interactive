namespace SampleBot
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using Discord;
    using Discord.Addons.Interactive;
    using Discord.Commands;
    using Discord.WebSocket;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    /// <summary>
    /// The program.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The client.
        /// </summary>
        private DiscordSocketClient client;

        /// <summary>
        /// The handler.
        /// </summary>
        private CommandHandler handler;

        /// <summary>
        /// The first thing that will fire then the bor starts
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        public static void Main()
        {
            new Program().StartAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Initializes the bot
        /// </summary>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        public async Task StartAsync()
        {
            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "setup/")))
            {
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "setup/"));
            }

            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            });

			var token = ""; // TODO

            try
            {
                await client.LoginAsync(TokenType.Bot, token);
                await client.StartAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine("Discord Token Rejected\n" +
                                $"{e}");
            }

            var serviceProvider = new ServiceCollection()
                .AddSingleton(client)
                .AddSingleton(new InteractiveService(client))
                .AddSingleton(new CommandService(
                    new CommandServiceConfig { CaseSensitiveCommands = false, ThrowOnError = false })).BuildServiceProvider();
            handler = new CommandHandler(serviceProvider);
            await handler.ConfigureAsync();

            client.Log += Client_LogAsync;
            await Task.Delay(-1);
        }

        /// <summary>
        /// Logs messages
        /// </summary>
        /// <param name="arg">
        /// The arg.
        /// </param>
        /// <returns>
        /// The <see cref="Task"/>.
        /// </returns>
        private static Task Client_LogAsync(LogMessage arg)
        {
            Console.WriteLine(arg.Message);
            return Task.CompletedTask;
        }
    }
}
