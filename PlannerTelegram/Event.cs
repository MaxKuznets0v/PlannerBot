using System;
using System.Collections.Generic;
using System.Linq;

namespace PlannerTelegram
{
    enum Importance
    {
        Important = 4,
        Medium = 3,
        Casual = 2
    }
    enum Time
    { 
        Today,
        Tomorrow,
        NoTerm
    }
    class Event
    {
        public long owner;
        public DateTime initTime;
        public List<DateTime> notifyTime = new List<DateTime>(); // notification time
        public string name;
        public Time time;
        public bool done;
        public Importance importance;

        public Event(string name, Time time, Importance imp, long owner)
        {
            this.owner = owner;
            this.name = name;
            this.time = time;
            importance = imp;
            initTime = DateTime.Now.AddHours(3);
            done = false;
        }
        public Event()
        {
            name = "";
            owner = new long();
            time = new Time();
            importance = new Importance();
            initTime = DateTime.Now.AddHours(3);
            done = false;
        }
        public Event(Event e)
        {
            owner = e.owner;
            name = e.name;
            importance = e.importance;
            done = e.done;
            initTime = e.initTime;
            time = e.time;
            if (e.notifyTime.Count() != 0)
            {
                notifyTime = e.notifyTime;
                return;
            }
            if (time != Time.NoTerm)
            {
                double notStep;
                if (time == Time.Today)
                {
                    notStep = (new DateTime(initTime.Year, initTime.Month, initTime.Day).AddDays(1) - initTime).TotalSeconds / (int)importance;
                    for (int i = 0; i < (int)importance - 1; ++i)
                        notifyTime.Add(initTime.AddSeconds((i + 1) * notStep));
                }
                else
                    for (int i = 0; i < (int)importance - 1; ++i)
                        notifyTime.Add(new DateTime(initTime.Year, initTime.Month, initTime.Day, 0, 0, 0).AddDays(1).AddHours((i + 1) * (24 / (int)importance)));
            }
        }
        static public bool operator >(Event lhs, Event rhs)
        {
            return lhs.time > rhs.time;
        }
        static public bool operator <(Event lhs, Event rhs)
        {
            return lhs.time < rhs.time;
        }
    }
}
