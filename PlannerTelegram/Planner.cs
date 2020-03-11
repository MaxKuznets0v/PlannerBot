using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlannerTelegram
{
    class Planner
    {
        private Dictionary<long, List<Event>> events;
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
            
            events[userId].Add(e);
        }
        //public void Delay(Event e, Time t)
        //{
            
        //}
    }
}
