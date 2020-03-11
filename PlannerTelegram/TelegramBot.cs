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

        private static void CommandsHandler(object sender, Telegram.Bot.Args.MessageEventArgs e)
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
                    Send(userId, "Type <business name-time,importance>, where " +
                    "time = Today/Tomorrow/NoTerm, importance = Important/Medium/Casual");
                    bot.OnMessage -= CommandsHandler;

                    bot.OnMessage += AddHandler;
                    //Console.ReadKey();
                    break;
                case "/show":
                    
                    break;
                case "/mark":

                    break;
                case "/delay":

                    break;
                case "/help":
                    Send(userId, "This bot helps you to plan your businesses!\n" +
                        "/add add new plan then type <business name-time,importance>, where " +
                        "time = Today/Tomorrow/NoTerm, importance = Important/Medium/Casual" +
                        "\n/mark then choose a bussiness to mark it done\n" +
                        "/show shows all records\n/delay then choose a different time for your business");
                    break;
                default:
                    Send(userId, "Enter a proper command! Type /help to get more info");
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

                    planner.Add(e.Message.Chat.Id, new Event(name, t, impnc ));
                    Send(e.Message.Chat.Id, "Record added!");
                    return;
                }
            }
            Send(e.Message.Chat.Id, "Wrong format!");
        }
    }
}
