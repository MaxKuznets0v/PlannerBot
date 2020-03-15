using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace PlannerTelegram
{
    class Planner
    {
        private static Dictionary<long, List<Event>> events;
        //private static string dbpath = Directory.GetCurrentDirectory().ToString() + @"\Users\user_data.txt";  // path for storing user events
        //for debug
        private static string dbPath = Directory.GetParent(Directory.GetParent(Directory.GetCurrentDirectory().ToString()).ToString()) + @"\Users\user_data.txt";  // path for storing user events
        //static void Main(string[] args)
        //{
            
        //    //return JsonConvert.SerializeObject(events);
        //}
        public Planner()
        {
            events = new Dictionary<long, List<Event>>();

        }
        public bool Contains(long userId)
        {
            return events.ContainsKey(userId);
        }
        public void Add(long userId, Event e)
        {
            if (!Contains(userId))
                events.Add(userId, new List<Event>());

            for(int i = 0; i < events[userId].Count(); ++i)
            {
                if (e < events[userId][i])
                {
                    events[userId].Insert(i, e);
                    return;
                }
            }
            events[userId].Add(e);
        }
        public void Mark(long userId, int eventInd, bool state)
        {
            if (!Contains(userId))
                return;
            events[userId][eventInd].done = state;
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
            events[userId][eventInd].time = time;
            var changingEvent = new Event(events[userId][eventInd]);
            changingEvent.notifyTime = new List<DateTime>();
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
                        changingEvent.notifyTime.Add(new DateTime(changingEvent.initTime.Year, changingEvent.initTime.Month, changingEvent.initTime.Day, 0, 0, 0).AddDays(1).AddHours((i + 1) * 6));
            }
            else
                changingEvent.notifyTime = new List<DateTime>();
            events[userId].RemoveAt(eventInd);
            Add(userId, changingEvent);
        }
        public void Update()
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
            if (DateTime.Now.TimeOfDay != new DateTime().TimeOfDay)
                return;
            var updatedEvents = new Dictionary<long, List<Event>>();
            foreach (var user in events)
            {
                updatedEvents.Add(user.Key, new List<Event>());
                foreach (var ev in user.Value)
                {
                    if (ev.done)
                    {
                        //store done events

                    }
                    else
                    {
                        if (ev.time == Time.Today)
                        {
                            //store delayed events
                        }
                        else if (ev.time == Time.Tomorrow)
                            ev.time = Time.Today;
                        updatedEvents[user.Key].Add(ev);
                    }
                }
            }
            events = updatedEvents;
        }
        public void Notify()
        {
            foreach (var user in events)
                foreach (var ev in user.Value)
                {
                    if (ev.time == Time.Today)
                    {
                        
                    }   
                    else
                        break;
                }
                    

        }
    }
}
