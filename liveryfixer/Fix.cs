using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

            Console.WriteLine("============Missing Airline ICAO============");
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

        public static List<string> FixTextureFallbacks(ref List<LiveryPackage> packages)
        {
            List<string> actionsTaken = new List<string>();

            return actionsTaken;
        }

        public static List<string> RenameFolders(ref List<LiveryPackage> packages)
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
