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
        //public void Delay(Event e, Time t)
        //{
            
        //}
    }
}
