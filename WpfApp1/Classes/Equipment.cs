using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    class Equipment
    {
        // TODO - fix initalizations

        public String name;
        public int type; // identifier for the tool
        public List<bool> functionalities = new List<bool>(); // ordered list, with a boolean value for each functionality
        public List<bool> SOs = new List<bool>(); // ordered list, with a boolean value for each SO
        public List<ScheduleEntry> schedule;  // this is a list of schedules

        public Equipment(String name, int type)
        {
            this.name = name;
            this.type = type;

            this.schedule = new List<ScheduleEntry>();
        }

        public Equipment(int type)
        {
            this.type = type;
        }

        //For given example of schedule
        public Equipment(String name)
        {
            this.name = name;

            this.schedule = new List<ScheduleEntry>();
        }
    }
}
