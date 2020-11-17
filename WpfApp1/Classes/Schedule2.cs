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

namespace WpfApp1
{
    class Schedule2
    {
        public bool inconceivable;
        public Juice inconceiver;
        public bool late;
        public Juice lateJuice;
        public Equipment lateTool;

        public DateTime scheduleID;

        // all of the equipment in the system
        public List<Equipment> extras;          // tools with only one functionality, type == functionality, should be sorted by type
        public List<Equipment> systems;         // tools with multiple functionalities
        public Equipment thawRoom;              // the thaw room
        public List<Equipment> tanks;           // blend tanks/ mix tanks
        public List<Equipment> transferLines;   // transfer lines
        public List<Equipment> aseptics;        // aseptic/pasteurizers

        // each piece of equipment belongs to a cip group, only one piece of equipment in the group
        // can CIP at a time
        public List<Equipment> cipGroups;

        // info about the system
        public int numFunctions; 
        public int numSOs;
        
        // lists of juices
        public List<Juice> finished;
        public List<Juice> inprogress;
        
        /// <summary>
        /// Creates a Schedule object, initializing lists of equipment
        /// </summary>
        public Schedule2()
        {
            scheduleID = DateTime.Now;
            extras = new List<Equipment>();
            systems = new List<Equipment>();
            tanks = new List<Equipment>();
            transferLines = new List<Equipment>();
            cipGroups = new List<Equipment>();
            finished = new List<Juice>();
            inprogress = new List<Juice>();

            inconceivable = false;
            late = false;
        }

