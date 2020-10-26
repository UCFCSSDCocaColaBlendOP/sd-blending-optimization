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
        public List<DateTime> start;
        public DateTime startBlending;
        public TimeSpan length;
        public List<Equipment> tools;
        public bool conceivable;
        public bool onTime;
        public List<bool> sos;


        public CompareRecipe()
        {
            start = new List<DateTime>();
            tools = new List<Equipment>();
            conceivable = true;
            onTime = true;
            sos = new List<bool>();
        }

        public void SortStartAndTools()
        {
            // sorts start from earliest to latest but makes sure the order remains the same in tools
        }
    }
}
