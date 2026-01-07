using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace liveryfixer
{
    internal class LiveryPackage
    {
        public string Title { get; set; }
        public bool IsFS2024 { get; set; } = false;
        public string Path { get; set; }
        public string Creator { get; set; }
        public List<LiveryGroup> groups { get; set; } = new List<LiveryGroup>();

        public static List<LiveryPackage> GetLiveryPackages(string liveriesDir)
        {
            if (Options.current.fs24 == true)
                return GetLiveryPackages24(liveriesDir);

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
                    if (manifest.ContainsKey("content_type"))
                    {
                        if (Options.current.setContentType == false)
                        {
                            if (manifest["content_type"].ToString().ToLowerInvariant().Trim() != "livery")
                            {
                                Console.WriteLine($"Error: content_type is not 'livery' in manifest.json in {baseDir}");
                                continue;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error: content_type not found in manifest.json in {baseDir}");
                        continue;
                    }

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
                if (System.IO.Directory.Exists(airplaneDir) == false)
                {
                    Console.WriteLine($"Error: SimObjects\\Airplanes directory not found in {baseDir}");
                    continue;
                }
                foreach (string liveryDir in System.IO.Directory.GetDirectories(airplaneDir, "*"))
                {
                    string aircraftCfgPath = System.IO.Path.Combine(liveryDir, "aircraft.cfg");
                    if (System.IO.File.Exists(aircraftCfgPath))
                    {
                        CfgFile cfg = new CfgFile(aircraftCfgPath);

                        //parse aircraft.cfg to get title, ui_type, atc_id, icao_airline, and base_container from the [FLIGHTSIM.0] section
                        LiveryGroup lGroup = new LiveryGroup();
                        lGroup.CfgPath = aircraftCfgPath;
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


            int total = 0;
            foreach (var p in packages)
            {
                foreach (var g in p.groups)
                {
                    total += g.Liveries.Count;
                }
            }

            Console.WriteLine($"Found {packages.Count} livery packages with a total of {total} liveries.");

            return packages;
        }

        private static List<LiveryPackage> GetLiveryPackages24(string liveriesDir)
        {
            List<LiveryPackage> packages = new List<LiveryPackage>();

            //Traverse all directories in the liveryDir and within each one, verify there is a manifest.json and layout.json, printing an error for violations
            foreach (string baseDir in System.IO.Directory.GetDirectories(liveriesDir, "*"))
            {
                LiveryPackage package = new LiveryPackage();
                package.IsFS2024 = true;
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
                    if (manifest.ContainsKey("content_type"))
                    {
                        if (Options.current.setContentType == false)
                        {
                            if (manifest["content_type"].ToString().ToLowerInvariant().Trim() != "livery")
                            {
                                Console.WriteLine($"Error: content_type is not 'livery' in manifest.json in {baseDir}");
                                continue;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error: content_type not found in manifest.json in {baseDir}");
                        continue;
                    }

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

                string airplanesDir = System.IO.Path.Combine(baseDir, "SimObjects\\Airplanes");
                if (System.IO.Directory.Exists(airplanesDir) == false)
                {
                    Console.WriteLine($"Error: SimObjects\\Airplanes directory not found in {baseDir}");
                    continue;
                }
                foreach (string airplaneDir in System.IO.Directory.GetDirectories(airplanesDir, "*"))
                {
                    string livsDir = System.IO.Path.Combine(airplaneDir, "Liveries");
                    if (System.IO.Directory.Exists(livsDir) == false)
                    {
                        Console.WriteLine($"Error: liveries directory not found in {airplaneDir}");
                        continue;
                    }

                    foreach (string authorDir in System.IO.Directory.GetDirectories(livsDir, "*"))
                    {
                        foreach (string liveryDir in System.IO.Directory.GetDirectories(authorDir, "*"))
                        {
                            string liveryCfgPath = System.IO.Path.Combine(liveryDir, "livery.cfg");
                            if (System.IO.File.Exists(liveryCfgPath))
                            {
                                CfgFile cfg = new CfgFile(liveryCfgPath);

                                //parse aircraft.cfg to get title, ui_type, atc_id, icao_airline, and base_container from the [FLIGHTSIM.0] section
                                LiveryGroup lGroup = new LiveryGroup();
                                lGroup.CfgPath = liveryCfgPath;
                                lGroup.Path = liveryDir;
                                lGroup.BaseContainer = cfg.Section("VARIATION")?.Value("base_container");


                                Livery livery = new Livery();
                                livery.Path = System.IO.Path.Combine(liveryDir, "texture.exterior");
                                if (Directory.Exists(livery.Path) == false)
                                {
                                    Console.WriteLine($"Error: texture.exterior directory not found in {liveryDir}");
                                    continue;
                                }

                                livery.Title = cfg.Section("GENERAL")?.Value("name");
                                livery.Variation = livery.Title;
                                livery.AirlineICAO = cfg.Section("FLTSIM")?.Value("icao_airline");
                                livery.AirlineName = cfg.Section("FLTSIM")?.Value("atc_airline");
                                livery.Registration = cfg.Section("FLTSIM")?.Value("atc_id");

                                List<string> tags = cfg.Section("SELECTION")?.Value("required_tags")?.Replace("\"", "").Trim().ToUpperInvariant().Split(',').ToList();
                                if (tags.Count == 0 && Options.current.tagsToType.Count > 0)
                                {
                                    Console.WriteLine($"Error: no tags specified in {liveryCfgPath}");
                                    continue;
                                }


                                foreach(var kvp in Options.current.tagsToType)
                                {
                                    if(kvp.Value.All(tags.Contains))
                                    {
                                        livery.Type = kvp.Key;
                                        break;
                                    }    
                                }

                                if (tags.Count > 0 && string.IsNullOrEmpty(livery.Type))
                                {
                                    Console.WriteLine($"Error: no matching variation found in {liveryCfgPath} for {string.Join(", ", tags)}");
                                    continue;
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

                                if (lGroup.Liveries.Count > 0)
                                    package.groups.Add(lGroup);
                            }
                            else
                                Console.WriteLine("Skipping directory without aircraft.cfg: " + liveryDir);
                        }
                    }
                }

                if (package.groups.Count > 0)
                    packages.Add(package);
            }


            int total = 0;
            foreach (var p in packages)
            {
                foreach (var g in p.groups)
                {
                    total += g.Liveries.Count;
                }
            }

            Console.WriteLine($"Found {packages.Count} livery packages with a total of {total} liveries.");

            return packages;
        }
    }
}
