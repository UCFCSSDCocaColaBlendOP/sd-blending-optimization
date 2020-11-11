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
        public List<Equipment> aseptics;

        public List<Equipment> cipGroups;

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

        public Schedule()
        {
            this.scheduleID = DateTime.Now;

            this.machines = new List<Equipment>();
            this.blendtanks = new List<Equipment>();
            this.transferLines = new List<Equipment>();
            this.numFunctions = 10; // TODO: need to change this (1)
            this.finished = new List<Juice>();
            this.inprogress = new List<Juice>();
            this.juices_line8 = new List<Juice>();
            //this.juices_line9 = new List<Juice>();
        }

        //TODO: if string is empty then we should pop up an ERROR box
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
                        inprogress.Add(new_juice);
                    }
                    
                }
            }

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
            for(int i=0; i<inprogress.Count; i++)
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
        void PullEquipment()
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

        // TODO - comments
        public DateTime FindTimeInTheThawRoom(Juice x, int recipeID)
        {
            // try to find space in the thaw room schedule near the ideal time
            DateTime ideal = x.idealTime[recipeID].Subtract(new TimeSpan(0, x.recipes[recipeID][0], 0));
            DateTime limit = ideal.Subtract(new TimeSpan(12, 0, 0));

            int firstBefore = -1;

            if (thawRoom.schedule.Count == 0)
                return ideal;

            if (DateTime.Compare(thawRoom.schedule[thawRoom.schedule.Count - 1].end, ideal) < 0)
                return ideal;

            for (int i = thawRoom.schedule.Count - 2; i >= 0; i--)
            {
                if (DateTime.Compare(thawRoom.schedule[i].end, ideal) > 0)
                    continue;
                if (firstBefore == -1)
                    firstBefore = i;
                if (DateTime.Compare(thawRoom.schedule[i + 1].start, limit) < 0)
                    break;
                if (TimeSpan.Compare(thawRoom.schedule[i + 1].start.Subtract(thawRoom.schedule[i].end), new TimeSpan(0, x.recipes[recipeID][0], 0)) >= 0)
                    return thawRoom.schedule[i].end;
            }

            if (DateTime.Compare(thawRoom.schedule[0].start, ideal) > 0)
            {
                if (DateTime.Compare(ideal.Add(new TimeSpan(0, x.recipes[recipeID][0], 0)), thawRoom.schedule[0].start) < 0)
                    return ideal;
                else if (DateTime.Compare(thawRoom.schedule[0].start.Subtract(new TimeSpan(0, x.recipes[recipeID][0], 0)), limit) >= 0)
                    return thawRoom.schedule[0].start.Subtract(new TimeSpan(0, x.recipes[recipeID][0], 0));
            }

            for (int i = firstBefore + 1; i < thawRoom.schedule.Count - 1; i++)
                if (TimeSpan.Compare(thawRoom.schedule[i + 1].start.Subtract(thawRoom.schedule[i].end), new TimeSpan(0, x.recipes[recipeID][0], 0)) >= 0)
                    return thawRoom.schedule[i].end;

            return thawRoom.schedule[thawRoom.schedule.Count - 1].end;
        }

        // TODO - comments
        public int FindExtraForType(int type, Juice x, int y, CompareRecipe option, bool[] sos)
        {
            Equipment choice = null;
            int choiceidx = -1;
            DateTime begin = new DateTime(0,0,0);
            int j;

            for (j = 0; j < extras.Count; j++)
            {
                if (extras[j].type != type && choice != null)
                    break;
                if (extras[j].type != type)
                    continue;

                bool flag = false;
                for (int i = 0; i < sos.Length; i++)
                    if (extras[j].SOs[i] && sos[i])
                        flag = true;
                if (!flag)
                    continue;

                DateTime tempbegin = FindTimeInAnExtra(extras[j], x, y);

                if (choiceidx == -1)
                {
                    choice = extras[j];
                    choiceidx = j;
                    begin = tempbegin;
                }
                else if (DateTime.Compare(begin, x.idealTime[y]) > 0)
                {
                    if (DateTime.Compare(tempbegin, x.idealTime[y]) > 0)
                    {
                        if (DateTime.Compare(begin, tempbegin) > 0)
                        {
                            choice = extras[j];
                            choiceidx = j;
                            begin = tempbegin;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        choice = extras[j];
                        choiceidx = j;
                        begin = tempbegin;
                    }
                }
                else
                {
                    if (DateTime.Compare(tempbegin, x.idealTime[y]) > 0)
                    {
                        continue;
                    }
                    else
                    {
                        if (DateTime.Compare(begin, tempbegin) > 0)
                        {
                            continue;
                        }
                        else
                        {
                            choice = extras[j];
                            choiceidx = j;
                            begin = tempbegin;
                        }
                    }
                }
            }

            option.neededExtras.Add(choice);
            option.extraTimes.Add(begin);

            return j;
        }

        // TODO - comments
        public DateTime CIPGapSearch(Equipment group, DateTime start, DateTime end, TimeSpan size)
        {
            if (DateTime.Compare(end, start) == 0)
            {
                for (int i = 0; i < group.schedule.Count - 1; i++)
                {
                    if (DateTime.Compare(group.schedule[i].end, start) < 0)
                        continue;
                    if (TimeSpan.Compare(group.schedule[i + 1].start.Subtract(group.schedule[i].end), size) >= 0)
                        return group.schedule[i].end;
                }

                return group.schedule[group.schedule.Count - 1].end;
            }
            else
            {
                if (TimeSpan.Compare(end.Subtract(start), size) < 0)
                    return new DateTime(0, 0, 0);
                
                if (group.schedule.Count == 0)
                    return start;

                if (DateTime.Compare(group.schedule[group.schedule.Count - 1].end, start) < 0)
                    return start;

                for (int i = group.schedule.Count - 2; i >= 0; i--)
                {
                    if (DateTime.Compare(group.schedule[i].end, end) > 0)
                        continue;
                    if (TimeSpan.Compare(group.schedule[i + 1].start.Subtract(group.schedule[i].end), size) >= 0)
                    {
                        if (DateTime.Compare(group.schedule[i + 1].start.Subtract(size), start) < 0)
                            break;
                        if (DateTime.Compare(group.schedule[i].end.Add(size), end) > 0)
                            break;
                        else
                            return start;
                    }
                }

                if (DateTime.Compare(group.schedule[0].start, start) > 0)
                    if (DateTime.Compare(start.Add(size), group.schedule[0].start) < 0)
                        return start;

                return new DateTime(0,0,0);
            }
        }

        // TODO - comments
        public bool CIPGapSearchMixTank(Equipment group, DateTime start, TimeSpan size)
        {
            // find a gap at the start time, otherwise false

            if (group.schedule.Count == 0)
                return true;

            if (DateTime.Compare(group.schedule[group.schedule.Count - 1].end, start) < 0)
                return true;

            for (int i = group.schedule.Count - 2; i >= 0; i--)
            {
                if (DateTime.Compare(group.schedule[i].end, start) > 0)
                    continue;
                if (DateTime.Compare(group.schedule[i + 1].start, start) < 0)
                    break;
                if (TimeSpan.Compare(group.schedule[i + 1].start.Subtract(group.schedule[i].end), size) >= 0)
                {
                    if (DateTime.Compare(start.Add(size), group.schedule[i+1].start) > 0)
                        break;
                    else
                        return true;
                }
            }

            if (DateTime.Compare(group.schedule[0].start, start) > 0)
                if (DateTime.Compare(start.Add(size), group.schedule[0].start) < 0)
                    return true;
            
            return false;
        }

        // TODO - comments
        public DateTime FindTimeInTL(Equipment y, TimeSpan length, DateTime goal)
        {
            // ALISA TODO - cleaning space should hold the cleaning time if it's null, just leave it as new TimeSpan(0,0,0);
            TimeSpan cleaningspace = new TimeSpan(0, 0, 0);

            TimeSpan totalspace = cleaningspace.Add(length);

            DateTime ideal = goal.Subtract(totalspace);

            int firstBefore = -1;

            // schedule is empty
            if (y.schedule.Count == 0)
            {
                DateTime cleanStart = CIPGapSearch(y.cipGroup, ideal, ideal.Add(cleaningspace), cleaningspace);

                if (DateTime.Compare(cleanStart, new DateTime(0, 0, 0)) == 0)
                    return CIPGapSearch(y.cipGroup, ideal, ideal, cleaningspace).Add(cleaningspace);
                else
                    return cleanStart.Add(cleaningspace);
            }

            // all of the schedule is early
            if (DateTime.Compare(y.schedule[y.schedule.Count - 1].end, ideal) < 0)
            {
                DateTime start = CIPGapSearch(y.cipGroup, ideal, ideal.Add(cleaningspace), cleaningspace);

                if (DateTime.Compare(start, new DateTime(0, 0, 0)) == 0)
                    start = CIPGapSearch(y.cipGroup, ideal, ideal, cleaningspace);

                return start.Add(cleaningspace);
            }

            // working backwards to find an ontime gap
            for (int i = y.schedule.Count - 2; i >= 0; i--)
            {
                if (DateTime.Compare(y.schedule[i].end, ideal) > 0)
                    continue;
                if (firstBefore == -1)
                    firstBefore = i;
                if (DateTime.Compare(y.schedule[i + 1].start, ideal) < 0)
                    break;
                if (TimeSpan.Compare(y.schedule[i + 1].start.Subtract(y.schedule[i].end), totalspace) >= 0)
                {
                    DateTime start = CIPGapSearch(y.cipGroup, y.schedule[i].end, y.schedule[i + 1].start.Subtract(totalspace).Add(cleaningspace), cleaningspace);

                    if (DateTime.Compare(start, new DateTime(0, 0, 0)) != 0)
                        return start.Add(cleaningspace);
                }
            }

            if (DateTime.Compare(y.schedule[0].start, ideal) > 0)
            {
                if (DateTime.Compare(ideal.Add(totalspace), y.schedule[0].start) < 0)
                {
                    DateTime start = CIPGapSearch(y.cipGroup, ideal, ideal.Add(cleaningspace), cleaningspace);

                    if (DateTime.Compare(start, new DateTime(0, 0, 0)) == 0)
                        start = CIPGapSearch(y.cipGroup, ideal, y.schedule[0].start.Subtract(totalspace).Add(cleaningspace), cleaningspace);
                    else
                        return start.Add(cleaningspace);

                    if (DateTime.Compare(start, new DateTime(0, 0, 0)) != 0)
                        return start.Add(cleaningspace);
                }
            }

            for (int i = firstBefore + 1; i < y.schedule.Count - 1; i++)
            {
                if (TimeSpan.Compare(y.schedule[i + 1].start.Subtract(y.schedule[i].end), totalspace) >= 0)
                {
                    DateTime start = CIPGapSearch(y.cipGroup, y.schedule[i].end, y.schedule[i + 1].start.Subtract(totalspace).Add(cleaningspace), cleaningspace);

                    if (DateTime.Compare(start, new DateTime(0, 0, 0)) != 0)
                        return start.Add(cleaningspace);
                }
            }

            DateTime gotime = CIPGapSearch(y.cipGroup, y.schedule[y.schedule.Count - 1].end, y.schedule[y.schedule.Count - 1].end, cleaningspace);
            return gotime.Add(cleaningspace);
        }

        // TODO - comments
        public DateTime FindTimeInAnExtra(Equipment y, Juice x, int recipeID)
        {
            // ALISA TODO - cleaning space should hold the cleaning time if it's null, just leave it as new TimeSpan(0,0,0);
            TimeSpan cleaningspace = new TimeSpan(0, 0, 0);
            
            TimeSpan totalspace = cleaningspace.Add(new TimeSpan(0, x.recipes[recipeID][y.type], 0));

            DateTime ideal = x.idealTime[recipeID].Subtract(totalspace);
            DateTime limit = ideal.Subtract(new TimeSpan(1, 0, 0));

            int firstBefore = -1;

            // schedule is empty
            if (y.schedule.Count == 0)
            {
                DateTime cleanStart = CIPGapSearch(y.cipGroup, limit, ideal.Add(cleaningspace), cleaningspace);

                if (DateTime.Compare(cleanStart, new DateTime(0, 0, 0)) == 0)
                    return CIPGapSearch(y.cipGroup, limit, limit, cleaningspace).Add(cleaningspace);
                else
                    return cleanStart.Add(cleaningspace);
            }

            // all of the schedule is early
            if (DateTime.Compare(y.schedule[y.schedule.Count - 1].end, ideal) < 0)
            {
                DateTime start;

                if (DateTime.Compare(y.schedule[y.schedule.Count - 1].end, limit) < 0)
                {
                    start = CIPGapSearch(y.cipGroup, limit, ideal.Add(cleaningspace), cleaningspace);

                    if (DateTime.Compare(start, new DateTime(0, 0, 0)) == 0)
                        start = CIPGapSearch(y.cipGroup, limit, limit, cleaningspace);
                }
                else
                {
                    start = CIPGapSearch(y.cipGroup, y.schedule[y.schedule.Count - 1].end, ideal.Add(cleaningspace), cleaningspace);

                    if (DateTime.Compare(start, new DateTime(0, 0, 0)) == 0)
                        start = CIPGapSearch(y.cipGroup, y.schedule[y.schedule.Count - 1].end, y.schedule[y.schedule.Count - 1].end, cleaningspace);
                }

                return start.Add(cleaningspace);
            }

            // working backwards to find an ontime gap
            for (int i = y.schedule.Count - 2; i >= 0; i--)
            {
                if (DateTime.Compare(y.schedule[i].end, ideal) > 0)
                    continue;
                if (firstBefore == -1)
                    firstBefore = i;
                if (DateTime.Compare(y.schedule[i + 1].start, limit) < 0)
                    break;
                if (TimeSpan.Compare(y.schedule[i + 1].start.Subtract(y.schedule[i].end), totalspace) >= 0)
                {
                    DateTime start = CIPGapSearch(y.cipGroup, y.schedule[i].end, y.schedule[i + 1].start.Subtract(totalspace).Add(cleaningspace), cleaningspace);

                    if (DateTime.Compare(start, new DateTime(0,0,0)) != 0)
                        return start.Add(cleaningspace);
                }
            }

            if (DateTime.Compare(y.schedule[0].start, ideal) > 0)
            {
                if (DateTime.Compare(ideal.Add(totalspace), y.schedule[0].start) < 0)
                {
                    DateTime start = CIPGapSearch(y.cipGroup, limit, ideal.Add(cleaningspace), cleaningspace);

                    if (DateTime.Compare(start, new DateTime(0, 0, 0)) == 0)
                        start = CIPGapSearch(y.cipGroup, limit, y.schedule[0].start.Subtract(totalspace).Add(cleaningspace), cleaningspace);
                    else
                        return start.Add(cleaningspace);

                    if (DateTime.Compare(start, new DateTime(0,0,0)) != 0)
                        return start.Add(cleaningspace);
                }
                else if (DateTime.Compare(y.schedule[0].start.Subtract(totalspace), limit) >= 0)
                {
                    DateTime start = CIPGapSearch(y.cipGroup, limit, y.schedule[0].start.Subtract(totalspace).Add(cleaningspace), cleaningspace);

                    if (DateTime.Compare(start, new DateTime(0,0,0)) != 0)
                        return start.Add(cleaningspace);
                }
            }

            for (int i = firstBefore + 1; i < y.schedule.Count - 1; i++)
            {
                if (TimeSpan.Compare(y.schedule[i + 1].start.Subtract(y.schedule[i].end), totalspace) >= 0)
                {
                    DateTime start = CIPGapSearch(y.cipGroup, y.schedule[i].end, y.schedule[i + 1].start.Subtract(totalspace).Add(cleaningspace), cleaningspace);

                    if (DateTime.Compare(start, new DateTime(0,0,0)) != 0)
                        return start.Add(cleaningspace);
                }
            }

            DateTime gotime = CIPGapSearch(y.cipGroup, y.schedule[y.schedule.Count - 1].end, y.schedule[y.schedule.Count - 1].end, cleaningspace);
            return gotime.Add(cleaningspace);
        }

        // TODO - comments
        public ScheduleEntry FindEntryInThawRoom(Juice x, int recipeID)
        {
            // first you need to check if the juice already has allocated time on the schedule for the thaw room
            //      if yes start time is when this batch is done with the thaw room

            for (int i = 0; i < thawRoom.schedule.Count; i++)
            {
                if (thawRoom.schedule[i].juice.type == x.type && DateTime.Compare(thawRoom.schedule[i].end, x.idealTime[recipeID]) < 0)
                {
                    int batch = x.totalBatches - x.neededBatches - 1;
                    i += batch;
                    return thawRoom.schedule[i];
                }
            }

            return null;
        }

        // TODO - comments
        public ScheduleEntry FindEntryInThawRoom(Juice x, int recipeID, int slurry)
        {
            // first you need to check if the juice already has allocated time on the schedule for the thaw room
            //      if yes start time is when this batch is done with the thaw room

            for (int i = 0; i < thawRoom.schedule.Count; i++)
            {
                if (thawRoom.schedule[i].juice.type == x.type && DateTime.Compare(thawRoom.schedule[i].end, x.idealTime[recipeID]) < 0)
                {
                    int batch = x.totalBatches - x.neededBatches + slurry - 1;
                    i += batch;
                    return thawRoom.schedule[i];
                }
            }

            return null;
        }

        // TODO - comments
        public bool FindTimeInMixTank(Equipment y, TimeSpan length, DateTime goal)
        {
            // ALISA TODO - cleaning space should hold the cleaning time if it's null, just leave it as new TimeSpan(0,0,0);
            TimeSpan cleaningspace = new TimeSpan(0, 0, 0);

            TimeSpan totalspace = cleaningspace.Add(length);

            DateTime ideal = goal.Subtract(totalspace);

            int firstBefore = -1;

            // schedule is empty
            if (y.schedule.Count == 0)
                return CIPGapSearchMixTank(y.cipGroup, ideal.Subtract(cleaningspace), cleaningspace);

            // all of the schedule is early
            if (DateTime.Compare(y.schedule[y.schedule.Count - 1].end, ideal) < 0)
                return CIPGapSearchMixTank(y.cipGroup, ideal, cleaningspace);

            // working backwards to find an ontime gap
            for (int i = y.schedule.Count - 2; i >= 0; i--)
            {
                if (DateTime.Compare(y.schedule[i].end, ideal) > 0)
                    continue;
                if (firstBefore == -1)
                    firstBefore = i;
                if (DateTime.Compare(y.schedule[i + 1].start, ideal) < 0)
                    break;
                if (TimeSpan.Compare(y.schedule[i + 1].start.Subtract(y.schedule[i].end), totalspace) >= 0)
                {
                    DateTime start = CIPGapSearch(y.cipGroup, y.schedule[i].end, y.schedule[i + 1].start.Subtract(totalspace).Add(cleaningspace), cleaningspace);

                    if (DateTime.Compare(start, new DateTime(0, 0, 0)) != 0)
                        return true;
                }
            }

            if (DateTime.Compare(ideal.Add(totalspace), y.schedule[0].start) < 0)
                return CIPGapSearchMixTank(y.cipGroup, ideal, cleaningspace);

            return false;
        }

        // TODO - comments
        public CompareRecipe PrepRecipe(Juice x, int y)
        {
            CompareRecipe option = new CompareRecipe();
            bool pickedStartTime = false; // has option.startBlending been set
            bool preschedThaw = false;
            bool[] checkoffFunc = new bool[numFunctions];
            bool[] soChoices = new bool[numSOs];
            for (int j = 0; j < numSOs; j++)
                soChoices[j] = true;

            // if the thaw room is needed
            if (x.recipes[y][0] > 0)
            {
                if (thawRoom == null)
                {
                    option.conceivable = false;
                    return option;
                }

                DateTime begin;
                ScheduleEntry temp = FindEntryInThawRoom(x, y);
                if (temp == null)
                {
                    begin = FindTimeInTheThawRoom(x, y);
                    if (DateTime.Compare(begin, x.idealTime[y]) > 0)
                    {
                        option.onTime = false;
                        option.thawTime = begin;
                        option.startBlending = begin.Add(new TimeSpan(0, x.recipes[y][0], 0));
                        x.idealTime[y] = option.startBlending;
                    }
                    else
                    {
                        option.thawTime = begin;
                        option.startBlending = x.idealTime[y];
                    }
                    option.makeANewThawEntry = true;
                }
                else
                {
                    begin = temp.end;
                    preschedThaw = true;
                    option.thawTime = begin;

                    if (DateTime.Compare(begin, x.idealTime[y]) > 0)
                    {
                        option.onTime = false;
                        option.startBlending = begin.Add(new TimeSpan(0, x.recipes[y][0], 0));
                        x.idealTime[y] = option.startBlending;
                    }
                    else
                    {
                        option.startBlending = x.idealTime[y];
                    }
                }

                
                option.thawRoom = true;
                pickedStartTime = true;
                checkoffFunc[0] = true;
            }

            // if any of the extras are needed
            for (int j = 0; j < extras.Count; j++)
            {
                if (x.recipes[y][extras[j].type] < 0)
                    continue;

                j = FindExtraForType(extras[j].type, x, y, option, soChoices);
                checkoffFunc[extras[j].type] = true;

                DateTime begin = option.extraTimes[option.extraTimes.Count - 1];
                
                // if it couldn't find an extra that it needed
                if (option.neededExtras[option.neededExtras.Count - 1] == null)
                {
                    option.conceivable = false;
                    return option;
                }

                // this extra is late
                if (DateTime.Compare(begin, x.idealTime[y].AddHours(1)) > 0)
                {
                    x.idealTime[y] = begin;
                    option.onTime = false;
                    option.startBlending = begin;

                    if (option.thawRoom)
                    {
                        if (preschedThaw)
                        {
                            option.conceivable = false;
                            return option;
                        }

                        // it's acceptable for the thaw room to be up to 12 hours ahead of the blend
                        if (DateTime.Compare(begin, option.thawTime.Add(new TimeSpan(0, x.recipes[y][0], 0)).Add(new TimeSpan(12,0,0))) > 0)
                        {
                            // we need to retry thaw room
                            DateTime thawAttempt2 = FindTimeInTheThawRoom(x, y);

                            // if it's late again, just give up
                            if (DateTime.Compare(thawAttempt2, begin) > 0)
                            {
                                option.conceivable = false;
                                return option;
                            }
                            else
                            {
                                option.thawTime = thawAttempt2;
                            }
                        }
                    }

                    if (option.neededExtras.Count > 1)
                    {
                        int cap = option.neededExtras.Count - 1;
                        for (int k = 0; k < cap; k++)
                        {
                            FindExtraForType(option.neededExtras[k].type, x, y, option, soChoices);
                            DateTime newBegin = option.extraTimes[option.extraTimes.Count - 1];
                            if (DateTime.Compare(newBegin, begin.AddHours(1)) > 0)
                            {
                                option.conceivable = false;
                                return option;
                            }
                            else
                            {
                                option.neededExtras.RemoveAt(k);
                                option.extraTimes.RemoveAt(k);
                                k--;
                                cap--;
                            }
                        }
                    }
                }

                if (!pickedStartTime)
                {
                    option.startBlending = begin;
                    pickedStartTime = true;
                }

                if (DateTime.Compare(begin, option.startBlending) < 0)
                    option.startBlending = begin;

                // put in a check for SOs because extra equipment can limit them
                for (int i = 0; i < numSOs; i++)
                    if (soChoices[i] && !option.neededExtras[option.neededExtras.Count - 1].SOs[i])
                        soChoices[i] = false;
            }

            // do you need a blend synew TimeSpan(0, x.recipePreTimes[y], 0);stem
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
                        if (!checkoffFunc[k] && x.recipes[y][k] > 0 && !blendSystems[j].functionalities[k])
                            continue;

                    TimeSpan templength = new TimeSpan(0, 0, 0);
                    for (int k = 0; k < numFunctions; k++)
                        if (!checkoffFunc[k] && x.recipes[y][k] > 0)
                            templength.Add(new TimeSpan(0, x.recipes[y][k], 0));

                    // then start comparing this blendsystem to the last one to make a choice
                    DateTime tempstart = GetStart(blendSystems[j], templength, x.idealTime[y]);
                    if (DateTime.Compare(tempstart, new DateTime(0, 0, 0)) == 0)
                        continue;
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

                if (!pickedStartTime)
                {
                    option.startBlending = currentStart;
                    pickedStartTime = true;
                }
                if (DateTime.Compare(currentStart, option.startBlending) < 0)
                    option.startBlending = currentStart;

                for (int k = 0; k < numSOs; k++)
                    if (soChoices[k] && !option.blendSystem.SOs[k])
                        soChoices[k] = false;
            }

            // assign a mix tank
            // need to calculate the span of time we need the mix tank for
            TimeSpan mixtanktime = new TimeSpan(0, x.recipePreTimes[y], 0);
            mixtanktime = mixtanktime.Add(new TimeSpan(0, x.recipePostTimes[y], 0));
            DateTime goal;
            bool set = false;
            DateTime earliest = new DateTime(0,0,0);
            DateTime longest = new DateTime(0,0,0);
            if (option.neededExtras.Count > 0)
            {
                for (int i = 0; i < option.neededExtras.Count; i++)
                {
                    if (!set)
                    {
                        earliest = option.extraTimes[i];
                        longest = option.extraTimes[i].Add(new TimeSpan(0, x.recipes[y][option.neededExtras[i].type], 0));
                        set = true;
                    }
                    else
                    {
                        if (DateTime.Compare(earliest, option.extraTimes[i]) > 0)
                            earliest = option.extraTimes[i];
                        if (DateTime.Compare(longest, option.extraTimes[i].Add(new TimeSpan(0, x.recipes[y][option.neededExtras[i].type], 0))) < 0)
                            longest = option.extraTimes[i].Add(new TimeSpan(0, x.recipes[y][option.neededExtras[i].type], 0));
                    }
                }
            }

            if (needBlendSys)
            {
                if (DateTime.Compare(earliest, option.blendTime) > 0)
                    earliest = option.blendTime;
                if (DateTime.Compare(longest, option.blendTime.Add(option.blendLength)) < 0)
                    longest = option.blendTime.Add(option.blendLength);
            }

            goal = earliest.Subtract(new TimeSpan(0, x.recipePreTimes[y], 0));
            mixtanktime = mixtanktime.Add(longest.Subtract(earliest));
            mixtanktime = mixtanktime.Add(new TimeSpan(0, x.transferTime, 0));

            bool gottem = false;

            for (int i = 0; i < blendtanks.Count; i++)
            {
                if (!soChoices[blendtanks[i].type])
                    continue;

                bool flag = FindTimeInMixTank(blendtanks[i], mixtanktime, goal);

                if (flag)
                {
                    option.mixLength = mixtanktime;
                    option.mixTank = blendtanks[i];
                    option.mixTime = goal;
                    gottem = true;
                    break;
                }
                else
                    continue;
            }

            if (!gottem)
            {
                option.conceivable = false;
                return option;
            }

            // assign a transfer line
            DateTime tgoal = option.mixTime.Add(option.mixLength).Subtract(new TimeSpan(0, x.transferTime, 0));

            if (FindTimeInMixTank(transferLines[0], new TimeSpan(0, x.transferTime, 0), tgoal))
            {
                option.transferTime = tgoal;
                option.transferLine = transferLines[0];
                option.transferLength = new TimeSpan(0, x.transferTime, 0);
            }
            else if (FindTimeInMixTank(transferLines[1], new TimeSpan(0, x.transferTime, 0), tgoal))
            {
                option.transferTime = tgoal;
                option.transferLine = transferLines[1];
                option.transferLength = new TimeSpan(0, x.transferTime, 0);
            }
            else if (FindTimeInMixTank(transferLines[3], new TimeSpan(0, x.transferTime, 0), tgoal))
            {
                option.transferTime = tgoal;
                option.transferLine = transferLines[3];
                option.transferLength = new TimeSpan(0, x.transferTime, 0);
            }
            else if (FindTimeInMixTank(transferLines[2], new TimeSpan(0, x.transferTime, 0), tgoal))
            {
                option.transferTime = tgoal;
                option.transferLine = transferLines[2];
                option.transferLength = new TimeSpan(0, x.transferTime, 0);
            }
            else
            {
                option.conceivable = false;
                return option;
            }


            // decide if it's onTime
            option.onTime = DateTime.Compare(x.currentFillTime, option.mixTime.Add(option.mixLength)) == 0;

            return option;
        }
        
        // TODO - comments
        public CompareRecipe PrepRecipe(Juice x, int y, int slurrySize)
        {
            for (int i = 0; i < x.recipes[y].Count; i++)
                x.recipes[y][i] *= slurrySize;

            CompareRecipe option = new CompareRecipe();
            bool pickedStartTime = false; // has option.startBlending been set
            bool preschedThaw = false;
            bool[] checkoffFunc = new bool[numFunctions];
            bool[] soChoices = new bool[numSOs];
            for (int j = 0; j < numSOs; j++)
                soChoices[j] = true;

            // if the thaw room is needed
            if (x.recipes[y][0] > 0)
            {
                if (thawRoom == null)
                {
                    option.conceivable = false;

                    for (int z = 0; z < x.recipes[y].Count; z++)
                        x.recipes[y][z] /= slurrySize;

                    return option;
                }

                DateTime begin;
                ScheduleEntry temp = FindEntryInThawRoom(x, y, slurrySize);
                if (temp == null)
                {
                    begin = FindTimeInTheThawRoom(x, y);
                    if (DateTime.Compare(begin, x.idealTime[y]) > 0)
                    {
                        option.onTime = false;
                        option.thawTime = begin;
                        option.startBlending = begin.Add(new TimeSpan(0, x.recipes[y][0], 0));
                        x.idealTime[y] = option.startBlending;
                    }
                    else
                    {
                        option.thawTime = begin;
                        option.startBlending = x.idealTime[y];
                    }
                    option.makeANewThawEntry = true;
                }
                else
                {
                    begin = temp.end;
                    preschedThaw = true;
                    option.thawTime = begin;

                    if (DateTime.Compare(begin, x.idealTime[y]) > 0)
                    {
                        option.onTime = false;
                        option.startBlending = begin.Add(new TimeSpan(0, x.recipes[y][0], 0));
                        x.idealTime[y] = option.startBlending;
                    }
                    else
                    {
                        option.startBlending = x.idealTime[y];
                    }
                }


                option.thawRoom = true;
                pickedStartTime = true;
                checkoffFunc[0] = true;
            }

            // if any of the extras are needed
            for (int j = 0; j < extras.Count; j++)
            {
                if (x.recipes[y][extras[j].type] < 0)
                    continue;

                j = FindExtraForType(extras[j].type, x, y, option, soChoices);
                checkoffFunc[extras[j].type] = true;

                DateTime begin = option.extraTimes[option.extraTimes.Count - 1];

                // if it couldn't find an extra that it needed
                if (option.neededExtras[option.neededExtras.Count - 1] == null)
                {
                    option.conceivable = false;

                    for (int z = 0; z < x.recipes[y].Count; z++)
                        x.recipes[y][z] /= slurrySize;

                    return option;
                }

                // this extra is late
                if (DateTime.Compare(begin, x.idealTime[y].AddHours(1)) > 0)
                {
                    x.idealTime[y] = begin;
                    option.onTime = false;
                    option.startBlending = begin;

                    if (option.thawRoom)
                    {
                        if (preschedThaw)
                        {
                            option.conceivable = false;

                            for (int z = 0; z < x.recipes[y].Count; z++)
                                x.recipes[y][z] /= slurrySize;

                            return option;
                        }

                        // it's acceptable for the thaw room to be up to 12 hours ahead of the blend
                        if (DateTime.Compare(begin, option.thawTime.Add(new TimeSpan(0, x.recipes[y][0], 0)).Add(new TimeSpan(12, 0, 0))) > 0)
                        {
                            // we need to retry thaw room
                            DateTime thawAttempt2 = FindTimeInTheThawRoom(x, y);

                            // if it's late again, just give up
                            if (DateTime.Compare(thawAttempt2, begin) > 0)
                            {
                                option.conceivable = false;

                                for (int z = 0; z < x.recipes[y].Count; z++)
                                    x.recipes[y][z] /= slurrySize;

                                return option;
                            }
                            else
                            {
                                option.thawTime = thawAttempt2;
                            }
                        }
                    }

                    if (option.neededExtras.Count > 1)
                    {
                        int cap = option.neededExtras.Count - 1;
                        for (int k = 0; k < cap; k++)
                        {
                            FindExtraForType(option.neededExtras[k].type, x, y, option, soChoices);
                            DateTime newBegin = option.extraTimes[option.extraTimes.Count - 1];
                            if (DateTime.Compare(newBegin, begin.AddHours(1)) > 0)
                            {
                                option.conceivable = false;

                                for (int z = 0; z < x.recipes[y].Count; z++)
                                    x.recipes[y][z] /= slurrySize;

                                return option;
                            }
                            else
                            {
                                option.neededExtras.RemoveAt(k);
                                option.extraTimes.RemoveAt(k);
                                k--;
                                cap--;
                            }
                        }
                    }
                }

                if (!pickedStartTime)
                {
                    option.startBlending = begin;
                    pickedStartTime = true;
                }

                if (DateTime.Compare(begin, option.startBlending) < 0)
                    option.startBlending = begin;

                // put in a check for SOs because extra equipment can limit them
                for (int i = 0; i < numSOs; i++)
                    if (soChoices[i] && !option.neededExtras[option.neededExtras.Count - 1].SOs[i])
                        soChoices[i] = false;
            }

            // do you need a blend synew TimeSpan(0, x.recipePreTimes[y], 0);stem
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
                TimeSpan length = new TimeSpan(0,0,0);

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
                        if (!checkoffFunc[k] && x.recipes[y][k] > 0 && !blendSystems[j].functionalities[k])
                            continue;

                    TimeSpan templength = new TimeSpan(0, 0, 0);
                    for (int k = 0; k < numFunctions; k++)
                        if (!checkoffFunc[k] && x.recipes[y][k] > 0)
                            templength.Add(new TimeSpan(0, x.recipes[y][k], 0));

                    // then start comparing this blendsystem to the last one to make a choice
                    DateTime tempstart = GetStart(blendSystems[j], templength, x.idealTime[y]);
                    if (DateTime.Compare(tempstart, new DateTime(0, 0, 0)) == 0)
                        continue;
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

                    for (int z = 0; z < x.recipes[y].Count; z++)
                        x.recipes[y][z] /= slurrySize;

                    return option;
                }

                option.blendSystem = blendSystems[choice];
                option.blendTime = currentStart;
                // calculate blendLength
                option.blendLength = new TimeSpan(0, 0, 0);
                for (int k = 0; k < numFunctions; k++)
                    if (!checkoffFunc[k] && x.recipes[y][k] > 0)
                        option.blendLength.Add(new TimeSpan(0, x.recipes[y][k], 0));

                if (!pickedStartTime)
                {
                    option.startBlending = currentStart;
                    pickedStartTime = true;
                }
                if (DateTime.Compare(currentStart, option.startBlending) < 0)
                    option.startBlending = currentStart;

                for (int k = 0; k < numSOs; k++)
                    if (soChoices[k] && !option.blendSystem.SOs[k])
                        soChoices[k] = false;
            }

            // assign a mix tank
            // need to calculate the span of time we need the mix tank for
            TimeSpan mixtanktime = new TimeSpan(0, x.recipePreTimes[y], 0);
            mixtanktime = mixtanktime.Add(new TimeSpan(0, x.recipePostTimes[y], 0));
            DateTime goal;
            bool set = false;
            DateTime earliest = new DateTime(0, 0, 0);
            DateTime longest = new DateTime(0, 0, 0);
            if (option.neededExtras.Count > 0)
            {
                for (int i = 0; i < option.neededExtras.Count; i++)
                {
                    if (!set)
                    {
                        earliest = option.extraTimes[i];
                        longest = option.extraTimes[i].Add(new TimeSpan(0, x.recipes[y][option.neededExtras[i].type], 0));
                        set = true;
                    }
                    else
                    {
                        if (DateTime.Compare(earliest, option.extraTimes[i]) > 0)
                            earliest = option.extraTimes[i];
                        if (DateTime.Compare(longest, option.extraTimes[i].Add(new TimeSpan(0, x.recipes[y][option.neededExtras[i].type], 0))) < 0)
                            longest = option.extraTimes[i].Add(new TimeSpan(0, x.recipes[y][option.neededExtras[i].type], 0));
                    }
                }
            }

            if (needBlendSys)
            {
                if (DateTime.Compare(earliest, option.blendTime) > 0)
                    earliest = option.blendTime;
                if (DateTime.Compare(longest, option.blendTime.Add(option.blendLength)) < 0)
                    longest = option.blendTime.Add(option.blendLength);
            }

            goal = earliest.Subtract(new TimeSpan(0, x.recipePreTimes[y], 0));
            mixtanktime = mixtanktime.Add(longest.Subtract(earliest));
            mixtanktime = mixtanktime.Add(new TimeSpan(0, x.transferTime, 0));

            bool gottem = false;

            for (int i = 0; i < blendtanks.Count; i++)
            {
                if (!soChoices[blendtanks[i].type])
                    continue;

                bool flag = FindTimeInMixTank(blendtanks[i], mixtanktime, goal);

                if (flag)
                {
                    option.mixLength = mixtanktime;
                    option.mixTank = blendtanks[i];
                    option.mixTime = goal;
                    gottem = true;
                    break;
                }
                else
                    continue;
            }

            if (!gottem)
            {
                option.conceivable = false;

                for (int z = 0; z < x.recipes[y].Count; z++)
                    x.recipes[y][z] /= slurrySize;

                return option;
            }

            // assign a transfer line
            DateTime tgoal = option.mixTime.Add(option.mixLength).Subtract(new TimeSpan(0, x.transferTime, 0));

            if (FindTimeInMixTank(transferLines[3], new TimeSpan(0, x.transferTime, 0), tgoal))
            {
                option.transferTime = tgoal;
                option.transferLine = transferLines[3];
                option.transferLength = new TimeSpan(0, x.transferTime, 0);
            }
            else
            {
                option.transferTime = FindTimeInTL(transferLines[3], new TimeSpan(0, x.transferTime, 0), tgoal);
                option.transferLine = transferLines[3];
                option.transferLength = new TimeSpan(0, x.transferTime, 0);
                option.onTime = false;
            }

            // decide if it's onTime
            option.onTime = option.onTime && DateTime.Compare(x.currentFillTime, option.mixTime.Add(option.mixLength)) == 0;

            for (int z = 0; z < x.recipes[y].Count; z++)
                x.recipes[y][z] /= slurrySize;

            return option;
        }

        // TODO :: comment, add errors, add calls to functions that add to database
        public void GenerateNewSchedule()
        {
            SortByFillTime();

            while (inprogress.Count != 0)
            {
                if (inprogress[0].mixing)
                {
                    // you only have to acquire a transfer line
                    bool flag = AcquireTransferLine(inprogress[0].inline, inprogress[0], inprogress[0].readytotrans, inprogress[0].BlendTank);

                    if (flag)
                    {
                        // for whatever reason this batch is late because of the transfer line
                    }

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
                        bool flag = AcquireTransferLine(true, inprogress[0], inprogress[0].currentFillTime.Subtract(new TimeSpan(0,inprogress[0].transferTime,0)), inprogress[0].BlendTank);

                        if (flag)
                        {
                            // transfer line 3 is really behind and this batch is late
                        }

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
                                int bnum = inprogress[0].totalBatches - inprogress[0].neededBatches;

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
                        int batch = inprogress[0].totalBatches - inprogress[0].neededBatches;

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

            GrabJuiceSchedules();
            // call Alisa's functions to add schedules to database
        }

        // will find and schedule time for the juice x in the tank to use a transfer line
        // if the juice is inline, it will find time on transfer line 3, returning true for ontime and false for late
        // otherwise, it will find time first on transfer line 1/2 which is restricted to one SO and then try four then three
        //      to find a on time transfer line or the least late option. returns true for ontime and false for late
        public bool AcquireTransferLine(bool inline, Juice x, DateTime y, Equipment tank)
        {
            // go through list of transfer lines and pick the one that's best for x at time y
            // then assign the juice to it

            if (inline)
            {
                // grab a time on transfer line 3, inline can only use transfer line 3
                DateTime when = FindTimeInTL(transferLines[2], new TimeSpan(0, x.transferTime, 0), y);

                EnterScheduleLine(transferLines[2], when, x, x.totalBatches - x.neededBatches, new TimeSpan(0, x.transferTime, 0));
                
                if (DateTime.Compare(when, y) > 0)
                    return false;
                return true;
            }
            else
            {
                // try transfer line 1 or 2 (depends on tank
                DateTime when = FindTimeInTL(transferLines[tank.type], new TimeSpan(0, x.transferTime, 0), y);

                // tansfer line 1/2 is late
                if (DateTime.Compare(when, y) > 0)
                {
                    // try transfer line 4
                    DateTime when2 = FindTimeInTL(transferLines[3], new TimeSpan(0, x.transferTime, 0), y);

                    // transfer line 4 is ontime
                    if (DateTime.Compare(when2, y) == 0)
                    {
                        EnterScheduleLine(transferLines[3], when, x, x.totalBatches - x.neededBatches, new TimeSpan(0, x.transferTime, 0));
                        return true;
                    }
                    // transfer line four is less late than 1/2
                    else if (DateTime.Compare(when2, when) < 0)
                    {
                        // try 3
                        DateTime when3 = FindTimeInTL(transferLines[2], new TimeSpan(0, x.transferTime, 0), y);

                        // 3 is ontime
                        if (DateTime.Compare(when3, y) == 0)
                        {
                            EnterScheduleLine(transferLines[2], when, x, x.totalBatches - x.neededBatches, new TimeSpan(0, x.transferTime, 0));
                            return true;
                        }
                        // transfer line 3 is less late than 4
                        else if (DateTime.Compare(when3, when2) < 0)
                        {
                            EnterScheduleLine(transferLines[2], when, x, x.totalBatches - x.neededBatches, new TimeSpan(0, x.transferTime, 0));
                            return false;
                        }
                        // transfer line four is the least late
                        else
                        {
                            EnterScheduleLine(transferLines[3], when, x, x.totalBatches - x.neededBatches, new TimeSpan(0, x.transferTime, 0));
                            return false;
                        }
                    }
                    // transfer line 1/2 is less late than four
                    else
                    {
                        EnterScheduleLine(transferLines[tank.type], when, x, x.totalBatches - x.neededBatches, new TimeSpan(0, x.transferTime, 0));
                        return false;
                    }
                }
                // transfer line 1/2 is ontime
                else
                {
                    EnterScheduleLine(transferLines[tank.type], when, x, x.totalBatches - x.neededBatches, new TimeSpan(0, x.transferTime, 0));
                    return true;
                }
            }
        }

        // grab from alisa
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

        // takes a recipe and a tool and returns the number of functionalities the tool supports,
        // which the recipe doesn't need
        public int GetOtherFuncs(Equipment x, List<int> recipe)
        {
            int cnt = 0;
            
            for (int i = 0; i < numFunctions; i++)
                if (x.functionalities[i] && recipe[i] <= 0)
                    cnt++;

            return cnt;
        }

        // TODO - comment
        public DateTime GetStart(Equipment y, TimeSpan length, DateTime justForBlending)
        {
            // ALISA TODO - cleaning space should hold the cleaning time if it's null, just leave it as new TimeSpan(0,0,0);
            TimeSpan cleaningspace = new TimeSpan(0, 0, 0);

            TimeSpan totalspace = cleaningspace.Add(length);

            DateTime ideal = justForBlending.Subtract(totalspace);
            DateTime limit = ideal.Subtract(new TimeSpan(0, 30, 0));
            DateTime lateLimit = ideal.Add(new TimeSpan(0, 30, 0));

            int firstBefore = -1;

            // schedule is empty
            if (y.schedule.Count == 0)
            {
                DateTime cleanStart = CIPGapSearch(y.cipGroup, limit, ideal.Add(cleaningspace), cleaningspace);

                if (DateTime.Compare(cleanStart, new DateTime(0, 0, 0)) == 0)
                {
                    cleanStart = CIPGapSearch(y.cipGroup, limit, lateLimit, cleaningspace);
                    if (DateTime.Compare(cleanStart, new DateTime(0, 0, 0)) == 0)
                        return cleanStart;
                    else
                        return cleanStart.Add(cleaningspace);
                }
                else
                    return cleanStart.Add(cleaningspace);
            }

            // all of the schedule is early
            if (DateTime.Compare(y.schedule[y.schedule.Count - 1].end, ideal) < 0)
            {
                DateTime start;

                if (DateTime.Compare(y.schedule[y.schedule.Count - 1].end, limit) < 0)
                {
                    start = CIPGapSearch(y.cipGroup, limit, ideal.Add(cleaningspace), cleaningspace);

                    if (DateTime.Compare(start, new DateTime(0, 0, 0)) == 0)
                        start = CIPGapSearch(y.cipGroup, limit, lateLimit, cleaningspace);
                }
                else
                {
                    start = CIPGapSearch(y.cipGroup, y.schedule[y.schedule.Count - 1].end, ideal.Add(cleaningspace), cleaningspace);

                    if (DateTime.Compare(start, new DateTime(0, 0, 0)) == 0)
                        start = CIPGapSearch(y.cipGroup, y.schedule[y.schedule.Count - 1].end, lateLimit, cleaningspace);
                }

                if (DateTime.Compare(start, new DateTime(0, 0, 0)) == 0)
                    return start;
                else
                    return start.Add(cleaningspace);
            }

            // working backwards to find an ontime gap
            for (int i = y.schedule.Count - 2; i >= 0; i--)
            {
                if (DateTime.Compare(y.schedule[i].end, ideal) > 0)
                    continue;
                if (firstBefore == -1)
                    firstBefore = i;
                if (DateTime.Compare(y.schedule[i + 1].start, limit) < 0)
                    break;
                if (TimeSpan.Compare(y.schedule[i + 1].start.Subtract(y.schedule[i].end), totalspace) >= 0)
                {
                    DateTime start = CIPGapSearch(y.cipGroup, y.schedule[i].end, y.schedule[i + 1].start.Subtract(totalspace).Add(cleaningspace), cleaningspace);

                    if (DateTime.Compare(start, new DateTime(0, 0, 0)) != 0)
                        return start.Add(cleaningspace);
                }
            }

            // look at the time before the schedule
            if (DateTime.Compare(y.schedule[0].start, ideal) > 0)
            {
                // if the schedule is completely after the time we want
                if (DateTime.Compare(ideal.Add(totalspace), y.schedule[0].start) < 0)
                {
                    DateTime start = CIPGapSearch(y.cipGroup, limit, ideal.Add(cleaningspace), cleaningspace);

                    if (DateTime.Compare(start, new DateTime(0, 0, 0)) == 0)
                    {
                        if (DateTime.Compare(y.schedule[0].start.Subtract(totalspace).Add(cleaningspace), lateLimit) < 0)
                            start = CIPGapSearch(y.cipGroup, limit, y.schedule[0].start.Subtract(totalspace).Add(cleaningspace), cleaningspace);
                        else
                            start = CIPGapSearch(y.cipGroup, limit, lateLimit, cleaningspace);
                    }
                    else
                        return start.Add(cleaningspace);

                    if (DateTime.Compare(start, new DateTime(0, 0, 0)) != 0)
                        return start.Add(cleaningspace);
                }
                // as long as the time we want is after limit
                else if (DateTime.Compare(y.schedule[0].start.Subtract(totalspace), limit) >= 0)
                {
                    DateTime start = CIPGapSearch(y.cipGroup, limit, y.schedule[0].start.Subtract(totalspace).Add(cleaningspace), cleaningspace);

                    if (DateTime.Compare(start, new DateTime(0, 0, 0)) != 0)
                        return start.Add(cleaningspace);
                }
            }

            // working forwards to find a late gap
            for (int i = firstBefore + 1; i < y.schedule.Count - 1; i++)
            {
                if (DateTime.Compare(y.schedule[i].end, lateLimit) > 0)
                    return new DateTime(0, 0, 0);
                if (TimeSpan.Compare(y.schedule[i + 1].start.Subtract(y.schedule[i].end), totalspace) >= 0)
                {
                    DateTime start = CIPGapSearch(y.cipGroup, y.schedule[i].end, y.schedule[i + 1].start.Subtract(totalspace).Add(cleaningspace), cleaningspace);

                    if (DateTime.Compare(start, new DateTime(0, 0, 0)) != 0)
                        return start.Add(cleaningspace);
                }
            }

            DateTime gotime = CIPGapSearch(y.cipGroup, y.schedule[y.schedule.Count - 1].end, y.schedule[y.schedule.Count - 1].end, cleaningspace);

            if (DateTime.Compare(gotime.Add(cleaningspace), lateLimit) > 0)
                return new DateTime(0, 0, 0);

            return gotime.Add(cleaningspace);
        }
        
        // takes a list of SOs still available returns the number of those SOs that are also
        // available to the tool
        public int GetSOs(Equipment tool, bool[] sosavail)
        {
            int cnt = 0;

            for (int i = 0; i < numSOs; i++)
                if (tool.SOs[i] && sosavail[i])
                    cnt++;

            return cnt;
        }

        // grab from tati's and edit
        public void EnterScheduleLine(Equipment x, DateTime y, Juice z, int batch, TimeSpan q)
        {
            // mark x's schedule at time y for Juice z, batch for time span q
            // make sure to also apply the appropriate cleaning in between
        }

        // TODO :: 3
        public void ClaimMixTank(Equipment x, DateTime y, Juice z, int batch, int slurrySize)
        {
            // for when you need to mark a mix tank open ended, basically EnterScheduleLine
        }

        // TODO :: 2
        public void ReleaseMixTank(Equipment x, DateTime y)
        {
            // the other half of Claim Mix Tank
        }
        
        // TODO - comments
        public void GrabJuiceSchedules()
        {
            // goes through thawRoom, extras, blendSystems, blendtanks, transferLines, aseptics their schedules
            // and for juice entrys do <pieceofequipment>.schedule[i].juice.schedule.Add(<pieceofequipment>.schedule[i]);
            // then run through finished and sort each juice's scheduled

            for (int i = 0; i < thawRoom.schedule.Count; i++)
            {
                thawRoom.schedule[i].tool = thawRoom;
                thawRoom.schedule[i].juice.schedule.Add(thawRoom.schedule[i]);
            }

            for (int i = 0; i < extras.Count; i++)
            {
                for (int j = 0; j < extras[i].schedule.Count; j++)
                {
                    if (!extras[i].schedule[j].cleaning)
                    {
                        extras[i].schedule[j].tool = extras[i];
                        extras[i].schedule[j].juice.schedule.Add(extras[i].schedule[j]);
                    }
                }
            }

            for (int i = 0; i < blendSystems.Count; i++)
            {
                for (int j = 0; j < blendSystems[i].schedule.Count; j++)
                {
                    if (!blendSystems[i].schedule[j].cleaning)
                    {
                        blendSystems[i].schedule[j].tool = blendSystems[i];
                        blendSystems[i].schedule[j].juice.schedule.Add(blendSystems[i].schedule[j]);
                    }
                }
            }

            for (int i = 0; i < blendtanks.Count; i++)
            {
                for (int j = 0; j < blendtanks[i].schedule.Count; j++)
                {
                    if (!blendtanks[i].schedule[j].cleaning)
                    {
                        blendtanks[i].schedule[j].tool = blendtanks[i];
                        blendtanks[i].schedule[j].juice.schedule.Add(blendtanks[i].schedule[j]);
                    }
                }
            }

            for (int i = 0; i < transferLines.Count; i++)
            {
                for (int j = 0; j < transferLines[i].schedule.Count; j++)
                {
                    if (!transferLines[i].schedule[j].cleaning)
                    {
                        transferLines[i].schedule[j].tool = transferLines[i];
                        transferLines[i].schedule[j].juice.schedule.Add(transferLines[i].schedule[j]);
                    }
                }
            }

            for (int i = 0; i < finished.Count; i++)
                finished[i].SortSchedule();

        }
    }
}
