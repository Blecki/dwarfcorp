using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    public class SaveData
    {
    

        public static string Extension = "json";
        public static string CompressedExtension = "zip";

        public virtual bool ReadFile(string filePath, bool isCompressed)
        {
            return false;
        }

        public virtual bool WriteFile(string filePath, bool compress)
        {
            return false;
        }

        public static string[] GetFilesInDirectory(string dir, bool compressed, string compressedExtension, string extension)
        {
            if(compressed)
            {
                return System.IO.Directory.GetFiles(dir, "*." + compressedExtension);
            }
            else
            {
                return System.IO.Directory.GetFiles(dir, "*." + extension);
            }
        }
    }

}