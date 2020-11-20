using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Windows;

using System.Configuration;

namespace WpfApp1
{
    public class Juice
    {
        // info all juices have, input needed at runtime
        public DateTime OGFillTime;
        public int type;
        public int totalBatches;
        public int line;
        public string name;
        public string material;

        // info for starter juices
        public bool starter;
        public int batchesFilled;
        public bool filling;
        public bool fillingInline;
        public int fillingSlurry;
        public Equipment fillingTransferLine;
        public DateTime finishedWithTransferLine;
        public Equipment fillingTank;
        public bool mixing;
        public bool mixingInline;
        public int mixingSlurry;
        public Equipment mixingTank;
        public DateTime mixingDoneBlending;
        public List<Equipment> mixingEquipment;

        // info pulled from the database used in the scheduling process
        public List<List<int>> recipes; // for each recipe there's a list, each list a list of times each functionality is needed for, 0 if not needed
        public List<int> recipePreTimes;
        public List<int> recipePostTimes;
        public List<int> idealmixinglength;
        public List<bool> inlineflags; // marks whether or not each recipe is inline
        public bool inlineposs; // or of inlineflags
        public TimeSpan transferTime;
        public TimeSpan fillTime;

        // special fields used in scheduling
        public int neededBatches;
        public bool inline;
        public int slurryBatches;
        public DateTime currentFillTime;
        public List<DateTime> idealTime;
        public List<ScheduleEntry> schedule;
        public Equipment tank;

       //added for update juice functions
        public int num_Functions; 
        public List<int> recipeID = new List<int>();
        public List<String> recipename = new List<String>(); 
        
        /// <summary>
        /// Creates a Juice from the schedule.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="material"></param>
        /// <param name="line"></param>
        /// <param name="type"></param>
        /// <param name="fill"></param>
        /// <param name="batches"></param>
        public Juice(string name, string material, int line, int type, DateTime fill)
        {
            this.name = name;
            this.material = material;
            this.line = line;
            this.type = type;
            this.OGFillTime = fill;
           //this.totalBatches = batches;  //TODO: assign somewhere else, from frontend
        }

        /// <summary>
        /// Sets up a standard juice (ie not a starter) by pulling data and setting needed variables
        /// </summary>
        public void UpdateStandardJuice()
        {
            neededBatches = totalBatches;
            inline = false;
            currentFillTime = OGFillTime;
            schedule = new List<ScheduleEntry>();

            getRecipes(); 
            for(int i=0; i <recipeID.Count; i++)
            {
                getFunctionality(recipeID[i]); 
            }
            
            // set inlineposs
            inlineposs = false;
            for (int i = 0; i < inlineflags.Count; i++)
                inlineposs = inlineposs || inlineflags[i];

            InitializeIdealTime();
        }
        private void getNumFunctions()
        {
            
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
                if (dt.Rows.Count > 0) { 
                num_Functions = Convert.ToInt32(dt.Rows[0]["id"]);
                }
                conn.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        public void getRecipes()
        {

            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_juice_rec]";
                cmd.Parameters.Add("juice_type", SqlDbType.BigInt).Value = this.type;
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        this.recipeID.Add(Convert.ToInt32(dr["id"]));
                        this.recipename.Add(dr.Field<String>("Name"));
                        this.recipePreTimes.Add(Convert.ToInt32(dr["preBlend"]));
                        this.recipePostTimes.Add(Convert.ToInt32(dr["postBlend"]));
                        this.idealmixinglength.Add(Convert.ToInt32(dr["mixingTime"]));
                        this.inlineflags.Add(Convert.ToBoolean(dr["inline"]));
                    }
                }

