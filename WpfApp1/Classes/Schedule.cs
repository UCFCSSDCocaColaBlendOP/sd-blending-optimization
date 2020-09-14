using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.Classes
{
    class Schedule
    {
        List<Juice> juices = new List<Juice>();
        List<List<Equipment>> SOs = new List<List<Equipment>>(); // index zero is the list of shared equipment
        int SOcount;
        int functionCount;
        DateTime scheduleID;

        // some identifing information for the schedule instance

        public Schedule()
        {
            scheduleID = DateTime.Now;
        }

        // called in the first stage of GNS to parse the CSV and initialize juices

        //I think this will become a string name in which the location of the file is in.
        //StreamReader CSV

        // called in the first stage of GNS after which juices is verified
        // reads through the csv and populates juices accordingly
        // find the right sections of the csv, read each line:
        //      convert the juice name to find the right type in the database
        // if CSV is null, return juices anyway as an empty list so user can
        //      manually add juice orders
        // if some value cannot be identified, set parsingFlag to true
        List<Juice> ProcessCSV(string fileName)
        {
            string whole_file = System.IO.File.ReadAllText(fileName);

            //split the file into lines
            whole_file = whole_file.Replace('\n', '\r');
            string[] lines = whole_file.Split(new char[] { '\r' },
                StringSplitOptions.RemoveEmptyEntries);

            int num_rows = lines.Length;
            int num_cols = lines[0].Split(',').Length;

            // Allocate the data array.
            string[,] values = new string[num_rows, num_cols];

            int row_start = 0;
            // Load the array.
            for (int r = 0; r < num_rows; r++)
            {
                string[] line_r = lines[r].Split(',');
                for (int c = 0; c < num_cols; c++)
                {
                    if (line_r[c].Contains("F_LINE"))
                    {
                        row_start = r;
                    }
                    values[r, c] = line_r[c];
                }
            }

            List<Juice> juices = new List<Juice>();

            for (int i = row_start; i < num_rows; i++)
            {
                if (values[i, num_cols - 1].Contains("F_LINE"))
                {

                    int quantity = Int32.Parse(values[i, 4]);

                    string line_name = values[i, 8];
                    int line = Int32.Parse(line_name.Substring(line_name.Length - 1, 1));

                    // if it's not line 1,2,3,or 7, we can continue to the next line
                    if (!(line == 1 || line == 2 || line == 3 || line == 7))
                    {
                        continue;
                    }

                    //might have to parse some of the end to it???
                    string name = values[i, 3];

                    string date = values[i, 0];
                    string seconds = values[i, 1];
                    DateTime fillTime = Convert.ToDateTime((date + " " + seconds)); //see if this works later

                    //bool parsingFlag = juiceExists(name) ? true : false;

                    //is this right?
                    bool starterFlag = Int32.Parse(values[i, 5]) != 0 ? true : false;

                    //??
                    //bool whereami = starterFlag ? true : false;

                    Juice new_juice = new Juice(quantity, line, name, fillTime,  starterFlag);
                    juices.Add(new_juice);
                }
            }

            return juices;
        }

        //go back to
        public bool juiceExists(string name)
        {
            //check through the list of juices to see if a name matches
            return false;
        }

        // called before the second stage of GNS, after ProcessCSV() to reconcile this schedule with the last
        Schedule ReconcileSchedules(Schedule old)
        {
            PullEquipment();

            foreach (Juice x in juices)
            {
                Juice temp;

                // find the juices from the SAP schedule that are in active production at the beginning of the schedule
                if (x.starterFlag)
                {
                    // first we need to try to find the corresponding juice in the old schedule
                    temp = old.FindJuice(x);
                    // if you can find it, set whereami to false and copy over the current action
                    if (temp != null)
                    {
                        x.whereami = false;

                        ScheduleEntry current = temp.FindState(scheduleID);


                    }
                }
            }
            // 2 look through the juices in the old schedule and find the ones in state 1 or 2 (in active production) at the current time
            //   copy over their immediate action to their schedule and the corresponding equipment schedule, don't copy future schedule,
            //   just the first entry, mark the whereami of the juice to false
            // 3 look through the equipment and copy over the current states of everybody else (if they weren't affected by the juices
            //   they might be cleaning or their waiting)
            // 4 add the initial state to all the remaining juices
            // 5 moves the old schedule to the archive

            // assume newly added equipment is clean (state = 0)

            return this;
        }

        // called by ReconcileSchedules to initialize the SO lists
        void PullEquipment()
        {
            // access the database
            // initialize SOcount and functionCount
            for (int i = 0; i < SOcount + 1; i++)
                SOs.Add(new List<Equipment>());

            // find the equipment list in the database
            // iterate through each piece of equipment
            Equipment temp;
            int count = 0;
            int first = 0; //TODO: I assigned this beause you hadn't assigned it. Please check what you want to assign it to
            for (; ; )
            {
                temp = new Equipment();
                // add the name and identifing number (type)
                // add the functionalities
                // add the SOs incrementing count for each SO temp is connected to
                // note the first SO temp is connected to

                if (count > 1)
                    SOs[0].Add(temp);
                else
                    SOs[first].Add(temp);
            }
        }

        // searches through the juice list to find a corresponding juice
        Juice FindJuice(Juice x)
        {
            Juice found = null;

            foreach (Juice i in juices)
            {
                if (i.quantity == x.quantity && i.line == x.line && i.type == x.type && i.fillTime.CompareTo(x.fillTime) == 0)
                {
                    found = i;
                    break;
                }
            }

            return found;
        }

        // called during the third stage to make changes created by the user, called per piece of equipment
        // case for clean equipment or equipment that's being cleaned
        void UpdateSchedule(Equipment x, int state, int eta)
        {
            // x is the piece of equipment whose state is being changed
            // eta is the time when it can change state next
            // state is if
            //              = 0, clean and waiting
            //              < 0, the cleaning type * -1
            // look at the current state of x, if x is clean or in cleaning make the change
            //      if x is currently marked as in production with a juice, go into that juice
            //      delete their schedule and change whereami to true
        }

        // called during the third stage to make changes created by the user, called per piece of equipment
        // case for equipment in active production
        void UpdateSchedule(Equipment x, Juice y, int eta)
        {
            // x is the piece of equipment whose state is being changed
            // eta is the time when it can change state next
            // y is the juice in active production on x
            // look at the current state of x, if x is clean or in cleaning make the change
            //      if x is currently marked as in production with a juice, go into that juice
            //      delete their schedule and change whereami to true
            // look at the current state of y, if y.starterFlag = false, set it to true. set
            //      y.whereami to false, update the initial state, if y.whereami is already set to
            //      false, then you need to go the first schedule entry and update the corresponding
            //      piece of equipment
        }

        // a function to save the schedule to the database
        // some functions to pull schedule information from the database
        // a funtion for editing old schedules in the database, called every 30 days

        // called at the end of the third stage to generate the schedule
        void GenerateSchedule()
        {
            // at this point there all of the juices and equipment should have a single entry in their schedules
            // juices should either have a piece of equipment or not started yet
            // equipment should either have a juice, a cleaning, or waiting

            // start by sorting the juices by fill time
            // work first on juices that are in active production, even if there are juices with earlier fill times
            // if a juice is in active production, you already know what SO it's in, and that it has access to necessary equipment
        }

        /*
        static List<Boolean> possibilities = new List<Boolean>();
        // for each SO, whether the SO has the necessary equipment access for a juice
        static List<Boolean> timeliness = new List<Boolean>();
        // if the juice can be ready for bottling on time in this line
        static List<DateTime> earliestStartTimes = new List<DateTime>();
        // for each SO, the earliest time a particular juice could start blending
        static List<DateTime> effect = new List<DateTime>();
        // for each SO, the earliest time the next juice after a particular juice could start
        // blending assuming 2 tanks and all the equiment unique to the SO

        static void GenerateSchedule()
        {
            int SOpick;

            for (int i = 0; i < juices.Count; i++)
            {
                SOpick = ShopForSO(juices[i]);

                if (SOpick == -1) // this is the case that the juice will be behind schedule
                {
                    // check that it's possible to make the juice, regardless of time
                    Boolean error = true;
                    for (int j = 0; j < SOCount; j++)
                        if (possibilities[j] == true)
                            error = false;
                    if (error)
                        ; // error because it's physically impossible to make the juice

                    SOpick = 0;

                    // i'm like 99% sure using < to check that a DateTime is earlier than another, is wrong, but that's what i'm trying to do here
                    // basically, find the earliest option and pick it
                    for (int j = 1; j < SOCount; j++)
                    {
                        if (earliestStartTimes[j] < earliestStartTimes[SOpick])
                            SOpick = j;
                        else if (earliestStartTimes[j] == earliestStartTimes[SOpick] && effect[j] < effect[SOpick])
                            SOpick = j;
                    }

                    // make sure that the juice is passed by reference
                    UpdateSchedule(juices[i], SOpick);
                }
                else
                {
                    // i'm like 99% sure using < to check that a DateTime is earlier than another, is wrong, but that's what i'm trying to do here
                    // basically, find the earliest valid option and pick it
                    for (int j = SOpick + 1; j < SOCount; j++)
                    {
                        if (possibilities[j] && timeliness[j])
                        {
                            if (earliestStartTimes[j] < earliestStartTimes[SOpick])
                                SOpick = j;
                            else if (earliestStartTimes[j] == earliestStartTimes[SOpick] && effect[j] < effect[SOpick])
                                SOpick = j;
                        }
                    }

                    // make sure that the juice is passed by reference
                    UpdateSchedule(juices[i], SOpick);
                }
            }
        }

        static void UpdateSchedule(Juice x, int SO)
        {
            // updates the juice's schedule and the equipment schedules, based on the SO choice
        }

        static int ShopForSO(Juice x)
        {
            // fills in possibilities, timeliness, earliestStartTimes, and effect for Juice x
            // if possibility is false because of equipment requirements, set both earliestStartTimes and effect
            //      to absurdly large numbers
            // returns the index of the first SO with possibility = true and timeliness = true, -1 if all are false

            return 0;
        }
        */
    }
}
