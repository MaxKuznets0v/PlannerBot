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
        static Planner planner = new Planner();
        public static ITelegramBotClient bot;
        private static string token = "";
        private static HttpToSocks5Proxy proxy = new HttpToSocks5Proxy("96.113.166.133", 1080);
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
                    //Send(userId, "Choose time and importance below");
                    //Buttons for time
                    //bot.OnCallbackQuery += AddButtonsTime;
                    Send(userId, "Type business name");
                    string name = "";
                    bot.OnMessage -= CommandsHandler;
                    bot.OnMessage += (object sendr, Telegram.Bot.Args.MessageEventArgs ev) =>
                    {
                        name = ev.Message.Text;
                    };
                    var TimeMarkup = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup(new []
                    {
                        new []
                        {
                            new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Today"),
                            new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Tomorrow"),
                            new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("No Term")
                        }
                    });
                    TimeMarkup.OneTimeKeyboard = true;
                    await bot.SendTextMessageAsync(userId, "Choose Time below!", replyMarkup: TimeMarkup);
                    var time = new Time();
                    bot.OnMessage += (object sendr, Telegram.Bot.Args.MessageEventArgs ev) =>
                    {
                        switch (ev.Message.Text)
                        {
                            case "Today":
                                time = Time.Today;
                                break;
                            case "Tomorrow":
                                time = Time.Tomorrow;
                                break;
                            case "No Term":
                                time = Time.NoTerm;
                                break;
                        }
                    };
                    //Buttons for importance
                    var ImpMarkup = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup(new []
                    {
                        new[]
                        {
                            new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Important"),
                            new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Medium"),
                            new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Casual")
                        }
                    });
                    ImpMarkup.OneTimeKeyboard = true;
                    await bot.SendTextMessageAsync(userId, "Choose importance below!", replyMarkup: ImpMarkup);
                    var imp = new Importance();
                    bot.OnMessage += async (object sendr, Telegram.Bot.Args.MessageEventArgs ev) =>
                    {
                        switch (ev.Message.Text)
                        {
                            case "Important":
                                imp = Importance.Important;
                                break;
                            case "Medium":
                                imp = Importance.Medium;
                                break;
                            case "Casual":
                                imp = Importance.Casual;
                                break;
                        }
                    };
                    
                    planner.Add(e.Message.Chat.Id, new Event(name, time, imp));
                    Send(e.Message.Chat.Id, "Record added!");
                    bot.OnMessage += CommandsHandler;
                    break;
                case "/show":
                    
                    break;
                case "/mark":

                    break;
                case "/delay":

                    break;
                case "/help":
                    Send(userId, "This bot helps you to plan your businesses!\n" +
                        "/add adds new plan then type <business name-time,importance>, where " +
                        "time = Today/Tomorrow/NoTerm, importance = Important/Medium/Casual" +
                        "\n/mark then choose a bussiness to mark it done\n" +
                        "/show shows all records\n/delay then choose a different time for your business");
                    break;
                default:
                    Send(userId, "Enter a proper command! Type /help to get more info");
                    break;
            }
        }

        private static void AddButtonsTime(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            bot.OnCallbackQuery -= AddButtonsTime;
            bot.OnCallbackQuery += AddButtonsImp;
            var respond = e.CallbackQuery.Message.Text;
            switch(respond)
            {
                case "Today":
                    break;
                case "Tomorrow":
                    break;
                case "No Term":
                    break;
            }
        }

        private static void AddButtonsImp(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            bot.OnCallbackQuery -= AddButtonsImp;
            var respond = e.CallbackQuery.Message.Text;
            switch (respond)
            {
                case "Important":
                    break;
                case "Medium":
                    break;
                case "Casual":
                    break;
            }
        }

        private static void AddHandler(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            bot.OnMessage -= AddHandler;
            bot.OnMessage += CommandsHandler;

            var text = e.Message.Text;
            string name = "", time = "", imp = "";
            Time t = new Time();
            Importance impnc = new Importance();
            int i = 0;
            for (i = 0; i < text.Length; ++i)
            {
                if (text[i] != '-')
                    name += text[i];
                else
                    break;
            }
            ++i;
            for (int j = i; j < text.Length; ++j, i = j)
            {
                if (text[j] != ',')
                    time += text[j];
                else
                    break;
            }
            ++i;
            for (int j = i; j < text.Length; ++j, i = j)
            {
                if (text[j] != ',')
                    imp += text[j];
                else
                    break;
            }
            time.ToLower();
            imp.ToLower();
            if (name != "" && time != "" && imp != "")
            {
                if ((time == "today" || time == "tomorrow" || time == "noterm") &&
                    (imp == "important" || imp == "medium" || imp == "casual"))
                {
                    switch (time)
                    {
                        case "today":
                            t = Time.Today;
                            break;
                        case "tomorrow":
                            t = Time.Tomorrow;
                            break;
                        case "noterm":
                            t = Time.NoTerm;
                            break;
                    }
                    switch (imp)
                    {
                        case "important":
                            impnc = Importance.Important;
                            break;
                        case "medium":
                            impnc = Importance.Medium;
                            break;
                        case "casual":
                            impnc = Importance.Casual;
                            break;
                    }

                    planner.Add(e.Message.Chat.Id, new Event(name, t, impnc));
                    Send(e.Message.Chat.Id, "Record added!");
                    return;
                }
            }
            Send(e.Message.Chat.Id, "Wrong format!");
        }
    }
}