                conn.Close();
                transferTime = new TimeSpan(2, 0, 0);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        public void getFunctionality(int id_rec)
        {
            getNumFunctions();
            int id; 
            List < int >func = new List<int>(num_Functions+1);
            for(int z=0; z<func.Count; z++)
            {
                func[z] = 0; 
            }
            try
            {
                SqlConnection conn = new SqlConnection();
                conn.ConnectionString = ConfigurationManager.ConnectionStrings["conn"].ConnectionString;
                conn.Open();

                SqlCommand cmd = new SqlCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[select_rec_func]";
                cmd.Parameters.Add("rec_id", SqlDbType.BigInt).Value = id_rec;
                cmd.Connection = conn;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow dr in dt.Rows)
                    {
                        
                        id=Convert.ToInt32(dr["func_id"]);
                       
                        for(int j=0; j < func.Count; j++)
                        {
                            if (j == id)
                            {
                                func[j] = Convert.ToInt32(dr["time"]); 
                            }
                            else if (id > j)
                            {
                                break; 
                            }
                        }
                    }
                }
                this.recipes.Add(func); 
                conn.Close();          
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        /// <summary>
        /// Sets up a starter juice by filling in equipment schedules and necessary fields and by pulling from database
        /// </summary>
        /// <param name="scheduleID"></param>
        public void UpdateStarterJuice(DateTime scheduleID)
        {
            // calculate needed batches
            neededBatches = totalBatches - batchesFilled;
            if (filling)
                neededBatches--;

            // // insert code to pull info from the database using attribute type to find the correct juice
            // initialize and fill: recipes, recipePreTimes, recipePostTimes, inlineflags, and transferTime
            getRecipes();
            for (int i = 0; i < recipeID.Count; i++)
            {
                getFunctionality(recipeID[i]);
            }
            transferTime = new TimeSpan(2, 0, 0);

            // deal with the filling juice
            if (filling)
            {
                // you're finishing up with a slurry
                if (fillingInline && fillingSlurry == 1)
                {
                    // mark the transferline 
                    fillingTransferLine.schedule.Add(new ScheduleEntry(scheduleID, finishedWithTransferLine, this, true, 1));
                    // mark the blend tank, it ends at finishedwithtransferline
                    fillingTank.schedule.Add(new ScheduleEntry(scheduleID, finishedWithTransferLine, this, true, 1));
                }
                // you're part way through a slurry
                else if (fillingInline)
                {
                    inline = true;
                    slurryBatches = fillingSlurry - 1;
                    tank = fillingTank;
                    // mark the transferline
                    fillingTransferLine.schedule.Add(new ScheduleEntry(scheduleID, finishedWithTransferLine, this, true, slurryBatches + 1));
                    // mark the blend tank, open ended
                    fillingTank.schedule.Add(new ScheduleEntry(scheduleID, this));
                }
                // it's a batch
                else
                {
                    // mark the transferline
                    fillingTransferLine.schedule.Add(new ScheduleEntry(scheduleID, finishedWithTransferLine, this, false, totalBatches - neededBatches));
                    // mark the blend tank, it ends at finishedwithtransferline
                    fillingTank.schedule.Add(new ScheduleEntry(scheduleID, finishedWithTransferLine, this, false, totalBatches - neededBatches));
                }
            }

            // deal with mixing batch
            if (mixing)
            {
                // mixing a slurry
                if (mixingInline)
                {
                    inline = true;
                    slurryBatches = mixingSlurry - 1;
                    tank = mixingTank;
                    // mark the blend tank open ended
                    mixingTank.schedule.Add(new ScheduleEntry(scheduleID, this));
                    // mark all the blend equipment
                    for (int i = 0; i < mixingEquipment.Count; i++)
                        mixingEquipment[i].schedule.Add(new ScheduleEntry(scheduleID, mixingEquipment[i].endMixing, this, true, slurryBatches + 1));
                }
                // mixing a batch
                else
                {
                    // mark the blend tank
                    mixingTank.schedule.Add(new ScheduleEntry(scheduleID, mixingDoneBlending.Add(transferTime), this, false, totalBatches - neededBatches));
                    // mark the blend equipment
                    for (int i = 0; i < mixingEquipment.Count; i++)
                        mixingEquipment[i].schedule.Add(new ScheduleEntry(scheduleID, mixingEquipment[i].endMixing, this, false, totalBatches - neededBatches));
                }
            }

            // set up fill and ideal times
            if (filling)
            {
                currentFillTime = finishedWithTransferLine;
                InitializeIdealTime();
            }
            else
            {
                currentFillTime = OGFillTime;
                for (int i = 0; i < totalBatches - neededBatches; i++)
                    currentFillTime = currentFillTime.Add(transferTime);
                InitializeIdealTime();
            }

            schedule = new List<ScheduleEntry>();
        }

        /// <summary>
        /// Calculates the new fill and ideal times. Recalculates based on when the last batch ended,
        /// allowing for that batch to be early or late
        /// </summary>
        /// <param name="lastbatchend"></param>
        public void RecalculateFillTime()
        {
            // find the fill time for the next batch
            currentFillTime = currentFillTime.Add(fillTime);
            
            // find the ideal times for each recipe
            for (int i = 0; i < idealTime.Count; i++)
                idealTime[i] = currentFillTime.Subtract(new TimeSpan(0, idealmixinglength[i],0));
        }

        /// <summary>
        /// Calculates the initial ideal times by subtracting the ideal length from the fill time
        /// </summary>
        public void InitializeIdealTime()
        {
            for (int i = 0; i < recipes.Count; i++)
                idealTime.Add(currentFillTime.Subtract(new TimeSpan(0, idealmixinglength[i], 0)));
        }

    }
}
