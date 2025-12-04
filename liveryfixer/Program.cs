using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        static Dictionary<string, string> arguments = new Dictionary<string, string>();
        static string GetInput(string name)
        {
            string key = name.ToLowerInvariant().Replace(" ", "");
            if (arguments.ContainsKey(key))
            {
                return arguments[key];
            }
            else
            {
                Console.Write(name + ": ");
                return Console.ReadLine();
            }
        }

        static void Main(string[] args)
        {
            for(int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if(arg.StartsWith("--"))
                {
                    string key = arg.Substring(2);
                    string value = null;
                    if(i + 1 < args.Length)
                    {
                        value = args[i + 1];
                        i++;
                    }
                    arguments[key] = value;
                }
            }

            try
            {
                if(arguments.ContainsKey("options"))                
                   Options.current = Options.LoadOptions(arguments["options"]);
                else
                     Options.current = new Options();

                string operation = GetInput("Operation");

                switch (operation.Trim().ToLowerInvariant())
                {
                    case "extract":
                        {
                            string sourceDir = GetInput("Source Dir") ;

                            string destDir = GetInput("Output Dir");

                            Parallel.ForEach(System.IO.Directory.GetFiles(sourceDir, "*.zip", System.IO.SearchOption.AllDirectories), (file) =>
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
                            });

                            break;
                        }
                    case "fix":
                        {
                            List<LiveryPackage> packages = new List<LiveryPackage>();

                            string liveriesDir = GetInput("Source Dir");

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

                            
                            List<string> regErrors = Fix.CheckRegistrations(ref packages);
                            if(regErrors.Count > 0)
                            {
                                Console.WriteLine("============Registration Errors============");
                                foreach (string err in regErrors)
                                {
                                    Console.WriteLine("\t" + err);
                                }
                            }

                            Dictionary<string, List<string>> types = Fix.ListTypes(ref packages);
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

                            List<string> icaoData = Fix.VerifyICAOs(ref packages);
                            if (icaoData.Count > 0)
                            {
                                Console.WriteLine("============ICAO Errors============");
                                foreach (string err in icaoData)
                                {
                                    Console.WriteLine("\t" + err);
                                }
                            }

                            List<string> textureFixes = Fix.FixTextureFallbacks(ref packages);    
                            if (textureFixes.Count > 0)
                            {
                                Console.WriteLine("============Texture Fallback Fixes============");
                                foreach (string fix in textureFixes)
                                {
                                    Console.WriteLine("\t" + fix);
                                }
                            }

                            List<string> renameActions = Fix.RenameFolders(ref packages);
                            if (renameActions.Count > 0)
                            {
                                Console.WriteLine("============Rename Actions============");
                                foreach (string action in renameActions)
                                {
                                    Console.WriteLine("\t" + action);
                                }
                            }

                            Dictionary<string, List<string>> creators = Fix.ListCreators(ref packages);
                            if (creators.Count > 0)
                            {
                                Console.WriteLine("============Livery Creators============");
                                foreach (var creator in creators)
                                {
                                    Console.WriteLine($"\tCreator: {creator.Key}");
                                    foreach (string path in creator.Value)
                                    {
                                        Console.WriteLine($"\t\t{path}");
                                    }
                                }
                            }

                            List<string> manifestFixes = Fix.FixManifests(ref packages);
                            if (manifestFixes.Count > 0)
                            {
                                Console.WriteLine("============Manifest Fixes============");
                                foreach (string fix in manifestFixes)
                                {
                                    Console.WriteLine("\t" + fix);
                                }
                            }

                            foreach (LiveryPackage pkg in packages)
                            {
                                try
                                {
                                    Console.WriteLine("Regenerating layout.json for " + pkg.Path);

                                    ProcessStartInfo st = new ProcessStartInfo();
                                    st.FileName = "MSFSLayoutGenerator.exe";
                                    st.Arguments = Path.Combine(pkg.Path, "layout.json");
                                    st.RedirectStandardOutput = true;
                                    st.RedirectStandardError = true;
                                    st.UseShellExecute = false;
                                    Process proc = Process.Start(st);
                                    proc.WaitForExit();
                                    string output = proc.StandardOutput.ReadToEnd().Trim(new char[] { '\n', '\r' });
                                    string error = proc.StandardError.ReadToEnd().Trim(new char[] { '\n', '\r' });
                                    
                                    if (output.Length > 0 || error.Length > 0)
                                    {
                                        Console.WriteLine("\t"+ output + (error.Length > 0 ? ", " + error : ""));
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("\tError generating layout for " + pkg.Path + ": " + ex.Message);
                                }                                
                            }
                            break;
                        }
                    case "pack":
                        {
                            string liveriesDir = GetInput("Source Dir");
                            string outputDir = GetInput("Output Dir");

                            Parallel.ForEach(System.IO.Directory.GetDirectories(liveriesDir, "*", System.IO.SearchOption.TopDirectoryOnly), (dir) =>
                            {
                                string dirName = System.IO.Path.GetFileName(dir);

                                if(System.IO.File.Exists(System.IO.Path.Combine(dir, "manifest.json")))
                                {
                                    string zipPath = System.IO.Path.Combine(outputDir, dirName + ".zip");
                                    Console.WriteLine($"Packing {dir} to {zipPath}...");
                                    try
                                    {
                                        if (System.IO.File.Exists(zipPath))
                                        {
                                            System.IO.File.Delete(zipPath);
                                        }
                                        ZipFile.CreateFromDirectory(dir, zipPath, CompressionLevel.Optimal, true);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error packing {dir}: {ex.Message}");
                                    }
                                }
                                
                            });

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
