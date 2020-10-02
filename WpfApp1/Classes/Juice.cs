using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1
{
    class Juice
    {
        // these fields are specified at runtime
        public int quantity; // how much is being ordered
        public int line; // which aseptic tank it needs to start filling on
        public int type; // what kind of juice it is ie Apple, Fruit Punch, etc
        public string name; // psuedonym in the SAP schedule
        public string material; //material number of juice in string form 
        public DateTime fillTime; // when bottling starts
        public bool parsingFlag; // true if there was an error in parsing the juice type, look to name for the SAP pseudonym
        public bool starterFlag; // true if the juice is starting in active production
        public bool whereami; // true if the starterFlag is true and the schedule hasn't been specified yet
        public bool askForBatches;
        public bool no_batches; //true if it's almost done in the fill lines: col[4]<=col[5]
        public bool currentBatch;

        //TODO: join no_btaches into the constructors
        //the one with no batches, quantity=0
        //quantity is not identified in constructor

        // these fields are pulled from the database
        //public int batchSize; // how much will be produced using the schedule in the recipe
        public List<ScheduleEntry> recipe = new List<ScheduleEntry>();
        public List<int> allowedSOs = new List<int>(); // by default this should be all

        // this is the important bit
        public List<ScheduleEntry> schedule = new List<ScheduleEntry>();
        public int assignedSO;

        // case when juice type can be parsed from the SAP schedule??? Not sure anymore
        public Juice(int line, int type, string material, string name, DateTime fill, bool started, bool no_batches)
        {
            this.line = line;   
            this.type = type;
            this.material = material;
            this.name = name;
            this.parsingFlag = false;
            this.fillTime = fill;
            this.starterFlag = started;
            if (starterFlag)
                this.whereami = true;
            this.no_batches = no_batches;


            if(no_batches)
            {
                quantity = 0;
            }

            // get the batchSize, recipe, and allowedSOs from the database
        }

        // case when juice type cannot be parsed from the SAP schedule
        // UpdateJuice will be called later to fix type and database dependent fields
        public Juice(int quantity, int line, string material, string name, DateTime fill, bool started)
        {
            this.quantity = quantity;
            this.line = line;
            this.name = name;
            this.parsingFlag = true;
            this.fillTime = fill;
            this.starterFlag = started;
            if (starterFlag)
                this.whereami = true;
        }

        // case when juice is specified by the user, so there is no name
        public Juice(int type, int line, string material, int quantity, DateTime startFill, bool started)
        {
            this.type = type;
            this.quantity = quantity;
            this.line = line;
            this.material = material;
            this.fillTime = startFill;
            this.parsingFlag = false;
            this.starterFlag = started;
            if (starterFlag)
                this.whereami = true;
            // figure out something to set name to

            // get batchSize, recipe, and allowedSOs from the database
        }

        // called during the second stage of GNS when CSV entries are confirmed
        public void UpdateJuice(int newQuantity, int newLine, int newType, DateTime newFill)
        {
            if (parsingFlag)
            {
                // add name as a pseudonym for the type specified by newType
                type = newType;
                // get the batchSize, recipe, and allowed SOs from the database
            }
            else if (newType != type)
            {
                type = newType;
                // get the batchSize, recipe, and allowedSOs from the database
            }

            quantity = newQuantity;
            line = newLine;
            fillTime = newFill;
        }

        // called by ReconcileSchedules to find the current state of the juice
        public ScheduleEntry FindState(DateTime goal)
        {
            int i = 0;
            while (i < schedule.Count && schedule[i].start.CompareTo(goal) <= 0)
            {
                i++;
            }

            if (i == schedule.Count)
                return schedule[schedule.Count - 1];
            else
                return schedule[i];
        }
    
    }
}
