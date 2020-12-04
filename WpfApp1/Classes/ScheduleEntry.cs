using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    public class ScheduleEntry
    {
        public bool slurry;
        public int batch;

        public DateTime start;
        public DateTime end;

        // for equipment schedules:
        public Juice juice;
        public bool cleaning;
        public int cleaningType;
        public string cleaningname;
        public bool userGen;

        // for juice schedule
        public Equipment tool;

        /// <summary>
        /// Creates a juice schedule entry for an equipment schedule
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="juice"></param>
        /// <param name="slurry"></param>
        /// <param name="batch"></param>
        public ScheduleEntry(DateTime start, DateTime end, Juice juice, bool slurry, int batch)
        {
            this.start = start;
            this.end = end;
            this.juice = juice;
            this.slurry = slurry;
            this.batch = batch;
        }

        /// <summary>
        /// for adding entries to the thaw room schedule
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="name"></param>
        /// <param name="juiceType"></param>
        public ScheduleEntry(DateTime start, DateTime end, string name, int juiceType)
        {
            userGen = true;
            this.start = start;
            this.end = end;
            this.juice = new Juice(name, juiceType);
            slurry = false;
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
            this.slurry = true;
            this.end = DateTime.MaxValue;
        }

        /// <summary>
        /// Creates a cleaning schedule entry for an equipment schedule
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="type"></param>
        /// <param name="name"></param>
        public ScheduleEntry(DateTime start, DateTime end, int type, string name)
        {
            this.start = start;
            this.end = end;
            this.cleaning = true;
            this.cleaningType = type;
            this.cleaningname = name;
            this.slurry = false;
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

        /// <summary>
        /// Given a schedule for a mix tank holding a slurry, marks the end of the last entry of the schedule
        /// </summary>
        /// <param name="schedule"></param>
        /// <param name="end"></param>
        public static void ReleaseMixTank(List<ScheduleEntry> schedule, DateTime end)
        {
            schedule[schedule.Count - 1].end = end;
        }

        // uses insertion sort to sort through the schedule based on the start time
        public static void SortSchedule(List<ScheduleEntry> schedule)
        {
            if (schedule.Count > 1)
            {
                ScheduleEntry temp_entry;
                for (int i = 1; i < schedule.Count; i++)
                {
                    for (int j = i; j > 0; j--)
                    {
                        if (schedule[j - 1].start > schedule[j].start)
                        {
                            temp_entry = schedule[j];
                            schedule[j] = schedule[j - 1];
                            schedule[j - 1] = temp_entry;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }
    }
}
