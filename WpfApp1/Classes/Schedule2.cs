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

namespace WpfApp1
{
    public class Schedule2
    {
        public string message;
        public Equipment lateTool;

        public DateTime scheduleID;

        // all of the equipment in the system
        public List<Equipment> extras;          // tools with only one functionality, type == functionality, should be sorted by type
        public List<Equipment> systems;         // tools with multiple functionalities
        public Equipment thawRoom;              // the thaw room
        public List<Equipment> tanks;           // blend tanks/ mix tanks
        public List<Equipment> transferLines;   // transfer lines
        public List<Equipment> aseptics;        // aseptic/pasteurizers
        public List<Equipment> waters;
        public List<Equipment> sucroses;

        // each piece of equipment belongs to a cip group, only one piece of equipment in the group
        // can CIP at a time
        public List<Equipment> cipGroups;

        // info about the system
        public int numFunctions;
        public int numSOs;
        public TimeSpan CIPSpan;
        public int thawID;

        // lists of juices
        public List<Juice> finished;
        public List<Juice> inprogress;


        /// <summary>
        /// Creates a Schedule object, initializing lists of equipment
        /// </summary>
        public Schedule2(string filename)
        {
            scheduleID = DateTime.Now;
            extras = new List<Equipment>();
            systems = new List<Equipment>();
            tanks = new List<Equipment>();
            transferLines = new List<Equipment>();
            cipGroups = new List<Equipment>();
            finished = new List<Juice>();
            aseptics = new List<Equipment>();
            waters = new List<Equipment>();
            sucroses = new List<Equipment>();

            ProcessCSV(filename); 
        }

        //TODO: lines are converted, so line 1 is value 0, line 2 is value 1, line 3 is value 2, and line 7 is value 3
        //TODO: if string is empty then we should pop up an ERROR box
        /// <summary>
        /// Initializes juice orders from a CSV file as input.
        /// </summary>
        /// <param name="fileName"></param>
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

            int num_rows = lines.Count;
            List<Juice> cips = new List<Juice>();
            inprogress = new List<Juice>();

            //Get all the info for each "F_LINE" to make each juice needed
            for (int i = row_start; i < num_rows; i++)
            {
                if (lines[i][0] != "*" && lines[i][8].Contains("F_LINE"))
                {

                    string line_name = lines[i][8];
                    int line = Int32.Parse(line_name.Substring(line_name.Length - 1, 1));

                    // if it's not line 1,2,3,7, we can continue to the next line
                    if (!(line == 1 || line == 2 || line == 3 || line == 7))
                    {
                        continue;
                    }

                    string material = lines[i][2];

                    //Processing quantities to check if the juice is at it's ending stage
                    //int quantity_juice = int.Parse(lines[i][4], NumberStyles.AllowThousands);
                    //int quantity_juice_2 = int.Parse(lines[i][5], NumberStyles.AllowThousands);
                    //bool no_batches = quantity_juice <= quantity_juice_2;


                    string name = lines[i][3];

                    int type = name.Contains("CIP") ? -1 : getJuiceType(name);
                    //Console.WriteLine(name + " " + type);

                    string date = lines[i][0];
                    string seconds = lines[i][1];
                    string dateTime = date + " " + seconds;
                    DateTime fillTime = Convert.ToDateTime(dateTime);

                    //bool starterFlag = quantity_juice_2 != 0;

                    Dictionary<int, int> line_number_pair = new Dictionary<int, int>();
                    line_number_pair.Add(1, 0);
                    line_number_pair.Add(2, 1);
                    line_number_pair.Add(3, 2);
                    line_number_pair.Add(7, 3);

                    int line_num_use = line_number_pair[line];

                    Juice new_juice = new Juice(name, material, line_num_use, type, fillTime);

                    if (name.Contains("CIP"))
                    {
                        cips.Add(new_juice);
                    }
                    else
                    {
                        inprogress.Add(new_juice);
                    }
                }
            }

            for (int c = 0; c < cips.Count; c++)
            {
                inprogress.Add(cips[c]);
            }

            PullEquipment();
            numFunctions = numFunctions + 1;
            numSOs = numSOs + 1; 
            thawRoom = new Equipment("Thaw Room", thawID, 720);
            thawRoom.schedule = new List<ScheduleEntry>();
            thawRoom.functionalities = new List<bool>();
            for (int i = 0; i < numFunctions; i++)
                thawRoom.functionalities.Add(false);
            thawRoom.functionalities[thawID] = true;
            thawRoom.SOs = new List<bool>();
            for (int i = 0; i < numSOs; i++)
                thawRoom.SOs.Add(true);
            thawRoom.SOs[0] = false; 
        }

        /// <summary>
        /// Given a string material_name, goes through the pseudonym table to find the correct juice type, returns -2 if not found
        /// </summary>
        /// <param name="material_name"></param>
        /// <returns>juice type</returns>
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

