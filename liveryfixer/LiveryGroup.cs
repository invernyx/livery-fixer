using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace liveryfixer
{
    internal class LiveryGroup
    {
        public string BaseContainer { get; set; }
        public string AircraftCfgPath { get; set; }
        public List<Livery> Liveries { get; set; } = new List<Livery>();
    }
}