        // TODO :: comment, add errors, add calls to functions that add to database
        public void GenerateNewSchedule()
        {
            // sort inprogress juices so inprogress[0] is the juice with the earliest currentFillTime
            SortByFillTime();

            // while there are juices with batches left
            while (inprogress.Count != 0)
            {
                // if the current batch is mixing at run time
                if (inprogress[0].mixing)
                {
                    inprogress[0].mixing = false;

                    // you only have to acquire a transfer line
                    // get the time it's gonna start transferring at
                    DateTime done = AcquireTransferLineAndAseptic(inprogress[0].inline, inprogress[0], inprogress[0].mixingDoneBlending, inprogress[0].tank);

                    // if the transfer line is late, that's an error and we need to stop
                    if (DateTime.Compare(done.Add(inprogress[0].transferTime), inprogress[0].currentFillTime) > 0)
                    {
                        late = true;
                        lateJuice = inprogress[0];
                        return;
                    }
                    // if the transfer line is down, that's an error and we need to stop
                    else if (DateTime.Compare(done, DateTime.MinValue) == 0)
                    {
                        inconceivable = true;
                        inconceiver = inprogress[0];
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
                        DateTime done = AcquireTransferLineAndAseptic(true, inprogress[0], inprogress[0].currentFillTime.Subtract(inprogress[0].transferTime), inprogress[0].tank);

                        // if the transfer line is late, that's an error and we need to stop
                        if (DateTime.Compare(done.Add(inprogress[0].transferTime), inprogress[0].currentFillTime) > 0)
                        {
                            late = true;
                            lateJuice = inprogress[0];
                            return;
                        }
                        // if the transfer line is down, that's an error and we need to stop
                        else if (DateTime.Compare(done, DateTime.MinValue) == 0)
                        {
                            inconceivable = true;
                            inconceiver = inprogress[0];
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

                                    // get info about recipe
                                    CompareRecipe test = PrepRecipe(inprogress[0], j, i);
                                    if (!test.conceivable || !test.onTime)
                                        continue;

                                    canDo = true;

                                    if (pick == null || size < i || DateTime.Compare(goTime, test.startBlending) < 0)
                                    {
                                        pick = test;
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
                                pick.Actualize(thawRoom, true, inprogress[0]);

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
                            temp = PrepRecipe(inprogress[0], i);

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
                            inconceivable = true;
                            inconceiver = inprogress[0];
                            return;
                        }

                        // all our choices are late
                        if (!onTime)
                        {
                            late = true;
                            lateJuice = inprogress[0];
                            lateTool = choice.lateMaker;
                            return;
                        }

                        // assign equipment
                        // all of the choices have been made and the times are in choice
                        choice.Actualize(thawRoom, false, inprogress[0]);

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
                    transferLines[2].schedule.Add(new ScheduleEntry(transferLines[2].cleanTime, transferLines[2].cleanTime.Add(transferLines[2].cleanLength), transferLines[2].cleanType));
                
                transferLines[2].schedule.Add(new ScheduleEntry(three, three.Add(juice.transferTime), juice, true, -1));

                // aseptic
                if (aseptics[juice.type].needsCleaned)
                    aseptics[juice.type].schedule.Add(new ScheduleEntry(aseptics[juice.type].cleanTime, aseptics[juice.type].cleanTime.Add(aseptics[juice.type].cleanLength), aseptics[juice.type].cleanType));
                aseptics[juice.type].schedule.Add(new ScheduleEntry(three, three.Add(juice.transferTime), juice, true, -1));
                return three;
            }
            else
            {
                Equipment choice = null;
                DateTime start = DateTime.MinValue;

                // try transfer line 1 or two
                if (!transferLines[tank.type].down)
                {
                    choice = transferLines[tank.type];
                    start = transferLines[tank.type].FindTime(startTrans, juice.type, scheduleID);
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
                    choice.schedule.Add(new ScheduleEntry(choice.cleanTime, choice.cleanTime.Add(choice.cleanLength), choice.cleanType));
                choice.schedule.Add(new ScheduleEntry(start, start.Add(juice.transferTime), juice, true, -1));

                // aseptic
                if (aseptics[juice.type].needsCleaned)
                    aseptics[juice.type].schedule.Add(new ScheduleEntry(aseptics[juice.type].cleanTime, aseptics[juice.type].cleanTime.Add(aseptics[juice.type].cleanLength), aseptics[juice.type].cleanType));
                aseptics[juice.type].schedule.Add(new ScheduleEntry(start, start.Add(juice.transferTime), juice, true, -1));
                
                return start;
            }
        }

        /// <summary>
        /// Will create a CompareRecipe object for this batched recipe of this juice
        /// </summary>
        /// <param name="juice"></param>
        /// <param name="recipe"></param>
        /// <returns></returns>
        public CompareRecipe PrepRecipe(Juice juice, int recipe)
        {
            CompareRecipe option = new CompareRecipe();
            bool pickedStartTime = false; // has option.startBlending been set
            bool[] checkoffFunc = new bool[numFunctions];
            bool[] soChoices = new bool[numSOs];
            for (int j = 0; j < numSOs; j++)
                soChoices[j] = true;

            // if the thaw room is needed
            if (juice.recipes[recipe][0] > 0)
            {
                // if the thaw room is down we can't do this recipe
                if (thawRoom.down)
                {
                    option.conceivable = false;
                    return option;
                }

                option.thawLength = new TimeSpan(0, juice.recipes[recipe][0], 0);
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
                        option.startBlending = juice.idealTime[recipe];
                    }
                    // otherwise note
                    else
                    {
                        option.lateMaker = thawRoom;
                        option.onTime = false;
                        option.startBlending = begin.Add(option.thawLength);
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
                        option.onTime = false;
                        option.thawTime = begin;
                        option.startBlending = begin.Add(option.thawLength);
                    }
                }

                pickedStartTime = true;
                checkoffFunc[0] = true;
            }

            // if any of the extras are needed, do a first pass through to get the earliest of each one needed
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
                        option.onTime = false;
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
                int sos = 0;
                int otherfuncs = 0;
                TimeSpan length = TimeSpan.Zero;
                DateTime cStart = DateTime.MinValue;
                TimeSpan cLength = TimeSpan.Zero;
                int cType = -1;

                // choose a system
                for (int j = 0; j < systems.Count; j++)
                {
                    // first check if it can connect to the sos
                    bool flag = false;
                    for (int k = 0; k < numSOs; k++)
                        if (soChoices[k] && systems[j].SOs[k])
                            flag = true;
                    if (!flag)
                        continue;

                    // then check if it has the functionalities the recipe needs
                    for (int k = 1; k < numFunctions; k++)
                        if (!checkoffFunc[k] && juice.recipes[recipe][k] > 0 && !systems[j].functionalities[k])
                            continue;

                    // then figure out how long it needs on that system
                    TimeSpan templength = new TimeSpan(0, 0, 0);
                    for (int k = 0; k < numFunctions; k++)
                        if (!checkoffFunc[k] && juice.recipes[recipe][k] > 0)
                            templength.Add(new TimeSpan(0, juice.recipes[recipe][k], 0));

                    // then start comparing this blendsystem to the last one to make a choice
                    DateTime tempstart = systems[j].FindTime(juice.idealTime[recipe].Add(new TimeSpan(0, juice.recipePreTimes[recipe], 0)), juice.type, scheduleID);
                    DateTime tempCStart = DateTime.MinValue;
                    TimeSpan tempCLength = TimeSpan.Zero;
                    int tempCType = -1;
                    if (systems[j].needsCleaned)
                    {
                        tempCStart = systems[j].cleanTime;
                        tempCLength = systems[j].cleanLength;
                        tempCType = systems[j].cleanType;
                    }
                    int tempsos = systems[j].GetSOs(soChoices);
                    int tempotherfuncs = systems[j].GetOtherFuncs(juice.recipes[recipe]);

                    // there is no current
                    if (pick == -1)
                    {
                        pick = j;
                        currentStart = tempstart;
                        sos = tempsos;
                        otherfuncs = tempotherfuncs;
                        length = templength;
                        cStart = tempCStart;
                        cLength = tempCLength;
                        cType = tempCType;
                    }
                    // temp and current are the same time
                    else if (DateTime.Compare(tempstart, currentStart) == 0)
                    {
                        if (otherfuncs > tempotherfuncs)
                        {
                            pick = j;
                            currentStart = tempstart;
                            sos = tempsos;
                            otherfuncs = tempotherfuncs;
                            length = templength;
                            cStart = tempCStart;
                            cLength = tempCLength;
                            cType = tempCType;
                        }
                        else if (otherfuncs == tempotherfuncs && tempsos > sos)
                        {
                            pick = j;
                            currentStart = tempstart;
                            sos = tempsos;
                            otherfuncs = tempotherfuncs;
                            length = templength;
                            cStart = tempCStart;
                            cLength = tempCLength;
                            cType = tempCType;
                        }
                        else if (otherfuncs == tempotherfuncs && tempsos == sos && TimeSpan.Compare(tempCLength, cLength) < 0)
                        {
                            pick = j;
                            currentStart = tempstart;
                            sos = tempsos;
                            otherfuncs = tempotherfuncs;
                            length = templength;
                            cStart = tempCStart;
                            cLength = tempCLength;
                            cType = tempCType;
                        }
                    }
                    // temp is at the ideal time, current is not, if current was also at the ideal time, it would have been caught in the last check
                    else if (DateTime.Compare(tempstart, juice.idealTime[recipe].Add(new TimeSpan(0, juice.recipePreTimes[recipe], 0))) == 0)
                    {
                        pick = j;
                        currentStart = tempstart;
                        sos = tempsos;
                        otherfuncs = tempotherfuncs;
                        length = templength;
                        cStart = tempCStart;
                        cLength = tempCLength;
                        cType = tempCType;
                    }
                    // current is later than ideal
                    else
                    {
                        // temp is later than current
                        if (DateTime.Compare(tempstart, currentStart) > 0)
                            continue;
                        else
                        {
                            pick = j;
                            currentStart = tempstart;
                            sos = tempsos;
                            otherfuncs = tempotherfuncs;
                            length = templength;
                            cStart = tempCStart;
                            cLength = tempCLength;
                            cType = tempCType;
                        }
                    }
                }

                // no system satisfied
                if (pick == -1)
                {
                    option.conceivable = false;
                    return option;
                }

                // save info to option
                option.system = systems[pick];
                option.systemTime = currentStart;
                option.systemLength = length;
                option.systemCleaningStart = cStart;
                option.systemCleaningLength = cLength;
                option.systemCleaningType = cType;

                // update metrics
                if (!pickedStartTime)
                {
                    option.startBlending = currentStart;
                    pickedStartTime = true;
                    if (DateTime.Compare(currentStart, juice.idealTime[recipe].Add(new TimeSpan(0, juice.recipePreTimes[recipe], 0))) > 0)
                    {
                        option.onTime = false;
                        option.lateMaker = option.system;
                    }
                }
                // address lateness
                else if (DateTime.Compare(currentStart, option.startBlending) > 0)
                {
                    option.startBlending = currentStart;
                    option.onTime = false;
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
                    if (TimeSpan.Compare(longest, option.tankLength) < 0)
                        longest = option.tankLength;
                }
                else
                    longest = option.tankLength;
            }

            mixtanktime = mixtanktime.Add(longest);
            mixtanktime = mixtanktime.Add(juice.transferTime);

            DateTime goal = option.startBlending.Subtract(new TimeSpan(0, juice.recipePreTimes[recipe], 0));

            // search through the mix tanks
            Equipment tank = null;
            DateTime start = DateTime.MinValue;
            DateTime cleanStart = DateTime.MinValue;
            TimeSpan cleanLength = TimeSpan.Zero;
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

                if (tank == null)
                {
                    // swap
                    tank = tanks[i];
                    start = tempstart;
                    cleanStart = tempCleanStart;
                    cleanLength = tempCleanLength;
                    cleanType = tempCleanType;
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
                    }
                }
            }

