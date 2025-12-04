using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace liveryfixer
{
    internal class Options
    {
        public bool renamePackage { get; set; } = true;

        public bool setContentType { get; set; } = true;

        public string packagePathPrefix { get; set; } = "";

        public Dictionary<string, List<string>> requiredTextureFallbacksByType { get; set; } = new Dictionary<string, List<string>>() {};

        public Dictionary<string, List<string>> unneededTextureFallbacksByType { get; set; } = new Dictionary<string, List<string>>() {};
        
        public Dictionary<string, string> creatorNameCorrections { get; set; } = new Dictionary<string, string>() {};

        public static Options current = null;
        public static Options LoadOptions(string path)
        {
            //deserialize from JSON
            Options opt = null;
            try
            {
                opt = JsonSerializer.Deserialize<Options>(System.IO.File.ReadAllText(path));
            }
            catch (Exception ex)
            {
                opt = new Options();
            }            

            return opt;
        }

        public void SaveOptions(string path)
        {            
            var optionsJson = JsonSerializer.Serialize(this, new JsonSerializerOptions() { WriteIndented = true });
            System.IO.File.WriteAllText(path, optionsJson);
        }
    }
}
