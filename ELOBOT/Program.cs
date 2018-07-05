﻿using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using ELOBOT.Discord.Context;
using ELOBOT.Handlers;
using ELOBOT.Models;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Serilog;

namespace ELOBOT
{
    public class Program
    {
        private CommandHandler _handler;
        public DiscordSocketClient Client;

        public static void Main(string[] args)
        {
            new Program().Start().GetAwaiter().GetResult();
        }

        public async Task Start()
        {
            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "setup/")))
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "setup/"));

            ConfigModel.CheckExistence();

            Client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            });

            var token = ConfigModel.Load().Token;

            try
            {
                await Client.LoginAsync(TokenType.Bot, token);
                await Client.StartAsync();
            }
            catch (Exception e)
            {
                Log.Information("Discord Token Rejected\n" +
                                $"{e}", LogSeverity.Critical);
            }

            var serviceProvider = new ServiceCollection()
                .AddSingleton(Client)
                .AddSingleton(new DocumentStore
                {
                    Database = ConfigModel.Load().DBName,
                    Urls = new[]
                    {
                        ConfigModel.Load().DBUrl
                    }
                }.Initialize())
                .AddSingleton(new DatabaseHandler(new DocumentStore
                {
                    Urls = new[]
                    {
                        ConfigModel.Load().DBUrl
                    }
                }.Initialize()))
                .AddSingleton(new InteractiveService(Client))
                .AddSingleton(new CommandService(
                    new CommandServiceConfig {CaseSensitiveCommands = false, ThrowOnError = false})).BuildServiceProvider();


            _handler = new CommandHandler(serviceProvider);
            await _handler.ConfigureAsync();

            Client.Log += Client_Log;
            await Task.Delay(-1);
        }

        private static Task Client_Log(LogMessage arg)
        {
            LogHandler.LogMessage(arg.Message, arg.Severity);
            return Task.CompletedTask;
        }
    }
}