            // if you couldn't find a tank inconceivable
            if (tank == null)
            {
                option.conceivable = false;
                return option;
            }

            // save info about tank
            option.tank = tank;
            option.tankTime = start;
            option.tankLength = mixtanktime;
            option.tankCleaningStart = cleanStart;
            option.tankCleaningLength = cleanLength;
            option.tankCleaningType = cleanType;

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

            // assign a transfer line
            DateTime tgoal = option.tankTime.Add(option.tankLength).Subtract(juice.transferTime);
            Equipment choice = null;
            DateTime goTime = DateTime.MinValue;
            DateTime clSt = DateTime.MinValue;
            TimeSpan clL = TimeSpan.Zero;
            int cltype = -1;

            // try transfer line 1
            if (soChoices[0] && !transferLines[0].down)
            {
                choice = transferLines[0];
                goTime = transferLines[0].FindTime(tgoal, juice.type, scheduleID);
                clSt = transferLines[0].cleanTime;
                clL = transferLines[0].cleanLength;
                cltype = transferLines[0].cleanType;
            }

            // try transfer line 2
            if (soChoices[1] && !transferLines[0].down)
            {
                if (choice == null)
                {
                    choice = transferLines[1];
                    goTime = transferLines[1].FindTime(tgoal, juice.type, scheduleID);
                    clSt = transferLines[1].cleanTime;
                    clL = transferLines[1].cleanLength;
                    cltype = transferLines[1].cleanType;
                }
                else
                {
                    DateTime tempStart = transferLines[1].FindTime(tgoal, juice.type, scheduleID);
                    DateTime tempClSt = transferLines[1].cleanTime;
                    TimeSpan tempClL = transferLines[1].cleanLength;
                    int tempCltype = transferLines[1].cleanType;

                    // transfer line 2 is ontime, transfer line 1 is not
                    if (DateTime.Compare(tempStart, goTime) < 0)
                    {
                        choice = transferLines[1];
                        goTime = tempStart;
                        clSt = tempClSt;
                        clL = tempClL;
                        cltype = tempCltype;
                    }
                    // they're both on time
                    else if (DateTime.Compare(tempStart, goTime) == 0 && DateTime.Compare(tempStart, juice.currentFillTime) == 0)
                    {
                        // transfer line 2 takes less time to clean
                        if (TimeSpan.Compare(clL, tempClL) > 0)
                        {
                            choice = transferLines[1];
                            goTime = tempStart;
                            clSt = tempClSt;
                            clL = tempClL;
                            cltype = tempCltype;
                        }
                    }
                }
            }

