using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    class Juice
    {
        public bool parsing;
        public bool starter; // check for whereami, askForBatches, no_batches, currentBatch
        public bool mixing;
        public DateTime readytotrans; // set when mixing == true, this is the time the transferline should be acquired by
        // i got rid of the no_batches flag since we're asking about batches regardless

        public int totalBatches; // used to be quantity
        public int neededBatches;

        public bool inline;
        public int slurryBatches;

        public int orderID; // for finding schedule entries for this juice in equipment schedules
        public DateTime OGFillTime; // used to be fillTime
        public DateTime currentFillTime;

        public int line;
        public int type;
        public string name;
        public string material;

        // TODO - make these initializations elsewhere... (2)

        public List<ScheduleEntry> schedule = new List<ScheduleEntry>();

        public List<List<int>> recipes = new List<List<int>>(); // for each recipe there's a list, each list a list of times each functionality is needed for, -1 if not needed
        public List<int> recipePreTimes = new List<int>();
        public List<int> recipePostTimes = new List<int>();
        public List<bool> inlineflags = new List<bool>(); // marks whether or not each recipe is inline
        public bool inlineposs; // or of inlineflags
        public int transferTime;

        public List<DateTime> idealTime = new List<DateTime>();
            /* ideal start time = fill time - (the time it takes to transfer from blend to aseptic + 
								                postblend time +
								                the sum of all the blend equipment times in the recipe) */
        public int numUniqueToolsNeeded; // the number of tools which only one machine supports that all the recipes need

        public Equipment BlendTank;

        // TODO - decide on cosntructor with Noelle (1)
        // case when juice type can be parsed from the SAP schedule??? Not sure anymore
        public Juice(int line, int type, string material, string name, DateTime fill, bool started, bool no_batches)
        {
            this.line = line;
            this.type = type;
            this.material = material;
            this.name = name;
            this.parsing = false;
            this.OGFillTime = fill;
            this.starter = started;

            // pull info from database
        }

        // case when juice type cannot be parsed from the SAP schedule
        // UpdateJuice will be called later to fix type and database dependent fields
        public Juice(int quantity, int line, string material, string name, DateTime fill, bool started)
        {
            this.line = line;
            this.name = name;
            this.parsing = true;
            this.OGFillTime = fill;
            this.starter = started;
        }

        // TODO : add a version of this for starters and also, this needs a closer pass through for correctness
        // called during the second stage of GNS when CSV entries are confirmed
        public void UpdateJuice(int batches, int newLine, int newType, DateTime newFill)
        {
            if (parsing)
            {
                // add name as a pseudonym for the type specified by newType
                type = newType;
                // pull info from database
            }
            else if (newType != type)
            {
                type = newType;
                // pull info from database
            }

            totalBatches = batches;
            line = newLine;
            OGFillTime = newFill;
        }

        // TODO - fill in function
        public void RecalculateFillTime()
        {
            // find the fill time for the next batch
            // also find the ideal times for each recipe
        }

        // TODO - fill in function
        public DateTime CanDoInline()
        {
            return new DateTime(10, 10, 2020); //delete just so no error
            // checks if a juice can do inline
            // first and most obviously check if it has inline recipes
            // check
        }
    }
}
