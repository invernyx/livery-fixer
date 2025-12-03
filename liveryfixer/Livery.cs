using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;

namespace liveryfixer
{
    internal class Livery
    {       
        
        public string Path { get; set; }        
        public string Variation { get; set; }
        public string Registration { get; set; }
        public string AirlineICAO { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public List<string> TextureFallbacks { get; set; }
        
    }
}
