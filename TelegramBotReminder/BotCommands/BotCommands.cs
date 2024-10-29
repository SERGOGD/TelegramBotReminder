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


public static class BotCommands
{
    private static Dictionary<long, List<(string ReminderText, DateTime ReminderTime)>> userReminders = new();
    private static Dictionary<long, string> userStates = new(); // Словарь для отслеживания состояния пользователя

    // Отображение главного меню с кнопками
    public static async Task ShowMainMenu(ITelegramBotClient botClient, long chatId)
    {
        var replyKeyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("Добавить напоминание"), new KeyboardButton("Посмотреть напоминания") },
            //new[] { new KeyboardButton("Посмотреть напоминания") }
        })
        {
            ResizeKeyboard = true
        };

        await botClient.SendTextMessageAsync(chatId, "Выберите действие:", replyMarkup: replyKeyboard);
    }
    // Обработка нажатий кнопок
    public static async Task HandleUserInput(ITelegramBotClient botClient, long chatId, string userInput, CallbackQuery callbackQuery)
    {
        if (userInput == "Добавить напоминание")
        {
            userStates[chatId] = "waiting_for_reminder"; // Устанавливаем состояние ожидания напоминания
            await botClient.SendTextMessageAsync(chatId, "Введите напоминание:");
        }
        else if (userInput == "Посмотреть напоминания")
        {
            await ShowReminders(botClient, chatId);
        }
        else if (userStates.ContainsKey(chatId))
        {

            // Если пользователь вводит напоминание
            if (userStates[chatId] == "waiting_for_reminder")
            {
                userStates.Remove(chatId); // Удаляем состояние
                await AskForReminderTime(botClient, chatId, userInput);

            }
            // Если пользователь вводит время для напоминания
            else if (userStates[chatId] == "waiting_for_time")
            {
                //userStates.Remove(chatId); // Удаляем состояние
                await AddReminder(botClient, chatId, userInput); // Передаем введенное время
            }
            if (userStates[chatId] == "waiting_for_deletion")
            {
                userStates.Remove(chatId); // Удаляем состояние
                await DeleteReminder(botClient, chatId, userInput);
            }

        }
        else
        {
            await botClient.SendTextMessageAsync(chatId, "Пожалуйста, используйте кнопки для взаимодействия.");
        }
    }

    // Запрос времени для напоминания
    private static async Task AskForReminderTime(ITelegramBotClient botClient, long chatId, string reminderText)
    {
        if (!userReminders.ContainsKey(chatId))
        {
            userReminders[chatId] = new List<(string ReminderText, DateTime ReminderTime)>();
        }

        // Добавляем напоминание с временной меткой по умолчанию
        userReminders[chatId].Add((reminderText, default));
        userStates[chatId] = "waiting_for_time"; // Устанавливаем состояние ожидания времени
        await botClient.SendTextMessageAsync(chatId, "Когда напомнить о нём? Введите дату и время в формате 'YYYY-MM-DD HH:mm'.");
    }

    // Добавление нового напоминания
    private static async Task AddReminder(ITelegramBotClient botClient, long chatId, string timeInput)
    {
        if (userReminders[chatId].Count > 0)
        {
            var reminderIndex = userReminders[chatId].Count - 1; // Индекс последнего добавленного напоминания
            var reminder = userReminders[chatId][reminderIndex]; // Получаем последнее добавленное напоминание

            if (DateTime.TryParse(timeInput, out DateTime reminderTime))
            {
                userReminders[chatId][reminderIndex] = (reminder.ReminderText, reminderTime); // Обновляем время напоминания
                // Запускаем задачу для уведомления пользователя в указанное время
                var delay = reminderTime - DateTime.Now;
                if (delay > TimeSpan.Zero)
                {
                    using (var context = new ReminderContext())
                    {
                        // Добавление нового напоминания в таблицу
                        var remainders = new Reminder
                        {
                            tgId = chatId,
                            ReminderText = reminder.ReminderText,
                            ReminderTime = reminderTime,
                        };
                        context.Add(remainders);
                        context.SaveChanges();
                    }
                    Task.Run(async () =>
                {
                    await Task.Delay(delay);
                    await botClient.SendTextMessageAsync(chatId, $"{reminderIndex}) Напоминание: {reminder.ReminderText}");//проверить индекс
                });
                    await botClient.SendTextMessageAsync(chatId, "Напоминание добавлено!");
                }
                else
                {
                    await botClient.SendTextMessageAsync(chatId, "Указанное время уже прошло. Напоминание не добавлено.");
                }
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId, "Неверный формат даты и времени. Пожалуйста, попробуйте снова.");
            }
        }
    }
    // Отображение списка напоминаний
    private static async Task ShowReminders(ITelegramBotClient botClient, long chatId)
    {
        // if (userReminders.ContainsKey(chatId) && userReminders[chatId].Count > 0)
        // {
        //     var remindersList = new StringBuilder("Ваши напоминания:\n");

        //     foreach (var reminder in userReminders[chatId])
        //     {
        //         remindersList.AppendLine($"Напоминание: {reminder.ReminderText}, Время: {reminder.ReminderTime}");
        //     }

        //     var inlineKeyboard = new InlineKeyboardMarkup(new[]
        //     {
        //     new[] { InlineKeyboardButton.WithCallbackData("Назад", "back") },
        //     new[] { InlineKeyboardButton.WithCallbackData("Удалить напоминание", "deletion") }
        //     });

        //     await botClient.SendTextMessageAsync(chatId, remindersList.ToString(), replyMarkup: inlineKeyboard);
        // }
        // else
        // {
        //     await botClient.SendTextMessageAsync(chatId, "У вас нет активных напоминаний.");
        // }
        using (var db = new ReminderContext())
        {
            if (db.Reminders.Count() > 0)
            {
                // получаем все объекты из таблицы
                var remindersList = db.Reminders.ToList();
                var messageText = "Список ваших напоминаний:\n\n";

                foreach (var u in remindersList)
                {
                    messageText += $"Id: {u.Id}\nНапоминание: {u.ReminderText}\nВремя: {u.ReminderTime}\n\n";
                }

                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                new[] { InlineKeyboardButton.WithCallbackData("Назад", "back"),  InlineKeyboardButton.WithCallbackData("Удалить", "deletion")},
                //new[] { InlineKeyboardButton.WithCallbackData("Удалить напоминание", "deletion") }
                });
                // Отправляем сообщение пользователю
                await botClient.SendTextMessageAsync(chatId, messageText, replyMarkup: inlineKeyboard);
            }
            else
            {
                await botClient.SendTextMessageAsync(chatId: chatId, text: "У вас нет напоминаний.");
            }




        }
    }


    //обработка нажатия на кнопки назад и удалить
    public static async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var chatId = callbackQuery.Message.Chat.Id;

        if (callbackQuery.Data == "back")
        {
            // Код для возврата в главное меню
            await botClient.DeleteMessageAsync(chatId, callbackQuery.Message.MessageId);
            // Здесь можно вызвать метод для отображения главного меню
            // Закрыть инлайн кнопку
            //await botClient.EditMessageReplyMarkupAsync(chatId, callbackQuery.Message.MessageId);
            await botClient.SendTextMessageAsync(chatId, "Вы вернулись в главное меню.");
            //await ShowMainMenu(botClient, chatId);
        }
        if (callbackQuery.Data == "deletion")
        {
            // Если пользователь хочет удалить напоминание
            userStates[chatId] = "waiting_for_deletion";
            await botClient.SendTextMessageAsync(chatId, "Введите номер напоминания для удаления:");
        }

        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id);
    }

    // Удаление напоминания
    private static async Task DeleteReminder(ITelegramBotClient botClient, long chatId, string input)
    {
        // if (int.TryParse(input, out int reminderIndex) && userReminders.TryGetValue(chatId, out var reminders) && reminderIndex > 0 && reminderIndex <= reminders.Count)
        // {
        //     reminders.RemoveAt(reminderIndex - 1); // Удаляем напоминание по индексу
        //     await botClient.SendTextMessageAsync(chatId, "Напоминание удалено.");
        // }
        // else
        // {
        //     await botClient.SendTextMessageAsync(chatId, "Неверный номер напоминания. Пожалуйста, попробуйте снова.");
        // }

        // userStates.Remove(chatId); // Сбрасываем состояние
        if (int.TryParse(input, out int bdId) && bdId > 0)
            using (var db = new ReminderContext())
            {
                if (db.Reminders.Count() <= bdId)
                {
                    // получаем объект таблицы по его ID
                    Reminder? reminder = db.Reminders.Find(bdId); //<= db.Reminders.Count()
                                                                  //удаляем объект
                    db.Reminders.Remove(reminder);
                    db.SaveChanges();
                    await botClient.SendTextMessageAsync(chatId, "Напоминание удалено.");


                }
            }
        else
        {
            await botClient.SendTextMessageAsync(chatId, "Неверный номер напоминания. Пожалуйста, попробуйте снова.");
        }

    }

}
