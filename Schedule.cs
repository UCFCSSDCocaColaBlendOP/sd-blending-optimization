using System;
using System.Collections.Generic;

public class Schedule
{

    // TODO :: make everything public (it's just the schedule class that's inconsistent as shit about it)
    // TODO :: function to extrapolate juice schedule from equipment schedules
    List<Equipment> machines = new List<Equipment>();
    List<Equipment> blendtanks = new List<Equipment>();
    List<Equipment> transferLines = new List<Equipment>();
    int numFunctions;
    int numSOs;
    bool[] uniqueTools = new bool[numFunctions]; // a function is true if only one machine supports that functionality, false otherwise

    List<Juice> finished = new List<Juice>();
    List<Juice> inprogress = new List<Juice>(); // this is "juices" i went through and changed all references to "juices" even in commented out sections
    List<Juice> juices_line8 = new List<Juice>();
    List<Juice> juices_line9 = new List<Juice>(); // thaw room
    List<int> incorrect_batches_for_juice; // i left this here but the part of your code that uses it is commented out so ??
    DateTime scheduleID;


    public Schedule()
    {
        scheduleID = DateTime.Now;
    }

    // called in the first stage of GNS to parse the CSV and initialize inprogress

    //I think this will become a string name in which the location of the file is in.
    //StreamReader CSV

    // called in the first stage of GNS after which inprogress is verified
    // reads through the csv and populates inprogress accordingly
    // find the right sections of the csv, read each line:
    //      convert the juice name to find the right type in the database
    // if CSV is null, return inprogress anyway as an empty list so user can
    //      manually add juice orders
    // if some value cannot be identified, set parsingFlag to true

    //will set the list inprogress. I think this is the right way to do it because then we would have to somehow set the global variable
    //TODO: if list is empty then we should pop up an ERROR box
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
        inprogress = new List<Juice>();
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
                    inprogress.Add(new_juice);
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
                        while (inprogress[count_juice].material.Contains("CIP-FILL") || inprogress[count_juice].no_batches)
                        {
                            Console.WriteLine(inprogress[count_juice].name + " is the juice we are skipping and it's " + count_juice + " out of " + inprogress.Count + "total juices");
                            if (count_juice == inprogress.Count)
                            {
                                break;
                            }
                            count_juice++;
                        }
                        //If the juice doesn't belong the schedule (no matching juices), then ignore it
                        Console.WriteLine("Added the quantity " + count_batches + " to " + current_material);
                        Console.WriteLine(inprogress[count_juice].name + " is the juice and it's " + count_juice + " out of " + inprogress.Count + "total juices");
                        inprogress[count_juice].quantity = count_batches;
                        inprogress[count_juice].currentBatch = current_batching;
                        count_materials_in_line[current_line]++;
                        current_batching = false;
                        if (count_juice < inprogress.Count)
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
                            while (inprogress[count_juice].material.Contains("CIP-FILL") || inprogress[count_juice].no_batches)
                            {
                                if (count_juice == inprogress.Count)
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
                                if (current_batchTime < inprogress[count_juice].fillTime)
                                {
                                    inprogress[count_juice].quantity = count_batches;
                                    inprogress[count_juice].currentBatch = current_batching;
                                    Console.WriteLine("Added the quantity " + count_batches + " to " + current_material);
                                    Console.WriteLine(inprogress[count_juice].name + " is the juice and it's " + count_juice + " out of " + inprogress.Count + "total juices");
                                    count_materials_in_line[current_line]++;
                                    current_batching = false;
                                    if (count_juice < inprogress.Count)
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
            Console.WriteLine("Name: " + juices_line8[i].name);
        }

    }

    // TODO:: ALISA
    void PullEquipment()
    {
        // access the database
        // initialize SOcount and functionCount

        // find the equipment list in the database
        // iterate through each piece of equipment
        Equipment temp;
        for (; ; )
        {
            temp = new Equipment();
            // add the name and identifing number (type)
            // add the functionalities
            // add the SOs 

            machines.Add(temp); 0
        }
    }