            // try transfer line 4
            if (!transferLines[3].down)
            {
                if (choice == null)
                {
                    choice = transferLines[3];
                    goTime = transferLines[3].FindTime(tgoal, juice.type, scheduleID);
                    clSt = transferLines[3].cleanTime;
                    clL = transferLines[3].cleanLength;
                    cltype = transferLines[3].cleanType;
                }
                else
                {
                    DateTime tempStart = transferLines[3].FindTime(tgoal, juice.type, scheduleID);
                    DateTime tempClSt = transferLines[3].cleanTime;
                    TimeSpan tempClL = transferLines[3].cleanLength;
                    int tempCltype = transferLines[3].cleanType;

                    // transfer line 4 is ontime, transfer line 1/2 is not
                    if (DateTime.Compare(tempStart, goTime) < 0)
                    {
                        choice = transferLines[3];
                        goTime = tempStart;
                        clSt = tempClSt;
                        clL = tempClL;
                        cltype = tempCltype;
                    }
                    // they're both on time
                    else if (DateTime.Compare(tempStart, goTime) == 0 && DateTime.Compare(tempStart, juice.currentFillTime) == 0)
                    {
                        // transfer line 4 takes less time to clean
                        if (TimeSpan.Compare(clL, tempClL) > 0)
                        {
                            choice = transferLines[3];
                            goTime = tempStart;
                            clSt = tempClSt;
                            clL = tempClL;
                            cltype = tempCltype;
                        }
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
                }
                else
                {
                    DateTime tempStart = transferLines[2].FindTime(tgoal, juice.type, scheduleID);
                    DateTime tempClSt = transferLines[2].cleanTime;
                    TimeSpan tempClL = transferLines[2].cleanLength;
                    int tempCltype = transferLines[2].cleanType;

                    // transfer line 2 is ontime, transfer line 1 is not
                    if (DateTime.Compare(tempStart, goTime) < 0)
                    {
                        choice = transferLines[2];
                        goTime = tempStart;
                        clSt = tempClSt;
                        clL = tempClL;
                        cltype = tempCltype;
                    }
                }
            }

            // store transfer choice
            if (choice == null)
            {
                option.conceivable = false;
                return option;
            }

            option.transferLine = choice;
            option.transferTime = goTime;
            option.transferLength = juice.transferTime;
            option.transferCleaningStart = clSt;
            option.transferCleaningLength = clL;
            option.transferCleaningType = cltype;

            // decide if it's onTime
            option.onTime = DateTime.Compare(juice.currentFillTime, option.transferTime) <= 0;

            if (DateTime.Compare(option.transferTime.Subtract(option.tankLength).Add(juice.transferTime), option.startBlending) > 0)
                option.lateMaker = option.transferLine;

            return option;
        }

