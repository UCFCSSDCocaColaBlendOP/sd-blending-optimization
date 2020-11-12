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
    class Schedule2
    {
        private List<Equipment> extras;
        private List<Equipment> blendSystems;
        private Equipment thawRoom;
        private List<Equipment> blendtanks;
        private List<Equipment> transferLines;

        // TODO - make sure all global variables are intialized when they have to be.. make it's supposed to be in a function and not the constructor
        // TODO - (time permitting) make variables that are needed private
        // TODO :: function to extrapolate juice schedule from equipment schedules
        private List<Equipment> machines;
        public int numFunctions; // TODO: need to fill this in as soon as possible to use it in the rest of the code (1)
        public int numSOs;

        private List<Juice> finished;
        private List<Juice> inprogress;// this is "juices" i went through and changed all references to "juices" even in commented out sections
        private List<Juice> juices_line8;
        public DateTime scheduleID;

        
        public Schedule2(string filename)
        {
            this.scheduleID = DateTime.Now;

            this.machines = new List<Equipment>();
            this.blendtanks = new List<Equipment>();
            this.transferLines = new List<Equipment>();
            this.numFunctions = 10; // TODO: need to change this (1)
            this.finished = new List<Juice>();
            this.inprogress = new List<Juice>();
            this.juices_line8 = new List<Juice>();
            this.blendSystems = new List<Equipment>();
            this.extras = new List<Equipment>(); 
            
            //ExampleOfSchedule();
            //ExampleOfSchedule2(); 
            //ProcessCSV(filename);
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
                    temp.cleaningProcess = 1;
                    temp.e_type = equip_type; 
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
            /*
            Console.WriteLine("blendsystem");
            for (int i = 0; i < blendSystems.Count; i++)
            {
                Console.WriteLine(blendSystems[i].name);
            }
            */
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
                    temp.cleaningProcess =3;
                    temp.e_type = id_tl; 
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
                    name_mt = dr.Field<String>("Mix Tank");
                    Equipment temp = new Equipment(name_mt, id_so);
                    temp.cleaningProcess = 2;
                    temp.e_type = 1; 
                    blendtanks.Add(temp);
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
                    if(flag==1 && i==1)
                    {
                        i = 0;
                    }

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
                        temp.cleaningProcess = 1;
                        extras.Add(temp);
                        blendmachine = blendSystems[i];

                        blendSystems.Remove(blendmachine);
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
                                Equipment e = new Equipment(name_func, id_func);
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
                                e.cleaningProcess = 1;
                                e.e_type = 0; 
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
            /*
            Console.WriteLine("extras"); 
            for (int i = 0; i < extras.Count; i++)
            {
                Console.WriteLine(extras[i].name);
            }
            */
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

        private int GetSOs(Equipment tool, bool[] sosavail)
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
            
            if (schedule.Count == 0)
            {
                schedule.Add(new ScheduleEntry(startTime, startTime.Add(timeSpan), j));
            }
            else
            {
                int index_insert = 0;
                for (int i = 0; i < schedule.Count; i++)
                {
                    if (schedule[i].start > startTime)
                    {
                        index_insert = i;
                        break;
                    }
                }

                //Deal with cleaning
                if (x.type == 8) //might need to change this to 0
                {
                    schedule.Insert(index_insert, new ScheduleEntry(startTime, startTime.Add(timeSpan), j));
                }
                else
                {
                    //find cleaning string

                    int flag = 0; 
                    int juice1 = j.type;
                    if (index_insert != 0)
                    {
                        int juice2 = schedule[index_insert - 1].juice.type;
                        int process=0;
                       
                        int cleaningTimes =0;
                        String cleaning="";
                        if (juice1 != juice2)
                        {
                            try
                            {
                                SqlConnection conn = new SqlConnection();
                                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                                conn.Open();

                                SqlCommand cmd = new SqlCommand();
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.CommandText = "[select_Flavor_Process]";
                                cmd.Parameters.Add("juice1_type", SqlDbType.BigInt).Value = juice1;
                                cmd.Parameters.Add("juice2_type", SqlDbType.BigInt).Value = juice2;

                                cmd.Connection = conn;

                                SqlDataAdapter da = new SqlDataAdapter(cmd);
                                DataTable dt = new DataTable();
                                da.Fill(dt);

                                process = Convert.ToInt32(dt.Rows[0]["process_id"]);
                                cleaning = Convert.ToString(dt.Rows[0]["process"]);
                                
                                //Console.WriteLine(process);
                                //Console.WriteLine(cleaning);
                                if(process!=0)
                                {
                                    flag = 1; 
                                }
                                  
                                conn.Close();      
                            }

                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.ToString());
                            }

                            // go to the table and get cleaning time
                            // for mix tanks that the blendtank list, is there a way to 
                            if (flag == 1)
                            {
                                if (x.e_type != 0)
                                {
                                    if (x.cleaningProcess == 1)
                                    {
                                        cleaningTimes = getEquipCleaningTimes(x.e_type, process);
                                        if (cleaningTimes != 0)
                                        {
                                            TimeSpan q = TimeSpan.FromMinutes(cleaningTimes);
                                            schedule.Insert(index_insert, new ScheduleEntry(startTime.Subtract(q), startTime, cleaning));
                                            //schedule.Insert(index_insert, new ScheduleEntry(startTime, startTime.Add(timeSpan), j));
                                        }
                                    }
                                    else if (x.cleaningProcess == 2)
                                    {
                                        cleaningTimes = getMixTanksCleaningTimes(x.e_type, process);
                                        if (cleaningTimes != 0)
                                        {
                                            TimeSpan q = TimeSpan.FromMinutes(cleaningTimes);
                                            schedule.Insert(index_insert, new ScheduleEntry(startTime.Subtract(q), startTime, cleaning));
                                        }
                                    }
                                    else if (x.cleaningProcess == 3)
                                    {
                                        cleaningTimes = getTLCleaningTimes(x.e_type, process);
                                        if (cleaningTimes != 0)
                                        {
                                            TimeSpan q = TimeSpan.FromMinutes(cleaningTimes);
                                            schedule.Insert(index_insert, new ScheduleEntry(startTime.Subtract(q), startTime, cleaning));
                                        }

                                    }
                                    /*
                                    // Aseptic Tanks
                                    else if (x.cleaningProcess == 4)
                                    {

                                        cleaningTimes = getATCleaningTimes(x.cleaningProcess, process);
                                        if (cleaningTimes != 0)
                                        {
                                            TimeSpan q = TimeSpan.FromMinutes(cleaningTimes);
                                            schedule.Insert(index_insert, new ScheduleEntry(startTime.Subtract(q), startTime, cleaning));
                                        }
                                    }
                                    */
                                    //how long the cleaning will take
                                    //TimeSpan q = 0; 
                                    //schedule.Insert(index_insert, new ScheduleEntry(startTime.Subtract(q), startTime, cleaning));
                                    //schedule.Insert(index_insert, new ScheduleEntry(startTime, startTime.Add(timeSpan), j));

                                }
                            }
                        }
                    }
                    schedule.Insert(index_insert, new ScheduleEntry(startTime, startTime.Add(timeSpan), j));
                }
            }
        }

        /*
        // get Asceptic Cleaning Times
        private int getATCleaningTimes(int equipType, int process)
        {
            // get the cleaning time and return
            //  set public cip
            int time=0; 
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_ATCleaningType]";
                cmd.Parameters.Add("processID", SqlDbType.BigInt).Value = process;
                cmd.Parameters.Add("equipType", SqlDbType.BigInt).Value = equipType;
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                time = Convert.ToInt32(dt.Rows[0]["time"]);
                //id_cip = Convert.ToInt32(dt.Rows[0]["cip_id"]);
                conn.Close();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            return time; 

        }
        */
        private int getMixTanksCleaningTimes(int equipType, int process)
        {
            int time = 0;
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_MTCleaningType]";
                cmd.Parameters.Add("processID", SqlDbType.BigInt).Value = process;
                cmd.Parameters.Add("equipType", SqlDbType.BigInt).Value = equipType;
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                time = Convert.ToInt32(dt.Rows[0]["time"]);
                //id_cip = Convert.ToInt32(dt.Rows[0]["cip_id"]);
                conn.Close();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            return time;
        }
        private int getTLCleaningTimes(int equipType,int process)
        {
            int time = 0;
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_TLCleaningType]";
                cmd.Parameters.Add("processID", SqlDbType.BigInt).Value = process;
                cmd.Parameters.Add("equipType", SqlDbType.BigInt).Value = equipType;
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                time = Convert.ToInt32(dt.Rows[0]["time"]);
                //id_cip = Convert.ToInt32(dt.Rows[0]["cip_id"]);
                conn.Close();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            return time;
        }

        private int getEquipCleaningTimes(int equipType, int process)
        {
            int time = 0;
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_EquipCleaningType]";
                cmd.Parameters.Add("processID", SqlDbType.BigInt).Value = process;
                cmd.Parameters.Add("equipType", SqlDbType.BigInt).Value = equipType;
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                time = Convert.ToInt32(dt.Rows[0]["time"]);
                //id_cip = Convert.ToInt32(dt.Rows[0]["cip_id"]);
                conn.Close();
            }

            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }

            return time;
        }

        private void ClaimMixTank(Equipment x, DateTime y, Juice z, int batch, int slurrySize)
        {
            // for when you need to mark a mix tank open ended, basically EnterScheduleLine
        }

        private void ReleaseMixTank(Equipment x, DateTime y)
        {
            // the other half of Claim Mix Tank
        }

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
        }

        public void insertingEquipSchedule(int id_so, String equipname, DateTime start, DateTime end, String juice)
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
        /*
        public String checkProductionSchedule(int so, String equipname, DateTime start, DateTime end)
        {
           
            String name = "0";
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[check_ProdSch]";
                cmd.Parameters.Add("so_id", SqlDbType.BigInt).Value = so;
                cmd.Parameters.Add("equipname", SqlDbType.VarChar).Value =equipname;
                cmd.Parameters.Add("start", SqlDbType.DateTime).Value = start;
                cmd.Parameters.Add("end", SqlDbType.DateTime).Value = end;
                //cmd.Parameters.Add("entryid", SqlDbType.VarChar).Value = entry;
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);
                foreach (DataRow dr in dt.Rows)
                {
                    name= dr.Field<String>("Juice");
                }
                conn.Close();
            } 
           

            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
            }
            return name; 
        }
        */

        private void ExampleOfSchedule2()
        {
            List<Equipment> equips = new List<Equipment>();
            //SO1
            Equipment mix1_so1 = new Equipment("Mix Tank 1");
            mix1_so1.so = 1;
            mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 22:30:00"), Convert.ToDateTime("02/19/2020 23:00:00"), new Juice("Rinse")));
            mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 02:15:00"), Convert.ToDateTime("02/20/2020 06:30:00"), new Juice("Lemonade Strawberry")));
            mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 08:15:00"), Convert.ToDateTime("02/20/2020 12:30:00"), new Juice("Lemonade Strawberry")));
            equips.Add(mix1_so1);

            Equipment mix2_so1 = new Equipment("Mix Tank 2");
            mix2_so1.so = 1;
            mix2_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 20:30:00"), Convert.ToDateTime("02/19/2020 07:15:00"), new Juice("Rinse")));
            mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 04:15:00"), Convert.ToDateTime("02/20/2020 08:45:00"), new Juice("Honest Green Tea Jasmine Honey")));
            mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 08:45:00"), Convert.ToDateTime("02/20/2020 11:45:00"), new Juice("7 Step Hot Clean")));
            mix1_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 15:10:00"), Convert.ToDateTime("02/20/2020 19:00:00"), new Juice("Lemonade Blueberry")));
            equips.Add(mix2_so1);

            Equipment mix3_so1 = new Equipment("Mix Tank 3");
            mix2_so1.so = 1;
            mix3_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 16:30:00"), Convert.ToDateTime("02/19/2020 17:00:00"), new Juice("Rinse")));
            mix3_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 00:15:00"), Convert.ToDateTime("02/20/2020 04:45:00"), new Juice("Honest Green Tea Jasmine Honey")));
            mix3_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 04:45:00"), Convert.ToDateTime("02/20/2020 07:55:00"), new Juice("7 Step Hot Clean")));
            mix3_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 17:10:00"), Convert.ToDateTime("02/20/2020 21:00:00"), new Juice("Lemonade Blueberry")));
            equips.Add(mix3_so1);

            Equipment mix4_so1 = new Equipment("Mix Tank 4");
            mix4_so1.so = 1;
            mix4_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 18:30:00"), Convert.ToDateTime("02/19/2020 19:00:00"), new Juice("Rinse")));
            mix4_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 18:30:00"), Convert.ToDateTime("02/19/2020 19:00:00"), new Juice("Honest Green Tea Jasmine Honey")));
            equips.Add(mix4_so1);

            Equipment water_so1 = new Equipment("Water");
            water_so1.so = 1;
            water_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 00:45:00"), Convert.ToDateTime("02/20/2020 01:15:00"), new Juice("Honest Green Tea Jasmine Honey")));
            water_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 02:45:00"), Convert.ToDateTime("02/20/2020 03:15:00"), new Juice("Honest Green Tea Jasmine Honey")));
            water_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 03:15:00"), Convert.ToDateTime("02/20/2020 03:45:00"), new Juice("Lemonade Strawberry")));
            water_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 04:45:00"), Convert.ToDateTime("02/20/2020 05:15:00"), new Juice("Honest Green Tea Jasmine Honey")));
            water_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 08:45:00"), Convert.ToDateTime("02/20/2020 09:15:00"), new Juice("Lemonade Strawberry")));
            water_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 15:40:00"), Convert.ToDateTime("02/20/2020 16:10:00"), new Juice("Lemonade Blueberry")));
            water_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 17:40:00"), Convert.ToDateTime("02/20/2020 18:10:00"), new Juice("Lemonade Blueberry")));
            equips.Add(water_so1);

            Equipment sucrose_so1 = new Equipment("Sucrose");
            sucrose_so1.so = 1;
            sucrose_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 03:15:00"), Convert.ToDateTime("02/20/2020 03:45:00"), new Juice("Lemonade Strawberry")));
            sucrose_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 08:45:00"), Convert.ToDateTime("02/20/2020 09:15:00"), new Juice("Lemonade Strawberry")));
            sucrose_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 15:40:00"), Convert.ToDateTime("02/20/2020 16:10:00"), new Juice("Lemonade Blueberry")));
            sucrose_so1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 17:40:00"), Convert.ToDateTime("02/20/2020 18:10:00"), new Juice("Lemonade Blueberry")));
            equips.Add(sucrose_so1);

            //SO2
            Equipment mix1_so2 = new Equipment("Mix Tank 1");
            mix1_so2.so = 2;
            mix1_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 22:00:00"), Convert.ToDateTime("02/19/2020 22:20:00"), new Juice("Rinse")));
            mix1_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 00:45:00"), Convert.ToDateTime("02/20/2020 04:15:00"), new Juice("Watermelon")));
            mix1_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 04:15:00"), Convert.ToDateTime("02/20/2020 06:35:00"), new Juice("3 Step Hot Clean")));
            mix1_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 08:15:00"), Convert.ToDateTime("02/20/2020 11:45:00"), new Juice("Fruit Punch")));
            mix1_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 12:15:00"), Convert.ToDateTime("02/20/2020 15:45:00"), new Juice("Fruit Punch")));
            mix1_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 16:15:00"), Convert.ToDateTime("02/20/2020 19:45:00"), new Juice("Fruit Punch")));
            equips.Add(mix1_so2);

            Equipment mix2_so2 = new Equipment("Mix Tank 2");
            mix2_so2.so = 2;
            mix2_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 16:45:00"), Convert.ToDateTime("02/19/2020 17:05:00"), new Juice("Rinse")));
            mix2_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 02:45:00"), Convert.ToDateTime("02/20/2020 06:15:00"), new Juice("Watermelon")));
            mix2_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 06:15:00"), Convert.ToDateTime("02/20/2020 08:35:00"), new Juice("3 Step Hot Clean")));
            mix2_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 10:15:00"), Convert.ToDateTime("02/20/2020 13:45:00"), new Juice("Fruit Punch")));
            mix2_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 14:15:00"), Convert.ToDateTime("02/20/2020 17:45:00"), new Juice("Fruit Punch")));
            equips.Add(mix2_so2);

            Equipment mix3_so2 = new Equipment("Mix Tank 3");
            mix3_so2.so = 2;
            mix3_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 18:45:00"), Convert.ToDateTime("02/19/2020 19:05:00"), new Juice("Rinse")));
            mix3_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 04:15:00"), Convert.ToDateTime("02/20/2020 08:30:00"), new Juice("Lemonade Strawberry")));
            mix3_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 08:30:00"), Convert.ToDateTime("02/20/2020 08:50:00"), new Juice("Rinse")));
            mix3_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 21:25:00"), Convert.ToDateTime("02/21/2020 03:15:00"), new Juice("Simply Oranje Juice")));
            equips.Add(mix3_so2);

            Equipment mix4_so2 = new Equipment("Mix Tank 4");
            mix4_so2.so = 2;
            mix4_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 20:30:00"), Convert.ToDateTime("02/19/2020 20:50:00"), new Juice("Rinse")));
            mix4_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 06:15:00"), Convert.ToDateTime("02/20/2020 10:30:00"), new Juice("Lemonade Strawberry")));
            mix4_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 10:30:00"), Convert.ToDateTime("02/20/2020 10:50:00"), new Juice("Rinse")));
            mix4_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 18:30:00"), Convert.ToDateTime("02/20/2020 20:30:00"), new Juice("Simply Apple")));
            equips.Add(mix4_so2);

            Equipment water_so2 = new Equipment("Water");
            water_so2.so = 2;
            water_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 01:15:00"), Convert.ToDateTime("02/19/2020 01:45:00"), new Juice("Watermelon")));
            water_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 03:15:00"), Convert.ToDateTime("02/19/2020 03:45:00"), new Juice("Watermelon")));
            water_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 04:45:00"), Convert.ToDateTime("02/19/2020 05:15:00"), new Juice("Lemonade Strawberry")));
            water_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 06:45:00"), Convert.ToDateTime("02/19/2020 07:15:00"), new Juice("Lemonade Strawberry")));
            water_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 08:45:00"), Convert.ToDateTime("02/19/2020 09:15:00"), new Juice("Fruit Punch")));
            water_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 10:45:00"), Convert.ToDateTime("02/19/2020 11:15:00"), new Juice("Fruit Punch")));
            water_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 12:45:00"), Convert.ToDateTime("02/19/2020 13:15:00"), new Juice("Fruit Punch")));
            water_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 14:45:00"), Convert.ToDateTime("02/19/2020 15:15:00"), new Juice("Fruit Punch")));
            water_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 16:45:00"), Convert.ToDateTime("02/19/2020 17:15:00"), new Juice("Fruit Punch")));
            equips.Add(water_so2);

            Equipment sucrose_so2 = new Equipment("Sucrose");
            sucrose_so2.so = 2;
            sucrose_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 01:15:00"), Convert.ToDateTime("02/20/2020 01:45:00"), new Juice("Watermelon")));
            sucrose_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 03:15:00"), Convert.ToDateTime("02/20/2020 03:45:00"), new Juice("Watermelon")));
            sucrose_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 04:45:00"), Convert.ToDateTime("02/20/2020 05:15:00"), new Juice("Lemonade Strawberry")));
            sucrose_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 06:45:00"), Convert.ToDateTime("02/20/2020 07:15:00"), new Juice("Lemonade Strawberry")));
            sucrose_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 08:45:00"), Convert.ToDateTime("02/20/2020 09:15:00"), new Juice("Fruit Punch")));
            sucrose_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 10:45:00"), Convert.ToDateTime("02/20/2020 11:15:00"), new Juice("Fruit Punch")));
            sucrose_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 12:45:00"), Convert.ToDateTime("02/20/2020 13:15:00"), new Juice("Fruit Punch")));
            sucrose_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 14:45:00"), Convert.ToDateTime("02/20/2020 15:15:00"), new Juice("Fruit Punch")));
            sucrose_so2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 16:45:00"), Convert.ToDateTime("02/20/2020 17:15:00"), new Juice("Fruit Punch")));
            equips.Add(sucrose_so2);

            Equipment soft_melt_chopper = new Equipment("Soft Melt Chopper");
            soft_melt_chopper.so = 2;
            equips.Add(soft_melt_chopper);

            Equipment tote_system = new Equipment("Tote System");
            tote_system.so = 3;
            tote_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 08:45:00"), Convert.ToDateTime("02/20/2020 09:20:00"), new Juice("Fruit Punch")));
            tote_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 10:45:00"), Convert.ToDateTime("02/20/2020 11:20:00"), new Juice("Fruit Punch")));
            tote_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 12:45:00"), Convert.ToDateTime("02/20/2020 13:20:00"), new Juice("Fruit Punch")));
            tote_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 14:45:00"), Convert.ToDateTime("02/20/2020 15:20:00"), new Juice("Fruit Punch")));
            tote_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 16:45:00"), Convert.ToDateTime("02/20/2020 17:20:00"), new Juice("Fruit Punch")));
            equips.Add(tote_system);

            Equipment rt_tank = new Equipment("RT Tank");
            rt_tank.so = 3;
            equips.Add(rt_tank);

            Equipment thaw_room = new Equipment("Thaw Room");
            thaw_room.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 08:03:00"), Convert.ToDateTime("02/19/2020 08:19:00"), new Juice("Watermelon")));
            thaw_room.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 08:19:00"), Convert.ToDateTime("02/19/2020 08:35:00"), new Juice("Watermelon")));
            thaw_room.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 17:25:00"), Convert.ToDateTime("02/19/2020 17:33:00"), new Juice("Fruit Punch")));
            thaw_room.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 17:33:00"), Convert.ToDateTime("02/19/2020 17:41:00"), new Juice("Fruit Punch")));
            thaw_room.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 17:41:00"), Convert.ToDateTime("02/19/2020 17:49:00"), new Juice("Fruit Punch")));
            thaw_room.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 17:49:00"), Convert.ToDateTime("02/19/2020 17:57:00"), new Juice("Fruit Punch")));
            thaw_room.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 17:57:00"), Convert.ToDateTime("02/19/2020 18:05:00"), new Juice("Fruit Punch")));
            equips.Add(thaw_room);

            Equipment so1_blend_system = new Equipment("SO1 Blend System");
            so1_blend_system.so = 1;
            so1_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 18:00:00"), Convert.ToDateTime("02/19/2020 02:40:00"), new Juice("Rinse")));
            so1_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 00:45:00"), Convert.ToDateTime("02/20/2020 02:15:00"), new Juice("Honest Green Tea Jasmine Honey")));
            so1_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 02:45:00"), Convert.ToDateTime("02/20/2020 04:15:00"), new Juice("Honest Green Tea Jasmine Honey")));
            so1_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 04:45:00"), Convert.ToDateTime("02/20/2020 06:15:00"), new Juice("Honest Green Tea Jasmine Honey")));
            equips.Add(so1_blend_system);

            Equipment so2_blend_system = new Equipment("SO2 Blend System");
            so2_blend_system.so = 2;
            so2_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 18:00:00"), Convert.ToDateTime("02/19/2020 18:40:00"), new Juice("Rinse")));
            so2_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 01:15:00"), Convert.ToDateTime("02/20/2020 01:40:00"), new Juice("Watermelon")));
            so2_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 03:15:00"), Convert.ToDateTime("02/20/2020 03:40:00"), new Juice("Watermelon")));
            so2_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 03:40:00"), Convert.ToDateTime("02/20/2020 05:15:00"), new Juice("3 Step Hot Clean")));
            so2_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 08:45:00"), Convert.ToDateTime("02/20/2020 09:20:00"), new Juice("Fruit Punch")));
            so2_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 10:45:00"), Convert.ToDateTime("02/20/2020 11:20:00"), new Juice("Fruit Punch")));
            so2_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 12:45:00"), Convert.ToDateTime("02/20/2020 13:20:00"), new Juice("Fruit Punch")));
            so2_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 14:45:00"), Convert.ToDateTime("02/20/2020 15:20:00"), new Juice("Fruit Punch")));
            so2_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 16:45:00"), Convert.ToDateTime("02/20/2020 17:20:00"), new Juice("Fruit Punch")));
            equips.Add(so2_blend_system);

            Equipment so3_blend_system = new Equipment("SO3 Blend System");
            so3_blend_system.so = 3;
            so3_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 20:00:00"), Convert.ToDateTime("02/19/2020 20:40:00"), new Juice("Rinse")));
            so3_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 02:45:00"), Convert.ToDateTime("02/19/2020 04:00:00"), new Juice("Lemonade Strawberry")));
            so3_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 04:45:00"), Convert.ToDateTime("02/19/2020 06:00:00"), new Juice("Lemonade Strawberry")));
            so3_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 06:45:00"), Convert.ToDateTime("02/19/2020 08:00:00"), new Juice("Lemonade Strawberry")));
            so3_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 08:45:00"), Convert.ToDateTime("02/19/2020 10:00:00"), new Juice("Lemonade Strawberry")));
            so3_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 10:00:00"), Convert.ToDateTime("02/19/2020 10:40:00"), new Juice("Rinse")));
            so3_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 15:40:00"), Convert.ToDateTime("02/19/2020 16:50:00"), new Juice("Lemonade Blueberry")));
            so3_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 17:40:00"), Convert.ToDateTime("02/19/2020 18:50:00"), new Juice("Lemonade Blueberry")));
            so3_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 18:50:00"), Convert.ToDateTime("02/19/2020 19:30:00"), new Juice("Rinse")));
            so3_blend_system.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 21:55:00"), Convert.ToDateTime("02/19/2020 22:45:00"), new Juice("Simply Orange Juice")));
            equips.Add(so3_blend_system);

            Equipment tl_1 = new Equipment("TL 1");
            tl_1.so = 4;
            tl_1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 20:30:00"), Convert.ToDateTime("02/19/2020 23:45:00"), new Juice("Rinse")));
            tl_1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 04:45:00"), Convert.ToDateTime("02/20/2020 04:45:00"), new Juice("Honest Green Tea Jasmine Honey")));
            tl_1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 04:45:00"), Convert.ToDateTime("02/20/2020 06:45:00"), new Juice("Honest Green Tea Jasmine Honey")));
            tl_1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 06:45:00"), Convert.ToDateTime("02/20/2020 08:45:00"), new Juice("Honest Green Tea Jasmine Honey")));
            equips.Add(tl_1);

            Equipment tl_2 = new Equipment("TL 2");
            tl_2.so = 4;
            tl_2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 18:45:00"), Convert.ToDateTime("02/19/2020 18:55:00"), new Juice("Rinse")));
            tl_2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 02:15:00"), Convert.ToDateTime("02/20/2020 04:15:00"), new Juice("Watermelon")));
            tl_2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 04:15:00"), Convert.ToDateTime("02/20/2020 06:15:00"), new Juice("Watermelon")));
            tl_2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 06:15:00"), Convert.ToDateTime("02/20/2020 07:15:00"), new Juice("3 Step Hot Clean")));
            tl_2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 09:45:00"), Convert.ToDateTime("02/20/2020 11:45:00"), new Juice("Fruit Punch")));
            tl_2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 11:45:00"), Convert.ToDateTime("02/20/2020 13:45:00"), new Juice("Fruit Punch")));
            tl_2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 13:45:00"), Convert.ToDateTime("02/20/2020 15:45:00"), new Juice("Fruit Punch")));
            tl_2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 15:45:00"), Convert.ToDateTime("02/20/2020 17:45:00"), new Juice("Fruit Punch")));
            tl_2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 17:45:00"), Convert.ToDateTime("02/20/2020 19:45:00"), new Juice("Fruit Punch")));
            equips.Add(tl_2);

            Equipment tl_3_inline = new Equipment("TL 3 INLINE");
            tl_3_inline.so = 4;
            tl_3_inline.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 22:00:00"), Convert.ToDateTime("02/19/2020 22:25:00"), new Juice("Rinse")));
            tl_3_inline.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 18:30:00"), Convert.ToDateTime("02/20/2020 20:30:00"), new Juice("Simply Apple")));
            tl_3_inline.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 20:30:00"), Convert.ToDateTime("02/20/2020 20:55:00"), new Juice("Rinse")));
            tl_3_inline.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 23:15:00"), Convert.ToDateTime("02/21/2020 01:15:00"), new Juice("Simply Orange Juice")));
            tl_3_inline.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/21/2020 01:15:00"), Convert.ToDateTime("02/21/2020 03:15:00"), new Juice("Simply Orange Juice")));
            equips.Add(tl_3_inline);

            Equipment tl_4 = new Equipment("TL 4");
            tl_4.so = 4;
            tl_4.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 22:30:00"), Convert.ToDateTime("02/19/2020 22:45:00"), new Juice("Rinse")));
            tl_4.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 04:30:00"), Convert.ToDateTime("02/20/2020 06:30:00"), new Juice("Lemonade Strawberry")));
            tl_4.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 06:30:00"), Convert.ToDateTime("02/20/2020 08:30:00"), new Juice("Lemonade Strawberry")));
            tl_4.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 08:30:00"), Convert.ToDateTime("02/20/2020 10:30:00"), new Juice("Lemonade Strawberry")));
            tl_4.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 10:30:00"), Convert.ToDateTime("02/20/2020 12:30:00"), new Juice("Lemonade Strawberry")));
            tl_4.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 12:30:00"), Convert.ToDateTime("02/20/2020 12:45:00"), new Juice("Rinse")));
            tl_4.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 17:00:00"), Convert.ToDateTime("02/20/2020 19:00:00"), new Juice("Lemonade Blueberry")));
            tl_4.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 19:00:00"), Convert.ToDateTime("02/20/2020 21:00:00"), new Juice("Lemonade Blueberry")));
            equips.Add(tl_4);

            Equipment aseptic_1 = new Equipment("Aseptic 1");
            aseptic_1.so = 5;
            aseptic_1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 21:00:00"), Convert.ToDateTime("02/19/2020 03:00:00"), new Juice("CIP")));
            aseptic_1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 02:45:00"), Convert.ToDateTime("02/20/2020 08:45:00"), new Juice("Honest Green Tea Jasmine Honey")));
            aseptic_1.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 12:22:00"), Convert.ToDateTime("02/20/2020 18:22:00"), new Juice("CIP")));
            equips.Add(aseptic_1);

            Equipment aseptic_2 = new Equipment("Aseptic 2");
            aseptic_2.so = 5;
            aseptic_2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 18:45:00"), Convert.ToDateTime("02/19/2020 18:55:00"), new Juice("Rinse")));
            aseptic_2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 02:15:00"), Convert.ToDateTime("02/20/2020 06:15:00"), new Juice("Watermelon")));
            aseptic_2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 06:15:00"), Convert.ToDateTime("02/20/2020 09:15:00"), new Juice("3 Step Hot Clean")));
            aseptic_2.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 09:45:00"), Convert.ToDateTime("02/20/2020 19:45:00"), new Juice("Fruit Punch")));
            equips.Add(aseptic_2);

            Equipment aseptic_3 = new Equipment("Aseptic 3");
            aseptic_3.so = 5;
            aseptic_3.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 22:30:00"), Convert.ToDateTime("02/19/2020 22:40:00"), new Juice("Rinse")));
            aseptic_3.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 04:30:00"), Convert.ToDateTime("02/20/2020 12:30:00"), new Juice("Lemonade Strawberry")));
            aseptic_3.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 12:30:00"), Convert.ToDateTime("02/20/2020 12:40:00"), new Juice("Rinse")));
            aseptic_3.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 17:00:00"), Convert.ToDateTime("02/20/2020 21:00:00"), new Juice("Lemonade Blueberry")));
            equips.Add(aseptic_3);

            Equipment aseptic_7 = new Equipment("Aseptic 7");
            aseptic_7.so = 5;
            aseptic_7.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/19/2020 22:00:00"), Convert.ToDateTime("02/19/2020 22:10:00"), new Juice("CIP")));
            aseptic_7.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 18:30:00"), Convert.ToDateTime("02/20/2020 20:30:00"), new Juice("Simply Apple")));
            aseptic_7.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 20:30:00"), Convert.ToDateTime("02/20/2020 20:40:00"), new Juice("Rinse")));
            aseptic_7.schedule.Add(new ScheduleEntry(Convert.ToDateTime("02/20/2020 23:15:00"), Convert.ToDateTime("02/21/2020 03:15:00"), new Juice("Simply Orange Juice")));
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

        }
    }
}
