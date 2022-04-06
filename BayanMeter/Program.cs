﻿using System;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Ninject;
using Telegram.Bot;
using Tolltech.BayanMeter.Psql;
using Tolltech.Core;
using Tolltech.PostgreEF.Integration;
using Tolltech.TelegramCore;
using Telegram.Bot.Extensions.Polling;
using Tolltech.BayanMeterLib.TelegramClient;
using Tolltech.KonturPaymentsLib;

namespace Tolltech.BayanMeter
{
    class Program
    {
        class AppSettings
        {
            public string ConnectionString { get; set; }
            public BotSettings[] BotSettings { get; set; }
        }
        
        class BotSettings
        {
            public string Token { get; set; }
            public string BotName { get; set; }
        }

        private static TelegramBotClient client;

        static void Main(string[] args)
        {
            Console.WriteLine($"Start Bots {DateTime.Now}");

            var argsFileName = "args.txt";
            var botSettingsStr = args.Length > 0 ? args[0] :
                File.Exists(argsFileName) ? File.ReadAllText(argsFileName) : string.Empty;

            var appSettings = JsonConvert.DeserializeObject<AppSettings>(botSettingsStr);

            var kernel = new StandardKernel(new ConfigurationModule("log4net.config"));
            var connectionString = appSettings?.ConnectionString;

            Console.WriteLine($"Read {connectionString} connectionString");

            kernel.Rebind<IConnectionString>().ToConstant(new ConnectionString(connectionString));

            var botSettings = appSettings?.BotSettings ?? Array.Empty<BotSettings>();
            Console.WriteLine($"Read {botSettings.Length} bot settings");

            kernel.Unbind<IBotDaemon>();
            kernel.Bind<IBotDaemon>().To<EasyMemeBotDaemon>().Named("EasyMeme");
            kernel.Bind<IBotDaemon>().To<KonturPaymentsBotDaemon>().Named("KonturPayments");

            using var cts = new CancellationTokenSource();

            foreach (var botSetting in botSettings)
            {
                var token = botSetting.Token;
             
                Console.WriteLine($"Start bot {token}");

                client = new TelegramBotClient(token);

                var receiverOptions = new ReceiverOptions
                {
                    AllowedUpdates = { } // receive all update types
                };

                kernel.Bind<TelegramBotClient>().ToConstant(client).WhenAnyAncestorNamed(botSetting.BotName);

                var botDaemon = kernel.Get<IBotDaemon>(botSetting.BotName);
                client.StartReceiving(
                    botDaemon.HandleUpdateAsync,
                    botDaemon.HandleErrorAsync,
                    receiverOptions,
                    cancellationToken: cts.Token);

                var me = client.GetMeAsync(cts.Token).GetAwaiter().GetResult();

                Console.WriteLine($"Start listening for @{me.Username}");
            }

            Console.ReadLine();

            cts.Cancel();

            Console.WriteLine($"End Bots {DateTime.Now}");
        }
    }
}