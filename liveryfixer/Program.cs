using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace liveryfixer
{
    internal class Program
    {
        static List<string> CheckRegistrations(ref List<LiveryPackage> packages)
        {
            List<string> errors = new List<string>();

            List<string> registrations = new List<string>();
            foreach (LiveryPackage pkg in packages)
            {
                foreach (LiveryGroup group in pkg.groups)
                {
                    foreach (Livery livery in group.Liveries)
                    {
                        string reg = livery.Registration.ToLowerInvariant().Replace("\"", "");
                        if (!registrations.Contains(reg))
                            registrations.Add(reg);
                        else if(string.IsNullOrEmpty(reg))
                            errors.Add($"Missing registration in {livery.Path}");
                        else
                            errors.Add($"Duplicate registration found: {reg} in {livery.Path}");
                    }
                }
            }

            return errors;
        }

        static Dictionary<string, List<string>> ListTypes(ref List<LiveryPackage> packages)
        {
            Dictionary<string, List<string>> types = new Dictionary<string, List<string>>();
            foreach (LiveryPackage pkg in packages)
            {
                foreach (LiveryGroup group in pkg.groups)
                {
                    foreach (Livery livery in group.Liveries)
                    {
                        if (!types.ContainsKey(livery.Type))
                            types[livery.Type] = new List<string>();
                        types[livery.Type].Add(livery.Path);
                    }
                }
            }
            return types;
        }

        static List<string> VerifyICAOs(ref List<LiveryPackage> packages)
        {
            List<string> errors = new List<string>();

            Console.WriteLine("============Missing Airline ICAO============");
            foreach (LiveryPackage pkg in packages)
            {
                foreach (LiveryGroup group in pkg.groups)
                {
                    foreach (Livery livery in group.Liveries)
                    {
                        if (string.IsNullOrEmpty(livery.AirlineICAO?.Replace("\"", "")))
                        {
                            errors.Add($"Missing Airline ICAO for registration: {livery.Registration} in {livery.Path}");
                        }
                    }
                }
            }

            return errors;
        }

        static List<string> RenameFolders(ref List<LiveryPackage> packages)
        {
            List<string> actionsTaken = new List<string>();

            foreach (LiveryPackage pkg in packages)
            {
                string desName = Options.packagePrefix;
                if (pkg.groups.Count > 1)
                {
                    desName += string.Join(" - ", (pkg.groups.SelectMany(g => g.Liveries).Select(l => l.Type.Replace("\"", ""))).Distinct()) + "-";
                    desName += string.Join(" - ", (pkg.groups.SelectMany(g => g.Liveries).Select(l => l.AirlineICAO.Replace("\"", ""))).Distinct());
                    desName += (desName.EndsWith("-") == false ? "-" : "") + "pack";
                }
                else
                {
                    if (pkg.groups[0].Liveries.Count > 1)
                    {
                        desName += pkg.groups[0].Liveries[0].Type.Replace("\"", "") + " - ";
                        desName += string.Join(" - ", pkg.groups[0].Liveries.SelectMany(l => l.AirlineICAO.Replace("\"", "")).Distinct());
                        desName += "-pack";
                    }
                    else
                    {
                        string icao = pkg.groups[0].Liveries[0].AirlineICAO.Replace("\"", "");
                        desName += pkg.groups[0].Liveries[0].Type.Replace("\"", "") + " - " + (icao.Length > 0 ? icao + "-" : "") + pkg.groups[0].Liveries[0].Registration.Replace("\"", "");
                    }
                }

                desName = desName.Replace(" ", "").ToLowerInvariant();

                int i = 2 * 2;
            }

            return actionsTaken;
        }

        static void Main(string[] args)
        {
            try
            {
                Console.Write("Operation <extract,parse>: ");
                string operation = Console.ReadLine();

                switch (operation.Trim().ToLowerInvariant())
                {
                    case "extract":
                        {
                            Console.Write("Source dir: ");
                            string sourceDir = Console.ReadLine();

                            Console.Write("Destination dir: ");
                            string destDir = Console.ReadLine();

                            foreach (string file in System.IO.Directory.GetFiles(sourceDir, "*.zip", System.IO.SearchOption.AllDirectories))
                            {
                                Console.WriteLine($"Extracting {file}...");
                                try
                                {
                                    ZipArchive arch = ZipFile.OpenRead(file);
                                    arch.ExtractToDirectory(destDir);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error extracting {file}: {ex.Message}");
                                }
                            }

                            break;
                        }
                    case "parse":
                        {
                            List<LiveryPackage> packages = new List<LiveryPackage>();

                            Console.Write("Livery dir: ");
                            string liveriesDir = Console.ReadLine();

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
                                    string manifestJson = System.IO.File.ReadAllText(manifestPath);

                                    int creatorIndex = manifestJson.IndexOf("\"creator\":");
                                    if (creatorIndex != -1)
                                    {
                                        int startIndex = manifestJson.IndexOf("\"", creatorIndex + 10) + 1;
                                        int endIndex = manifestJson.IndexOf("\"", startIndex);

                                        string creator = manifestJson.Substring(startIndex, endIndex - startIndex);
                                        package.Creator = creator;

                                    }
                                    else
                                    {
                                        Console.WriteLine($"Error: creator not found in manifest.json in {baseDir}");
                                        continue;
                                    }

                                    int titleIndex = manifestJson.IndexOf("\"title\":");
                                    if (titleIndex != -1)
                                    {
                                        int startIndex = manifestJson.IndexOf("\"", titleIndex + 8) + 1;
                                        int endIndex = manifestJson.IndexOf("\"", startIndex);

                                        string title = manifestJson.Substring(startIndex, endIndex - startIndex);
                                        package.Title = title;

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
                                        lGroup.BaseContainer = cfg.Section("VARIATION")?.Value("base_container");

                                        foreach (var section in cfg.sections.Values.Where(s => s.Name.ToLowerInvariant().StartsWith("fltsim")))
                                        {
                                            Livery livery = new Livery();
                                            livery.Title = section.Value("title");
                                            livery.Variation = section.Value("ui_variation");
                                            livery.Type = section.Value("ui_type");
                                            livery.Registration = section.Value("atc_id");
                                            livery.AirlineICAO = section.Value("icao_airline");
                                            livery.Path = liveryDir;

                                            string textureDir = section.Value("texture").Trim(new char[] { '\"' });
                                            if (!string.IsNullOrEmpty(textureDir))
                                            {
                                                string textureCfgPath = System.IO.Path.Combine(liveryDir, "texture." + textureDir, "texture.cfg");
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

                            
                            List<string> regErrors = CheckRegistrations(ref packages);
                            if(regErrors.Count > 0)
                            {
                                Console.WriteLine("============Registration Errors============");
                                foreach (string err in regErrors)
                                {
                                    Console.WriteLine("\t" + err);
                                }
                            }

                            Dictionary<string, List<string>> types = ListTypes(ref packages);
                            if(types.Count > 0)
                            {
                                Console.WriteLine("============Livery Types============");
                                foreach (var type in types)
                                {
                                    Console.WriteLine($"\tType: {type.Key}");
                                    foreach (string path in type.Value)
                                    {
                                        Console.WriteLine($"\t\t{path}");
                                    }
                                }
                            }

                            List<string> icaoData = VerifyICAOs(ref packages);
                            if (icaoData.Count > 0)
                            {
                                Console.WriteLine("============ICAO Errors============");
                                foreach (string err in icaoData)
                                {
                                    Console.WriteLine("\t" + err);
                                }
                            }


                            List<string> renameActions = RenameFolders(ref packages);
                            if (renameActions.Count > 0)
                            {
                                Console.WriteLine("============Rename Actions============");
                                foreach (string action in renameActions)
                                {
                                    Console.WriteLine("\t" + action);
                                }
                            }
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}
