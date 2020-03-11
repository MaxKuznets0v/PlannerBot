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
    class Event
    {
        public string name;
        public DateTime date;
        private Importance importance;
        public Event(string name, DateTime date, Importance imp = Importance.Casual)
        {
            this.name = name;
            this.date = date;
            this.importance = imp;
        }
    }
}
