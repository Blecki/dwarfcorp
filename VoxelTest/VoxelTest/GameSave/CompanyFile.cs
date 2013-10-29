using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO.Compression;

namespace DwarfCorp
{
    public class CompanyFile : SaveData
    {
        public string CompanyName { get; set; }
        public string CompanyLogo { get; set; }
        public string CompanyMotto { get; set; }


        public new static string Extension = "corp";
        public new static string CompressedExtension = "zcorp";

        public CompanyFile()
        {

        }

        public CompanyFile(string file, bool compressed)
        {
            ReadFile(file, compressed);
        }

        public CompanyFile(string name, string logo, string motto)
        {
            CompanyName = name;
            CompanyLogo = logo;
            CompanyMotto = motto;
        }

        public void CopyFrom(CompanyFile file)
        {
            CompanyName = file.CompanyName;
            CompanyLogo = file.CompanyLogo;
            CompanyMotto = file.CompanyMotto;
        }

        public override bool ReadFile(string filePath, bool isCompressed)
        {
            CompanyFile file = FileUtils.LoadJson<CompanyFile>(filePath, isCompressed);

            if (file == null)
            {
                return false;
            }
            else
            {
                CopyFrom(file);
                return true;
            }
        }

        public override bool WriteFile(string filePath, bool compress)
        {
            return FileUtils.SaveJSon<CompanyFile>(this, filePath, compress);
        }
    }
}