    CompareRecipe[] prepRecipes(Juice x)
    {
        CompareRecipe[] options = new CompareRecipe[x.recipes.Count];

        // make equipment choices for each recipe
        for (int i = 0; i < x.recipes.Count; i++)
        {
            List<List<Equipment>> recipecopy = sortByOptions(x.recipes[i]);
            bool[] checkoffFunc = new bool[numFunctions];
            int cntFunc = 0;
            bool[] checkoffsos = new bool[numSOs];
            int cntSOs = numSOs;

            for (int j = 0; j < numFunctions; j++)
            {
                // all the functionalities have been covered
                if (cntFunc == checkoffFunc.Length)
                    break;

                // find out what function this list of equipm
                int func = recipecopy[j][0].type * -1;

                // case that the recipe doesn't need that function
                if (recipecopy[j].Count == 1 && x.recipes[func] < 0)
                {
                    checkoffFunc[func] = true;
                    cntFunc++;
                    continue;
                }

                // case that the recipe needs the function and it isn't available
                if (recipecopy[j].Count == 1)
                {
                    options[i].possible = false;
                    break;
                }

                // pick a tool
                int choice = -1;
                DateTime currentStart;
                int otherfuncs;
                int sos;
                bool otherUnique;

                for (int k = 1; k < recipecopy[j].Count; k++)
                {
                    // check that equipment can connect

                    DateTime tempstart = getStart(recipecopy[j][k], x.recipes[i]);
                    int tempfuncs = getFuncs(recipecopy[j][k], x.recipes[i], checkoffFunc);
                    int tempsos = getSOs(recipecopy[j][k], checkoffsos);
                    bool containsUnneededUnique = getOtherUnique();

                    if (choice == -1)
                    {
                        choice = k;
                        currentStart = tempstart;
                        otherfuncs = tempfuncs;
                        sos = tempsos;
                        otherUnique = containsUnneededUnique;
                    }
                    else if (DateTime.Compare(tempstart, x.idealTime[i]) > 0 && DateTime.Compare(tempstart, currentStart) < 0)
                    {
                        choice = k;
                        currentStart = tempstart;
                        otherfuncs = tempfuncs;
                        sos = tempsos;
                        otherUnique = containsUnneededUnique;
                    }
                    else if (DateTime.Compare(tempstart, x.idealTime[i]) > 0)
                    {
                        continue;
                    }
                    else if (DateTime.Compare(tempstart, currentStart) == 0)
                    {
                        if (otherUnique && !containsUnneededUnique)
                        {
                            choice = k;
                            currentStart = tempstart;
                            otherfuncs = tempfuncs;
                            sos = tempsos;
                            otherUnique = containsUnneededUnique;
                        }
                        else if ((containsUnneededUnique && !otherUnique) || otherfuncs > tempfuncs)
                        {
                            continue;
                        }
                        else if (tempfuncs > otherfuncs)
                        {
                            choice = k;
                            currentStart = tempstart;
                            otherfuncs = tempfuncs;
                            sos = tempsos;
                            otherUnique = containsUnneededUnique;
                        }
                        else if (tempsos > sos)
                        {
                            choice = k;
                            currentStart = tempstart;
                            otherfuncs = tempfuncs;
                            sos = tempsos;
                            otherUnique = containsUnneededUnique;
                        }
                    }
                    else if (DateTime.Compare(tempstart, currentStart) < 0)
                    {
                        if (TimeSpan.Compare(currentStart.Subtract(x.idealTime[i]), new TimeSpan(1, 0, 0)) <= 0)
                        {
                            if (otherUnique && !containsUnneededUnique)
                            {
                                choice = k;
                                currentStart = tempstart;
                                otherfuncs = tempfuncs;
                                sos = tempsos;
                                otherUnique = containsUnneededUnique;
                            }
                            else if ((containsUnneededUnique && !otherUnique) || otherfuncs > tempfuncs)
                            {
                                continue;
                            }
                            else if (tempfuncs > otherfuncs)
                            {
                                choice = k;
                                currentStart = tempstart;
                                otherfuncs = tempfuncs;
                                sos = tempsos;
                                otherUnique = containsUnneededUnique;
                            }
                            else if (tempsos > sos)
                            {
                                choice = k;
                                currentStart = tempstart;
                                otherfuncs = tempfuncs;
                                sos = tempsos;
                                otherUnique = containsUnneededUnique;
                            }
                        }
                        else
                        {
                            choice = k;
                            currentStart = tempstart;
                            otherfuncs = tempfuncs;
                            sos = tempsos;
                            otherUnique = containsUnneededUnique;
                        }
                    }
                    else if (TimeSpan.Compare(tempstart.Subtract(x.idealTime[i]), new TimeSpan(1, 0, 0)) <= 0)
                    {
                        if (otherUnique && !containsUnneededUnique)
                        {
                            choice = k;
                            currentStart = tempstart;
                            otherfuncs = tempfuncs;
                            sos = tempsos;
                            otherUnique = containsUnneededUnique;
                        }
                        else if ((containsUnneededUnique && !otherUnique) || otherfuncs > tempfuncs)
                        {
                            continue;
                        }
                        else if (tempfuncs > otherfuncs)
                        {
                            choice = k;
                            currentStart = tempstart;
                            otherfuncs = tempfuncs;
                            sos = tempsos;
                            otherUnique = containsUnneededUnique;
                        }
                        else if (tempsos > sos)
                        {
                            choice = k;
                            currentStart = tempstart;
                            otherfuncs = tempfuncs;
                            sos = tempsos;
                            otherUnique = containsUnneededUnique;
                        }
                    }

                }


                // mark off the rest of the functionalities that piece supports


            }
        }

        return options;
    }