        /// <summary>
        /// Will create a CompareRecipe object for this inline recipe of this juice
        /// </summary>
        /// <param name="juice"></param>
        /// <param name="recipe"></param>
        /// <param name="slurrySize"></param>
        /// <returns></returns>
        public CompareRecipe PrepRecipe(Juice juice, int recipe, int slurrySize)
        {
            for (int i = 0; i < juice.recipes[recipe].Count; i++)
                juice.recipes[recipe][i] *= slurrySize;

            CompareRecipe option = new CompareRecipe();
            bool pickedStartTime = false; // has option.startBlending been set
            bool[] checkoffFunc = new bool[numFunctions];
            bool[] soChoices = new bool[numSOs];
            for (int j = 0; j < numSOs; j++)
                soChoices[j] = true;

            // if the thaw room is needed
            if (juice.recipes[recipe][0] > 0)
            {
                // if the thaw room is down we can't do this recipe
                if (thawRoom.down)
                {
                    option.conceivable = false;
                    for (int z = 0; z < juice.recipes[recipe].Count; z++)
                        juice.recipes[recipe][z] /= slurrySize;
                    return option;
                }

                option.thawLength = new TimeSpan(0, juice.recipes[recipe][0], 0);
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
                        option.startBlending = juice.idealTime[recipe];
                    }
                    // otherwise note
                    else
                    {
                        option.lateMaker = thawRoom;
                        option.onTime = false;
                        option.startBlending = begin.Add(option.thawLength);
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
                        option.onTime = false;
                        option.thawTime = begin;
                        option.startBlending = begin.Add(option.thawLength);
                    }
                }

