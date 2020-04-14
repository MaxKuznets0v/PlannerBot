using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
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
        static private Dictionary<long, Tuple<Event, int>> tempEvents =  new Dictionary<long, Tuple<Event, int>>();
        static readonly Planner planner = new Planner();
        static private Dictionary<long, State> states = new Dictionary<long, State>();
        public static ITelegramBotClient bot;
        private readonly static string token = "";
        private readonly static string logPath = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory().ToString()).ToString()) + @"\Users\logs.txt";
        //private readonly static HttpToSocks5Proxy proxy = new HttpToSocks5Proxy("96.96.33.133", 1080);
        static void Main(string[] args)
        {
            //bot = new TelegramBotClient(token, proxy) { Timeout = TimeSpan.FromSeconds(10) };
            bot = new TelegramBotClient(token) { Timeout = TimeSpan.FromSeconds(10) };
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
            ThreadPool.QueueUserWorkItem(Save);

            // updating records
            ThreadPool.QueueUserWorkItem(MidnightUpdate, bot);

            // Sending notifications
            ThreadPool.QueueUserWorkItem(planner.Notify, bot);

            bot.StartReceiving();

            Console.ReadKey();
        }
        public static void MidnightUpdate(Object state)
        {
            Thread.Sleep(DateTime.Now.AddHours(3).AddDays(1).Date - DateTime.Now.AddHours(3));
            while (true)
            {
                planner.MidnightUpdate(bot);
                Thread.Sleep(TimeSpan.FromDays(1));
            }
        }
        public static void Save(Object state)
        {
            while (true)
            {
                Thread.Sleep(30000);
                planner.Save();
            }
        }
        static public async void Send(long userId, string text)
        {
            try
            {
                await bot.SendTextMessageAsync(
                            chatId: userId,
                            text: text
                            ).ConfigureAwait(false);
            }
            catch
            {
                Console.WriteLine($"Error: forbidden user {userId}");
                planner.DeleteUser(userId);
                states.Remove(userId);
                tempEvents.Remove(userId);
            }
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
            Tuple<Event, int> tempEvent;
            switch (states[userId])
            {
                case State.MarkName:
                    int curEvent = Int16.Parse(e.CallbackQuery.Data);
                    tempEvent = new Tuple<Event, int>(new Event(planner.Get(e.CallbackQuery.Message.Chat.Id)[curEvent]), curEvent);
                    tempEvents[userId] = tempEvent;
                    var markup = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup(new[]
                    {
                        new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Done"),
                        new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Not done")
                    });
                    try
                    {
                        await bot.SendTextMessageAsync(userId, "Choose state", replyMarkup: markup);
                    }
                    catch
                    {
                        Console.WriteLine($"Error: forbidden user {userId}");
                        planner.DeleteUser(userId);
                        states.Remove(userId);
                        tempEvents.Remove(userId);
                        break;
                    }
                    states[userId] = State.MarkDone;
                    break;
                case State.DelayName:
                    int curEventDelayName = Int16.Parse(e.CallbackQuery.Data);
                    tempEvent = new Tuple<Event, int>(new Event(planner.Get(e.CallbackQuery.Message.Chat.Id)[curEventDelayName]), curEventDelayName);
                    tempEvents[userId] = tempEvent;
                    var markupDelayName = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardMarkup(new[]
                    {
                        new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Today"),
                        new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("Tomorrow"),
                        new Telegram.Bot.Types.ReplyMarkups.KeyboardButton("No Term")
                    });
                    try
                    {
                        await bot.SendTextMessageAsync(e.CallbackQuery.Message.Chat.Id, "Choose state", replyMarkup: markupDelayName);
                    }
                    catch
                    {
                        Console.WriteLine($"Error: forbidden user {userId}");
                        planner.DeleteUser(userId);
                        states.Remove(userId);
                        tempEvents.Remove(userId);
                        break;
                    }
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
            try
            {
                //logging messages
                File.AppendAllText(logPath, $"{userId}({e.Message.Chat.FirstName} {e.Message.Chat.LastName}): {text}, {e.Message.Date}\n");
            }
            catch
            {
                File.AppendAllText(logPath, $"{userId}({e.Message.Chat.FirstName}): {text}, {e.Message.Date}\n");
            }
            Console.WriteLine($"User {userId}({e.Message.Chat.FirstName}) sent {text}");
            if (!states.ContainsKey(userId))
                states.Add(userId, State.CommandReciever);

            if (!tempEvents.ContainsKey(userId))
                tempEvents[userId] = new Tuple<Event, int>(new Event(), 0);
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
                                "/remove then choose a record to remove\n" +
                                "(You'll receive notifications and daily statistics)");
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
                            try
                            {
                                await bot.SendTextMessageAsync(userId, "Choose a deal you want to mark:", replyMarkup: markupMarkName);
                            }
                            catch
                            {
                                Console.WriteLine($"Error: forbidden user {userId}");
                                planner.DeleteUser(userId);
                                states.Remove(userId);
                                tempEvents.Remove(userId);
                                break;
                            }
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
                            try
                            { 
                                await bot.SendTextMessageAsync(userId, "Choose a deal you want to delay:", replyMarkup: markupDelay);
                            }
                            catch
                            {
                                Console.WriteLine($"Error: forbidden user {userId}");
                                planner.DeleteUser(userId);
                                states.Remove(userId);
                                tempEvents.Remove(userId);
                                break;
                            }
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
                            try
                            {
                                await bot.SendTextMessageAsync(userId, "Choose a deal you want to delay:", replyMarkup: markupRemove);
                            }
                            catch
                            {
                                Console.WriteLine($"Error: forbidden user {userId}");
                                planner.DeleteUser(userId);
                                states.Remove(userId);
                                tempEvents.Remove(userId);
                                break;
                            }
                            break;
                        default:
                            Send(userId, "Enter a proper command! Type /help to get more info");
                            break;
                    }
                    break;
                case State.AddName:
                    tempEvents[userId].Item1.name = text;
                    if (tempEvents[userId].Item1.name == "")
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
                        try
                        {
                            await bot.SendTextMessageAsync(userId, "Choose Time below!", replyMarkup: TimeMarkup);
                        }
                        catch
                        {
                            Console.WriteLine($"Error: forbidden user {userId}");
                            planner.DeleteUser(userId);
                            states.Remove(userId);
                            tempEvents.Remove(userId);
                            break;
                        }
                    }
                    states[userId] = State.AddTime;
                    break;
                case State.AddTime:
                    switch (text)
                    {
                        case "Today":
                            tempEvents[userId].Item1.time = Time.Today;
                            break;
                        case "Tomorrow":
                            tempEvents[userId].Item1.time = Time.Tomorrow;
                            break;
                        case "No Term":
                            tempEvents[userId].Item1.time = Time.NoTerm;
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
                    try
                    {
                        await bot.SendTextMessageAsync(userId, "Choose importance below!", replyMarkup: ImpMarkup);
                    }
                    catch
                    {
                        Console.WriteLine($"Error: forbidden user {userId}");
                        planner.DeleteUser(userId);
                        states.Remove(userId);
                        tempEvents.Remove(userId);
                        break;
                    }
                    states[userId] = State.AddImportance;
                    break;
                case State.AddImportance:
                    switch (text)
                    {
                        case "Important":
                            tempEvents[userId].Item1.importance = Importance.Important;
                            break;
                        case "Medium":
                            tempEvents[userId].Item1.importance = Importance.Medium;
                            break;
                        case "Casual":
                            tempEvents[userId].Item1.importance = Importance.Casual;
                            break;
                        default:
                            Send(userId, "Push the buttons!");
                            return;
                    }
                    tempEvents[userId].Item1.owner = userId;
                    planner.Add(userId, new Event(tempEvents[userId].Item1.name, tempEvents[userId].Item1.time, tempEvents[userId].Item1.importance, tempEvents[userId].Item1.owner));
                    tempEvents[userId] = new Tuple<Event, int>(new Event(), 0);

                    var markup = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardRemove();
                    try
                    {
                        await bot.SendTextMessageAsync(e.Message.Chat.Id, "Record added!", replyMarkup: markup);
                    }
                    catch
                    {
                        Console.WriteLine($"Error: forbidden user {userId}");
                        planner.DeleteUser(userId);
                        states.Remove(userId);
                        tempEvents.Remove(userId);
                        break;
                    }
                    states[userId] = State.CommandReciever;
                    break;
                case State.MarkDone:
                    switch (text)
                    {
                        case "Done":
                            tempEvents[userId].Item1.done = true;
                            break;
                        case "Not done":
                            tempEvents[userId].Item1.done = false;
                            break;
                        default:
                            Send(userId, "Push the buttons!");
                            return;
                    }
                    var markupMarkDone = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardRemove();
                    try
                    {
                        await bot.SendTextMessageAsync(e.Message.Chat.Id, $"State was successfully changed!", replyMarkup: markupMarkDone);
                    }
                    catch
                    {
                        Console.WriteLine($"Error: forbidden user {userId}");
                        planner.DeleteUser(userId);
                        states.Remove(userId);
                        tempEvents.Remove(userId);
                        break;
                    }
                    planner.Mark(e.Message.Chat.Id, tempEvents[userId].Item2, tempEvents[userId].Item1.done);
                    tempEvents[userId] = new Tuple<Event, int>(new Event(), 0);
                    states[userId] = State.CommandReciever;
                    break;
                case State.DelayTime:
                    switch (text)
                    {
                        case "Today":
                            tempEvents[userId].Item1.time = Time.Today;
                            break;
                        case "Tomorrow":
                            tempEvents[userId].Item1.time = Time.Tomorrow;
                            break;
                        case "No Term":
                            tempEvents[userId].Item1.time = Time.NoTerm;
                            break;
                        default:
                            Send(userId, "Push the buttons!");
                            return;
                    }
                    var markupDelayTime = new Telegram.Bot.Types.ReplyMarkups.ReplyKeyboardRemove();
                    try
                    {
                        await bot.SendTextMessageAsync(userId, $"Changes saved!", replyMarkup: markupDelayTime);
                    }
                    catch
                    {
                        Console.WriteLine($"Error: forbidden user {userId}");
                        planner.DeleteUser(userId);
                        states.Remove(userId);
                        tempEvents.Remove(userId);
                        break;
                    }
                    planner.Delay(userId, tempEvents[userId].Item2, tempEvents[userId].Item1.time);
                    tempEvents[userId] = new Tuple<Event, int>(new Event(), 0);
                    states[userId] = State.CommandReciever;
                    break;
                default:
                    tempEvents[userId] = new Tuple<Event, int>(new Event(), 0);
                    //Send(userId, "Something went wrong! Code: Message");
                    Send(userId, "Try to push the buttons! Try again");
                    states[userId] = State.CommandReciever;
                    return;
            }
        }
    }
}
