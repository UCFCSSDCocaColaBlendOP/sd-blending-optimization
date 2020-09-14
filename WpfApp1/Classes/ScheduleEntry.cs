using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    class ScheduleEntry
    {
        public DateTime start;
        public DateTime end;
        public TimeSpan totalTime;
        public int state;
        // for equipment, state =
        //                      0 - clean, waiting
        //                      1 - cleaning
        //                      2 - juice active
        //                      3 - out
        // for juice, state =
        //                      0 - not started yet
        //                      1 - in blend tank only
        //                      2 - using other equipment
        //                      3 - done

        // for equipment schedules:
        Juice juice;
        string cleaning;

        // for juice recipe
        int equipmentFunctionality;

        // for juice schedule
        Equipment tool;
        int recipeStage; // 0 for pre recipe, -1 for post recipe

        public ScheduleEntry(DateTime start, DateTime end, Juice juice)
        {
            this.start = start;
            this.end = end;
            this.totalTime = this.end.Subtract(this.start);

            this.state = 2;
            this.juice = juice;
        }

        public ScheduleEntry(TimeSpan length, int function)
        {
            this.totalTime = length;
            this.equipmentFunctionality = function;
        }

        public ScheduleEntry(DateTime start, DateTime end, string cleaning)
        {
            this.start = start;
            this.end = end;
            this.totalTime = this.end.Subtract(this.start);

            this.state = 1;
            this.cleaning = cleaning;
        }

        public ScheduleEntry(DateTime start, DateTime end, Equipment tool, int recipeStage)
        {
            this.start = start;
            this.end = end;
            this.totalTime = this.end.Subtract(this.start);

            // come back
        }
    }
}
