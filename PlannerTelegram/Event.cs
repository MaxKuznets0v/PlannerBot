using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlannerTelegram
{
    enum Importance
    {
        Important,
        Medium,
        Casual
    }
    enum Time
    { 
        Today,
        Tomorrow,
        NoTerm
    }
    class Event
    {
        public string name;
        //public DateTime date;
        public Time time;
        bool done;
        public Importance importance;
        //public Event(string name, DateTime date, Importance imp = Importance.Casual)
        //{
        //    this.name = name;
        //    this.date = date;
        //    this.importance = imp;
        //}
        public Event(string name, Time time, Importance imp = Importance.Casual)
        {
            this.name = name;
            this.time = time;
            this.importance = imp;
            done = false;
        }
        public Event()
        {
            name = "";
            time = new Time();
            importance = new Importance();
            done = false;
        }
        public Event(Event e)
        {
            this.name = e.name;
            this.time = e.time;
            this.importance = e.importance;
            this.done = e.done;
        }
    }
}
