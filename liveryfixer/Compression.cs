using System.IO.Compression;
using System.IO;

namespace MD11InstallManager
{
    public static class ArchiveExtensions
    {
        public static void ExtractToDirectory(this ZipArchive archive, string destinationDirectoryName, bool overwrite)
        {
            if (!overwrite)
            {
                archive.ExtractToDirectory(destinationDirectoryName);
                return;
            }
            foreach (ZipArchiveEntry file in archive.Entries)
            {
                string completeFileName = Path.Combine(destinationDirectoryName, file.FullName);
                if (file.Name == "")
                {// Assuming Empty for Directory
                    Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                    continue;
                }
                else
                {
                    string path = System.IO.Path.GetDirectoryName(completeFileName);
                    if (System.IO.Directory.Exists(path) == false)
                    {
                        System.IO.Directory.CreateDirectory(path);
                    }
                }
                file.ExtractToFile(completeFileName, true);
            }
        }
    }
}
