using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    class Equipment
    {
        public String name;
        public int type; // for extras, type = functionality, for blend tanks, type = SO
        public List<bool> functionalities;
        public List<bool> SOs;
        public List<ScheduleEntry> schedule;
        public Equipment cipGroup;

        public int earlyLimit;
        public bool canBeLate;

        /// <summary>
        /// Creates a new piece of Equipment and initializes functionalities, Sos, and schedule
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="early"></param>
        /// <param name="late"></param>
        public Equipment(String name, int type, int early, bool late)
        {
            this.name = name;
            this.type = type;
            functionalities = new List<bool>();
            SOs = new List<bool>();
            schedule = new List<ScheduleEntry>();
            this.earlyLimit = early;
            this.canBeLate = late;
        }

        public DateTime FindTime(DateTime goal, TimeSpan length, int juicetype, DateTime scheduleID)
        {
            DateTime early = goal.Subtract(new TimeSpan(0, earlyLimit, 0));
            DateTime temp;

            // empty schedule
            if (schedule.Count == 0)
            {
                
            }

            // spot before schedule is late enough after early to do something
            if (DateTime.Compare(early.Add(length), schedule[0].start) <= 0)
            {
                // spot before schedule is late enough after goal to do something
                if (DateTime.Compare(goal.Add(length), schedule[0].start) <= 0)
                {
                    // thaw room or no cleaning required
                    if (type == 0 || zeroClean)
                        return goal;

                    // find out if you can clean on time
                    temp = cipGroup.CIP(scheduleID, goal, cleaning);

                    // you can clean on time before the schedule
                    if (DateTime.Compare(temp, new DateTime(0, 0, 0)) != 0)
                        return goal;
                    // you can't clean on time, but it's okay if you're late
                    else if (canBeLate)
                    {
                        // you found a time before the schedule!
                        temp = cipGroup.CIP(scheduleID, schedule[0].start.Subtract(length), cleaning);
                        if (DateTime.Compare(temp, new DateTime(0, 0, 0)) != 0)
                            return temp;
                        // otherwise try to find a gap
                    }
                    // try to find a gap
                }

                // at this point we're early
                if (type == 0 || zeroClean)
                    return schedule[0].start.Subtract(length);

                // if you hit this place, either you're repeating something you did earlier, which is okay
                //      OR goal came after schedule[0].start and you're trying to be early
                temp = cipGroup.CIP(scheduleID, schedule[0].start.Subtract(length), cleaning);
                if (DateTime.Compare(temp, new DateTime(0, 0, 0)) != 0)
                    return temp;
            }

            // try to find a gap between schedule entries
            DateTime choice = new DateTime(0, 0, 0);
            
            for (int i = 0; i < schedule.Count - 1; i++)
            {
                // this gap comes entirely before early
                if (DateTime.Compare(schedule[i + 1].start, early) <= 0)
                    continue;
                // this gap come entirely after goal
                if (DateTime.Compare(schedule[i].end, goal.Subtract(cleaning)) > 0)
                {
                    // you can't be late and there ain't no more ontime gaps
                    if (!canBeLate)
                        return choice;

                    // we're only gonna get later and later, so we don't care about choice anymore
                    // as soon as we find a big enough gap, we're returning it

                    // we found a big enough gap
                    if (TimeSpan.Compare(schedule[i+1].start.Subtract(schedule[i].end), length.Add(cleaning)) >= 0)
                    {
                        if (type == 0 || zeroClean)
                            return schedule[i+1].start
                        // see if we can clean in our gap
                        temp = cipGroup.CIP(schedule[i].end.Add(cleaning), schedule[i + 1].start.Subtract(length), cleaning);
                        if (DateTime.Compare(temp, new DateTime(0, 0, 0)) != 0)
                            return temp;
                    }

                    continue;
                }

                // check if our gap is big enough
                if (TimeSpan.Compare(schedule[i+1].start.Subtract(schedule[i].end), length.Add(cleaning)) >= 0)
                {

                }
            }
        }

        public DateTime CIP(DateTime start, DateTime end, TimeSpan length)
        {
            return new DateTime(0, 0, 0);
        }
    }
}