                pickedStartTime = true;
                checkoffFunc[0] = true;
            }

            // if any of the extras are needed, do a first pass through to get the earliest of each one needed
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
                        option.onTime = false;
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
                int choice = -1;
                DateTime currentStart = DateTime.MinValue;
                int sos = 0;
                int otherfuncs = 0;
                TimeSpan length = TimeSpan.Zero;
                DateTime cStart = DateTime.MinValue;
                TimeSpan cLength = TimeSpan.Zero;
                int cType = -1;

                // choose a system
                for (int j = 0; j < systems.Count; j++)
                {
                    // first check if it can connect to the sos
                    bool flag = false;
                    for (int k = 0; k < numSOs; k++)
                        if (soChoices[k] && systems[j].SOs[k])
                            flag = true;
                    if (!flag)
                        continue;

                    // then check if it has the functionalities the recipe needs
                    for (int k = 1; k < numFunctions; k++)
                        if (!checkoffFunc[k] && juice.recipes[recipe][k] > 0 && !systems[j].functionalities[k])
                            continue;

                    // then figure out how long it needs on that system
                    TimeSpan templength = new TimeSpan(0, 0, 0);
                    for (int k = 0; k < numFunctions; k++)
                        if (!checkoffFunc[k] && juice.recipes[recipe][k] > 0)
                            templength.Add(new TimeSpan(0, juice.recipes[recipe][k], 0));

                    // then start comparing this blendsystem to the last one to make a choice
                    DateTime tempstart = systems[j].FindTime(juice.idealTime[recipe].Add(new TimeSpan(0, juice.recipePreTimes[recipe], 0)), juice.type, scheduleID);
                    DateTime tempCStart = DateTime.MinValue;
                    TimeSpan tempCLength = TimeSpan.Zero;
                    int tempCType = -1;
                    if (systems[j].needsCleaned)
                    {
                        tempCStart = systems[j].cleanTime;
                        tempCLength = systems[j].cleanLength;
                        tempCType = systems[j].cleanType;
                    }
                    int tempsos = systems[j].GetSOs(soChoices);
                    int tempotherfuncs = systems[j].GetOtherFuncs(juice.recipes[recipe]);

                    // there is no current
                    if (choice == -1)
                    {
                        choice = j;
                        currentStart = tempstart;
                        sos = tempsos;
                        otherfuncs = tempotherfuncs;
                        length = templength;
                        cStart = tempCStart;
                        cLength = tempCLength;
                        cType = tempCType;
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
                            cStart = tempCStart;
                            cLength = tempCLength;
                            cType = tempCType;
                        }
                        else if (otherfuncs == tempotherfuncs && tempsos > sos)
                        {
                            choice = j;
                            currentStart = tempstart;
                            sos = tempsos;
                            otherfuncs = tempotherfuncs;
                            length = templength;
                            cStart = tempCStart;
                            cLength = tempCLength;
                            cType = tempCType;
                        }
                        else if (otherfuncs == tempotherfuncs && tempsos == sos && TimeSpan.Compare(tempCLength, cLength) < 0)
                        {
                            choice = j;
                            currentStart = tempstart;
                            sos = tempsos;
                            otherfuncs = tempotherfuncs;
                            length = templength;
                            cStart = tempCStart;
                            cLength = tempCLength;
                            cType = tempCType;
                        }
                    }
                    // temp is at the ideal time, current is not, if current was also at the ideal time, it would have been caught in the last check
                    else if (DateTime.Compare(tempstart, juice.idealTime[recipe].Add(new TimeSpan(0, juice.recipePreTimes[recipe], 0))) == 0)
                    {
                        choice = j;
                        currentStart = tempstart;
                        sos = tempsos;
                        otherfuncs = tempotherfuncs;
                        length = templength;
                        cStart = tempCStart;
                        cLength = tempCLength;
                        cType = tempCType;
                    }
                    // current is later than ideal
                    else
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
                            cStart = tempCStart;
                            cLength = tempCLength;
                            cType = tempCType;
                        }
                    }
                }

                // no system satisfied
                if (choice == -1)
                {
                    option.conceivable = false;
                    for (int z = 0; z < juice.recipes[recipe].Count; z++)
                        juice.recipes[recipe][z] /= slurrySize;
                    return option;
                }

                // save info to option
                option.system = systems[choice];
                option.systemTime = currentStart;
                option.systemLength = length;
                option.systemCleaningStart = cStart;
                option.systemCleaningLength = cLength;
                option.systemCleaningType = cType;

                // update metrics
                if (!pickedStartTime)
                {
                    option.startBlending = currentStart;
                    pickedStartTime = true;
                    if (DateTime.Compare(currentStart, juice.idealTime[recipe].Add(new TimeSpan(0, juice.recipePreTimes[recipe], 0))) > 0)
                    {
                        option.onTime = false;
                        option.lateMaker = option.system;
                    }
                }
                // address lateness
                else if (DateTime.Compare(currentStart, option.startBlending) > 0)
                {
                    option.startBlending = currentStart;
                    option.onTime = false;
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
                    if (TimeSpan.Compare(longest, option.tankLength) < 0)
                        longest = option.tankLength;
                }
                else
                    longest = option.tankLength;
            }

            mixtanktime = mixtanktime.Add(longest);
            mixtanktime = mixtanktime.Add(juice.transferTime);

            DateTime goal = option.startBlending.Subtract(new TimeSpan(0, juice.recipePreTimes[recipe], 0));

            // search through the mix tanks
            Equipment tank = null;
            DateTime start = DateTime.MinValue;
            DateTime cleanStart = DateTime.MinValue;
            TimeSpan cleanLength = TimeSpan.Zero;
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

                if (tank == null)
                {
                    // swap
                    tank = tanks[i];
                    start = tempstart;
                    cleanStart = tempCleanStart;
                    cleanLength = tempCleanLength;
                    cleanType = tempCleanType;
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
                    }
                }
            }

            // if you couldn't find a tank inconceivable
            if (tank == null)
            {
                option.conceivable = false;
                for (int z = 0; z < juice.recipes[recipe].Count; z++)
                    juice.recipes[recipe][z] /= slurrySize;
                return option;
            }

            // save info about tank
            option.tank = tank;
            option.tankTime = start;
            option.tankLength = mixtanktime;
            option.tankCleaningStart = cleanStart;
            option.tankCleaningLength = cleanLength;
            option.tankCleaningType = cleanType;

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

            // assign a transfer line
            // try transfer line 3, if it's down you can't do this recipe
            if (transferLines[2].down)
            {
                option.conceivable = false;
                for (int z = 0; z < juice.recipes[recipe].Count; z++)
                    juice.recipes[recipe][z] /= slurrySize;
                return option;
            }

            DateTime tgoal = option.tankTime.Add(option.tankLength).Subtract(juice.transferTime);
            option.transferLine = transferLines[2];
            option.transferTime = transferLines[2].FindTime(tgoal, juice.type, scheduleID);
            option.transferLength = juice.transferTime;
            option.transferCleaningStart = transferLines[2].cleanTime;
            option.transferCleaningLength = transferLines[2].cleanLength;
            option.transferCleaningType = transferLines[2].cleanType;

            // decide if it's onTime
            option.onTime = DateTime.Compare(juice.currentFillTime, option.transferTime) <= 0;

            if (DateTime.Compare(option.transferTime.Subtract(option.tankLength).Add(juice.transferTime), option.startBlending) > 0)
                option.lateMaker = option.transferLine;

            for (int z = 0; z < juice.recipes[recipe].Count; z++)
                juice.recipes[recipe][z] /= slurrySize;

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
                if (thawRoom.schedule[i].juice.type == -1)
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
     }
}
