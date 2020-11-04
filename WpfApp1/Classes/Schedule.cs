﻿using System;
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
        public List<Equipment> extras= new List<Equipment>();
        public List<Equipment> blendSystems= new List<Equipment>();
        public Equipment thawRoom;
        public List<Equipment> blendtanks= new List<Equipment>();
        public List<Equipment> transferLines = new List<Equipment>();

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

            // transfer lines and the sos
            getTransferLines();

            // blendtanks and their type is their sos
            getBlendTanks();

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
                    blendSystems.Add(temp);
                }
                conn.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            getBlendSystem_FuncSos();
            getExtras();
            getExtraSorted(); 

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
        public void getTransferLines()
        {
            int id_tl;
            String name_tl;
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();
                SqlCommand cmd = new SqlCommand();

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.CommandText = "[select_TransferLines]";

                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                da.Fill(dt);
                SqlDataReader rd = cmd.ExecuteReader();
                foreach (DataRow dr in dt.Rows)
                {
                    id_tl = Convert.ToInt32(dr["id"]);
                    name_tl = dr.Field<String>("Transfer Lines"); ;
                    Equipment temp = new Equipment(name_tl, id_tl);
                    for (int i = 0; i < numSOs + 1; i++)
                    {
                        temp.SOs.Add(false);
                    }
                    transferLines.Add(temp);
                }
                conn.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            getTransferSOs();
        }
        public void getTransferSOs()
        {
            try
            {
                int id_tl;
                int id_so;
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();
                SqlCommand cmd = new SqlCommand();

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.CommandText = "[select_TL_SO]";
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                da.Fill(dt);
                SqlDataReader rd = cmd.ExecuteReader();

                foreach (DataRow dr in dt.Rows)
                {
                    id_tl = Convert.ToInt32(dr["id_TL"]);
                    id_so = Convert.ToInt32(dr["id_SO"]);
                    for (int i = 0; i < transferLines.Count; i++)
                    {
                        if (transferLines[i].type == id_tl)
                        {
                            transferLines[i].SOs[id_so] = true;
                            break;
                        }
                    }
                }

                conn.Close();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void getBlendTanks()
        {
            int id_so;
            String name_mt;
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();
                SqlCommand cmd = new SqlCommand();

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.CommandText = "[select_MixTanks]";

                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                da.Fill(dt);
                SqlDataReader rd = cmd.ExecuteReader();
                foreach (DataRow dr in dt.Rows)
                {
                    id_so = Convert.ToInt32(dr["id_SO"]);
                    name_mt = dr.Field<String>("Mix Tank"); ;
                    Equipment temp = new Equipment(name_mt, id_so);

                    blendtanks.Add(temp);
                }
                conn.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void getBlendSystem_FuncSos()
        {
            try
            {
                int flag = 0;
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
                    for (int i = 0; i < blendSystems.Count; i++)
                    {
                        if (blendSystems[i].type == id_equip)
                        {
                            blendSystems[i].functionalities[id_func] = true;
                            blendSystems[i].SOs[id_so] = true;
                        }
                    }
                }
                Equipment blendmachine;
                for (int i = 0; i < blendSystems.Count; i++)
                {
                    flag = 0;
                    int x = 0;
                    String name_func = "";
                    for (int j = 0; j < blendSystems[i].functionalities.Count; j++)
                    {
                        if (blendSystems[i].functionalities[j] == true)
                        {
                            flag++;
                            x = j;
                            name_func = blendSystems[i].name;
                        }
                        if (flag == 2)
                        {
                            break;
                        }
                    }
                    if (flag == 1)
                    {
                        Equipment temp = new Equipment(name_func, x);
                        temp.SOs = blendSystems[i].SOs;
                        extras.Add(temp);
                        blendmachine = blendSystems[i];
                        blendSystems.Remove(blendmachine);
                    }

                }
                conn.Close();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        public void getExtras()
        {
            try
            {
                int count = 1;
                int id_func;
                String name_func; 
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();
                SqlCommand cmd = new SqlCommand();

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.CommandText = "[select_FuncEquip]";
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                da.Fill(dt);
                SqlDataReader rd = cmd.ExecuteReader();
                int sum = numSOs;

                foreach (DataRow dr in dt.Rows)
                {
                    if (dr["id_Equip"] == DBNull.Value)
                    {
                        if (dr.Field<String>("Functionality") != "Thaw Room")
                        {
                            name_func = dr.Field<String>("Functionality");
                            id_func = Convert.ToInt32(dr["id"]);
                            sum = numSOs;
                            while (numSOs + 1 != count)
                            {
                                Equipment e = new Equipment(name_func,id_func);
                                for (int y = 0; y < sum + 1; y++)
                                {
                                    if (count == y)
                                    {
                                        e.SOs.Add(true);
                                    }
                                    else
                                    {
                                        e.SOs.Add(false);
                                    }
                                }
                                extras.Add(e);
                                count++;
                            }
                            count = 1;
                        }
                    }
                }
                conn.Close();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void getExtraSorted()
        {
            Equipment temp;
            for (int i = 1; i < extras.Count; i++)
            {
                for (int j = i; j > 0; j--)
                {

                    if (extras[j - 1].type > extras[j].type)
                    {
                        temp = extras[j - 1];
                        extras[j - 1] = extras[j];
                        extras[j] = temp;
                    }
                    else
                    {
                        break;
                    }
                }
            }
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
                        if (!checkoffFunc[k] && x.recipes[i][k] > 0 && !blendSystems[j].functionalities[k])
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
                        AcquireTransferLine(inprogress[0], inprogress[0].currentFillTime.Subtract(new TimeSpan(0,inprogress[0].transferTime,0)), inprogress[0].BlendTank);

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
            List<Equipment> temp;
            for (int i = 1; i < options.Count; i++)
            {
                for (int j = i; j > 0; j--)
                {

                    if (options[j - 1].Count > options[j].Count)
                    {
                        temp = options[j - 1];
                        options[j - 1] = options[j];
                        options[j] = temp;
                    }
                    else
                    {
                        break;
                    }
                }
            }

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

        public void EnterScheduleLine(Equipment x, DateTime y, Juice z, int batch, TimeSpan q)
        {
            // mark x's schedule at time y for Juice z, batch for time span q
            // make sure to also apply the appropriate cleaning in between
        }

        public void ClaimMixTank(Equipment x, DateTime y, Juice z, int batch, int slurrySize)
        {
            // for when you need to mark a mix tank open ended, basically EnterScheduleLine
        }

        public void ReleaseMixTank(Equipment x, DateTime y)
        {
            // the other half of Claim Mix Tank
        }

    }
}
