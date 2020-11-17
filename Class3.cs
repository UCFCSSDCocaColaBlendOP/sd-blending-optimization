using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    class ScheduleEntry
    {
        public bool late;

        public DateTime start;
        public DateTime end;

        // for equipment schedules:
        public Juice juice;
        public bool cleaning;
        public string cleaningdescription;

        // for juice schedule
        public Equipment tool;

        /// <summary>
        /// Creates a juice schedule entry for an equipment schedule
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="juice"></param>
        /// <param name="late"></param>
        public ScheduleEntry(DateTime start, DateTime end, Juice juice, bool late)
        {
            this.start = start;
            this.end = end;
            this.juice = juice;
            this.late = late;
        }

        /// <summary>
        /// Creates a schedule entry for a mix tank holding an inline slurry
        /// </summary>
        /// <param name="start"></param>
        /// <param name="juice"></param>
        public ScheduleEntry(DateTime start, Juice juice)
        {
            this.start = start;
            this.juice = juice;
        }

        /// <summary>
        /// Creates a cleaning schedule entry for an equipment schedule
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="cleaning"></param>
        public ScheduleEntry(DateTime start, DateTime end, string cleaning)
        {
            this.start = start;
            this.end = end;
            this.cleaningdescription = cleaning;
            this.cleaning = true;
        }

        /// <summary>
        /// Creates a schedule entry for a juice schedule
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="tool"></param>
        /// <param name="recipeStage"></param>
        public ScheduleEntry(DateTime start, DateTime end, Equipment tool)
        {
            this.start = start;
            this.end = end;
            this.tool = tool;
        }
    }
}
