using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    public class CompareRecipe
    {
        // basic variables
        public DateTime startBlending;
        public bool conceivable;
        public bool onTime;
        public Equipment lateMaker;
        public bool slurry;
        public int batch;

        // info about thaw room
        public bool makeANewThawEntry;
        public DateTime thawTime;
        public TimeSpan thawLength;

        // info about extras
        public List<Equipment> extras;
        public List<DateTime> extraTimes;
        public List<TimeSpan> extraLengths;
        public List<TimeSpan> extraCleaningLengths;
        public List<DateTime> extraCleaningStarts;
        public List<int> extraCleaningTypes;
        public List<string> extraCleaningNames;

        // info about blend system
        public Equipment system;
        public DateTime systemTime;
        public TimeSpan systemLength;
        public DateTime systemCleaningStart;
        public TimeSpan systemCleaningLength;
        public int systemCleaningType;
        public string systemCleaningName;

        // info about mix tank
        public Equipment tank;
        public DateTime tankTime;
        public TimeSpan tankLength;
        public DateTime tankCleaningStart;
        public TimeSpan tankCleaningLength;
        public int tankCleaningType;
        public string tankCleaningName;

        // info about transfer line
        public Equipment transferLine;
        public DateTime transferTime;
        public TimeSpan transferLength;
        public DateTime transferCleaningStart;
        public TimeSpan transferCleaningLength;
        public int transferCleaningType;
        public string transferCleaningName;


        // info about aseptic tank/pasteurizer
        public Equipment aseptic;
        public DateTime asepticTime;
        public TimeSpan asepticLength;
        public DateTime asepticCleaningStart;
        public TimeSpan asepticCleaningLength;
        public int asepticCleaningType;
        public string asepticCleaningName;

        /// <summary>
        /// Creates a CompareRecipe object, conceivable and onTime are initially true
        /// </summary>
        public CompareRecipe()
        {
            conceivable = true;
            onTime = true;

            makeANewThawEntry = false;

            extras = new List<Equipment>();
            extraTimes = new List<DateTime>();
            extraLengths = new List<TimeSpan>();
            extraCleaningStarts = new List<DateTime>();
            extraCleaningLengths = new List<TimeSpan>();
            extraCleaningTypes = new List<int>();
            extraCleaningNames = new List<string>();

            system = null;
            systemCleaningType = -1;
            tankCleaningType = -1;
            transferCleaningType = -1;
            asepticCleaningType = -1;
            slurry = false;
        }

        /// <summary>
        /// Take all the data in the CompareRecipe object and use it to add schedule entries to all of the needed equipment
        /// </summary>
        /// <param name="thaw"></param>
        /// <param name="inline"></param>
        /// <param name="juice"></param>
        public void Actualize(Equipment thaw, bool inline, Juice juice)
        {
            // create entry for thaw room if needed
            if (makeANewThawEntry)
            {
                thaw.schedule.Add(new ScheduleEntry(thawTime, thawTime.Add(thawLength), juice, slurry, batch));
                ScheduleEntry.SortSchedule(thaw.schedule);
            }

            // create entries for extras
            for (int i = 0; i < extras.Count; i++)
            {
                // you need to add a cleaning entry
                if (extraCleaningTypes[i] != -1)
                    extras[i].schedule.Add(new ScheduleEntry(extraCleaningStarts[i], extraCleaningStarts[i].Add(extraCleaningLengths[i]), extraCleaningTypes[i], extraCleaningNames[i]));

                extras[i].schedule.Add(new ScheduleEntry(extraTimes[i], extraTimes[i].Add(extraLengths[i]), juice, slurry, batch));
            }

            // create entry for blend system
            if (system != null)
            {
                // cleaning entry
                if (systemCleaningType != -1)
                    system.schedule.Add(new ScheduleEntry(systemCleaningStart, systemCleaningStart.Add(systemCleaningLength), systemCleaningType, systemCleaningName));

                system.schedule.Add(new ScheduleEntry(systemTime, systemTime.Add(systemLength), juice, slurry, batch));
            }

            // create entry for mix tank
            if (tankCleaningType != -1)
                tank.schedule.Add(new ScheduleEntry(tankCleaningStart, tankCleaningStart.Add(tankCleaningLength), tankCleaningType, tankCleaningName));

            // if it's inline in needs to be open ended
            if (!inline)
                tank.schedule.Add(new ScheduleEntry(tankTime, tankTime.Add(tankLength), juice,  slurry, batch));
            else
                tank.schedule.Add(new ScheduleEntry(tankTime, juice));

            // create entry for transfer line
            if (transferCleaningType != -1)
                transferLine.schedule.Add(new ScheduleEntry(transferCleaningStart, transferCleaningStart.Add(transferCleaningLength), transferCleaningType, transferCleaningName));

            transferLine.schedule.Add(new ScheduleEntry(transferTime, transferTime.Add(transferLength), juice, slurry, batch));

            // create entry for aseptic
            if (asepticCleaningType != -1)
                aseptic.schedule.Add(new ScheduleEntry(asepticCleaningStart, asepticCleaningStart.Add(asepticCleaningLength), asepticCleaningType, asepticCleaningName));

            aseptic.schedule.Add(new ScheduleEntry(asepticTime, asepticTime.Add(asepticLength), juice, slurry, batch));
        }

    }
}
