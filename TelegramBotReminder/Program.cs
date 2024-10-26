using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;
using System.Text;
using System.Runtime.InteropServices;
using static BotCommands;

static class Program
{
    private static ITelegramBotClient botClient;

    static async Task Main(string[] args)
    {
        botClient = new TelegramBotClient("557207430:AAHzpkxqxcYPlzBBCJi7QVYjrOwpRi3umi8");

        var me = await botClient.GetMeAsync();
        Console.WriteLine($"Hello, I am user {me.Id} and my name is {me.FirstName}.");

        var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var offset = 0;
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var updates = await botClient.GetUpdatesAsync(offset, cancellationToken: cancellationToken);
                foreach (var update in updates)
                {
                    if (update.Type == UpdateType.Message && update.Message.Text == "/start")
                    {
                        await ShowMainMenu(botClient, update.Message.Chat.Id);
                    }

                    // Обработка текстовых сообщений
                    if (update.Type == UpdateType.Message && update.Message.Text != "/start")
                    {
                        await HandleUserInput(botClient, update.Message.Chat.Id, update.Message.Text, update.CallbackQuery);
                    }

                    if (update.Type == UpdateType.CallbackQuery)
                    {
                        await HandleCallbackQuery(botClient, update.CallbackQuery);
                    }

                    offset = update.Id + 1; // Увеличиваем offset
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                await Task.Delay(1000); // Задержка перед повторной попыткой
            }
        }
    }
}
