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
        static private Event tempEvent = new Event();
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

        private static async void AddButtonsTime(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            bot.OnMessage -= AddButtonsTime;
            bot.OnMessage += AddButtonsImp;
            var respond = e.Message.Text;
            switch(respond)
            {
                case "Today":
                    tempEvent.time = Time.Today;
                    break;
                case "Tomorrow":
                    tempEvent.time = Time.Tomorrow;
                    break;
                case "No Term":
                    tempEvent.time = Time.NoTerm;
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
                    tempEvent.importance = Importance.Important;
                    break;
                case "Medium":
                    tempEvent.importance = Importance.Medium;
                    break;
                case "Casual":
                    tempEvent.importance = Importance.Casual;
                    break;
            }
            planner.Add(e.Message.Chat.Id, new Event(tempEvent));
            tempEvent = new Event();
            Send(e.Message.Chat.Id, "Record added!");
            bot.OnMessage += CommandsHandler;
        }

        private static async void AddHandler(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            bot.OnMessage -= AddHandler;
            tempEvent.name = e.Message.Text;
            if (tempEvent.name == "")
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
