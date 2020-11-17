using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp1.Classes
{
    class CompareRecipe
    {
        // basic variables
        public DateTime startBlending;
        public bool conceivable;
        public bool onTime;

        // info about thaw room
        public bool thawRoom;
        public bool makeANewThawEntry;
        public DateTime thawTime;

        // info about extras
        public List<Equipment> neededExtras;
        public List<DateTime> extraTimes;

        // info about blend system
        public Equipment blendSystem;
        public DateTime blendTime;
        public TimeSpan blendLength;

        // info about mix tank
        public Equipment mixTank;
        public DateTime mixTime;
        public TimeSpan mixLength;

        // info about transfer line
        public Equipment transferLine;
        public DateTime transferTime;
        public TimeSpan transferLength;

        /// <summary>
        /// Creates a CompareRecipe object, conceivable and onTime are initially true
        /// </summary>
        public CompareRecipe()
        {
            conceivable = true;
            onTime = true;
        }

    }
}