    // TODO - add call to preprecipes
	void GenerateNewSchedule()
    {
        SortByFillTime();

        while (inprogress.Count != 0)
        {
            if (inprogress[0].mixing)
            {
                // you only have to acquire a transfer line

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

                    // update the batch counts
                    inprogress[0].neededBatches--;
                    inprogress[0].slurryBatches--;

                    // move to finished list or continue
                    if (inprogress[0].neededBatches == 0)
                    {
                        finished.Add(inprogress[0]);
                        inprogress.RemoveAt(0);
                    }
                    else
                    {
                        if (inprogress[0].slurryBatches == 0)
                            inprogress[0].inline = false;

                        inprogress[0].RecalculateFillTime();
                        SortByFillTime();
                    }
                }   
                else
                {
                    // it wouldn't make sense to do inline for a single batch
                    if (inprogress[0].neededBatches == 1)
                    {
                        // pick a batched recipe
                        // assign equipment

                        // move to finished list
                        finished.Add(inprogress[0]);
                        inprogress.RemoveAt(0);
                    }
                    else
                    {
                        // decide if you can do inline: can you finish the slurry for 2,3,4,or5 batches before the fill time?
                        // if you do end up doing inline, slurryBatches does not include the current batch


                        // finish with current juice and move on
                        inprogress[0].neededBatches--;
                        inprogress[0].RecalculateFillTime();
                        SortByFillTime();
                    }    
                }    
            }
        }
    }

    void SortByFillTime()
    {
        // sorts inprogress by current filltime
        // use insertion sort because most calls will be on an already sorted list
    }

    List<List<Equipment>> sortByOptions(List<int> x)
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

    bool getOtherUnique()
    {

    }

    DateTime getStart(Equipment tool, List<int> recipe)
    {
        DateTime start = new DateTime();
    }

    TimeSpan getlength(Equipment tool, List<int> recipe, bool[] funcsclaimed)
    {
        TimeSpan length = new TimeSpan();

        for (int i = 0; i < numFunctions; i++)
            if (tool.functionalities[i] && !funcsclaimed[i] && recipe[i] > 0)
                length.Add(0, recipe[i], 0);
    }

    int getFuncs(Equipment tool, List<int> recipe, bool[] funcsclaimed)
    {
        int cnt = 0;
        for (int i = 0; i < numFunctions; i++)
            if (tool.functionalities[i] && !funcsclaimed[i] && recipe[i] > 0)
                cnt++;

        return cnt;
    }

    int getSOs(Equipment tool, bool[] sosclaimed)
    {

    }
}

