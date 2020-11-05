using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Web.Caching;
using System.Windows;
using System.Xml.Serialization;
using Microsoft.VisualBasic.FileIO;

namespace WpfApp1.Classes
{
    class Schedule
    {
        public List<Equipment> extras;
        public List<Equipment> blendSystems;
        public Equipment thawRoom;
        public List<Equipment> blendtanks;
        public List<Equipment> transferLines;

        // TODO - make sure all global variables are intialized when they have to be.. make it's supposed to be in a function and not the constructor
        // TODO - (time permitting) make variables that are needed private
        // TODO :: function to extrapolate juice schedule from equipment schedules
        public List<Equipment> machines;
        public int numFunctions; // TODO: need to fill this in as soon as possible to use it in the rest of the code (1)
        public int numSOs;

        public List<Juice> finished;
        public List<Juice> inprogress;// this is "juices" i went through and changed all references to "juices" even in commented out sections
        public List<Juice> juices_line8;
        public DateTime scheduleID;


        public Schedule(string filename)
        {
            this.scheduleID = DateTime.Now;

            this.machines = new List<Equipment>();
            this.blendtanks = new List<Equipment>();
            this.transferLines = new List<Equipment>();
            this.numFunctions = 10; // TODO: need to change this (1)
            this.finished = new List<Juice>();
            this.inprogress = new List<Juice>();
            this.juices_line8 = new List<Juice>();

            ProcessCSV(filename);
            //this.juices_line9 = new List<Juice>();
        }

        //TODO: if string is empty then we should pop up an ERROR box
        public void ProcessCSV(string fileName)
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

            inprogress = new List<Juice>();
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

