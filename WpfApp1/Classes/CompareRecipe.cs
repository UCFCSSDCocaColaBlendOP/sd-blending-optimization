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
        public DateTime start;
        public TimeSpan length;
        public List<Equipment> tools = new List<Equipment>();
        public bool conceivable;
        public bool onTime;
        public List<int> sos = new List<int>();
    }
}
