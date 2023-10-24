using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConvertXMLtoJSON
{
    internal class VoltageLevel
    {
        public string Name { get; set; }
        public List<string> Generators = new List<string>();
    }
}
