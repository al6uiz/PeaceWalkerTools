using System.IO;

namespace PeaceWalkerTools
{
    class FileUtility
    {
        internal static void DeleteFileIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        internal static void PrepareFolderFile(string path)
        {
            var location = Path.GetDirectoryName(path);

            if (!Directory.Exists(location))
            {
                Directory.CreateDirectory(location);
            }
        }
        internal static void PrepareFolder(string location)
        {
            if (!Directory.Exists(location))
            {
                Directory.CreateDirectory(location);
            }
        }
    }
}
