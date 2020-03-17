using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using Telegram.Bot;

namespace PlannerTelegram
{
    class Planner
    {
        private readonly object locker = new object();
        private static Dictionary<long, List<Event>> events;
        private static Dictionary<long, List<Event>> stats;
        private static List<Tuple<DateTime, Event>> todayNotif;
        //private static string dbpath = Directory.GetCurrentDirectory().ToString() + @"\Users\user_data.txt";  // path for storing user events
        //private static string dbpath = Directory.GetCurrentDirectory().ToString() + @"\Users\user_stat.txt";  // path for storing user stats
        //for debug
        private readonly static string dbPath = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory().ToString()).ToString()) + @"\Users\user_data.txt";  // path for storing user events
        private readonly static string statPath = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory().ToString()).ToString()) + @"\Users\user_stats.txt";  // path for storing user stats

        public Planner()
        {
            var database = File.ReadAllText(dbPath);
            todayNotif = new List<Tuple<DateTime, Event>>();
            events = JsonConvert.DeserializeObject<Dictionary<long, List<Event>>>(database);
            if (events == null)
                events = new Dictionary<long, List<Event>>();
            stats = new Dictionary<long, List<Event>>();
            // initiate notifications queue
            foreach (var user in events)
                foreach (var ev in user.Value)
                    if (ev.time == Time.Today)
                        foreach (var time in ev.notifyTime)
                            if (time >= DateTime.Now)
                                todayNotif.Add(new Tuple<DateTime, Event>(time, ev));
        }
        public bool Contains(long userId)
        {
            return events.ContainsKey(userId);
        }
        public void Add(long userId, Event e)
        {
            lock(locker)
            {
                if (!Contains(userId))
                    events.Add(userId, new List<Event>());

                bool added = false;
                for (int i = 0; i < events[userId].Count(); ++i)
                {
                    if (e < events[userId][i])
                    {
                        events[userId].Insert(i, e);
                        added = true;
                        break;
                    }
                }
                if (!added)
                    events[userId].Add(e);
                if (e.time == Time.Today && !e.done)
                    AddToQueue(e);
            }
        }
        public void Mark(long userId, int eventInd, bool state)
        {
            if (!Contains(userId))
                return;
            // deleting or adding event to the queue
            if (events[userId][eventInd].done && !state)
                AddToQueue(events[userId][eventInd]);
            else if (!events[userId][eventInd].done && state)
                RemoveFromQueue(events[userId][eventInd]);

            events[userId][eventInd].done = state;
        }
        public void Remove(long userId, int eventInd)
        {
            lock (locker)
            {
                if (events[userId][eventInd].time == Time.Today)
                {
                    // deleting from notification queue
                    RemoveFromQueue(events[userId][eventInd]);
                }
                events[userId].RemoveAt(eventInd);
            }
        }
        public List<Event> Get(long userId)
        {
            if (Contains(userId))
                return events[userId];
            return new List<Event>();
        }
        public void Delay(long userId, int eventInd, Time time)
        {
            if (!Contains(userId) || events[userId][eventInd].time == time)
                return;

            // we have to keep list sorted 
            var changingEvent = new Event(events[userId][eventInd])
            {
                time = time,
                notifyTime = new List<DateTime>()
            };

            Remove(userId, eventInd);

            // changing notification time
            if (time != Time.NoTerm)
            {
                double notStep;
                if (time == Time.Today)
                {
                    notStep = (new DateTime(changingEvent.initTime.Year, changingEvent.initTime.Month, changingEvent.initTime.Day).AddDays(1) - changingEvent.initTime).TotalSeconds / (int)changingEvent.importance;
                    for (int i = 0; i < (int)changingEvent.importance - 1; ++i)
                        changingEvent.notifyTime.Add(changingEvent.initTime.AddSeconds((i + 1) * notStep));
                }
                else
                    for (int i = 0; i < (int)changingEvent.importance - 1; ++i)
                        changingEvent.notifyTime.Add(new DateTime(changingEvent.initTime.Year, changingEvent.initTime.Month, changingEvent.initTime.Day, 0, 0, 0).AddDays(1).AddHours((i + 1) * (24 / (int)changingEvent.importance)));
            }
            else
                changingEvent.notifyTime = new List<DateTime>();
            Add(userId, changingEvent);
        }
        public void MidnightUpdate()
        {
            // Today 
            // if in the end of the day the deal was not done then it 
            // stays at "Today" (it means we push this deal to the next day)
            // if in the end of the day the deal was done then it will be deleted from actual list
            // Tomorrow
            // if it's done - delete else change to "Today"
            // No Term
            // doesn't change its state
            // Deleted events are being stored
            var updatedEvents = new Dictionary<long, List<Event>>();
            lock(locker)
            {
                foreach (var user in events)
                {
                    updatedEvents.Add(user.Key, new List<Event>());
                    if (!stats.ContainsKey(user.Key))
                        stats.Add(user.Key, new List<Event>());
                    foreach (var ev in user.Value)
                    {
                        if (ev.done)
                        {
                            //store done events
                            stats[user.Key].Add(ev);
                        }
                        else
                        {
                            if (ev.time == Time.Today)
                            {
                                //store delayed events
                                stats[user.Key].Add(ev);
                                ev.notifyTime.Clear();
                                ev.time = Time.Tomorrow;
                                var add = new Event(ev)
                                {
                                    time = Time.Today
                                };
                                updatedEvents[user.Key].Add(add);
                                AddToQueue(add);
                            }
                            else if (ev.time == Time.Tomorrow)
                            {
                                ev.time = Time.Today;
                                AddToQueue(ev);
                                updatedEvents[user.Key].Add(new Event(ev));
                            }
                        }
                    }
                }
                events = updatedEvents;
            }
            string st = JsonConvert.SerializeObject(stats, Formatting.Indented);
            File.WriteAllText(statPath, st);
        }
        public async void Notify(Object state)
        {
            var bot = (ITelegramBotClient)state;
            while (true)
            {
                if (todayNotif.Count() != 0)
                {
                    var nextNotif = todayNotif[0];
                    if (nextNotif.Item1 <= DateTime.Now)
                    {
                        await bot.SendTextMessageAsync(
                            chatId: nextNotif.Item2.owner,
                            text: $"{nextNotif.Item2.importance}: {nextNotif.Item2.name}"
                            ).ConfigureAwait(false);
                        todayNotif.RemoveAt(0);
                    }
                }

                Thread.Sleep(60000);
            }
        }
        public void Save()
        {
            string ev = JsonConvert.SerializeObject(events, Formatting.Indented);
            File.WriteAllText(dbPath, ev);
        }
        private void AddToQueue(Event e)
        {
            for (int i = e.notifyTime.Count() - 1; i >= 0; --i)
            {
                bool added = false;
                for (int j = 0; j < todayNotif.Count(); ++j)
                {
                    if (todayNotif[j].Item1 > e.notifyTime[i])
                    {
                        todayNotif.Insert(j, new Tuple<DateTime, Event>(e.notifyTime[i], e));
                        added = true;
                        break;
                    }
                }
                if (!added)
                    todayNotif.Add(new Tuple<DateTime, Event>(e.notifyTime[i], e));
            }
            todayNotif.Sort();
        }
        private void RemoveFromQueue(Event e)
        {
            var newtodayNotif = new List<Tuple<DateTime, Event>>();
            if (e.time == Time.Today)
            {
                foreach (var n in todayNotif)
                {
                    bool contains = false;

                    foreach (var elem in e.notifyTime)
                    {
                        if (n.Item1 == elem)
                        {
                            contains = true;
                            break;
                        }
                    }
                    if (!contains)
                        newtodayNotif.Add(n);
                }
                newtodayNotif.Sort();
                todayNotif = newtodayNotif;
            }
        }
    }
}
