using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Caching;
using System.Windows;
using Microsoft.VisualBasic.FileIO;

namespace WpfApp1.Classes
{
    class Schedule
    {
        public List<Juice> juices;
        public List<Juice> juices_line8;
        public List<Juice> juices_line9; //thaw room
        public List<int> incorrect_batches_for_juice;
        List<Equipment> machines;
        int SOcount;
        int functionCount;
        DateTime scheduleID;

        // some identifing information for the schedule instance

        public Schedule()
        {
            scheduleID = DateTime.Now;

            machines = new List<Equipment>();
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

        //will set the list juices. I think this is the right way to do it because then we would have to somehow set the global variable
        //TODO: if list is empty then we should pop up an ERROR box
        public void ProcessCSV(string fileName)
        {
            List<String[]> lines = new List<string[]>();
            int row_start = 0;
            bool row_starter = false;
            int counter = 0;

            if(!fileName.Contains("csv"))
            {
                throw new SystemException("The selected file is not a csv.");
            }

            using(TextFieldParser parser = new TextFieldParser(fileName))
            {
                parser.TrimWhiteSpace = true;
                parser.Delimiters = new string[] { "," };
                parser.HasFieldsEnclosedInQuotes = true;
                while (!parser.EndOfData)
                {
                    string[] line = parser.ReadFields();
                    if(line[8].Contains("F_LINE") && !row_starter) 
                    { 
                        row_start = counter;
                        row_starter = true;
                    }
                    if(line[0] == "") {break;}
                    lines.Add(line);
                    counter++;
                }
            }

            Equipment thaw_room = new Equipment("Thaw Room", 0);
            machines.Add(thaw_room);


            int num_rows = lines.Count;

            juices = new List<Juice>();
            juices_line8 = new List<Juice>();

            //Get all the info for each "F_LINE" to make each juice needed
            for (int i = row_start; i < num_rows; i++)
            {
                if (lines[i][0] != "*" && lines[i][8].Contains("F_LINE"))
                {

                    string line_name = lines[i][8];
                    int line = Int32.Parse(line_name.Substring(line_name.Length - 1, 1));

                    // if it's not line 1,2,3,7, or 8, we can continue to the next line
                    if (!(line == 1 || line == 2 || line == 3 || line == 7 || line == 8))
                    {
                        continue;
                    }

                    string material = lines[i][2];

                    //Processing quantities to check if the juice is at it's ending stage
                    int quantity_juice = int.Parse(lines[i][4], NumberStyles.AllowThousands);
                    int quantity_juice_2 = int.Parse(lines[i][5], NumberStyles.AllowThousands);
                    bool no_batches = quantity_juice <= quantity_juice_2;
                    

                    string name = lines[i][3];

                    int type = name.Contains("CIP") ? -1: getJuiceType(name);
                    Console.WriteLine(name + " " + type);

                    string date = lines[i][0];
                    string seconds = lines[i][1];
                    string dateTime = date + " " + seconds;
                    DateTime fillTime = Convert.ToDateTime(dateTime);

                    bool starterFlag = quantity_juice_2 != 0;

                    Juice new_juice = new Juice(0, line, material, name, fillTime,  starterFlag, no_batches);

                    if(line == 8)
                    {
                        juices_line8.Add(new_juice);
                    } else
                    {
                        juices.Add(new_juice);
                    }
                    
                }
            }

            PrintAllJuices();
        }

        /*
        public void ProcessCSV2(string fileName)
        {
            List<String[]> lines = new List<string[]>();
            int row_start = 0;
            bool row_starter = false;
            int counter = 0;

            if (!fileName.Contains("csv"))
            {
                throw new SystemException("The selected file is not a csv.");
            }

            using (TextFieldParser parser = new TextFieldParser(fileName))
            {
                parser.TrimWhiteSpace = true;
                parser.Delimiters = new string[] { "," };
                parser.HasFieldsEnclosedInQuotes = true;
                while (!parser.EndOfData)
                {
                    string[] line = parser.ReadFields();
                    if (line[8].Contains("F_LINE") && !row_starter)
                    {
                        row_start = counter;
                        row_starter = true;
                    }
                    if (line[0] == "") { break; }
                    lines.Add(line);
                    counter++;
                }
            }

            Equipment thaw_room = new Equipment("Thaw Room", 0);
            machines.Add(thaw_room);


            int num_rows = lines.Count;

            juices = new List<Juice>();
            juices_line8 = new List<Juice>();

            int[] count_f_line_materials = new int[9];  //Record the number of different juices in each line

            for (int i = 0; i < count_f_line_materials.Length; i++)
            {
                count_f_line_materials[i] = 0;
            }

            string current_material = "";
            int current_line = 0;
            bool first_juice = true;

            //Get all the info for each "F_LINE" to make each juice needed
            for (int i = row_start; i < num_rows; i++)
            {
                if (lines[i][0] != "*" && lines[i][8].Contains("F_LINE"))
                {

                    string line_name = lines[i][8];
                    int line = Int32.Parse(line_name.Substring(line_name.Length - 1, 1));

                    // if it's not line 1,2,3,7, or 8, we can continue to the next line
                    if (!(line == 1 || line == 2 || line == 3 || line == 7 || line == 8))
                    {
                        continue;
                    }

                    string material = lines[i][2];

                    //Processing quantities to check if the juice is at it's ending stage
                    int quantity_juice = int.Parse(lines[i][4], NumberStyles.AllowThousands);
                    int quantity_juice_2 = int.Parse(lines[i][5], NumberStyles.AllowThousands);
                    bool no_batches = quantity_juice <= quantity_juice_2;

                    //Processing batch matching 
                    if (!no_batches && !material.Contains("CIP-FILL"))
                    {
                        if (material != current_material && current_line == line)
                        {
                            count_f_line_materials[line]++;
                        }
                        else if (first_juice)
                        {
                            current_line = line;
                            current_material = material;
                            count_f_line_materials[line]++;
                            first_juice = false;
                        }
                        else if (current_line != line)
                        {
                            current_material = material;
                            current_line = line;
                            count_f_line_materials[line]++;
                        }
                    }


                    string name = lines[i][3];

                    string date = lines[i][0];
                    string seconds = lines[i][1];
                    string dateTime = date + " " + seconds;
                    DateTime fillTime = Convert.ToDateTime(dateTime);

                    bool starterFlag = quantity_juice_2 != 0;

                    Juice new_juice = new Juice(0, line, material, name, fillTime, starterFlag, no_batches);

                    if (line == 8)
                    {
                        juices_line8.Add(new_juice);
                    }
                    else
                    {
                        juices.Add(new_juice);
                    }

                }
            }

            //only lines 1,2,3,7
            int[] count_materials_in_line = new int[9];    //Record the number of juices for each line
            for (int i = 0; i < count_materials_in_line.Length; i++)
            {
                count_materials_in_line[i] = 0;
            }
            int count_juice = 0;    //Count the position of the juice in the list of juices
            int count_batches = 0;  //Count how many batches appear on the sys line for each juice
            current_material = "";
            current_line = 0;
            DateTime current_batchTime = new DateTime(2000, 01, 01);
            first_juice = true;
            bool current_batching = false;

            for (int j = 0; j < row_start; j++)
            {
                Console.WriteLine("\n Batch: " + j);
                if (lines[j][8].Contains("B_SYS"))
                {
                    string line_name = lines[j][8];
                    int line = Int32.Parse(line_name.Substring(line_name.Length - 1, 1));
                    if (!(line == 1 || line == 2 || line == 3 || line == 7))
                    {
                        continue;
                    }
                    else
                    {
                        string material = lines[j][2];

                        if (lines[j][0].Contains("*"))
                        {
                            Console.WriteLine("This batch is a * one so end of juice");
                            while (juices[count_juice].material.Contains("CIP-FILL") || juices[count_juice].no_batches)
                            {
                                Console.WriteLine(juices[count_juice].name + " is the juice we are skipping and it's " + count_juice + " out of " + juices.Count + "total juices");
                                if (count_juice == juices.Count)
                                {
                                    break;
                                }
                                count_juice++;
                            }

                            //If the juice doesn't belong the schedule (no matching juices), then ignore it
                            Console.WriteLine("Added the quantity " + count_batches + " to " + current_material);
                            Console.WriteLine(juices[count_juice].name + " is the juice and it's " + count_juice + " out of " + juices.Count + "total juices");
                            juices[count_juice].quantity = count_batches;
                            juices[count_juice].currentBatch = current_batching;
                            count_materials_in_line[current_line]++;
                            current_batching = false;

                            if (count_juice < juices.Count)
                            {
                                count_juice++;
                            }

                            current_material = "";
                            current_line = 0;
                            count_batches = 0;
                            first_juice = true;
                            Console.WriteLine("Starting from fresh in a new line");
                        }
                        else
                        {
                            //Mark the curently batch being made in the juice
                            int quantity_batch_2 = int.Parse(lines[j][5], NumberStyles.AllowThousands);
                            if (quantity_batch_2 > 0)
                            {
                                current_batching = true;

                            }

                            string date = lines[j][0];
                            DateTime batchTime = Convert.ToDateTime(date);

                            if (material == current_material && current_line == line && current_batchTime == batchTime)
                            {
                                count_batches++;
                                Console.WriteLine("Adding a batch: " + material + " making it " + count_batches + " batches so far");
                            }
                            else if (first_juice)
                            {
                                Console.WriteLine("This is the first juice: " + material);
                                current_line = line;
                                current_material = material;
                                current_batchTime = batchTime;
                                count_batches++;
                                first_juice = false;
                            }
                            else
                            {
                                while (juices[count_juice].material.Contains("CIP-FILL") || juices[count_juice].no_batches)
                                {
                                    if (count_juice == juices.Count)
                                    {
                                        break;
                                    }
                                    count_juice++;
                                }

                                if (material == current_material && current_line == line)
                                {
                                    count_batches++;
                                    Console.WriteLine("Adding a batch: " + material + " making it " + count_batches + " batches so far");
                                }
                                else
                                {
                                    //If the juice doesn't belong the schedule (no matching juices), then ignore it
                                    if (current_batchTime < juices[count_juice].fillTime)
                                    {
                                        juices[count_juice].quantity = count_batches;
                                        juices[count_juice].currentBatch = current_batching;
                                        Console.WriteLine("Added the quantity " + count_batches + " to " + current_material);
                                        Console.WriteLine(juices[count_juice].name + " is the juice and it's " + count_juice + " out of " + juices.Count + "total juices");
                                        count_materials_in_line[current_line]++;
                                        current_batching = false;

                                        if (count_juice < juices.Count)
                                        {
                                            count_juice++;
                                        }
                                    }

                                    current_batchTime = batchTime;
                                    current_material = material;
                                    current_line = line;
                                    count_batches = 1;
                                    Console.WriteLine("Adding a batch: " + material + " making it " + count_batches + " batches so far");
                                }

                            }
                        }


                    }

                }
            }

            Console.WriteLine("\n");
            //Set up a flag to tell the user to double check the batches for each juice in that line
            incorrect_batches_for_juice = new List<int>();
            for (int i = 0; i < count_f_line_materials.Length; i++)
            {
                Console.WriteLine(count_materials_in_line[i] + " vs " + count_f_line_materials[i]);
                if (count_materials_in_line[i] != count_f_line_materials[i])
                {
                    incorrect_batches_for_juice.Add(i);
                    Console.WriteLine("incorrect lines: " + i);
                }
            }

            PrintAllJuices();

            Console.WriteLine("\n " + count_f_line_materials[7]);
        }
        */

        private int getJuiceType(String material_name)
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_JuiceId]";
                cmd.Parameters.Add("mat_name", SqlDbType.VarChar).Value = material_name;
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                return Convert.ToInt32(dt.Rows[0]["juice_id"]);
            }

            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
            }

            return -1;
        }

        private void PrintAllJuices()
        {
            Console.WriteLine("Juices in lne 1,2,3,7:");
            for(int i=0; i<juices.Count; i++)
            {
                Console.WriteLine("Name: " + juices[i].name);
            }

            Console.WriteLine("Juices in line 8:");
            for (int i = 0; i < juices_line8.Count; i++)
            {
                Console.WriteLine("Name: " + juices[i].name);
            }

        }

        //go back to
        public bool juiceExists(string name)
        {
            //check through the list of juices to see if a name matches
            return false;
        }

        /*
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
        } */

        /*
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
        */
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