                    int type = name.Contains("CIP") ? -1 : getJuiceType(name);
                    Console.WriteLine(name + " " + type);

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
                        inprogress.Add(new_juice);
                    }

                }
            }

            PullEquipment();
            PrintAllJuices();
        }

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
            for (int i = 0; i < inprogress.Count; i++)
            {
                Console.WriteLine("Name: " + inprogress[i].name);
            }

            Console.WriteLine("Juices in line 8:");
            for (int i = 0; i < juices_line8.Count; i++)
            {
                Console.WriteLine("Name: " + inprogress[i].name);
            }

        }

        // TODO - add pull equipment function
        // extras are pieces of equipment with a single functionality, their type is their functionality
        // blendtanks are blendtanks their type is their SO
        private void PullEquipment()
        {
            // access the database
            // initialize SOcount and functionCount
            //methods used to get the maximum sos and functionalities

            numSOs = getNumSOs();
            numFunctions = getNumFunctions();

            // find the equipment list in the database
            // iterate through each piece of equipment

            try
            {
                int equip_type;
                String equip_name;
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();
                SqlCommand cmd = new SqlCommand();

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.CommandText = "[select_Equip_id]";
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                da.Fill(dt);
                SqlDataReader rd = cmd.ExecuteReader();

                foreach (DataRow dr in dt.Rows)
                {
                    equip_type = Convert.ToInt32(dr["id"]);
                    equip_name = dr.Field<String>("Equipment");
                    Equipment temp = new Equipment(equip_name, equip_type);

                    //set all the number of functions in the list
                    //set all to false
                    //add 1 to numFunctions and num SOs because ids start with 1 instead of 0
                    for (int i = 0; i < numFunctions + 1; i++)
                    {
                        temp.functionalities.Add(false);
                    }

                    for (int j = 0; j < numSOs + 1; j++)
                    {
                        temp.SOs.Add(false);
                    }
                    machines.Add(temp);
                }
                conn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            // sees what functionality and sos each equipment has
            // sets the index the correlates to the functionality and so id true if 
            // equipment has that functionality and so
            getEquipFuncSos();
        }

        //gets the maximum number of functions
        private int getNumFunctions()
        {
            int numofFunctions = 0;
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();
                SqlCommand cmd = new SqlCommand();

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.CommandText = "[select_FuncMaxId]";

                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                da.Fill(dt);
                SqlDataReader rd = cmd.ExecuteReader();

                numofFunctions = Convert.ToInt32(dt.Rows[0]["id"]);
                conn.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


            return numofFunctions;
        }

        // get maximum number of sos
        private int getNumSOs()
        {
            int s = 0;
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();
                SqlCommand cmd = new SqlCommand();

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.CommandText = "[select_SOsMaxId]";

                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                da.Fill(dt);
                SqlDataReader rd = cmd.ExecuteReader();

                s = Convert.ToInt32(dt.Rows[0]["id"]);
                conn.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


            return s;
        }

        private void getEquipFuncSos()
        {
            try
            {
                int id_equip;
                int id_func;
                int id_so;
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();
                SqlCommand cmd = new SqlCommand();

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.CommandText = "[select_FuncSO]";
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                da.Fill(dt);
                SqlDataReader rd = cmd.ExecuteReader();

                foreach (DataRow dr in dt.Rows)
                {
                    id_equip = Convert.ToInt32(dr["id_Equip"]);
                    id_func = Convert.ToInt32(dr["id_Func"]);
                    id_so = Convert.ToInt32(dr["id_SO"]);
                    for (int i = 0; i < machines.Count; i++)
                    {
                        if (machines[i].type == id_equip)
                        {
                            machines[i].functionalities[id_func] = true;
                            machines[i].SOs[id_so] = true;
                        }
                    }
                }
                /*
                for (int i = 0; i < machines.Count; i++)
                {
                    Console.WriteLine(machines[i].type);
                    for (int j = 0; j < machines[i].functionalities.Count; j++)
                    {
                        Console.WriteLine(j);
                        Console.WriteLine(machines[i].functionalities[j]);
                    }
                    Console.WriteLine();
                }
                for (int i = 0; i < machines.Count; i++)
                {
                    Console.WriteLine(machines[i].type);
                    for (int j = 0; j < machines[i].SOs.Count; j++)
                    {
                        Console.WriteLine(j);
                        Console.WriteLine(machines[i].SOs[j]);
                    }

                    Console.WriteLine();
                }
                */

                conn.Close();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public DateTime FindTimeInTheThawRoom(Juice x)
        {
            // try to find space in the thaw room schedule near the ideal time
            return new DateTime(0, 0, 0);
        }

        public DateTime FindEntryInThawRoom(Juice x)
        {
            // first you need to check if the juice already has allocated time on the schedule for the thaw room
            //      if yes start time is when this batch is done with the thaw room
            return new DateTime(0, 0, 0);
        }

        public CompareRecipe PrepRecipe(Juice x, int y)
        {
            CompareRecipe option = new CompareRecipe();
            bool pickedStartTime = false; // has option.startBlending been set
            bool[] checkoffFunc = new bool[numFunctions];
            bool[] soChoices = new bool[numSOs];
            for (int j = 0; j < numSOs; j++)
                soChoices[j] = true;

            // if the thaw room is needed
            if (x.recipes[y][0] > 0)
            {


                //              option.makeANewThawEntry = true;
                // set option.thawTime
                // if there isn't availablity in the thawroom set conceivable to false and return
                option.thawRoom = true;
                pickedStartTime = true;
                checkoffFunc[0] = true;
            }

            // if any of the extras are needed
            for (int j = 0; j < extras.Count; j++)
            {
                if (x.recipes[y][extras[j].type] < 0)
                    continue;

                // schedule time near the ideal time
                // account for possibility of copies of an extra
                if (!pickedStartTime)
                {
                    pickedStartTime = true;
                }

                // put in a check for SOs because extra equipment can limit them
                // add an entry to option.neededExtras and option.extraTimes

                // if you need an extra and there isn't one available set conceivable to false and return

                checkoffFunc[extras[j].type] = true;
            }

            // do you need a blend system
            bool needBlendSys = false;
            for (int i = 0; i < numFunctions; i++)
                if (x.recipes[y][i] > 0 && !checkoffFunc[i])
                    needBlendSys = true;

            if (needBlendSys)
            {
                int choice = -1;
                DateTime currentStart = new DateTime(0, 0, 0);
                int sos = 0;
                int otherfuncs = 0;
                TimeSpan length;

                for (int j = 0; j < blendSystems.Count; j++)
                {
                    // first check if it can connect to the sos
                    bool flag = false;
                    for (int k = 0; k < numSOs; k++)
                        if (soChoices[k] && blendSystems[j].SOs[k])
                            flag = true;
                    if (!flag)
                        continue;

                    // then check if it has the functionalities the recipe needs
                    for (int k = 1; k < numFunctions; k++)
                        if (!checkoffFunc[k] && x.recipes[j][k] > 0 && !blendSystems[j].functionalities[k])
                            continue;

                    TimeSpan templength = new TimeSpan(0, 0, 0);
                    for (int k = 0; k < numFunctions; k++)
                        if (!checkoffFunc[k] && x.recipes[y][k] > 0)
                            templength.Add(new TimeSpan(0, x.recipes[y][k], 0));

                    // then start comparing this blendsystem to the last one to make a choice
                    DateTime tempstart = GetStart(blendSystems[j], templength, x.idealTime[y]);
                    int tempsos = GetSOs(blendSystems[j], soChoices);
                    int tempotherfuncs = GetOtherFuncs(blendSystems[j], x.recipes[y]);

                    // there is no current
                    if (choice == -1)
                    {
                        choice = j;
                        currentStart = tempstart;
                        sos = tempsos;
                        otherfuncs = tempotherfuncs;
                        length = templength;
                    }
                    // temp and current are the same time
                    else if (DateTime.Compare(tempstart, currentStart) == 0)
                    {
                        if (otherfuncs > tempotherfuncs)
                        {
                            choice = j;
                            currentStart = tempstart;
                            sos = tempsos;
                            otherfuncs = tempotherfuncs;
                            length = templength;
                        }
                        else if (otherfuncs < tempotherfuncs)
                        {
                            continue;
                        }
                        else if (tempsos > sos)
                        {
                            choice = j;
                            currentStart = tempstart;
                            sos = tempsos;
                            otherfuncs = tempotherfuncs;
                            length = templength;
                        }
                    }
                    // temp is at the ideal time, current is not, if current was also at the ideal time, it would have been caught in the last check
                    else if (DateTime.Compare(tempstart, x.idealTime[y]) == 0)
                    {
                        choice = j;
                        currentStart = tempstart;
                        sos = tempsos;
                        otherfuncs = tempotherfuncs;
                        length = templength;
                    }
                    // current is later than ideal
                    else if (DateTime.Compare(x.idealTime[y], currentStart) < 0)
                    {
                        // temp is later than current
                        if (DateTime.Compare(tempstart, currentStart) > 0)
                            continue;
                        else
                        {
                            choice = j;
                            currentStart = tempstart;
                            sos = tempsos;
                            otherfuncs = tempotherfuncs;
                            length = templength;
                        }
                    }
                    // current is earlier than ideal
                    else
                    {
                        // temp is later than ideal
                        if (DateTime.Compare(tempstart, x.idealTime[y]) > 0)
                            continue;
                        // current is within an hour of ideal
                        else if (TimeSpan.Compare(currentStart.Subtract(x.idealTime[y]), new TimeSpan(1, 0, 0)) <= 0)
                        {
                            // temp is also within an hour of ideal
                            if (TimeSpan.Compare(tempstart.Subtract(x.idealTime[y]), new TimeSpan(1, 0, 0)) <= 0)
                            {
                                if (otherfuncs > tempotherfuncs)
                                {
                                    choice = j;
                                    currentStart = tempstart;
                                    sos = tempsos;
                                    otherfuncs = tempotherfuncs;
                                    length = templength;
                                }
                                else if (otherfuncs < tempotherfuncs)
                                {
                                    continue;
                                }
                                else if (tempsos > sos)
                                {
                                    choice = j;
                                    currentStart = tempstart;
                                    sos = tempsos;
                                    otherfuncs = tempotherfuncs;
                                    length = templength;
                                }
                            }
                            // temp is more than an hour earlier than ideal
                            else
                            {
                                continue;
                            }
                        }
                        // current is more than an hour earlier than ideal
                        else
                        {
                            // temp is later than current but earlier than ideal
                            if (DateTime.Compare(tempstart, currentStart) > 0)
                            {
                                choice = j;
                                currentStart = tempstart;
                                sos = tempsos;
                                otherfuncs = tempotherfuncs;
                                length = templength;
                            }
                            // temp is earlier than current
                            else
                            {
                                continue;
                            }
                        }
                    }

                }

                if (choice == -1)
                {
                    option.conceivable = false;
                    return option;
                }

                option.blendSystem = blendSystems[choice];
                option.blendTime = currentStart;
                // calculate blendLength
                option.blendLength = new TimeSpan(0, 0, 0);
                for (int k = 0; k < numFunctions; k++)
                    if (!checkoffFunc[k] && x.recipes[y][k] > 0)
                        option.blendLength.Add(new TimeSpan(0, x.recipes[y][k], 0));

                for (int k = 0; k < numSOs; k++)
                    if (soChoices[k] && !option.blendSystem.SOs[k])
                        soChoices[k] = false;
            }

            // assign a mix tank
            Equipment pick = null;
            int pickIdx = -1;
            DateTime start = new DateTime(0, 0, 0);
            TimeSpan totalMixTime;

            for (int i = 0; i < blendtanks.Count; i++)
            {
                if (!soChoices[blendtanks[i].type])
                    continue;

                DateTime tempStart;
            }

            // assign a transfer line


            // decide if it's onTime

            return option;
        }

        public CompareRecipe PrepRecipe(Juice x, int y, int slurrySize)
        {
            return new CompareRecipe();
        }

        public void GenerateNewSchedule()
        {
            SortByFillTime();

            while (inprogress.Count != 0)
            {
                if (inprogress[0].mixing)
                {
                    // you only have to acquire a transfer line
                    AcquireTransferLine(inprogress[0], inprogress[0].readytotrans, inprogress[0].BlendTank);

                    // update the batch counts
                    inprogress[0].neededBatches--;
                    if (inprogress[0].inline)
                        inprogress[0].slurryBatches--;

                    // either move juice to finished list or recalculate fill time
                    if (inprogress[0].neededBatches == 0)
                    {
                        finished.Add(inprogress[0]);
                        inprogress.RemoveAt(0);
                    }
                    else
                    {
                        inprogress[0].RecalculateFillTime();
                        SortByFillTime();
                    }
                }
                else
                {
                    if (inprogress[0].inline)
                    {
                        // you only need to acquire a transfer line
                        AcquireTransferLine(inprogress[0], inprogress[0].currentFillTime.Subtract(new TimeSpan(0, inprogress[0].transferTime, 0)), inprogress[0].BlendTank);

                        // update the batch counts
                        inprogress[0].neededBatches--;
                        inprogress[0].slurryBatches--;

                        // move to finished list or continue
                        if (inprogress[0].neededBatches == 0)
                        {
                            finished.Add(inprogress[0]);
                            inprogress.RemoveAt(0);

                            // mark the mix tank ended
                            ReleaseMixTank(inprogress[0].BlendTank, inprogress[0].currentFillTime.Subtract(new TimeSpan(0, inprogress[0].transferTime, 0)));
                        }
                        else
                        {
                            if (inprogress[0].slurryBatches == 0)
                            {
                                inprogress[0].inline = false;

                                // mark the mix tank ended
                                ReleaseMixTank(inprogress[0].BlendTank, inprogress[0].currentFillTime.Subtract(new TimeSpan(0, inprogress[0].transferTime, 0)));
                            }

                            inprogress[0].RecalculateFillTime();
                            SortByFillTime();
                        }
                    }
                    else
                    {
                        // it wouldn't make sense to do inline for a single batch
                        if (inprogress[0].neededBatches != 1 && inprogress[0].inlineposs)
                        {
                            // decide if you can do inline: can you finish the slurry for 2,3,4,or5 batches before the fill time?

                            CompareRecipe pick = null;
                            int pickIdx = -1;
                            int size = -1;
                            DateTime goTime = new DateTime(0, 0, 0);

                            // try all the slurry sizes
                            for (int i = 2; i < 5; i++)
                            {
                                bool canDo = false;

                                // try all the inline recipes
                                for (int j = 0; j < inprogress[0].recipes.Count; j++)
                                {
                                    if (!inprogress[0].inlineflags[j])
                                        continue;

                                    CompareRecipe test = PrepRecipe(inprogress[0], j, i);
                                    if (!test.conceivable || !test.onTime)
                                        continue;

                                    canDo = true;

                                    if (pick == null || size < i || DateTime.Compare(goTime, test.startBlending) < 0)
                                    {
                                        pick = test;
                                        pickIdx = j;
                                        size = i;
                                        goTime = test.startBlending;
                                    }
                                }

                                // if three isn't possible 4 definitely won't be
                                if (!canDo)
                                    break;
                            }

                            // inline was possible and a choice was made
                            if (pick != null)
                            {
                                // assign equipment
                                int bnum = inprogress[0].totalBatches - inprogress[0].neededBatches + 1;

                                if (pick.makeANewThawEntry)
                                    EnterScheduleLine(thawRoom, pick.thawTime, inprogress[0], bnum, new TimeSpan(0, inprogress[0].recipes[pickIdx][0] * size, 0));

                                for (int i = 0; i < pick.neededExtras.Count; i++)
                                {
                                    TimeSpan ts = new TimeSpan(0, inprogress[0].recipes[pickIdx][pick.neededExtras[i].type] * size, 0);
                                    EnterScheduleLine(pick.neededExtras[i], pick.extraTimes[i], inprogress[0], bnum, ts);
                                }

                                if (pick.blendSystem != null)
                                    EnterScheduleLine(pick.blendSystem, pick.blendTime, inprogress[0], bnum, pick.blendLength);

                                ClaimMixTank(pick.mixTank, pick.mixTime, inprogress[0], bnum, size);
                                EnterScheduleLine(pick.transferLine, pick.transferTime, inprogress[0], bnum, pick.transferLength);


                                // set up for the next batch
                                inprogress[0].inline = true;
                                inprogress[0].BlendTank = pick.mixTank;
                                inprogress[0].slurryBatches = size - 1;
                                inprogress[0].neededBatches--;
                                inprogress[0].RecalculateFillTime();
                                SortByFillTime();

                                continue;
                            }

                            // otherwise continue on
                        }

                        // pick a batched recipe
                        CompareRecipe choice = null;
                        int choiceIdx = -1;
                        bool onTime = false;
                        DateTime start = new DateTime(0, 0, 0);

                        CompareRecipe temp;

                        for (int i = 0; i < inprogress[0].recipes.Count; i++)
                        {
                            if (inprogress[0].inlineflags[i])
                                continue;

                            temp = PrepRecipe(inprogress[0], i);

                            if (!temp.conceivable)
                                continue;

                            if (choice == null || (!onTime && temp.onTime))
                            {
                                choice = temp;
                                choiceIdx = i;
                                onTime = temp.onTime;
                                start = temp.startBlending;
                            }
                            else if (onTime && !temp.onTime)
                            {
                                continue;
                            }
                            else if (!onTime)
                            {
                                if (DateTime.Compare(start, temp.startBlending) < 0)
                                    continue;
                                else
                                {
                                    choice = temp;
                                    choiceIdx = i;
                                    onTime = temp.onTime;
                                    start = temp.startBlending;
                                }
                            }
                            else
                            {
                                if (DateTime.Compare(start, temp.startBlending) > 0)
                                    continue;
                                else
                                {
                                    choice = temp;
                                    choiceIdx = i;
                                    onTime = temp.onTime;
                                    start = temp.startBlending;
                                }
                            }
                        }

                        if (choice == null)
                        {
                            // error no recipe works, not even late
                        }
                        else if (!onTime)
                        {
                            // warning all recipes are late
                        }

                        // assign equipment
                        // all of the choices have been made and the times are in choice
                        int batch = inprogress[0].totalBatches - inprogress[0].neededBatches + 1;

                        if (choice.makeANewThawEntry)
                            EnterScheduleLine(thawRoom, choice.thawTime, inprogress[0], batch, new TimeSpan(0, inprogress[0].recipes[choiceIdx][0], 0));

                        for (int i = 0; i < choice.neededExtras.Count; i++)
                        {
                            TimeSpan ts = new TimeSpan(0, inprogress[0].recipes[choiceIdx][choice.neededExtras[i].type], 0);
                            EnterScheduleLine(choice.neededExtras[i], choice.extraTimes[i], inprogress[0], batch, ts);
                        }

                        if (choice.blendSystem != null)
                            EnterScheduleLine(choice.blendSystem, choice.blendTime, inprogress[0], batch, choice.blendLength);

                        EnterScheduleLine(choice.mixTank, choice.mixTime, inprogress[0], batch, choice.mixLength);
                        EnterScheduleLine(choice.transferLine, choice.transferTime, inprogress[0], batch, choice.transferLength);

                        // move to finished list if possible
                        inprogress[0].neededBatches--;

                        if (inprogress[0].neededBatches == 0)
                        {
                            finished.Add(inprogress[0]);
                            inprogress.RemoveAt(0);
                        }
                        else
                        {
                            inprogress[0].RecalculateFillTime();
                            SortByFillTime();
                        }
                    }
                }
            }
        }

        public void AcquireTransferLine(Juice x, DateTime y, Equipment tank)
        {
            // go through list of transfer lines and pick the one that's best for x at time y
            // then assign the juice to it
            // also mark the blend tank to say when the juice will be done
            //      except when inline is true and there is more than 1 batch left

            // we need to know how long a juice will have a transfer line for
        }

        // TODO - fill in function
        public void SortByFillTime()
        {
            // sorts inprogress by current filltime
            // use insertion sort because most calls will be on an already sorted list
            if (inprogress.Count > 1)
            {
                Juice tempjuice;
                for (int i = 1; i < inprogress.Count; i++)
                {
                    for (int j = i; j > 0; j--)
                    {
                        if (inprogress[j - 1].OGFillTime > inprogress[j].OGFillTime)
                        {
                            tempjuice = inprogress[j];
                            inprogress[j] = inprogress[j - 1];
                            inprogress[j - 1] = tempjuice;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
        }

        public List<List<Equipment>> SortByOptions(List<int> x)
        {
            // takes the recipe and builds a new recipe sorted by the availability of the equipment
            List<List<Equipment>> options = new List<List<Equipment>>();
            for (int i = 0; i < numFunctions; i++)
            {
                options.Add(new List<Equipment>());
                options[i].Add(new Equipment(-1 * i));

                if (x[i] < 0)
                    continue;

                for (int j = 0; j < machines.Count; j++)
                    if (machines[j].functionalities[i])
                        options[i].Add(machines[j]);
            }

            // sort options by the length of the lists

            return options;
        }

        public int GetOtherFuncs(Equipment x, List<int> recipes)
        {
            return 0;
        }

        // TODO - fill in function
        public DateTime GetStart(Equipment tool, TimeSpan length, DateTime ideal)
        {
            DateTime start = new DateTime();
            return start;
        }

        public int GetSOs(Equipment tool, bool[] sosavail)
        {
            int cnt = 0;

            for (int i = 0; i < numSOs; i++)
                if (tool.SOs[i] && sosavail[i])
                    cnt++;

            return cnt;
        }

        // Enter a line in the schedule of a given equipment
        private void EnterScheduleLine(Equipment x, DateTime startTime, Juice j, int batch, TimeSpan timeSpan)
        {
            List<ScheduleEntry> schedule = x.schedule;
            if(schedule.Count == 0)
            {
                schedule.Add(new ScheduleEntry(startTime, startTime.Add(timeSpan), j));
            } else
            {
                int index_insert = 0;
                for(int i=0; i<schedule.Count; i++)
                {
                    if(schedule[i].start > startTime)
                    {
                        index_insert = i;
                        break;
                    }
                }

                //Deal with cleaning
                if(x.name.Equals("Thaw Room")) //TODO! Is that the name given exactly?
                {
                    schedule.Insert(index_insert, new ScheduleEntry(startTime, startTime.Add(timeSpan), j));
                } else
                {
                    //TODO: find cleaning string
                    //string cleaning = 

                    //how long the cleaning will take
                    //TimeSpan q = 

                    //schedule.Insert(index_insert, new ScheduleEntry(startTime.Subtract(q), startTime, cleaning));
                    //schedule.Insert(index_insert, new ScheduleEntry(startTime, startTime.Add(timeSpan), j));
                }
            }
        }

        public void ClaimMixTank(Equipment x, DateTime y, Juice z, int batch, int slurrySize)
        {
            // for when you need to mark a mix tank open ended, basically EnterScheduleLine
        }

        public void ReleaseMixTank(Equipment x, DateTime y)
        {
            // the other half of Claim Mix Tank
        }

        private void ExampleOfSchedule()
        {
            //SO1
            Equipment mix1_so1 = new Equipment("Mix Tank 1");
            mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 01:45:00"), Convert.ToDateTime("02/19/2020 05:15:00"), new Juice("Grapefruit")));
            mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 05:45:00"), Convert.ToDateTime("02/19/2020 09:15:00"), new Juice("Grapefruit")));
            mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 09:45:00"), Convert.ToDateTime("02/19/2020 10:15:00"), new Juice("Rinse")));
            mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 10:30:00"), Convert.ToDateTime("02/19/2020 14:30:00"), new Juice("Lemonade Rasberry")));
            mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 14:30:00"), Convert.ToDateTime("02/19/2020 14:30:00"), new Juice("Lemonade Rasberry")));
            mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 18:30:00"), Convert.ToDateTime("02/19/2020 18:30:00"), new Juice("Lemonade Rasberry")));

            //List<ScheduleEntry> a = mix1_so1.schedule;
            //for(int a = 0; a)

            Equipment mix2_so1 = new Equipment("Mix Tank 2");
            mix2_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 03:45:00"), Convert.ToDateTime("02/19/2020 07:15:00"), new Juice("Grapefruit")));
            mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 07:15:00"), Convert.ToDateTime("02/19/2020 10:20:00"), new Juice("7 Step Hot Clean")));
            mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 16:00:00"), Convert.ToDateTime("02/19/2020 20:30:00"), new Juice("Apricot")));

            Equipment mix3_so1 = new Equipment("Mix Tank 3");
            mix3_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 12:00:00"), Convert.ToDateTime("02/19/2020 16:30:00"), new Juice("Apricot")));

            Equipment mix4_so1 = new Equipment("Mix Tank 4");
            mix4_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 14:00:00"), Convert.ToDateTime("02/19/2020 18:30:00"), new Juice("Apricot")));

            Equipment water_so1 = new Equipment("Water");
            water_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 11:00:00"), Convert.ToDateTime("02/19/2020 11:30:00"), new Juice("Rasberry")));
            water_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 12:30:00"), Convert.ToDateTime("02/19/2020 13:00:00"), new Juice("Apricot")));
            water_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 14:30:00"), Convert.ToDateTime("02/19/2020 15:00:00"), new Juice("Apricot")));
            water_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 15:00:00"), Convert.ToDateTime("02/19/2020 15:30:00"), new Juice("Rasberry")));
            water_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 16:30:00"), Convert.ToDateTime("02/19/2020 17:00:00"), new Juice("Apricot")));
            water_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 19:00:00"), Convert.ToDateTime("02/19/2020 19:30:00"), new Juice("Rasberry")));

            Equipment sucrose_so1 = new Equipment("Sucrose");
            sucrose_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 11:00:00"), Convert.ToDateTime("02/19/2020 11:30:00"), new Juice("Rasberry")));
            sucrose_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 15:00:00"), Convert.ToDateTime("02/19/2020 15:30:00"), new Juice("Rasberry")));
            sucrose_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 19:00:00"), Convert.ToDateTime("02/19/2020 19:30:00"), new Juice("Rasberry")));






            //SO2
            Equipment mix1_so2 = new Equipment("Mix Tank 1");
            Equipment mix2_so2 = new Equipment("Mix Tank 2");
            Equipment mix3_so2 = new Equipment("Mix Tank 3");
            Equipment mix4_so2 = new Equipment("Mix Tank 4");
            Equipment water_so2 = new Equipment("Water");
            Equipment sucrose_so2 = new Equipment("Sucrose");



        }

    }
}