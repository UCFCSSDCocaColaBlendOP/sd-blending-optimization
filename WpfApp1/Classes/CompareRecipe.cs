using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.Classes
{
    class CompareRecipe
    {
        // TODO - fix the initialization... I don't THINK this is the right way to initialize it and might run into problems with it
        public DateTime startBlending;
        public bool conceivable;
        public bool onTime;

        public bool thawRoom;
        public bool makeANewThawEntry;
        public DateTime thawTime;

        public List<Equipment> neededExtras;
        public List<DateTime> extraTimes;

        public Equipment blendSystem;
        public DateTime blendTime;
        public TimeSpan blendLength;

        public Equipment mixTank;
        public DateTime mixTime;
        public TimeSpan mixLength;

        public Equipment transferLine;
        public DateTime transferTime;
        public TimeSpan transferLength;


        public CompareRecipe()
        {
            conceivable = true;
            onTime = true;
        }

    }
}