class Juice
{
    public bool parsing;
    public bool starter; // check for whereami, askForBatches, no_batches, currentBatch
    public bool mixing;
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

    public List<ScheduleEntry> schedule = new List<ScheduleEntry>();

    public List<List<int>> recipes = new List<List<int>>(); // for each recipe there's a list, each list a list of times each functionality is needed for, -1 if not needed
    public List<bool> inlineflags = new List<bool>(); // marks whether or not each recipe is inline
    
    public List<DateTime> idealTime = new List<DateTime>();
    public int numUniqueToolsNeeded; // the number of tools which only one machine supports that all the recipes need



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

        totalBatches = batches
        line = newLine;
        OGFillTime = newFill;
    }
    
    public void RecalculateFillTime()
    {
        // find the fill time for the next batch
    }

    public DateTime CanDoInline()
    {
        // checks if a juice can do inline
        // first and most obviously check if it has inline recipes
        // check
    }
}

class Equipment
{
    String name;
    int type; // identifier for the tool
    List<bool> functionalities = new List<bool>(); // ordered list, with a boolean value for each functionality
    List<bool> SOs = new List<bool>(); // ordered list, with a boolean value for each SO
    List<ScheduleEntry> schedule = new List<ScheduleEntry>(); // this is a list of schedules

    public Equipment(String name, int type)
    {
        this.name = name;
        this.type = type;
    }

    public Equipment(int type)
    {
        this.type = type;
    }
}

// i just copied this over, idk if it's right
class ScheduleEntry
{
    public DateTime start;
    public DateTime end;
    public TimeSpan totalTime;
    public int state;
    // for equipment, state =
    //                      0 - clean, waiting
    //                      1 - cleaning
    //                      2 - juice active
    //                      3 - out
    // for juice, state =
    //                      0 - not started yet
    //                      1 - in blend tank only
    //                      2 - using other equipment
    //                      3 - done

    // for equipment schedules:
    Juice juice;
    string cleaning;

    // for juice recipe
    int equipmentFunctionality;

    // for juice schedule
    Equipment tool;
    int recipeStage; // 0 for pre recipe, -1 for post recipe

    public ScheduleEntry(DateTime start, DateTime end, Juice juice)
    {
        this.start = start;
        this.end = end;
        this.totalTime = this.end.Subtract(this.start);

        this.state = 2;
        this.juice = juice;
    }

    public ScheduleEntry(TimeSpan length, int function)
    {
        this.totalTime = length;
        this.equipmentFunctionality = function;
    }

    public ScheduleEntry(DateTime start, DateTime end, string cleaning)
    {
        this.start = start;
        this.end = end;
        this.totalTime = this.end.Subtract(this.start);

        this.state = 1;
        this.cleaning = cleaning;
    }

    public ScheduleEntry(DateTime start, DateTime end, Equipment tool, int recipeStage)
    {
        this.start = start;
        this.end = end;
        this.totalTime = this.end.Subtract(this.start);

        // come back
    }
}

class CompareRecipe
{
    DateTime start;
    TimeSpan length;
    List<Equipment> tools = new List<Equipment>();
    bool conceivable;
    bool onTime;
    List<int> sos = new List<int>();;
}