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
        public static ITelegramBotClient bot;
        private static string token = "";
        private static HttpToSocks5Proxy proxy = new HttpToSocks5Proxy("96.113.166.133", 1080);
        static void Main(string[] args)
        {
            bot = new TelegramBotClient(token, proxy) { Timeout = TimeSpan.FromSeconds(10) };
            var me = bot.GetMeAsync().Result;
            Console.WriteLine($"Bot connected id: {me.Id}. Bot name: {me.FirstName}");

            bot.OnMessage += Bot_OnMessage;
            bot.StartReceiving();


            bot.SendTextMessageAsync(
                        chatId: 270117918,
                        text: "You FOOL!"
                        );
            Console.ReadKey();
        }

        private static /*async*/ void Bot_OnMessage(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var text = e.Message.Text;
            if (text == null)
                return;
            Console.WriteLine($"User {e.Message.Chat.Id} sent {text}");

            switch (text)
            {
                case "/start":
                    bot.SendTextMessageAsync(
                        chatId: e.Message.Chat,
                        text: "You FOOL!"
                        );
                    break;
                case "/add":
                    break;
                default:

                    break;
            }
            //var text = e.Message.Text;
            //if (text == null)
            //    return;
            //Console.WriteLine($"Message {text} in chat id {e.Message.Chat.Id}");
            //await bot.SendTextMessageAsync(
            //    chatId: e.Message.Chat, 
            //    text: $"You said: {text}"
            //    ).ConfigureAwait(false);
        }
    }
}
