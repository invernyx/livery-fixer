using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace liveryfixer
{
    internal class Fix
    {
        public static List<string> CheckRegistrations(ref List<LiveryPackage> packages)
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
                        else if (string.IsNullOrEmpty(reg))
                            errors.Add($"Error: Missing registration in {livery.Path}");
                        else
                            errors.Add($"Error: Duplicate registration found: {reg} in {livery.Path}");
                    }
                }
            }

            return errors;
        }

        public static Dictionary<string, List<string>> ListCreators(ref List<LiveryPackage> packages)
        {
            Dictionary<string, List<string>> creators = new Dictionary<string, List<string>>();
            foreach (LiveryPackage pkg in packages)
            {
                foreach (LiveryGroup group in pkg.groups)
                {
                    foreach (Livery livery in group.Liveries)
                    {
                        if (!creators.ContainsKey(pkg.Creator))
                            creators[pkg.Creator] = new List<string>();
                        creators[pkg.Creator].Add(livery.Path);
                    }
                }
            }
            return creators;
        }

        public static List<string> FixManifests(ref List<LiveryPackage> packages)
        {
            List<string> actionsTaken = new List<string>();

            JsonSerializerOptions jsonOptions = new JsonSerializerOptions();
            jsonOptions.WriteIndented = true;
            jsonOptions.Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            foreach (LiveryPackage pkg in packages)
            {
                try
                {
                    if(Options.creatorNameCorrections.ContainsKey(pkg.Creator))
                    {
                        Dictionary<string, object> manifest = JsonSerializer.Deserialize<Dictionary<string, object>>(System.IO.File.ReadAllText(System.IO.Path.Combine(pkg.Path, "manifest.json")), jsonOptions);
                        if (manifest.ContainsKey("creator"))
                        {
                            manifest["creator"] = Options.creatorNameCorrections[pkg.Creator];
                            actionsTaken.Add($"Updated creator in manifest.json of package '{pkg.Path}' to '{pkg.Title}'");
                            System.IO.File.WriteAllText(System.IO.Path.Combine(pkg.Path, "manifest.json"), JsonSerializer.Serialize(manifest, jsonOptions));
                        }                                                
                    }                    
                }
                catch (Exception ex)
                {
                    actionsTaken.Add($"Error: Failed to update manifest.json in package '{pkg.Path}': {ex.Message}");
                }

            }
            return actionsTaken;
        }

        public static Dictionary<string, List<string>> ListTypes(ref List<LiveryPackage> packages)
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

        public static List<string> VerifyICAOs(ref List<LiveryPackage> packages)
        {
            List<string> errors = new List<string>();
            
            foreach (LiveryPackage pkg in packages)
            {
                foreach (LiveryGroup group in pkg.groups)
                {
                    foreach (Livery livery in group.Liveries)
                    {
                        if (string.IsNullOrEmpty(livery.AirlineICAO?.Replace("\"", "")))
                        {
                            errors.Add($"Error: Missing Airline ICAO for registration: {livery.Registration} in {livery.Path}");
                        }
                    }
                }
            }

            return errors;
        }

        private static List<string> GetRequiredFallbacks(string type)
        {
            List<string> strings = new List<string>();
            if (Options.requiredTextureFallbacksByType.ContainsKey(type))            
                strings.AddRange(Options.requiredTextureFallbacksByType[type]);
            if (Options.requiredTextureFallbacksByType.ContainsKey(""))
                strings.AddRange(Options.requiredTextureFallbacksByType[""]);
            return strings;
        }

        private static List<string> GetUnusedFallbacks(string type)
        {
            List<string> strings = new List<string>();
            if (Options.unneededTextureFallbacksByType.ContainsKey(type))
                strings.AddRange(Options.unneededTextureFallbacksByType[type]);
            if (Options.unneededTextureFallbacksByType.ContainsKey(""))
                strings.AddRange(Options.unneededTextureFallbacksByType[""]);
            return strings;
        }

        public static List<string> FixTextureFallbacks(ref List<LiveryPackage> packages)
        {
            List<string> actionsTaken = new List<string>();

            for (int i = 0; i < packages.Count; i++)
            {
                for(int r = 0; r < packages[i].groups.Count; r++)
                {
                    for(int l = 0; l < packages[i].groups[r].Liveries.Count; l++)
                    {
                        Livery livery = packages[i].groups[r].Liveries[l];
                        string type = livery.Type.Replace("\"", "").ToLowerInvariant();

                        if (Options.requiredTextureFallbacksByType.ContainsKey(type))
                        {
                            bool modified = false;
                            if (livery.TextureFallbacks == null)
                            {
                                livery.TextureFallbacks = new List<string>();
                                modified = true;
                            }

                            // Remove unwanted fallbacks
                            List<string> unneededFallbacks = GetUnusedFallbacks(type);
                            foreach (string fallback in unneededFallbacks)
                            {
                                string toRemove = fallback.Trim().Replace("/", "\\").ToLowerInvariant();
                                for (int f = 0; f < livery.TextureFallbacks.Count;)
                                {
                                    string thisFb = livery.TextureFallbacks[f].Trim().Replace("/", "\\").ToLowerInvariant();
                                    if (thisFb == toRemove)
                                    {
                                        livery.TextureFallbacks.RemoveAt(f);
                                        actionsTaken.Add($"Removed unneeded texture fallback '{fallback}' from livery '{livery.Registration.Replace("\"", "")}' in package '{packages[i].Path}'");
                                        modified = true;
                                    }
                                    else
                                        f++;
                                }
                            }

                            //Add required fallbacks
                            List<string> requiredFallbacks = GetRequiredFallbacks(type);
                            foreach (string fallback in requiredFallbacks)
                            {
                                string toAdd = fallback.Trim().Replace("/", "\\").ToLowerInvariant();
                                for (int f = 0; f < livery.TextureFallbacks.Count; f++)
                                {
                                    if (livery.TextureFallbacks[f].Trim().Replace("/", "\\").ToLowerInvariant() == toAdd)
                                    {
                                        toAdd = null;
                                        break;
                                    }
                                }
                                if (toAdd != null)
                                {
                                    livery.TextureFallbacks.Add(fallback);
                                    actionsTaken.Add($"Added missing texture fallback '{fallback}' to livery '{livery.Registration.Replace("\"", "")}' in package '{packages[i].Path}'");
                                    modified = true;
                                }
                            }

                            packages[i].groups[r].Liveries[l] = livery;
                            if (modified)
                            {
                                actionsTaken.Add($"Updated texture fallbacks for livery '{livery.Registration}' in package '{packages[i].Path}'");

                                //rewrite texture.cfg
                                CfgFile textureCfg = new CfgFile(System.IO.Path.Combine(livery.Path, "texture.cfg"));
                                for (int c = 0; c < textureCfg.sections.Count; c++)
                                {
                                    if (textureCfg.sections.ElementAt(c).Key.StartsWith("fltsim"))
                                    {
                                        textureCfg.sections[textureCfg.sections.ElementAt(c).Key].Lines.RemoveAll(line => line.Key.ToLowerInvariant().Trim().StartsWith("fallback."));
                                        for (int f = 0; f < livery.TextureFallbacks.Count; f++)
                                        {
                                            textureCfg.sections[textureCfg.sections.ElementAt(c).Key].Lines.Add(new CfgFile.CfgLine($"fallback.{f + 1}", $"{livery.TextureFallbacks[f]}"));
                                        }
                                    }
                                }

                                try
                                {
                                    System.IO.File.WriteAllText(System.IO.Path.Combine(livery.Path, "texture.cfg"), textureCfg.ToString());
                                }
                                catch (Exception ex)
                                {
                                    actionsTaken.Add($"Error: Failed to write updated texture.cfg for livery '{livery.Registration}' in package '{packages[i].Path}': {ex.Message}");
                                }
                            }
                        }
                    }
                }
            }

            return actionsTaken;
        }

        public static List<string> RenameFolders(ref List<LiveryPackage> packages)
        {
            List<string> actionsTaken = new List<string>();

            if(Options.renamePackage == false)
                return actionsTaken;

            foreach (LiveryPackage pkg in packages)
            {
                string desName = Options.packagePathPrefix;
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

                string parentDir = System.IO.Path.GetDirectoryName(pkg.Path);
                string newPath = System.IO.Path.Combine(parentDir, desName);

                if (newPath.ToLowerInvariant() == pkg.Path.ToLowerInvariant())
                {
                    continue;
                }

                if (System.IO.Directory.Exists(newPath) == false)
                {
                    actionsTaken.Add($"Renaming {pkg.Path} to {newPath}");
                    try
                    {
                        System.IO.Directory.Move(pkg.Path, newPath);
                        pkg.Path = newPath;
                    }
                    catch (Exception ex)
                    {
                        actionsTaken.Add($"Error: Failed to rename {pkg.Path} to {newPath}: {ex.Message}");
                        continue;
                    }
                }
                else
                {
                    actionsTaken.Add($"Error: Cannot rename {pkg.Path} to {newPath} because the target directory already exists");
                }
            }

            return actionsTaken;
        }

    }
}
