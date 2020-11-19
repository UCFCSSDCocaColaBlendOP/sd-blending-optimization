using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    public class Equipment
    {
        public bool down;
        public String name;

        public int so;
        
        //1 = SO 1
        //2 = SO 2
        //3 = SO 3
        //4 = TL
        //5 = Aseptic

        public int cleaningProcess;
        public int e_type; 
        public int type; // for extras, type = functionality, for blend tanks, type = SO
        public List<bool> functionalities;
        public List<bool> SOs;
        public List<ScheduleEntry> schedule;
        public Equipment cipGroup;

        public TimeSpan earlyLimit;

        public bool startClean;
        public int lastJuiceType;
        public int lastCleaningType;
        public bool startDirty;
        public DateTime endMixing;

        // set whenever FindTime is called
        public bool needsCleaned;
        public TimeSpan cleanLength;
        public int cleanType;
        public DateTime cleanTime;
        public string cleanName;

        // set on the equipment page of generate schedule
        public int state; // 0 == nothing, 1 == down, 2 == start clean, 3 == start dirty, 4 == cleaning
        public int juiceType; // -1 for nothing or down, the juice type of the last juice otherwise
        public int cleaningType; // -1 for nothing or down or start dirty, the cleaning type otherwise
        public DateTime time; // DateTime.MinValue for nothing, down, start dirty or start clean, a value for cleaning
        public string cleaning; // name of cleaning

        /// <summary>
        /// Creates a new piece of Equipment and initializes functionalities, Sos, and schedule
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="early"></param>
        public Equipment(String name, int type, int early)
        {
            this.name = name;
            this.type = type;
            functionalities = new List<bool>();
            SOs = new List<bool>();
            schedule = new List<ScheduleEntry>();
            this.earlyLimit = new TimeSpan(0, early, 0);
        }

        /// <summary>
        /// applies the fields set during generate
        /// </summary>
        public void UpdateTool(DateTime scheduleID)
        {
            // is down
            if (state == 1)
            {
                down = true;
            }
            // starts dirty
            else if (state == 2)
            {
                startDirty = true;
                lastJuiceType = juiceType;
            }
            // starts clean
            else if (state == 3)
            {
                startClean = true;
                lastJuiceType = juiceType;
                lastCleaningType = cleaningType;
            }
            // is cleaning
            else if (state == 4)
            {
                lastCleaningType = cleaningType;
                schedule.Add(new ScheduleEntry(scheduleID, time, cleaningType, cleaning));
            }
        }

        /// <summary>
        /// Returns the earliest time you can start using a tool. If the earliest time is before goal, returns goal.
        /// Sets needsCleaned, cleanLength, cleanType, and cleanTime attributes each time
        /// </summary>
        /// <param name="goal"></param>
        /// <param name="juicetype"></param>
        /// <param name="scheduleID"></param>
        /// <returns></returns>
        public DateTime FindTime(DateTime goal, int juicetype, DateTime scheduleID)
        {
            TimeSpan cleaning;

            // a tool is starting clean
            if (schedule.Count == 0 && startClean || schedule.Count == 1 && schedule[0].cleaning)
            {
                cleaning = GetCleaning(lastJuiceType, juicetype);
                bool oldcleaningenough = CheckCleaning(lastCleaningType, cleanType);

                // the old cleaning was enough
                if (oldcleaningenough)
                {
                    needsCleaned = false;
                    return goal;
                }
                // you need to do more cleaning
                else
                {
                    // it's currently cleaning
                    if (schedule.Count == 1)
                        cleanTime = cipGroup.FindTimePopulated(schedule[0].end, cleaning);
                    else
                        cleanTime = cipGroup.FindTimePopulated(scheduleID, cleaning);

                    //  early or late
                    if (DateTime.Compare(cleanTime.Add(cleaning), goal) <= 0)
                        return goal;
                    else
                        return cleanTime.Add(cleaning);
                }
            }
            // a tool is starting dirty
            else if (schedule.Count == 0 && startDirty)
            {
                cleaning = GetCleaning(lastJuiceType, juicetype);

                if (needsCleaned)
                {
                    cleanTime = cipGroup.FindTimePopulated(scheduleID, cleaning);
                    if (DateTime.Compare(cleanTime.Add(cleaning), goal) <= 0)
                        return goal;
                    else
                        return cleanTime.Add(cleaning);
                }
                else
                    return goal;

            }
            // a tool is starting and we don't care about cleanliness
            else if (schedule.Count == 0)
            {
                return goal;
            }

            // otherwise get cleaning between the last schedule entry (which must be a juice and the new juice
            cleaning = GetCleaning(schedule[schedule.Count - 1].juice.type, juicetype);
            bool goalLaterThanEnd = DateTime.Compare(goal, schedule[schedule.Count - 1].end) > 0;
            
            // you don't need to do a cleaning so you can either return goal or the end of the schedule
            if (!needsCleaned)
            {
                if (goalLaterThanEnd)
                    return goal;
                else
                    return schedule[schedule.Count - 1].end;
            }

            cleanTime = cipGroup.FindTimePopulated(schedule[schedule.Count - 1].end, cleaning);
            if (DateTime.Compare(cleanTime.Add(cleaning), goal) <= 0)
                return goal;
            else
                return cleanTime.Add(cleaning);
        }


        public TimeSpan GetCleaning(int juicetype1, int juicetype2)
        {
            // pull from database

            // set cleanLength and cleanType
            cleanLength = TimeSpan.Zero;
            cleanName = "";
            cleanType = -1;
            needsCleaned = false;

            return cleanLength;
        }

        public bool CheckCleaning(int last, int needed)
        {
            // checks whether last satisfies needed
            return true;
        }

        /// <summary>
        /// Finds the entry in the schedule for the desired batch, null otherwise. If batched, slurry = 1.
        /// You want the last batch of the slurry, to know when it's ready.
        /// </summary>
        /// <param name="juice"></param>
        /// <param name="slurry"></param>
        /// <returns></returns>
        public ScheduleEntry FindEntry(Juice juice, int slurry)
        {
            // first you need to check if the juice already has allocated time on the schedule

            // if slurry > 1, we need the entry for the last batch of the slurry

            for (int i = 0; i < schedule.Count; i++)
            {
                // find this juice
                if (schedule[i].juice == juice)
                {
                    // find the batch you need
                    int batch = juice.totalBatches - juice.neededBatches + slurry - 1;
                    i += batch;
                    return schedule[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Finds time in a populated schedule, assumes cleaning is not a concern. Returns the earliest possible time to start. Will always return a time, even a late one.
        /// </summary>
        /// <param name="early"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public DateTime FindTimePopulated(DateTime early, TimeSpan length)
        {
            // empty schedule
            if (schedule.Count == 0)
                return early;

            // preschedule
            if (DateTime.Compare(schedule[0].start.Subtract(length), early) >= 0)
                return early;

            // find gap in schedule
            for (int i = 0; i < schedule.Count - 1; i++)
            {
                // too early
                if (DateTime.Compare(schedule[i + 1].start, early) <= 0)
                    continue;
                if (TimeSpan.Compare(schedule[i + 1].start.Subtract(schedule[i].end), length) >= 0)
                    return schedule[i + 1].start;
            }

            // postschedule
            return schedule[schedule.Count - 1].end;
        }

        /// <summary>
        /// takes a recipe and returns the number of functionalities this tool supports,
        /// which the recipe doesn't need
        /// </summary>
        /// <param name="recipe"></param>
        /// <returns></returns>
        public int GetOtherFuncs(List<int> recipe)
        {
            int cnt = 0;

            for (int i = 0; i < recipe.Count; i++)
                if (functionalities[i] && recipe[i] <= 0)
                    cnt++;

            return cnt;
        }

        /// <summary>
        /// takes a list of SOs still available returns the number of those SOs that are also
        /// available to this tool
        /// </summary>
        /// <param name="sosavail"></param>
        /// <returns></returns>
        public int GetSOs(bool[] sosavail)
        {
            int cnt = 0;

            for (int i = 0; i < sosavail.Length; i++)
                if (SOs[i] && sosavail[i])
                    cnt++;

            return cnt;
        }
    }
}
