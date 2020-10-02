using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    class Equipment
    {
        String name;
        int type; // identifier for the tool
        List<bool> functionalities = new List<bool>(); // ordered list, with a boolean value for each functionality
        List<bool> SOs = new List<bool>(); // ordered list, with a boolean value for each SO
        List<List<ScheduleEntry>> schedule = new List<List<ScheduleEntry>>(); // this is a list of schedules, there should be one for each functionality

        
        public Equipment()
        {
            foreach (bool x in functionalities)
                schedule.Add(new List<ScheduleEntry>());
        }

        public Equipment(String name, int type)
        {
            this.name = name;
            this.type = type;
        }
    }
}