            return -2;
        }

        /// <summary>
        /// Initializes all equipment in the system by pulling from the database
        /// </summary>
        private void PullEquipment()
        {
            getCipGroups();
            getThawRoomID();
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
                int cip = 0;

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
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        equip_type = Convert.ToInt32(dr["id"]);
                        equip_name = dr.Field<String>("Equipment");
                        if (dr["cip_id"] != DBNull.Value)
                        {
                            cip = Convert.ToInt32(dr["cip_id"]);
                        }

                        Equipment temp = new Equipment(equip_name, equip_type, 0);

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
                        if (cip > 0)
                        {
                            for (int k = 0; k < cipGroups.Count; k++)
                            {
                                if (cip == cipGroups[k].type)
                                {
                                    temp.cipGroup = cipGroups[k];
                                }

                            }
                        }
                        else
                        {
                            temp.cipGroup = null;
                        }
                        //temp.cip_id=cip; 
                        temp.cleaningProcess = 1;
                        temp.e_type = equip_type;
                        systems.Add(temp);
                    }
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
            getAseptics();
        }

        /// <summary>
        /// Initializes the CIP groups
        /// </summary>
        public void getCipGroups()
        {
            int id;
            String name;
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();
                SqlCommand cmd = new SqlCommand();

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.CommandText = "[select_CIPs]";
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                da.Fill(dt);
                SqlDataReader rd = cmd.ExecuteReader();
                foreach (DataRow dr in dt.Rows)
                {

                    id = Convert.ToInt32(dr["id_CIP"]);
                    name = dr.Field<String>("CIP");
                    Equipment temp = new Equipment(name, id, 0);
                    cipGroups.Add(temp);
                }

                conn.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Sets thawID, the functionality index associated with the thaw room
        /// </summary>
        public void getThawRoomID()
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();
                SqlCommand cmd = new SqlCommand();

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.CommandText = "[select_ThawRoom_Id]";
                cmd.Parameters.Add("thaw_name", SqlDbType.NVarChar).Value = "Thaw Room";
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                da.Fill(dt);
                SqlDataReader rd = cmd.ExecuteReader();

                if (dt.Rows.Count > 0)
                {
                    thawID = Convert.ToInt32(dt.Rows[0]["id"]);
                }
                conn.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Finds the number of SOs in the system plus one (indexed from 1)
        /// </summary>
        /// <returns>numSOs</returns>
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
                if (dt.Rows.Count > 0)
                {
                    s = Convert.ToInt32(dt.Rows[0]["id"]);
                }
                conn.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return s;
        }

        /// <summary>
        /// Finds the number of functiionalities in the system plus one (indexed from 1)
        /// </summary>
        /// <returns>numFunctions</returns>
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
                if (dt.Rows.Count > 0)
                {
                    numofFunctions = Convert.ToInt32(dt.Rows[0]["id"]);
                }
                conn.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }


            return numofFunctions;
        }

        /// <summary>
        /// Creates the Equipment objects for each transfer line
        /// </summary>
        public void getTransferLines()
        {
            int id_tl;
            String name_tl;
            int cip;
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
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        id_tl = Convert.ToInt32(dr["id"]);
                        name_tl = dr.Field<String>("Transfer Lines");
                        cip = Convert.ToInt32(dr["id_CIP"]);
                        Equipment temp = new Equipment(name_tl, id_tl, 0);
                        for (int i = 0; i < numSOs + 1; i++)
                        {
                            temp.SOs.Add(false);
                        }
                        for (int k = 0; k < cipGroups.Count; k++)
                        {
                            if (cip == cipGroups[k].type)
                            {
                                temp.cipGroup = cipGroups[k];
                            }

                        }
                        temp.so_type = 4;
                        temp.cleaningProcess = 3;
                        temp.e_type = id_tl;
                        transferLines.Add(temp);
                    }
                }
                conn.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            getTransferSOs();
        }

        /// <summary>
        /// Populates the SO list of a transfer line
        /// </summary>
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
                if (dt.Rows.Count > 0)
                {
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
                }
                /*
                Console.WriteLine("tl");
                for (int i=0; i<transferLines.Count; i++)
                {
                    Console.WriteLine(transferLines[i].name); 
                }
                */

                conn.Close();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Creates the Equipment objects for each blend tank
        /// </summary>
        public void getBlendTanks()
        {
            int id_so;
            String name_mt;
            int cip;
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
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        id_so = Convert.ToInt32(dr["id_SO"]);
                        name_mt = dr.Field<String>("Mix Tank");
                        cip = Convert.ToInt32(dr["id_CIP"]);
                        Equipment temp = new Equipment(name_mt, id_so, 0);
                        for (int k = 0; k < cipGroups.Count; k++)
                        {
                            if (cip == cipGroups[k].type)
                            {
                                temp.cipGroup = cipGroups[k];
                            }

                        }
                        temp.so_type = id_so;
                        temp.cleaningProcess = 2;
                        temp.e_type = 1;

                        tanks.Add(temp);
                    }
                }
                conn.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            /*
            Console.WriteLine("blendTanks");
            for (int i = 0; i < blendtanks.Count; i++)
            {
                Console.WriteLine(blendtanks[i].name);
            }
            */
        }

        /// <summary>
        /// Creates the Equipment objects for each system
        /// </summary>
        private void getBlendSystem_FuncSos()
        {
            try
            {
                int flag = 0;
                int id_equip;
                int id_func;
                int id_so;
                int count = 0;
                int checking = 0;
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
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        id_equip = Convert.ToInt32(dr["id_Equip"]);
                        id_func = Convert.ToInt32(dr["id_Func"]);
                        id_so = Convert.ToInt32(dr["id_SO"]);
                        for (int i = 0; i < systems.Count; i++)
                        {
                            if (systems[i].type == id_equip)
                            {
                                systems[i].functionalities[id_func] = true;
                                systems[i].SOs[id_so] = true;

                            }
                        }
                    }
                }
                Equipment blendmachine;
                for (int i = 0; i < systems.Count; i++)
                {
                    count = 0;
                    if (flag == 1 && i == 1)
                    {
                        i = 0;
                    }

                    flag = 0;
                    int x = 0;
                    String name_func = "";
                    for (int j = 0; j < systems[i].functionalities.Count; j++)
                    {
                        if (systems[i].functionalities[j] == true)
                        {
                            flag++;
                            x = j;
                            name_func = systems[i].name;
                        }
                        if (flag == 2)
                        {
                            break;
                        }
                    }
                    for (int k = 0; k < systems[i].SOs.Count; k++)
                    {
                        if (systems[i].SOs[k] == true)
                        {
                            count++;
                            checking = k;
                        }

                    }
                    if (count >= 2)
                    {
                        systems[i].so_type = 3;
                    }
                    else
                    {
                        systems[i].so_type = checking;
                    }
                    if (flag == 1)
                    {
                        Equipment temp = new Equipment(name_func, x, 0);
                        temp.SOs = systems[i].SOs;
                        temp.cleaningProcess = 1;
                        temp.cipGroup = systems[i].cipGroup;
                        temp.so_type = systems[i].so_type;
                        extras.Add(temp);
                        blendmachine = systems[i];
                        systems.Remove(blendmachine);
                        if (i != 0)
                        {
                            i = i - 1;
                        }

                        //blendSystems.RemoveAt(i); 

                    }

                }

                conn.Close();

            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        
        /// <summary>
        /// Creates Equipment objects for each extra, water line, and liquid sucrose line
        /// </summary>
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
                            if (name_func == "Water")
                            {
                                while (numSOs + 1 != count)
                                {
                                    Equipment e = new Equipment(name_func, id_func, 0);
                                    for (int y = 0; y < sum + 1; y++)
                                    {
                                        if (count == y)
                                        {
                                            e.SOs.Add(true);
                                            e.name = e.name + "(SO" + y + ")";
                                            e.so_type = y;

                                        }
                                        else
                                        {
                                            e.SOs.Add(false);
                                        }
                                    }
                                    e.cipGroup = null;
                                    e.cleaningProcess = 1;
                                    e.e_type = 0;
                                    waters.Add(e);
                                    count++;
                                }
                                count = 1;
                            }
                            else if (name_func == "Liquid Sucrose")
                            {
                                while (numSOs + 1 != count)
                                {
                                    Equipment e = new Equipment(name_func, id_func, 0);
                                    for (int y = 0; y < sum + 1; y++)
                                    {
                                        if (count == y)
                                        {
                                            e.SOs.Add(true);
                                            e.name = e.name + "(SO" + y + ")";
                                            e.so_type = y;

                                        }
                                        else
                                        {
                                            e.SOs.Add(false);
                                        }
                                    }
                                    e.cipGroup = null;
                                    e.cleaningProcess = 1;
                                    e.e_type = 0;
                                    sucroses.Add(e);
                                    count++;
                                }
                                count = 1;
                            }
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

        /// <summary>
        /// Sorts extras by type
        /// </summary>
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
            /*
            Console.WriteLine("extras"); 
            for (int i = 0; i < extras.Count; i++)
            {
                Console.WriteLine(extras[i].name);
            }
            */
        }

        /// <summary>
        /// Creates Equipment objects for each aseptic
        /// </summary>
        public void getAseptics()
        {
            int id_at;
            String name_at;
            int id;
            int cip;
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();
                SqlCommand cmd = new SqlCommand();

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.CommandText = "[select_Aseptics]";

                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();

                da.Fill(dt);
                SqlDataReader rd = cmd.ExecuteReader();
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {

                        id_at = Convert.ToInt32(dr["id"]);
                        name_at = dr.Field<String>("Aseptic Tank");
                        cip = Convert.ToInt32(dr["id_cip"]);
                        Equipment temp = new Equipment(name_at, id_at, 0);
                        for (int i = 0; i < numSOs + 1; i++)
                        {
                            if (i > 0)
                            {
                                temp.SOs.Add(true);
                            }
                            else
                            {
                                temp.SOs.Add(false);
                            }
                        }
                        //temp.cip_id=cip; 
                        for (int k = 0; k < cipGroups.Count; k++)
                        {
                            if (cip == cipGroups[k].type)
                            {
                                temp.cipGroup = cipGroups[k];
                            }

                        }
                        temp.cleaningProcess = 4;
                        temp.e_type = id_at;
                        temp.so_type = 5;
                        aseptics.Add(temp);
                    }
                }
                conn.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            /*
            for(int i=0; i<aseptics.Count; i++)
            {
                Console.WriteLine(aseptics[i].name);
                Console.WriteLine(aseptics[i].type); 
            }
            */
        }

        /// <summary>
        /// Creates a schedule for each piece of equipment and juice in the system if it can be done.
        /// Sets message to report results to user. If Late or Inconceivable, will not write a schedule
        ///     to the database.
        /// </summary>
        public void GenerateNewSchedule()
        {
            // sort inprogress juices so inprogress[0] is the juice with the earliest currentFillTime
            SortByFillTime();

            // while there are juices with batches left
            while (inprogress.Count != 0)
            {
                // the line is doing a CIP
                if (inprogress[0].type == -1)
                {
                    aseptics[inprogress[0].line].schedule.Add(new ScheduleEntry(inprogress[0].OGFillTime, inprogress[0].OGFillTime.Add(CIPSpan), inprogress[0], false, 0));
                    inprogress.RemoveAt(0);
                }
                // if the current batch is mixing at run time
                else if (inprogress[0].mixing)
                {
                    inprogress[0].mixing = false;

                    // you only have to acquire a transfer line
                    // get the time it's gonna start transferring at
                    DateTime done = AcquireTransferLineAndAseptic(inprogress[0].inline, inprogress[0], inprogress[0].mixingDoneBlending, inprogress[0].tank);

                    // if the transfer line is late, that's an error and we need to stop
                    if (DateTime.Compare(done.Add(inprogress[0].transferTime), inprogress[0].currentFillTime) > 0)
                    {
                        message = "Late because of " + lateTool.name + " for " + inprogress[0].name;
                        return;
                    }
                    // if the transfer line is down, that's an error and we need to stop
                    else if (DateTime.Compare(done, DateTime.MinValue) == 0)
                    {
                        message = "Inconceivable because of " + inprogress[0].name;
                        return;
                    }

                    // otherwise release the mix tank
                    ScheduleEntry.ReleaseMixTank(inprogress[0].tank.schedule, done.Add(inprogress[0].transferTime));

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
                    // a slurry is already made
                    if (inprogress[0].inline)
                    {
                        // you only need to acquire a transfer line
                        DateTime done = AcquireTransferLineAndAseptic(true, inprogress[0], inprogress[0].currentFillTime, inprogress[0].tank);

                        // if the transfer line is late, that's an error and we need to stop
                        if (DateTime.Compare(done, inprogress[0].currentFillTime) > 0)
                        {
                            message = "Late because of " + lateTool.name + " for " + inprogress[0].name;
                            return;
                        }
                        // if the transfer line is down, that's an error and we need to stop
                        else if (DateTime.Compare(done, DateTime.MinValue) == 0)
                        {
                            message = "Inconceivable because of " + inprogress[0].name;
                            return;
                        }

                        // update the batch counts
                        inprogress[0].neededBatches--;
                        inprogress[0].slurryBatches--;

                        // move to finished list or continue
                        if (inprogress[0].neededBatches == 0)
                        {
                            // mark the mix tank ended
                            ScheduleEntry.ReleaseMixTank(inprogress[0].tank.schedule, done.Add(inprogress[0].transferTime));

                            finished.Add(inprogress[0]);
                            inprogress.RemoveAt(0);
                        }
                        else
                        {
                            // end of slurry
                            if (inprogress[0].slurryBatches == 0)
                            {
                                inprogress[0].inline = false;

                                // mark the mix tank ended
                                ScheduleEntry.ReleaseMixTank(inprogress[0].tank.schedule, done.Add(inprogress[0].transferTime));
                            }

                            inprogress[0].RecalculateFillTime();
                            SortByFillTime();
                        }
                    }
                    else
                    {
                        // it wouldn't make sense to do inline for a single batch
                        // decide if you can do inline: can you finish the slurry for 2,3,4,or5 batches before the fill time?
                        if (inprogress[0].neededBatches != 1 && inprogress[0].inlineposs)
                        {
                            CompareRecipe pick = null;
                            int size = -1;
                            DateTime goTime = DateTime.MinValue;

                            // try all the slurry sizes
                            for (int i = 2; i < 5 && i <= inprogress[0].neededBatches; i++)
                            {
                                bool breaker = false;

                                // try all the inline recipes
                                for (int j = 0; j < inprogress[0].recipes.Count; j++)
                                {
                                    if (!inprogress[0].inlineflags[j])
                                        continue;

                                    // get info about recipe
                                    CompareRecipe test = PrepRecipe(inprogress[0], j, i);
                                    if (!test.conceivable || !test.onTime)
                                    {
                                        breaker = true;
                                        break;
                                    }

                                    if (pick == null || size < i || DateTime.Compare(goTime, test.startBlending) < 0)
                                    {
                                        pick = test;
                                        size = i;
                                        goTime = test.startBlending;
                                    }
                                }

                                if (breaker)
                                    break;
                            }

                            // inline was possible and a choice was made
                            if (pick != null)
                            {
                                pick.Actualize(thawRoom, inprogress[0]);

                                // set up for the next batch
                                inprogress[0].inline = true;
                                inprogress[0].tank = pick.tank;
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
                        bool onTime = false;
                        DateTime start = DateTime.MinValue;

                        CompareRecipe temp;

                        // for each recipe
                        for (int i = 0; i < inprogress[0].recipes.Count; i++)
                        {
                            // skip if it's inline
                            if (inprogress[0].inlineflags[i])
                                continue;

                            // get info about the recipe
                            temp = PrepRecipe(inprogress[0], i, 1);

                            // you can't do it, skip
                            if (!temp.conceivable)
                                continue;

                            // this is the first valid option or this option is ontime while the previous wasn't
                            if (choice == null || (!onTime && temp.onTime))
                            {
                                choice = temp;
                                onTime = temp.onTime;
                                start = temp.startBlending;
                            }
                            // if the previous was ontime and this one isn't skip
                            else if (onTime && !temp.onTime)
                            {
                                continue;
                            }
                            // both are late
                            else if (!onTime)
                            {
                                // the previous choice was less late
                                if (DateTime.Compare(start, temp.startBlending) < 0)
                                    continue;
                                // this choice is less late
                                else
                                {
                                    choice = temp;
                                    onTime = temp.onTime;
                                    start = temp.startBlending;
                                }
                            }
                            // both are ontime
                            else
                            {
                                // the previous choice was closer to goal
                                if (DateTime.Compare(start, temp.startBlending) > 0)
                                    continue;
                                // this choice is closer to goal
                                else
                                {
                                    choice = temp;
                                    onTime = temp.onTime;
                                    start = temp.startBlending;
                                }
                            }
                        }

                        // error no recipe works, not even late
                        if (choice == null)
                        {
                            message = "Inconceivable because of " + inprogress[0].name;
                            return;
                        }

                        // all our choices are late
                        if (!onTime)
                        {
                            lateTool = choice.lateMaker;
                            message = "Late because of " + lateTool.name + " for " + inprogress[0].name;
                            return;
                        }

                        // assign equipment
                        // all of the choices have been made and the times are in choice
                        choice.Actualize(thawRoom, inprogress[0]);

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

            message = "Schedule Generated. Refresh Homepage or Select View Schedules to see it sorted by Juices.";

            GrabJuiceSchedules();
            // add to database
            AddEquipmentToDatabase();
            AddJuicesToDatabase();
            insertingScheduleID();
        }

        /// <summary>
        /// Sorts inprogress by currentFillTime
        /// </summary>
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
                        if (inprogress[j - 1].currentFillTime > inprogress[j].currentFillTime)
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

        /// <summary>
        /// will find and schedule time for the juice in the tank to use a transfer line
        /// if the juice is inline, it will find time on transfer line 3
        /// otherwise, it will find time first on transfer line 1/2 which is restricted to one SO and then try four then three
        ///      to find a on time transfer line or the least late option
        /// if all transfer lines are down it will return DateTime.MinValue
        /// </summary>
        /// <param name="inline"></param>
        /// <param name="juice"></param>
        /// <param name="startTrans"></param>
        /// <param name="tank"></param>
        /// <returns>transfer time, either ontime or late</returns>
        public DateTime AcquireTransferLineAndAseptic(bool inline, Juice juice, DateTime startTrans, Equipment tank)
        {
            // go through list of transfer lines and pick the one that's best for juice at transferTime
            // then assign the juice to it

            // if the aseptic is down you can't do it
            if (aseptics[juice.line].down)
                return DateTime.MinValue;

            DateTime ago = aseptics[juice.line].FindTime(startTrans, juice.type, scheduleID);
            // if aseptic is late we need to stop
            if (DateTime.Compare(ago, startTrans) > 0)
            {
                lateTool = aseptics[juice.line];
                return ago;
            }

            int batch = juice.totalBatches - juice.neededBatches;

            // inline can only use transfer line 3
            if (inline)
            {
                // if transfer line 3 is down we can't do it
                if (transferLines[2].down)
                    return DateTime.MinValue;

                DateTime three = transferLines[2].FindTime(startTrans, juice.type, scheduleID);

                // if our only choice is late, we need to stop
                if (DateTime.Compare(three, startTrans) > 0)
                {
                    lateTool = transferLines[2];
                    return three;
                }

                if (transferLines[2].needsCleaned)
                    transferLines[2].schedule.Add(new ScheduleEntry(transferLines[2].cleanTime, transferLines[2].cleanTime.Add(transferLines[2].cleanLength), transferLines[2].cleanType, transferLines[2].cleanName));

                transferLines[2].schedule.Add(new ScheduleEntry(three, three.Add(juice.transferTime), juice, true, batch));

                // aseptic
                if (aseptics[juice.line].needsCleaned)
                    aseptics[juice.line].schedule.Add(new ScheduleEntry(aseptics[juice.line].cleanTime, aseptics[juice.line].cleanTime.Add(aseptics[juice.line].cleanLength), aseptics[juice.line].cleanType, aseptics[juice.line].cleanName));
                aseptics[juice.line].schedule.Add(new ScheduleEntry(three, three.Add(juice.transferTime), juice, true, batch));
                return three;
            }
            else
            {
                Equipment choice = null;
                DateTime start = DateTime.MinValue;

                // try transfer line 1 or two
                if (!transferLines[tank.type - 1].down)
                {
                    choice = transferLines[tank.type - 1];
                    start = transferLines[tank.type - 1].FindTime(startTrans, juice.type, scheduleID);
                }

                // try four if necessary
                if (!transferLines[3].down && (choice == null || DateTime.Compare(start, startTrans) > 0))
                {
                    DateTime temp = transferLines[3].FindTime(startTrans, juice.type, scheduleID);

                    if (choice == null || DateTime.Compare(temp, start) < 0)
                    {
                        choice = transferLines[3];
                        start = temp;
                    }
                }

                // try three if necessary
                if (!transferLines[2].down && (choice == null || DateTime.Compare(start, startTrans) > 0))
                {
                    DateTime temp = transferLines[2].FindTime(startTrans, juice.type, scheduleID);

                    if (choice == null || DateTime.Compare(temp, start) < 0)
                    {
                        choice = transferLines[2];
                        start = temp;
                    }
                }

                // if we couldn't find an option
                if (choice == null)
                    return DateTime.MinValue;

                // if our only choice is late, we need to stop
                if (DateTime.Compare(start, startTrans) > 0)
                {
                    lateTool = choice;
                    return start;
                }

                if (choice.needsCleaned)
                    choice.schedule.Add(new ScheduleEntry(choice.cleanTime, choice.cleanTime.Add(choice.cleanLength), choice.cleanType, choice.cleanName));
                choice.schedule.Add(new ScheduleEntry(start, start.Add(juice.transferTime), juice, false, batch));

                // aseptic
                if (aseptics[juice.line].needsCleaned)
                    aseptics[juice.line].schedule.Add(new ScheduleEntry(aseptics[juice.line].cleanTime, aseptics[juice.line].cleanTime.Add(aseptics[juice.line].cleanLength), aseptics[juice.line].cleanType, aseptics[juice.line].cleanName));
                aseptics[juice.line].schedule.Add(new ScheduleEntry(start, start.Add(juice.transferTime), juice, false, batch));

                return start;
            }
        }

        /// <summary>
        /// Will create a CompareRecipe object for this juice
        /// </summary>
        /// <param name="juice"></param>
        /// <param name="recipe"></param>
        /// <param name="slurrySize"></param>
        /// <returns></returns>
        public CompareRecipe PrepRecipe(Juice juice, int recipe, int slurrySize)
        {
            int additionalminutes = 0;
            for (int i = 0; i < juice.recipes[recipe].Count; i++)
            {
                additionalminutes += juice.recipes[recipe][i] * (slurrySize - 1);
                juice.recipes[recipe][i] *= slurrySize;
            }
            juice.idealTime[recipe] = juice.idealTime[recipe].Subtract(new TimeSpan(0, additionalminutes, 0));

            CompareRecipe option = new CompareRecipe();
            if (slurrySize == 1)
            {
                option.batch = juice.totalBatches - juice.neededBatches + 1;
                option.slurry = false;
            }
            else
            {
                option.slurry = true;
                option.batch = slurrySize;
            }
            bool pickedStartTime = false; // has option.startBlending been set
            bool[] checkoffFunc = new bool[numFunctions];
            checkoffFunc[waters[0].type] = true;
            checkoffFunc[sucroses[0].type] = true;
            bool[] soChoices = new bool[numSOs];
            for (int j = 1; j < numSOs; j++)
                soChoices[j] = true;

            // if the thaw room is needed
            if (juice.recipes[recipe][thawID] > 0)
            {
                // if the thaw room is down we can't do this recipe
                if (thawRoom.down)
                {
                    option.conceivable = false;
                    for (int z = 0; z < juice.recipes[recipe].Count; z++)
                        juice.recipes[recipe][z] /= slurrySize;
                    juice.idealTime[recipe] = juice.idealTime[recipe].Add(new TimeSpan(0, additionalminutes, 0));
                    return option;
                }

                option.thawLength = new TimeSpan(0, juice.recipes[recipe][thawID], 0);
                DateTime begin;

                // try to find an existing entry in the thaw room
                ScheduleEntry temp = thawRoom.FindEntry(juice, 1);
                if (temp != null)
                {
                    begin = temp.end;
                    option.thawTime = begin;

                    // check to see if the thaw room is ready in time
                    if (DateTime.Compare(begin, juice.idealTime[recipe].Add(new TimeSpan(0, juice.recipePreTimes[recipe], 0))) <= 0)
                    {
                        option.startBlending = juice.idealTime[recipe].Add(new TimeSpan(0, juice.recipePreTimes[recipe], 0));
                    }
                    // otherwise note
                    else
                    {
                        option.lateMaker = thawRoom;
                        option.startBlending = begin;
                    }
                }
                // no entry exists, try to make one
                else
                {
                    option.makeANewThawEntry = true;
                    begin = thawRoom.FindTimePopulated(juice.idealTime[recipe].Add(new TimeSpan(0, juice.recipePreTimes[recipe], 0)).Subtract(thawRoom.earlyLimit), option.thawLength);

                    // can we do it ontime?
                    if (DateTime.Compare(begin, juice.idealTime[recipe].Add(new TimeSpan(0, juice.recipePreTimes[recipe], 0))) <= 0)
                    {
                        option.thawTime = begin;
                        option.startBlending = juice.idealTime[recipe].Add(new TimeSpan(0, juice.recipePreTimes[recipe], 0));
                    }
                    // we'll just have to do it late
                    else
                    {
                        option.lateMaker = thawRoom;
                        option.thawTime = begin;
                        option.startBlending = begin.Add(option.thawLength);
                    }
                }

                pickedStartTime = true;
                checkoffFunc[thawID] = true;
            }

            // if any of the extras are needed
            for (int j = 0; j < juice.recipes[recipe].Count; j++)
            {
                if (juice.recipes[recipe][j] == 0 || checkoffFunc[j])
                    continue;

                FindExtraForType(j, juice, option, soChoices, juice.idealTime[recipe].Add(new TimeSpan(0, juice.recipePreTimes[recipe], 0)), new TimeSpan(0, juice.recipes[recipe][j], 0));

                // if it couldn't find an extra that it needed
                if (option.extras[option.extras.Count - 1] == null)
                {
                    option.extras.RemoveAt(option.extras.Count - 1);
                }
                else
                {
                    checkoffFunc[j] = true;
                    // put in a check for SOs because extra equipment can limit them
                    for (int i = 0; i < numSOs; i++)
                        if (soChoices[i] && !option.extras[option.extras.Count - 1].SOs[i])
                            soChoices[i] = false;
                }
            }

            // now find the extra with the latest start time and correct
            if (extras.Count != 0)
            {
                // find the latest, if thaw room was late, startBlending would reflect thaw time
                DateTime latest;
                if (pickedStartTime)
                    latest = option.startBlending;
                else
                    latest = juice.idealTime[recipe].Add(new TimeSpan(0, juice.recipePreTimes[recipe], 0));

                int idx = -1;

                for (int i = 0; i < option.extras.Count; i++)
                {
                    if (DateTime.Compare(latest, option.extraTimes[i]) < 0)
                    {
                        latest = option.extraTimes[i];
                        idx = i;
                    }
                }

                // now to update
                if (idx != -1)
                {
                    option.startBlending = latest;
                    pickedStartTime = true;

                    // note lateness
                    if (DateTime.Compare(option.startBlending, juice.idealTime[recipe].Add(new TimeSpan(0, juice.recipePreTimes[recipe], 0))) > 0)
                    {
                        option.lateMaker = extras[idx];
                    }
                }
            }

            // do you need a blend system?
            bool needBlendSys = false;
            for (int i = 0; i < numFunctions; i++)
                if (juice.recipes[recipe][i] > 0 && !checkoffFunc[i])
                    needBlendSys = true;

            if (needBlendSys)
            {
                int pick = -1;
                DateTime currentStart = DateTime.MinValue;
                TimeSpan length = TimeSpan.Zero;
                DateTime cStart = DateTime.MinValue;
                TimeSpan cLength = TimeSpan.Zero;
                string cName = "";
                int cType = -1;

                if (!systems[1].down && soChoices[2])
                {
                    // does it have all you need?
                    bool flag = true;
                    for (int i = 0; i < numFunctions; i++)
                        if (juice.recipes[recipe][i] > 0 && !checkoffFunc[i] && !systems[1].functionalities[i])
                            flag = false;
                    
                    if (flag)
                    {
                        pick = 1;

                        TimeSpan templength = new TimeSpan(0, 0, 0);
                        for (int i = 0; i < numFunctions; i++)
                            if (!checkoffFunc[i] && juice.recipes[recipe][i] > 0)
                                templength = templength.Add(new TimeSpan(0, juice.recipes[recipe][i], 0));
                        length = templength;

                        currentStart = systems[1].FindTime(juice.idealTime[recipe].Add(new TimeSpan(0, juice.recipePreTimes[recipe], 0)), juice.type, scheduleID);
                        cStart = systems[1].cleanTime;
                        cLength = systems[1].cleanLength;
                        cName = systems[1].cleanName;
                        cType = systems[1].cleanType;
                    }
                }

                if (!systems[0].down && soChoices[1])
                {
                    // does it have all you need?
                    bool flag = true;
                    for (int i = 0; i < numFunctions; i++)
                        if (juice.recipes[recipe][i] > 0 && !checkoffFunc[i] && !systems[0].functionalities[i])
                            flag = false;

                    if (flag)
                    {
                        TimeSpan templength = new TimeSpan(0, 0, 0);
                        for (int i = 0; i < numFunctions; i++)
                            if (!checkoffFunc[i] && juice.recipes[recipe][i] > 0)
                                templength = templength.Add(new TimeSpan(0, juice.recipes[recipe][i], 0));

                        DateTime tempStart = systems[0].FindTime(juice.idealTime[recipe].Add(new TimeSpan(0, juice.recipePreTimes[recipe], 0)), juice.type, scheduleID);
                        TimeSpan tempCleanLen = systems[0].cleanLength;
                        
                        if (pick == -1 || DateTime.Compare(currentStart, tempStart) > 0)
                        {
                            pick = 0;
                            length = templength;
                            currentStart = tempStart;
                            cStart = systems[0].cleanTime;
                            cLength = systems[0].cleanLength;
                            cName = systems[0].cleanName;
                            cType = systems[0].cleanType;
                        }
                    }
                }

                if (!systems[2].down)
                {
                    // does it have all you need?
                    bool flag = true;
                    for (int i = 0; i < numFunctions; i++)
                        if (juice.recipes[recipe][i] > 0 && !checkoffFunc[i] && !systems[2].functionalities[i])
                            flag = false;

                    if (flag)
                    {
                        TimeSpan templength = new TimeSpan(0, 0, 0);
                        for (int i = 0; i < numFunctions; i++)
                            if (!checkoffFunc[i] && juice.recipes[recipe][i] > 0)
                                templength = templength.Add(new TimeSpan(0, juice.recipes[recipe][i], 0));

                        DateTime tempStart = systems[2].FindTime(juice.idealTime[recipe].Add(new TimeSpan(0, juice.recipePreTimes[recipe], 0)), juice.type, scheduleID);
                        TimeSpan tempCleanLen = systems[2].cleanLength;

                        if (pick == -1 || DateTime.Compare(currentStart, tempStart) > 0)
                        {
                            pick = 2;
                            length = templength;
                            currentStart = tempStart;
                            cStart = systems[2].cleanTime;
                            cLength = systems[2].cleanLength;
                            cName = systems[2].cleanName;
                            cType = systems[2].cleanType;
                        }
                    }
                }

                // no system satisfied
                if (pick == -1)
                {
                    option.conceivable = false;
                    for (int z = 0; z < juice.recipes[recipe].Count; z++)
                        juice.recipes[recipe][z] /= slurrySize;
                    juice.idealTime[recipe] = juice.idealTime[recipe].Add(new TimeSpan(0, additionalminutes, 0));
                    return option;
                }

                // save info to option
                option.system = systems[pick];
                option.systemTime = currentStart;
                option.systemLength = length;
                option.systemCleaningStart = cStart;
                option.systemCleaningLength = cLength;
                option.systemCleaningType = cType;
                option.systemCleaningName = cName;

                // update metrics
                if (!pickedStartTime)
                {
                    option.startBlending = currentStart;
                    pickedStartTime = true;
                    if (DateTime.Compare(currentStart, juice.idealTime[recipe].Add(new TimeSpan(0, juice.recipePreTimes[recipe], 0))) > 0)
                    {
                        option.lateMaker = option.system;
                    }
                }
                // address lateness
                else if (DateTime.Compare(currentStart, option.startBlending) > 0)
                {
                    option.startBlending = currentStart;
                    option.lateMaker = option.system;
                }

                // update sos
                for (int k = 0; k < numSOs; k++)
                    if (soChoices[k] && !option.system.SOs[k])
                        soChoices[k] = false;
            }

            // assign a mix tank
            // need to calculate the span of time we need the mix tank for by finding the tool with the longest timespan
            TimeSpan mixtanktime = new TimeSpan(0, juice.recipePreTimes[recipe], 0);
            mixtanktime = mixtanktime.Add(new TimeSpan(0, juice.recipePostTimes[recipe], 0));
            bool set = false;
            TimeSpan longest = TimeSpan.Zero;

            // check extras
            if (option.extras.Count > 0)
            {
                for (int i = 0; i < option.extras.Count; i++)
                {
                    if (!set)
                    {
                        longest = option.extraLengths[i];
                        set = true;
                    }
                    else
                    {
                        if (TimeSpan.Compare(longest, option.extraLengths[i]) < 0)
                            longest = option.extraLengths[i];
                    }
                }
            }

            // check blend system
            if (needBlendSys)
            {
                if (set)
                {
                    if (TimeSpan.Compare(longest, option.systemLength) < 0)
                        longest = option.systemLength;
                }
                else
                    longest = option.systemLength;
            }

            mixtanktime = mixtanktime.Add(longest);
            mixtanktime = mixtanktime.Add(juice.transferTime);

            DateTime goal = option.startBlending.Subtract(new TimeSpan(0, juice.recipePreTimes[recipe], 0));

            // search through the mix tanks
            Equipment tank = null;
            DateTime start = DateTime.MinValue;
            DateTime cleanStart = DateTime.MinValue;
            TimeSpan cleanLength = TimeSpan.Zero;
            string cleanName = "";
            int cleanType = -1;

            for (int i = 0; i < tanks.Count; i++)
            {
                // we can't connect to it
                if (!soChoices[tanks[i].type])
                    continue;

                DateTime tempstart = tanks[i].FindTime(goal, juice.type, scheduleID);
                DateTime tempCleanStart = tanks[i].cleanTime;
                TimeSpan tempCleanLength = tanks[i].cleanLength;
                int tempCleanType = tanks[i].cleanType;
                string tempCleanName = tanks[i].cleanName;

                if (tank == null)
                {
                    // swap
                    tank = tanks[i];
                    start = tempstart;
                    cleanStart = tempCleanStart;
                    cleanLength = tempCleanLength;
                    cleanType = tempCleanType;
                    cleanName = tempCleanName;
                }
                // current is late
                else if (DateTime.Compare(start, juice.idealTime[recipe]) > 0)
                {
                    // new option is earlier than current
                    if (DateTime.Compare(tempstart, start) < 0)
                    {
                        // swap
                        tank = tanks[i];
                        start = tempstart;
                        cleanStart = tempCleanStart;
                        cleanLength = tempCleanLength;
                        cleanType = tempCleanType;
                        cleanName = tempCleanName;
                    }
                }
                // current and new option are both on time
                else if (DateTime.Compare(tempstart, juice.idealTime[recipe]) == 0)
                {
                    if (TimeSpan.Compare(cleanLength, tempCleanLength) > 0)
                    {
                        // swap
                        tank = tanks[i];
                        start = tempstart;
                        cleanStart = tempCleanStart;
                        cleanLength = tempCleanLength;
                        cleanType = tempCleanType;
                        cleanName = tempCleanName;
                    }
                }
            }

            // if you couldn't find a tank inconceivable
            if (tank == null)
            {
                option.conceivable = false; for (int z = 0; z < juice.recipes[recipe].Count; z++)
                    juice.recipes[recipe][z] /= slurrySize;
                juice.idealTime[recipe] = juice.idealTime[recipe].Add(new TimeSpan(0, additionalminutes, 0));

                return option;
            }

            // save info about tank
            option.tank = tank;
            option.tankTime = start;
            option.tankLength = mixtanktime;
            option.tankCleaningStart = cleanStart;
            option.tankCleaningLength = cleanLength;
            option.tankCleaningType = cleanType;
            option.tankCleaningName = cleanName;

            if (!pickedStartTime)
                option.startBlending = start;
            else if (DateTime.Compare(start, option.startBlending) > 0)
                option.startBlending = start;

            // check for lateness
            if (DateTime.Compare(start, juice.idealTime[recipe]) > 0)
            {
                option.onTime = false;
                option.lateMaker = tank;
            }

            // pick a water line
            if (juice.recipes[recipe][waters[0].type] > 0)
            {
                option.water = waters[option.tank.type - 1];
                option.waterTime = option.water.FindTime(option.tankTime.Add(new TimeSpan(0, juice.recipePreTimes[recipe], 0)), juice.type, scheduleID);
                option.waterLength = new TimeSpan(0, juice.recipes[recipe][waters[0].type], 0);
            }
            
            // pick a sucrose line
            if (juice.recipes[recipe][sucroses[0].type] > 0)
            {
                option.sucrose = sucroses[option.tank.type - 1];
                option.sucroseTime = option.sucrose.FindTime(option.tankTime.Add(new TimeSpan(0, juice.recipePreTimes[recipe], 0)), juice.type, scheduleID);
                option.sucroseLength = new TimeSpan(0, juice.recipes[recipe][sucroses[0].type], 0);
            }

            // assign a transfer line
            DateTime tgoal = option.tankTime.Add(option.tankLength).Subtract(juice.transferTime);
            Equipment choice = null;
            DateTime goTime = DateTime.MinValue;
            DateTime clSt = DateTime.MinValue;
            TimeSpan clL = TimeSpan.Zero;
            string clN = "";
            int cltype = -1;

            // try transfer line 1
            if (option.tank.type == 1 && !transferLines[0].down && slurrySize == 1)
            {
                choice = transferLines[0];
                goTime = transferLines[0].FindTime(tgoal, juice.type, scheduleID);
                clSt = transferLines[0].cleanTime;
                clL = transferLines[0].cleanLength;
                cltype = transferLines[0].cleanType;
                clN = transferLines[0].cleanName;
            }

            // try transfer line 2
            if (option.tank.type == 2 && !transferLines[1].down && slurrySize == 1)
            {
                choice = transferLines[1];
                goTime = transferLines[1].FindTime(tgoal, juice.type, scheduleID);
                clSt = transferLines[1].cleanTime;
                clL = transferLines[1].cleanLength;
                cltype = transferLines[1].cleanType;
                clN = transferLines[1].cleanName;
            }

            // try transfer line 4
            if (!transferLines[3].down && slurrySize == 1)
            {
                if (choice == null)
                {
                    choice = transferLines[3];
                    goTime = transferLines[3].FindTime(tgoal, juice.type, scheduleID);
                    clSt = transferLines[3].cleanTime;
                    clL = transferLines[3].cleanLength;
                    cltype = transferLines[3].cleanType;
                    clN = transferLines[3].cleanName;
                }
                else
                {
                    DateTime tempStart = transferLines[3].FindTime(tgoal, juice.type, scheduleID);
                    DateTime tempClSt = transferLines[3].cleanTime;
                    TimeSpan tempClL = transferLines[3].cleanLength;
                    int tempCltype = transferLines[3].cleanType;
                    string tempClN = transferLines[3].cleanName;

                    // transfer line 4 is ontime, transfer line 1/2 is not
                    if (DateTime.Compare(tempStart, goTime) < 0)
                    {
                        choice = transferLines[3];
                        goTime = tempStart;
                        clSt = tempClSt;
                        clL = tempClL;
                        cltype = tempCltype;
                        clN = tempClN;
                    }
                }
            }

            // try transfer line 3 last bc it's inline and you want to save it for that
            if (!transferLines[2].down)
            {
                if (choice == null)
                {
                    choice = transferLines[2];
                    goTime = transferLines[2].FindTime(tgoal, juice.type, scheduleID);
                    clSt = transferLines[2].cleanTime;
                    clL = transferLines[2].cleanLength;
                    cltype = transferLines[2].cleanType;
                    clN = transferLines[2].cleanName;
                }
                else
                {
                    DateTime tempStart = transferLines[2].FindTime(tgoal, juice.type, scheduleID);
                    DateTime tempClSt = transferLines[2].cleanTime;
                    TimeSpan tempClL = transferLines[2].cleanLength;
                    int tempCltype = transferLines[2].cleanType;
                    string tempClN = transferLines[2].cleanName;

                    // transfer line 2 is ontime, transfer line 1 is not
                    if (DateTime.Compare(tempStart, goTime) < 0)
                    {
                        choice = transferLines[2];
                        goTime = tempStart;
                        clSt = tempClSt;
                        clL = tempClL;
                        cltype = tempCltype;
                        clN = tempClN;
                    }
                }
            }

            // store transfer choice
            if (choice == null)
            {
                option.conceivable = false;
                for (int z = 0; z < juice.recipes[recipe].Count; z++)
                    juice.recipes[recipe][z] /= slurrySize;
                juice.idealTime[recipe] = juice.idealTime[recipe].Add(new TimeSpan(0, additionalminutes, 0));
                return option;
            }

            option.transferLine = choice;
            option.transferTime = goTime;
            option.transferLength = juice.transferTime;
            option.transferCleaningStart = clSt;
            option.transferCleaningLength = clL;
            option.transferCleaningType = cltype;
            option.transferCleaningName = clN;

            // aseptics
            option.aseptic = aseptics[juice.line];
            option.asepticTime = option.transferTime;
            option.asepticLength = option.transferLength;
            option.aseptic.FindTime(option.asepticTime, juice.type, scheduleID);
            option.asepticCleaningLength = option.aseptic.cleanLength;
            option.asepticCleaningName = option.aseptic.cleanName;
            option.asepticCleaningType = option.aseptic.cleanType;
            option.asepticCleaningStart = option.aseptic.cleanTime;

            // decide if it's onTime
            if (DateTime.Compare(juice.currentFillTime, option.transferTime) < 0)
                option.onTime = false;
            else
                option.onTime = true;

            if (DateTime.Compare(option.transferTime, option.startBlending) > 0 || option.lateMaker == null)
                option.lateMaker = option.transferLine;
            
            for (int z = 0; z < juice.recipes[recipe].Count; z++)
                juice.recipes[recipe][z] /= slurrySize;
            juice.idealTime[recipe] = juice.idealTime[recipe].Add(new TimeSpan(0, additionalminutes, 0));

            return option;
        }

        /// <summary>
        /// Will find an extra for the functionality extraType and add it's selection info to option
        /// if it can't find one, will add null to the end of option.extras
        /// </summary>
        /// <param name="extraType"></param>
        /// <param name="juice"></param>
        /// <param name="option"></param>
        /// <param name="sos"></param>
        /// <param name="goal"></param>
        /// <param name="length"></param>
        public void FindExtraForType(int extraType, Juice juice, CompareRecipe option, bool[] sos, DateTime goal, TimeSpan length)
        {
            Equipment choice = null;
            DateTime begin = DateTime.MinValue;
            DateTime startClean = DateTime.MinValue;
            TimeSpan cleanFor = TimeSpan.Zero;
            int cleaning = -1;

            // search through extras
            for (int j = 0; j < extras.Count; j++)
            {
                // we've gone through all the available extras of the type we want
                if (extras[j].type != extraType && choice != null)
                    break;
                // we haven't found extras of the type we want
                if (extras[j].type != extraType)
                    continue;

                // check if we can connect to this tool
                bool flag = false;
                for (int i = 0; i < sos.Length; i++)
                    if (extras[j].SOs[i] && sos[i])
                        flag = true;
                if (!flag)
                    continue;

                // get a start time
                DateTime tempbegin = extras[j].FindTime(goal, juice.type, scheduleID);

                // we haven't made a choice yet
                if (choice == null)
                {
                    choice = extras[j];
                    begin = tempbegin;
                    startClean = extras[j].cleanTime;
                    cleanFor = extras[j].cleanLength;
                    cleaning = extras[j].cleanType;
                }
                // we're late
                else if (DateTime.Compare(tempbegin, goal) > 0)
                {
                    // the other choice is later
                    if (DateTime.Compare(begin, tempbegin) > 0)
                    {
                        choice = extras[j];
                        begin = tempbegin;
                        startClean = extras[j].cleanTime;
                        cleanFor = extras[j].cleanLength;
                        cleaning = extras[j].cleanType;
                    }
                }
                // we're both on time
                else if (DateTime.Compare(tempbegin, begin) == 0)
                {
                    // we take less time to clean
                    if (TimeSpan.Compare(extras[j].cleanLength, cleanFor) < 0)
                    {
                        choice = extras[j];
                        begin = tempbegin;
                        startClean = extras[j].cleanTime;
                        cleanFor = extras[j].cleanLength;
                        cleaning = extras[j].cleanType;
                    }
                }
            }

            // save choice info
            if (choice == null)
            {
                option.extras.Add(null);
                return;
            }

            option.extras.Add(choice);
            option.extraTimes.Add(begin);
            option.extraLengths.Add(length);
            option.extraCleaningStarts.Add(startClean);
            option.extraCleaningLengths.Add(cleanFor);
            option.extraCleaningTypes.Add(cleaning);
        }

        /// <summary>
        /// Extrapolates the juice schedules from the equipment schedules. Check finished for a list of juices with sorted schedules
        /// </summary>
        public void GrabJuiceSchedules()
        {
            // goes through thawRoom, extras, blendSystems, blendtanks, transferLines, aseptics their schedules
            // and for juice entrys do <pieceofequipment>.schedule[i].juice.schedule.Add(<pieceofequipment>.schedule[i]);
            // then run through finished and sort each juice's scheduled

            // work through thaw room schedule
            for (int i = 0; i < thawRoom.schedule.Count; i++)
            {
                // if the entry isn't for a juice in our system
                if (thawRoom.schedule[i].juice.type == -1 || thawRoom.schedule[i].userGen)
                    continue;

                thawRoom.schedule[i].tool = thawRoom;
                thawRoom.schedule[i].juice.schedule.Add(thawRoom.schedule[i]);
            }

            // work through extras
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

            // work through systems
            for (int i = 0; i < systems.Count; i++)
            {
                for (int j = 0; j < systems[i].schedule.Count; j++)
                {
                    if (!systems[i].schedule[j].cleaning)
                    {
                        systems[i].schedule[j].tool = systems[i];
                        systems[i].schedule[j].juice.schedule.Add(systems[i].schedule[j]);
                    }
                }
            }

            // work through mix tanks
            for (int i = 0; i < tanks.Count; i++)
            {
                for (int j = 0; j < tanks[i].schedule.Count; j++)
                {
                    if (!tanks[i].schedule[j].cleaning)
                    {
                        tanks[i].schedule[j].tool = tanks[i];
                        tanks[i].schedule[j].juice.schedule.Add(tanks[i].schedule[j]);
                    }
                }
            }

            // work through transferlines
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
                ScheduleEntry.SortSchedule(finished[i].schedule);
        }

        /// <summary>
        /// Add's all the equipment schedules to the database
        /// </summary>
        public void AddEquipmentToDatabase()
        {
            // thaw room
            if (thawRoom.schedule.Count != 0)
            {
                for (int i = 0; i < thawRoom.schedule.Count; i++)
                    insertingEquipSchedule(3, thawRoom.name, thawRoom.schedule[i].start, thawRoom.schedule[i].end, thawRoom.schedule[i].juice.name, thawRoom.schedule[i].slurry, thawRoom.schedule[i].batch);
            }

            // extras
            for (int i = 0; i < extras.Count; i++)
            {
                if (extras[i].schedule.Count != 0)
                {
                    for (int j = 0; j < extras[i].schedule.Count; j++)
                    {
                        if (extras[i].schedule[j].cleaning)
                            insertingEquipSchedule(extras[i].so_type, extras[i].name, extras[i].schedule[j].start, extras[i].schedule[j].end, extras[i].schedule[j].cleaningname, extras[i].schedule[j].slurry, extras[i].schedule[j].batch);
                        else
                            insertingEquipSchedule(extras[i].so_type, extras[i].name, extras[i].schedule[j].start, extras[i].schedule[j].end, extras[i].schedule[j].juice.name, extras[i].schedule[j].slurry, extras[i].schedule[j].batch);
                    }
                }
            }

            // systems
            for (int i = 0; i < systems.Count; i++)
            {
                if (systems[i].schedule.Count != 0)
                {
                    for (int j = 0; j < systems[i].schedule.Count; j++)
                    {
                        if (systems[i].schedule[j].cleaning)
                            insertingEquipSchedule(systems[i].so_type, systems[i].name, systems[i].schedule[j].start, systems[i].schedule[j].end, systems[i].schedule[j].cleaningname, systems[i].schedule[j].slurry, systems[i].schedule[j].batch);
                        else
                            insertingEquipSchedule(systems[i].so_type, systems[i].name, systems[i].schedule[j].start, systems[i].schedule[j].end, systems[i].schedule[j].juice.name, systems[i].schedule[j].slurry, systems[i].schedule[j].batch);
                    }
                }
            }

            // tanks
            for (int i = 0; i < tanks.Count; i++)
            {
                if (tanks[i].schedule.Count != 0)
                {
                    for (int j = 0; j < tanks[i].schedule.Count; j++)
                    {
                        if (tanks[i].schedule[j].cleaning)
                            insertingEquipSchedule(tanks[i].so_type, tanks[i].name, tanks[i].schedule[j].start, tanks[i].schedule[j].end, tanks[i].schedule[j].cleaningname, tanks[i].schedule[j].slurry, tanks[i].schedule[j].batch);
                        else
                            insertingEquipSchedule(tanks[i].so_type, tanks[i].name, tanks[i].schedule[j].start, tanks[i].schedule[j].end, tanks[i].schedule[j].juice.name, tanks[i].schedule[j].slurry, tanks[i].schedule[j].batch);
                    }
                }
            }

            // transfer lines
            for (int i = 0; i < transferLines.Count; i++)
            {
                if (transferLines[i].schedule.Count != 0)
                {
                    for (int j = 0; j < transferLines[i].schedule.Count; j++)
                    {
                        if (transferLines[i].schedule[j].cleaning)
                            insertingEquipSchedule(transferLines[i].so_type, transferLines[i].name, transferLines[i].schedule[j].start, transferLines[i].schedule[j].end, transferLines[i].schedule[j].cleaningname, transferLines[i].schedule[j].slurry, transferLines[i].schedule[j].batch);
                        else
                            insertingEquipSchedule(transferLines[i].so_type, transferLines[i].name, transferLines[i].schedule[j].start, transferLines[i].schedule[j].end, transferLines[i].schedule[j].juice.name, transferLines[i].schedule[j].slurry, transferLines[i].schedule[j].batch);
                    }
                }
            }

            // aseptics
            for (int i = 0; i < aseptics.Count; i++)
            {
                if (aseptics[i].schedule.Count != 0)
                {
                    for (int j = 0; j < aseptics[i].schedule.Count; j++)
                    {
                        if (aseptics[i].schedule[j].cleaning)
                            insertingEquipSchedule(aseptics[i].so_type, aseptics[i].name, aseptics[i].schedule[j].start, aseptics[i].schedule[j].end, aseptics[i].schedule[j].cleaningname, aseptics[i].schedule[j].slurry, aseptics[i].schedule[j].batch);
                        else
                            insertingEquipSchedule(aseptics[i].so_type, aseptics[i].name, aseptics[i].schedule[j].start, aseptics[i].schedule[j].end, aseptics[i].schedule[j].juice.name, aseptics[i].schedule[j].slurry, aseptics[i].schedule[j].batch);
                    }
                }
            }
        }

        /// <summary>
        /// Actually puts the schedule in the database
        /// </summary>
        /// <param name="id_so"></param>
        /// <param name="equipname"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="juice"></param>
        /// <param name="slurry"></param>
        /// <param name="batch"></param>
        public void insertingEquipSchedule(int id_so, String equipname, DateTime start, DateTime end, String juice, Boolean slurry, int batch)
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[insert_ProdSch]";
                cmd.Parameters.Add("scheduleID", SqlDbType.DateTime).Value = scheduleID;
                cmd.Parameters.Add("id_so", SqlDbType.Int).Value = id_so;
                cmd.Parameters.Add("equipname", SqlDbType.VarChar).Value = equipname;
                cmd.Parameters.Add("start", SqlDbType.DateTime).Value = start;
                cmd.Parameters.Add("end", SqlDbType.DateTime).Value = end;

                if (slurry == true)
                {
                    String adding = Convert.ToString(batch);
                    juice += " (slurry) " + adding;
                }
                cmd.Parameters.Add("juice", SqlDbType.VarChar).Value = juice;

                cmd.Connection = conn;

                cmd.ExecuteNonQuery();
                conn.Close();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// add's all the juice schedules to the database
        /// </summary>
        public void AddJuicesToDatabase()
        {
            for (int i = 0; i < finished.Count; i++)
            {
                if (finished[i].schedule.Count != 0)
                {
                    for (int j = 0; j < finished[i].schedule.Count; j++)
                    {
                        insertingJuiceSchedule(finished[i].name, finished[i].type, finished[i].schedule[j].slurry, finished[i].schedule[j].batch, finished[i].schedule[j].tool.name, finished[i].schedule[j].start, finished[i].schedule[j].end);
                    }
                }
            }
        }

        /// <summary>
        /// Actually puts the schedule in the database
        /// </summary>
        /// <param name="juice"></param>
        /// <param name="juice_type"></param>
        /// <param name="slurry"></param>
        /// <param name="batch"></param>
        /// <param name="equipname"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        public void insertingJuiceSchedule(String juice, int juice_type, Boolean slurry, int batch, String equipname, DateTime start, DateTime end)
        {

            //for equip_type that should return the name
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("scheduleID", SqlDbType.DateTime).Value = scheduleID;
                cmd.CommandText = "[insert_JuiceSch]";
                if (slurry == true)
                {
                    String adding = Convert.ToString(batch);
                    juice += " (slurry) " + adding;
                }
                cmd.Parameters.Add("juice", SqlDbType.VarChar).Value = juice;
                cmd.Parameters.Add("juicetype", SqlDbType.BigInt).Value = juice_type;
                cmd.Parameters.Add("start", SqlDbType.DateTime).Value = start;
                cmd.Parameters.Add("end", SqlDbType.DateTime).Value = end;
                cmd.Parameters.Add("equipname", SqlDbType.VarChar).Value = equipname;

                cmd.Connection = conn;

                cmd.ExecuteNonQuery();
                conn.Close();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        /// <summary>
        /// Add's schedule ID to a schedule entry
        /// </summary>
        public void insertingScheduleID()
        {
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[insert_ScheduleID]";
                cmd.Parameters.Add("scheduleID", SqlDbType.DateTime).Value = scheduleID;

                cmd.Connection = conn;

                cmd.ExecuteNonQuery();
                conn.Close();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }

}

/*
 * THIS IS AN EXAMPLE OF A SCHEDULE, IT WON'T RUN BECAUSE WE GOT RID OF SOME OF THE FUNCTIONS IT CALLS, BUT YOU CAN USE IT TO UNDERSTAND THE SYSTEM
private void ExampleOfSchedule()
{
    String checkname;
    int i = 1;
    List<Equipment> equips = new List<Equipment>();

    //SO1
    Equipment mix1_so1 = new Equipment("Mix Tank 1");
    mix1_so1.so = 1;
    mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 01:45:00"), Convert.ToDateTime("02/19/2020 05:15:00"), new Juice("Simply Grapefruit")));
    mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 05:45:00"), Convert.ToDateTime("02/19/2020 09:15:00"), new Juice("Simply Grapefruit")));
    mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 09:45:00"), Convert.ToDateTime("02/19/2020 10:15:00"), new Juice("Rinse")));
    mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 10:30:00"), Convert.ToDateTime("02/19/2020 14:30:00"), new Juice("Lemonade Rasberry")));
    mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 14:30:00"), Convert.ToDateTime("02/19/2020 14:30:00"), new Juice("Lemonade Rasberry")));
    mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 18:30:00"), Convert.ToDateTime("02/19/2020 18:30:00"), new Juice("Lemonade Rasberry")));
    equips.Add(mix1_so1);

    Equipment mix2_so1 = new Equipment("Mix Tank 2");
    mix2_so1.so = 1;
    mix2_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 03:45:00"), Convert.ToDateTime("02/19/2020 07:15:00"), new Juice("Simply Grapefruit")));
    mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 07:15:00"), Convert.ToDateTime("02/19/2020 10:20:00"), new Juice("7 Step Hot Clean")));
    mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 16:00:00"), Convert.ToDateTime("02/19/2020 20:30:00"), new Juice("Honest Black Tea Peach Apricot")));
    equips.Add(mix2_so1);

    Equipment mix3_so1 = new Equipment("Mix Tank 3");
    mix3_so1.so = 1;
    mix3_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 12:00:00"), Convert.ToDateTime("02/19/2020 16:30:00"), new Juice("Honest Black Tea Peach Apricot")));
    equips.Add(mix3_so1);

    Equipment mix4_so1 = new Equipment("Mix Tank 4");
    mix4_so1.so = 1;
    mix4_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 14:00:00"), Convert.ToDateTime("02/19/2020 18:30:00"), new Juice("Honest Black Tea Peach Apricot")));
    equips.Add(mix4_so1);

    Equipment water_so1 = new Equipment("Water");
    water_so1.so = 1;
    water_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 11:00:00"), Convert.ToDateTime("02/19/2020 11:30:00"), new Juice("Lemonade Rasberry")));
    water_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 12:30:00"), Convert.ToDateTime("02/19/2020 13:00:00"), new Juice("Honest Black Tea Peach Apricot")));
    water_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 14:30:00"), Convert.ToDateTime("02/19/2020 15:00:00"), new Juice("Honest Black Tea Peach Apricot")));
    water_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 15:00:00"), Convert.ToDateTime("02/19/2020 15:30:00"), new Juice("Lemonade Rasberry")));
    water_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 16:30:00"), Convert.ToDateTime("02/19/2020 17:00:00"), new Juice("Honest Black Tea Peach Apricot")));
    water_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 19:00:00"), Convert.ToDateTime("02/19/2020 19:30:00"), new Juice("Lemonade Rasberry")));
    equips.Add(water_so1);

    Equipment sucrose_so1 = new Equipment("Sucrose");
    sucrose_so1.so = 1;
    sucrose_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 11:00:00"), Convert.ToDateTime("02/19/2020 11:30:00"), new Juice("Lemonade Rasberry")));
    sucrose_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 15:00:00"), Convert.ToDateTime("02/19/2020 15:30:00"), new Juice("Lemonade Rasberry")));
    sucrose_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 19:00:00"), Convert.ToDateTime("02/19/2020 19:30:00"), new Juice("Lemonade Rasberry")));
    equips.Add(sucrose_so1);

    //SO2
    Equipment mix1_so2 = new Equipment("Mix Tank 1");
    mix1_so2.so = 2;
    mix1_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 00:15:00"), Convert.ToDateTime("02/19/2020 22:00:00"), new Juice("Simply Orange Juice")));
    equips.Add(mix1_so2);

    Equipment mix2_so2 = new Equipment("Mix Tank 2");
    mix2_so2.so = 2;
    mix2_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 09:15:00"), Convert.ToDateTime("02/19/2020 12:45:00"), new Juice("Peach")));
    mix2_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 13:15:00"), Convert.ToDateTime("02/19/2020 16:45:00"), new Juice("Peach")));
    equips.Add(mix2_so2);

    Equipment mix3_so2 = new Equipment("Mix Tank 3");
    mix3_so2.so = 2;
    mix3_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 11:15:00"), Convert.ToDateTime("02/19/2020 14:45:00"), new Juice("Peach")));
    mix3_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 15:15:00"), Convert.ToDateTime("02/19/2020 18:45:00"), new Juice("Peach")));
    equips.Add(mix3_so2);

    Equipment mix4_so2 = new Equipment("Mix Tank 4");
    mix4_so2.so = 2;
    mix4_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 12:30:00"), Convert.ToDateTime("02/19/2020 16:30:00"), new Juice("Lemonade Rasberry")));
    mix4_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 16:30:00"), Convert.ToDateTime("02/19/2020 20:30:00"), new Juice("Lemonade Rasberry")));
    equips.Add(mix4_so2);

    Equipment water_so2 = new Equipment("Water");
    water_so2.so = 2;
    water_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 9:45:00"), Convert.ToDateTime("02/19/2020 10:15:00"), new Juice("Peach")));
    water_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 11:45:00"), Convert.ToDateTime("02/19/2020 12:15:00"), new Juice("Peach")));
    water_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 13:00:00"), Convert.ToDateTime("02/19/2020 13:30:00"), new Juice("Lemonade Rasberry")));
    water_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 13:45:00"), Convert.ToDateTime("02/19/2020 14:15:00"), new Juice("Peach")));
    water_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 15:45:00"), Convert.ToDateTime("02/19/2020 16:15:00"), new Juice("Peach")));
    water_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 17:00:00"), Convert.ToDateTime("02/19/2020 17:30:00"), new Juice("Lemonade Rasberry")));
    equips.Add(water_so2);

    Equipment sucrose_so2 = new Equipment("Sucrose");
    sucrose_so2.so = 2;
    sucrose_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 09:45:00"), Convert.ToDateTime("02/19/2020 10:15:00"), new Juice("Peach")));
    sucrose_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 11:45:00"), Convert.ToDateTime("02/19/2020 12:15:00"), new Juice("Peach")));
    sucrose_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 13:00:00"), Convert.ToDateTime("02/19/2020 13:30:00"), new Juice("Lemonade Rasberry")));
    sucrose_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 13:45:00"), Convert.ToDateTime("02/19/2020 14:15:00"), new Juice("Peach")));
    sucrose_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 15:45:00"), Convert.ToDateTime("02/19/2020 16:15:00"), new Juice("Peach")));
    sucrose_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 17:00:00"), Convert.ToDateTime("02/19/2020 17:30:00"), new Juice("Lemonade Rasberry")));
    equips.Add(sucrose_so2);

    Equipment soft_melt_chopper = new Equipment("Soft Melt Chopper");
    soft_melt_chopper.so = 2;
    equips.Add(soft_melt_chopper);

    Equipment tote_system = new Equipment("Tote System");
    tote_system.so = 3;
    equips.Add(tote_system);

    Equipment rt_tank = new Equipment("RT Tank");
    rt_tank.so = 3;
    equips.Add(rt_tank);

    Equipment thaw_room = new Equipment("Thaw Room");
    thaw_room.so = 3;
    thaw_room.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/18/2020 10:04:00"), Convert.ToDateTime("02/18/2020 10:29:00"), new Juice("Peach")));
    thaw_room.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/18/2020 17:07:00"), Convert.ToDateTime("02/18/2020 17:32:00"), new Juice("Peach")));
    thaw_room.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/18/2020 17:32:00"), Convert.ToDateTime("02/18/2020 17:57:00"), new Juice("Peach")));
    thaw_room.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/18/2020 17:57:00"), Convert.ToDateTime("02/18/2020 17:22:00"), new Juice("Peach")));
    equips.Add(thaw_room);

    Equipment so1_blend_system = new Equipment("SO1 Blend System");
    so1_blend_system.so = 1;
    so1_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 02:15:00"), Convert.ToDateTime("02/19/2020 02:40:00"), new Juice("Simply Grapefruit")));
    so1_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 04:15:00"), Convert.ToDateTime("02/19/2020 04:40:00"), new Juice("Simply Grapefruit")));
    so1_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 06:15:00"), Convert.ToDateTime("02/19/2020 06:40:00"), new Juice("Simply Grapefruit")));
    so1_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 06:40:00"), Convert.ToDateTime("02/19/2020 10:00:00"), new Juice("7 Step Hot Clean")));
    so1_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 12:30:00"), Convert.ToDateTime("02/19/2020 14:00:00"), new Juice("Honest Black Tea Peach Apricot")));
    so1_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 14:30:00"), Convert.ToDateTime("02/19/2020 16:00:00"), new Juice("Honest Black Tea Peach Apricot")));
    so1_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 16:30:00"), Convert.ToDateTime("02/19/2020 18:00:00"), new Juice("Honest Black Tea Peach Apricot")));
    equips.Add(so1_blend_system);

    Equipment so2_blend_system = new Equipment("SO2 Blend System");
    so2_blend_system.so = 2;
    so2_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 00:45:00"), Convert.ToDateTime("02/19/2020 02:00:00"), new Juice("Simply Orange Juice")));
    so2_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 02:00:00"), Convert.ToDateTime("02/19/2020 02:25:00"), new Juice("Rinse")));
    so2_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 09:45:00"), Convert.ToDateTime("02/19/2020 10:15:00"), new Juice("Peach")));
    so2_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 11:45:00"), Convert.ToDateTime("02/19/2020 12:15:00"), new Juice("Peach")));
    so2_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 13:45:00"), Convert.ToDateTime("02/19/2020 14:15:00"), new Juice("Peach")));
    so2_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 15:45:00"), Convert.ToDateTime("02/19/2020 16:15:00"), new Juice("Peach")));
    equips.Add(so2_blend_system);

    Equipment so3_blend_system = new Equipment("SO3 Blend System");
    so3_blend_system.so = 3;
    so3_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 11:00:00"), Convert.ToDateTime("02/19/2020 12:00:00"), new Juice("Lemonade Rasberry")));
    so3_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 13:00:00"), Convert.ToDateTime("02/19/2020 14:00:00"), new Juice("Lemonade Rasberry")));
    so3_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 15:00:00"), Convert.ToDateTime("02/19/2020 16:00:00"), new Juice("Lemonade Rasberry")));
    so3_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 17:00:00"), Convert.ToDateTime("02/19/2020 18:00:00"), new Juice("Lemonade Rasberry")));
    so3_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 19:00:00"), Convert.ToDateTime("02/19/2020 20:00:00"), new Juice("Lemonade Rasberry")));
    equips.Add(so3_blend_system);

    Equipment tl_1 = new Equipment("TL 1");
    tl_1.so = 4;
    tl_1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 03:15:00"), Convert.ToDateTime("02/19/2020 05:15:00"), new Juice("Simply Grapefruit")));
    tl_1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 05:15:00"), Convert.ToDateTime("02/19/2020 07:15:00"), new Juice("Simply Grapefruit")));
    tl_1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 07:15:00"), Convert.ToDateTime("02/19/2020 09:15:00"), new Juice("Simply Grapefruit")));
    tl_1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 09:15:00"), Convert.ToDateTime("02/19/2020 11:00:00"), new Juice("7 Step Hot Clean")));
    tl_1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 14:30:00"), Convert.ToDateTime("02/19/2020 16:30:00"), new Juice("Honest Black Tea Peach Apricot")));
    tl_1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 16:30:00"), Convert.ToDateTime("02/19/2020 18:30:00"), new Juice("Honest Black Tea Peach Apricot")));
    tl_1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 18:30:00"), Convert.ToDateTime("02/19/2020 20:30:00"), new Juice("Honest Black Tea Peach Apricot")));
    equips.Add(tl_1);

    Equipment tl_2 = new Equipment("TL 2");
    tl_2.so = 4;
    tl_2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 10:45:00"), Convert.ToDateTime("02/19/2020 12:45:00"), new Juice("Peach")));
    tl_2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 12:45:00"), Convert.ToDateTime("02/19/2020 14:45:00"), new Juice("Peach")));
    tl_2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 14:45:00"), Convert.ToDateTime("02/19/2020 16:45:00"), new Juice("Peach")));
    tl_2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 16:45:00"), Convert.ToDateTime("02/19/2020 18:45:00"), new Juice("Peach")));
    equips.Add(tl_2);

    Equipment tl_3_inline = new Equipment("TL 3 INLINE");
    tl_3_inline.so = 4;
    tl_3_inline.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 02:30:00"), Convert.ToDateTime("02/19/2020 04:30:00"), new Juice("Simply Orange Juice")));
    tl_3_inline.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 04:30:00"), Convert.ToDateTime("02/19/2020 06:30:00"), new Juice("Simply Orange Juice")));
    tl_3_inline.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 20:00:00"), Convert.ToDateTime("02/19/2020 22:00:00"), new Juice("Simply Orange Juice")));
    equips.Add(tl_3_inline);

    Equipment tl_4 = new Equipment("TL 4");
    tl_4.so = 4;
    tl_4.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 12:30:00"), Convert.ToDateTime("02/19/2020 14:30:00"), new Juice("Lemonade Rasberry")));
    tl_4.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 14:30:00"), Convert.ToDateTime("02/19/2020 16:30:00"), new Juice("Lemonade Rasberry")));
    tl_4.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 16:30:00"), Convert.ToDateTime("02/19/2020 18:30:00"), new Juice("Lemonade Rasberry")));
    tl_4.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 18:30:00"), Convert.ToDateTime("02/19/2020 20:30:00"), new Juice("Lemonade Rasberry")));
    tl_4.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 20:30:00"), Convert.ToDateTime("02/19/2020 22:30:00"), new Juice("Lemonade Rasberry")));
    equips.Add(tl_4);

    Equipment aseptic_1 = new Equipment("Aseptic 1");
    aseptic_1.so = 5;
    aseptic_1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 14:30:00"), Convert.ToDateTime("02/19/2020 20:30:00"), new Juice("Honest Black Tea Peach Apricot")));
    aseptic_1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 21:00:00"), Convert.ToDateTime("02/20/2020 03:00:00"), new Juice("CIP")));
    equips.Add(aseptic_1);

    Equipment aseptic_2 = new Equipment("Aseptic 2");
    aseptic_2.so = 5;
    aseptic_2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 10:45:00"), Convert.ToDateTime("02/19/2020 18:45:00"), new Juice("Peach")));
    equips.Add(aseptic_2);

    Equipment aseptic_3 = new Equipment("Aseptic 3");
    aseptic_3.so = 5;
    aseptic_3.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 03:15:00"), Convert.ToDateTime("02/19/2020 09:15:00"), new Juice("Simply Grapefruit")));
    aseptic_3.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 09:15:00"), Convert.ToDateTime("02/19/2020 09:25:00"), new Juice("Rinse")));
    aseptic_3.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 12:30:00"), Convert.ToDateTime("02/19/2020 22:30:00"), new Juice("Lemonade Rasberry")));
    equips.Add(aseptic_3);

    Equipment aseptic_7 = new Equipment("Aseptic 7");
    aseptic_7.so = 5;
    aseptic_7.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 02:30:00"), Convert.ToDateTime("02/19/2020 06:30:00"), new Juice("Simply Orange Juice")));
    aseptic_7.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 20:00:00"), Convert.ToDateTime("02/19/2020 22:00:00"), new Juice("Simply Orange Juice")));
    equips.Add(aseptic_7);

    for (int e = 0; e < equips.Count; e++)
    {
        string equipment_name = equips[e].name;
        List<ScheduleEntry> schedule = equips[e].schedule;
        int x = equips[e].so;

        //go through each schedule entry in the equipment's schedule
        for (int s = 0; s < schedule.Count; s++)
        {
            DateTime startTime = schedule[s].start;
            DateTime endTime = schedule[s].end;
            string juice_name = schedule[s].juice.name;
            //checkname = checkProductionSchedule(x, equipment_name, startTime, endTime);
            //if (checkname != juice_name)
            //{
            insertingEquipSchedule(x, equipment_name, startTime, endTime, juice_name);
            //.....
            // }
        }
    }
} */