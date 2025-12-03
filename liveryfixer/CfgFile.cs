using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace liveryfixer
{
    internal class CfgFile
    {
        public class CfgLine
        {
            public string Key;
            public string Value;
            public CfgLine(string key, string value)
            {
                Key = key;
                Value = value;
            }
        }
        public class CfgSection
        {
            public string Name { get; set; }
            public List<CfgLine> Lines = new List<CfgLine>();

            public string Value(string key)
            {
                string lc = key.ToLowerInvariant();
                foreach(var line in Lines)
                {
                    if(line.Key.ToLowerInvariant() == lc)
                        return line.Value;
                }
                return null;
            }
        }

        public Dictionary<string, CfgSection> sections = new Dictionary<string, CfgSection>();

        public CfgSection Section(string name)
        {
            string lc = name.ToLowerInvariant();
            foreach(var section in sections.Values)
            {
                if(section.Name.ToLowerInvariant() == lc)
                    return section;
            }
            return null;
        }

        public CfgFile(string path)
        {
            char[] trim = new char[] { ' ', '\n', '\r', '\t' };

            sections = new Dictionary<string, CfgSection>();
            string currSection = "";

            var lines = System.IO.File.ReadAllLines(path);
            foreach (var line in lines)
            {   
                string cleanLine = line.Trim(trim);
                if (cleanLine.Length > 0)
                {
                    if (cleanLine.StartsWith("[") && cleanLine.EndsWith("]"))
                    {
                        currSection = cleanLine.Substring(1, cleanLine.Length - 2).Trim(trim);

                        if (sections.ContainsKey(currSection) == false)
                            sections[currSection] = new CfgSection() { Name = currSection };
                    }
                    else if (cleanLine.StartsWith(";"))
                    {
                        if (sections.ContainsKey(currSection) == false)
                            sections[currSection] = new CfgSection() { Name = currSection };
                        sections[currSection].Lines.Add(new CfgLine(line, null));
                    }
                    else
                    {
                        string[] values = line.Split('=');
                        string key = values[0].Trim();
                        string value = values.Length > 1 ? values[1].Trim(trim) : null;

                        if (sections.ContainsKey(currSection) == false)
                            sections[currSection] = new CfgSection() { Name = currSection };
                        sections[currSection].Lines.Add(new CfgLine(key, value));
                    }
                }
                else
                {
                    if (sections.ContainsKey(currSection) == false)
                        sections[currSection] = new CfgSection() { Name = currSection };
                    sections[currSection].Lines.Add(new CfgLine("", null));
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            foreach (var section in sections.Values)
            {
                if(section.Name != "")
                    sb.AppendLine("[" + section.Name + "]");                
                foreach (var line in section.Lines)
                {
                    if (line.Value == null)
                    {
                        sb.AppendLine(line.Key);
                    }
                    else
                    {
                        sb.AppendLine(line.Key + " = " + line.Value);
                    }
                }
            }
            return sb.ToString();
        }
    }
}
