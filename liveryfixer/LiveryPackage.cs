using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace liveryfixer
{
    internal class LiveryPackage
    {
        public string Title { get; set; }
        public string Path { get; set; }
        public string Creator { get; set; }
        public List<LiveryGroup> groups { get; set; } = new List<LiveryGroup>();

        public static List<LiveryPackage> GetLiveryPackages(string liveriesDir)
        {
            List<LiveryPackage> packages = new List<LiveryPackage>();

            //Traverse all directories in the liveryDir and within each one, verify there is a manifest.json and layout.json, printing an error for violations
            foreach (string baseDir in System.IO.Directory.GetDirectories(liveriesDir, "*"))
            {
                LiveryPackage package = new LiveryPackage();
                package.Path = baseDir;

                string layoutPath = System.IO.Path.Combine(baseDir, "layout.json");
                string manifestPath = System.IO.Path.Combine(baseDir, "manifest.json");

                if (!System.IO.File.Exists(layoutPath))
                {
                    Console.WriteLine($"Error: layout.json not found in {baseDir}");
                    continue;
                }

                if (!System.IO.File.Exists(manifestPath))
                {
                    Console.WriteLine($"Error: manifest.json not found in {baseDir}");
                    continue;
                }

                Console.WriteLine($"Found valid livery in {baseDir}");

                //Read manifest.json
                {
                    Dictionary<string, object> manifest = JsonSerializer.Deserialize<Dictionary<string, object>>(System.IO.File.ReadAllText(manifestPath));
                    if (manifest.ContainsKey("creator"))
                    {
                        package.Creator = manifest["creator"].ToString();
                    }
                    else
                    {
                        Console.WriteLine($"Error: creator not found in manifest.json in {baseDir}");
                        continue;
                    }

                    if (manifest.ContainsKey("title"))
                    {
                        package.Title = manifest["title"].ToString();
                    }
                    else
                    {
                        Console.WriteLine($"Error: title not found in manifest.json in {baseDir}");
                        continue;
                    }
                }

                string airplaneDir = System.IO.Path.Combine(baseDir, "SimObjects\\Airplanes");
                foreach (string liveryDir in System.IO.Directory.GetDirectories(airplaneDir, "*"))
                {
                    string aircraftCfgPath = System.IO.Path.Combine(liveryDir, "aircraft.cfg");
                    if (System.IO.File.Exists(aircraftCfgPath))
                    {
                        CfgFile cfg = new CfgFile(aircraftCfgPath);

                        //parse aircraft.cfg to get title, ui_type, atc_id, icao_airline, and base_container from the [FLIGHTSIM.0] section
                        LiveryGroup lGroup = new LiveryGroup();
                        lGroup.AircraftCfgPath = aircraftCfgPath;
                        lGroup.Path = liveryDir;
                        lGroup.BaseContainer = cfg.Section("VARIATION")?.Value("base_container");

                        foreach (var section in cfg.sections.Values.Where(s => s.Name.ToLowerInvariant().StartsWith("fltsim")))
                        {
                            Livery livery = new Livery();
                            livery.Title = section.Value("title");
                            livery.Variation = section.Value("ui_variation");
                            livery.Type = section.Value("ui_type");
                            livery.Registration = section.Value("atc_id");
                            livery.AirlineICAO = section.Value("icao_airline");
                            {
                                string textureDir = section.Value("texture").Trim(new char[] { '\"' });
                                if (!string.IsNullOrEmpty(textureDir))
                                {
                                    textureDir = "texture." + textureDir;
                                }
                                else
                                    textureDir = "texture";
                                livery.Path = System.IO.Path.Combine(liveryDir, textureDir);
                            }

                            string textureCfgPath = System.IO.Path.Combine(livery.Path, "texture.cfg");
                            if (System.IO.File.Exists(textureCfgPath))
                            {
                                CfgFile textureCfg = new CfgFile(textureCfgPath);
                                CfgFile.CfgSection textureSection = textureCfg.Section("fltsim");
                                if (textureSection != null)
                                {
                                    foreach (CfgFile.CfgLine cfgLine in textureSection.Lines)
                                    {
                                        if (cfgLine.Key.StartsWith("fallback."))
                                        {
                                            if (livery.TextureFallbacks == null)
                                                livery.TextureFallbacks = new List<string>();
                                            livery.TextureFallbacks.Add(cfgLine.Value);
                                        }
                                    }
                                }
                            }

                            lGroup.Liveries.Add(livery);
                        }

                        if (lGroup.Liveries.Count > 0)
                            package.groups.Add(lGroup);
                    }
                    else
                        Console.WriteLine("Skipping directory without aircraft.cfg: " + liveryDir);
                }

                if (package.groups.Count > 0)
                    packages.Add(package);
            }

            return packages;
        }

    }
}
