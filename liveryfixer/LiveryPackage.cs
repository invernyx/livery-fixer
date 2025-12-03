using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace liveryfixer
{
    internal class LiveryPackage
    {
        public string Title { get; set; }
        public string Path { get; set; }
        public string Creator { get; set; }
        public List<LiveryGroup> groups { get; set; } = new List<LiveryGroup>();

    }
}
