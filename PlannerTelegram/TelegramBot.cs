using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Telegram.Bot;
using MihaZupan;

namespace PlannerTelegram
{
    // states for each message
    enum State
    { 
        CommandReciever,
        AddName,
        AddTime,
        AddImportance,
        MarkName,
        MarkDone,
        DelayName,
        DelayTime,
        RemoveName
    }
    class TelegramBot
    {
        static private Tuple<Event, int> tempEvent = new Tuple<Event, int>(new Event(), 0);
        static readonly Planner planner = new Planner();
        static private Dictionary<long, State> states = new Dictionary<long, State>();
        public static ITelegramBotClient bot;
        private readonly static string token = "";
        private readonly static HttpToSocks5Proxy proxy = new HttpToSocks5Proxy("96.96.1.165", 1080);
        static void Main(string[] args)
        {
            bot = new TelegramBotClient(token, proxy) { Timeout = TimeSpan.FromSeconds(10) };
            try
            {
                var me = bot.GetMeAsync().Result;
                Console.WriteLine($"Bot connected id: {me.Id}. Bot name: {me.FirstName}");
            }
            catch (Exception)
            {
                Console.WriteLine("Time Out");
                return;
            }

            bot.OnMessage += OnMessageHandler;
            bot.OnCallbackQuery += OnCallbackQueryHandler;

            // Saving records every 30 sec
            var timerSave = new Timer(o => { planner.Save(); }, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            // updating records
            DateTime midnight = DateTime.Now.AddDays(1).Date;
            DateTime now = DateTime.Now;
            var timerMidUpd = new Timer(o => { planner.MidnightUpdate(); }, null, midnight - now, TimeSpan.FromDays(1));

            // Sending notifications
            ThreadPool.QueueUserWorkItem(planner.Notify, bot);

            bot.StartReceiving();

            Console.ReadKey();
        }

        static public async void Send(Telegram.Bot.Types.ChatId userId, string text)
        {
            await bot.SendTextMessageAsync(
                        chatId: userId,
                        text: text
                        ).ConfigureAwait(false);
        }

        private static async void OnCallbackQueryHandler(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
        {
            var userId = e.CallbackQuery.Message.Chat.Id;
            try
            {
                var a = states[userId];
            }
            catch (Exception)
            {
                return;
            }
            switch (states[userId])
            {
                case State.MarkName:
                    int curEvent = Int16.Parse(e.CallbackQuery.Data);
                    tempEvent = new Tuple<Event, int>(new Event(planner.Get(e.CallbackQuery.Message.Chat.Id)[curEvent]), curEvent);
                    var markup = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup(new[]
                    {
                        new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Done"),
                        new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Not done")
                    });
                    await bot.SendTextMessageAsync(userId, "Choose state", replyMarkup: markup);
                    states[userId] = State.MarkDone;
                    break;
                case State.DelayName:
                    int curEventDelayName = Int16.Parse(e.CallbackQuery.Data);
                    tempEvent = new Tuple<Event, int>(new Event(planner.Get(e.CallbackQuery.Message.Chat.Id)[curEventDelayName]), curEventDelayName);
                    var markupDelayName = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup(new[]
                    {
                        new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Today"),
                        new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Tomorrow"),
                        new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("No Term")
                    });
                    await bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "Choose state", replyMarkup: markupDelayName);
                    states[userId] = State.DelayTime;
                    break;
                case State.RemoveName:
                    int curEventRemoveName = Int16.Parse(e.CallbackQuery.Data);
                    planner.Remove(userId, curEventRemoveName);
                    Send(userId, "Record was removed!");
                    states[userId] = State.CommandReciever;
                    break;
                //default:
                //    Send(userId, "Something went wrong! Code: Callback");
                //    break;
            }
        }

        private static async void OnMessageHandler(object sender, Telegram.Bot.Args.MessageEventArgs e)
        {
            var text = e.Message.Text;
            var userId = e.Message.Chat.Id;
            if (text == null)
                return;
            Console.WriteLine($"User {userId}({e.Message.Chat.FirstName}) sent {text}");
            if (!states.ContainsKey(userId))
                states.Add(userId, State.CommandReciever);

            switch (states[userId])
            {
                case State.CommandReciever:
                    switch (text)
                    {
                        case "/start":
                            Send(userId, "This bot helps you to plan your businesses!\n" +
                                "Press /add to start planning!\nPress /help to read more about commands");
                            break;
                        case "/help":
                            Send(userId, "This bot helps you to plan your businesses!\n" +
                                "/add adds new plan" +
                                "\n/show shows all records\n" +
                                "/mark then choose a bussiness to mark it done\n" +
                                "/delay then choose a different time for your business\n" +
                                "/remove then choose a record to remove");
                            break;
                        case "/show":
                            if (!planner.Contains(userId) || planner.Get(userId).Count() == 0)
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
                        case "/add":
                            states[userId] = State.AddName;
                            Send(userId, "Type business name");
                            break;
                        case "/mark":
                            states[userId] = State.MarkName;
                            var userEventsMarkName = planner.Get(userId);
                            List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>> listMarkName = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>();
                            for (int i = 0; i < userEventsMarkName.Count(); ++i)
                            {
                                string cur = $"{userEventsMarkName[i].name} {userEventsMarkName[i].time} {userEventsMarkName[i].importance}";
                                var addingList = new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                                {
                                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData($"{cur}", $"{i}")
                                };
                                listMarkName.Add(addingList);
                            }
                            var markupMarkName = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(listMarkName);
                            await bot.SendTextMessageAsync(userId, "Choose a deal you want to mark:", replyMarkup: markupMarkName);
                            break;
                        case "/delay":
                            states[userId] = State.DelayName;
                            var userEvnts = planner.Get(userId);
                            List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>> listDelay = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>();
                            for (int i = 0; i < userEvnts.Count(); ++i)
                            {
                                string cur = $"{userEvnts[i].name} {userEvnts[i].time} {userEvnts[i].importance}";
                                var addingList = new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                                {
                                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData($"{cur}", $"{i}")
                                };
                                listDelay.Add(addingList);
                            }
                            var markupDelay = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(listDelay);
                            await bot.SendTextMessageAsync(userId, "Choose a deal you want to delay:", replyMarkup: markupDelay);
                            break;
                        case "/remove":
                            states[userId] = State.RemoveName;
                            var usrEvnts = planner.Get(userId);
                            List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>> listRemove = new List<List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>>();
                            for (int i = 0; i < usrEvnts.Count(); ++i)
                            {
                                string cur = $"{usrEvnts[i].name} {usrEvnts[i].time} {usrEvnts[i].importance}";
                                var addingList = new List<Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton>
                                {
                                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData($"{cur}", $"{i}")
                                };
                                listRemove.Add(addingList);
                            }
                            var markupRemove = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(listRemove);
                            await bot.SendTextMessageAsync(userId, "Choose a deal you want to delay:", replyMarkup: markupRemove);
                            break;
                        default:
                            Send(userId, "Enter a proper command! Type /help to get more info");
                            break;
                    }
                    break;
                case State.AddName:
                    tempEvent.Item1.name = text;
                    if (tempEvent.Item1.name == "")
                        Send(userId, "Enter non empty name!");
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
                        })
                        {
                            OneTimeKeyboard = true
                        };
                        await bot.SendTextMessageAsync(userId, "Choose Time below!", replyMarkup: TimeMarkup);
                    }
                    states[userId] = State.AddTime;
                    break;
                case State.AddTime:
                    switch (text)
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
                        default:
                            Send(userId, "Push the buttons!");
                            return;
                    }
                    var ImpMarkup = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Important"),
                            new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Medium"),
                            new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Casual")
                        }
                    })
                    {
                        OneTimeKeyboard = true
                    };
                    await bot.SendTextMessageAsync(userId, "Choose importance below!", replyMarkup: ImpMarkup);
                    states[userId] = State.AddImportance;
                    break;
                case State.AddImportance:
                    switch (text)
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
                        default:
                            Send(userId, "Push the buttons!");
                            return;
                    }
                    tempEvent.Item1.owner = userId;
                    planner.Add(userId, new Event(tempEvent.Item1));
                    tempEvent = new Tuple<Event, int>(new Event(), 0);

                    var markup = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardRemove();
                    await bot.SendTextMessageAsync(e.Message.Chat.Id, "Record added!", replyMarkup: markup);
                    states[userId] = State.CommandReciever;
                    break;
                case State.MarkDone:
                    switch (text)
                    {
                        case "Done":
                            tempEvent.Item1.done = true;
                            break;
                        case "Not done":
                            tempEvent.Item1.done = false;
                            break;
                        default:
                            Send(userId, "Push the buttons!");
                            return;
                    }
                    var markupMarkDone = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardRemove();
                    await bot.SendTextMessageAsync(e.Message.Chat.Id, $"State was successfully changed!", replyMarkup: markupMarkDone);
                    planner.Mark(e.Message.Chat.Id, tempEvent.Item2, tempEvent.Item1.done);
                    tempEvent = new Tuple<Event, int>(new Event(), 0);
                    states[userId] = State.CommandReciever;
                    break;
                case State.DelayTime:
                    switch (text)
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
                        default:
                            Send(userId, "Push the buttons!");
                            return;
                    }
                    var markupDelayTime = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardRemove();
                    await bot.SendTextMessageAsync(userId, $"Changes saved!", replyMarkup: markupDelayTime);
                    planner.Delay(userId, tempEvent.Item2, tempEvent.Item1.time);
                    tempEvent = new Tuple<Event, int>(new Event(), 0);
                    states[userId] = State.CommandReciever;
                    break;
                default:
                    tempEvent = new Tuple<Event, int>(new Event(), 0);
                    Send(userId, "Something went wrong! Code: Message");
                    Send(userId, "Push the buttons!");
                    states[userId] = State.CommandReciever;
                    return;
            }
        }
    }
}
