using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;
using MihaZupan;

namespace PlannerTelegram
{
    class TelegramBot
    {
        static bool Debug = false;
        static private Tuple<Event, int> tempEvent = new Tuple<Event, int>(new Event(), 0);
        static Planner planner = new Planner();
        public static ITelegramBotClient bot;
        private static string token = "";
        private static HttpToSocks5Proxy proxy = new HttpToSocks5Proxy("96.96.1.165", 1080);
        static void Main(string[] args)
        {
            if (!Debug)
            {
                bot = new TelegramBotClient(token, proxy) { Timeout = TimeSpan.FromSeconds(10) };
                var me = bot.GetMeAsync().Result;
                Console.WriteLine($"Bot connected id: {me.Id}. Bot name: {me.FirstName}");

                bot.OnMessage += CommandsHandler;
                bot.StartReceiving();
            }

            Console.ReadKey();
        }

        static async void Send(Telegram.Bot.Types.ChatId userId, string text)
        {
            await bot.SendTextMessageAsync(
                        chatId: userId,
                        text: text
                        ).ConfigureAwait(false);
        }

        private static async void CommandsHandler(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var text = e.Message.Text;
            var userId = e.Message.Chat.Id;
            if (text == null)
                return;
            Console.WriteLine($"User {userId}({e.Message.Chat.FirstName}) sent {text}");

            switch (text)
            {
                case "/start":
                    Send(userId, "This bot helps you to plan your businesses!\n" +
                        "Press /add to start planning!\nPress /help to read more about commands");
                    break;
                case "/add":
                    bot.OnMessage -= CommandsHandler;
                    Send(userId, "Type business name");
                    bot.OnMessage += AddHandler;
                    break;
                case "/show":
                    if (!planner.Contains(userId))
                        Send(userId, "You have no plans!");
                    else
                    {
                        var plans = planner.Get(userId);
                        string res = $"You have planned {plans.Count()} things!\n";
                        for (int i = 0; i < plans.Count(); ++i)
                        {
                            res += $"{i + 1}. {plans[i].name} {plans[i].time} {plans[i].importance}\n";
                        }
                        Send(userId, res);
                    }
                    break;
                case "/mark":
                    bot.OnMessage -= CommandsHandler;
                    var userEvents = planner.Get(userId);
                    List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton> list = new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>();
                    for (int i = 0; i < userEvents.Count(); ++i)
                    {
                        string cur = $"{userEvents[i].name} {userEvents[i].time} {userEvents[i].importance}";
                        list.Add(Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData($"{cur}", $"{i}"));
                    }
                    var markup = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(list);
                    await bot.SendTextMessageAsync(userId, "Choose a deal you want to mark:", replyMarkup: markup);

                    bot.OnCallbackQuery += MarkHandler;
                    break;
                case "/delay":

                    break;
                case "/help":
                    Send(userId, "This bot helps you to plan your businesses!\n" +
                        "/add adds new plan" +
                        "\n/show shows all records\n" +
                        "/mark then choose a bussiness to mark it done\n" +
                        "/delay then choose a different time for your business");
                    break;
                default:
                    Send(userId, "Enter a proper command! Type /help to get more info");
                    break;
            }
        }

        private static async void MarkHandler(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            bot.OnCallbackQuery -= MarkHandler;
            int curEvent = Int16.Parse(e.CallbackQuery.Data);
            tempEvent = new Tuple<Event, int>(planner.Get(e.CallbackQuery.Message.Chat.Id)[curEvent], curEvent);
            var markup = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup(new[]
            {
                new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Done"),
                new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Not done")
            });
            await bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "Choose state", replyMarkup: markup);
            bot.OnMessage += MarkChoose;
        }

        private static void MarkChoose(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            bot.OnMessage -= MarkChoose;
            switch (e.Message.Text)
            {
                case "Done":
                    tempEvent.Item1.done = true;
                    break;
                case "Not done":
                    tempEvent.Item1.done = false;
                    break;
            }
            var markup = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardRemove();
            bot.SendTextMessageAsync(e.Message.Chat.Id, $"State was successfully changed!", replyMarkup: markup);
            planner.Mark(e.Message.Chat.Id, tempEvent.Item2, tempEvent.Item1.done);
            tempEvent = new Tuple<Event, int>(new Event(), 0);
            bot.OnMessage += CommandsHandler;
        }

        private static async void AddButtonsTime(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            bot.OnMessage -= AddButtonsTime;
            bot.OnMessage += AddButtonsImp;
            var respond = e.Message.Text;
            switch(respond)
            {
                case "Today":
                    tempEvent.Item1.time = Time.Today;
                    break;
                case "Tomorrow":
                    tempEvent.Item1.time = Time.Tomorrow;
                    break;
                case "No Term":
                    tempEvent.Item1.time = Time.NoTerm;
                    break;
            }
            var ImpMarkup = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Important"),
                            new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Medium"),
                            new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Casual")
                        }
                    });
            ImpMarkup.OneTimeKeyboard = true;
            await bot.SendTextMessageAsync(e.Message.Chat.Id, "Choose importance below!", replyMarkup: ImpMarkup);
        }

        private static void AddButtonsImp(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            bot.OnMessage -= AddButtonsImp;
            var respond = e.Message.Text;
            switch (respond)
            {
                case "Important":
                    tempEvent.Item1.importance = Importance.Important;
                    break;
                case "Medium":
                    tempEvent.Item1.importance = Importance.Medium;
                    break;
                case "Casual":
                    tempEvent.Item1.importance = Importance.Casual;
                    break;
            }
            planner.Add(e.Message.Chat.Id, new Event(tempEvent.Item1));
            tempEvent = new Tuple<Event, int>(new Event(), 0);

            var markup = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardRemove();
            bot.SendTextMessageAsync(e.Message.Chat.Id, "Record added!", replyMarkup: markup);
            bot.OnMessage += CommandsHandler;
        }

        private static async void AddHandler(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            bot.OnMessage -= AddHandler;
            tempEvent.Item1.name = e.Message.Text;
            if (tempEvent.Item1.name == "")
            {
                Send(e.Message.Chat.Id, "Enter non empty name!");
                bot.OnMessage += CommandsHandler;
            }
            else
            {
                var TimeMarkup = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup(new[]
                    {
                        new []
                        {
                            new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Today"),
                            new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Tomorrow"),
                            new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("No Term")
                        }
                    });
                TimeMarkup.OneTimeKeyboard = true;
                await bot.SendTextMessageAsync(e.Message.Chat.Id, "Choose Time below!", replyMarkup: TimeMarkup);
                bot.OnMessage += AddButtonsTime;
            }

        }
    }
}
