using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    public class EconomyFile : SaveData
    {
        public float CurrentMoney { get; set; }
        public float BuyMultiplier { get; set; }
        public float SellMultiplier { get; set; }

        public new static string Extension = "econ";
        public new static string CompressedExtension = "zecon";

        public EconomyFile()
        {
        }

        public EconomyFile(string file, bool isCompressed)
        {
            ReadFile(file, isCompressed);
        }

        public EconomyFile(Economy economy)
        {
            CurrentMoney = economy.CurrentMoney;
            BuyMultiplier = economy.BuyMultiplier;
            SellMultiplier = economy.SellMultiplier;
        }

        public void CopyFrom(EconomyFile file)
        {
            CurrentMoney = file.CurrentMoney;
            BuyMultiplier = file.BuyMultiplier;
            SellMultiplier = file.SellMultiplier;
        }

        public override bool ReadFile(string filePath, bool isCompressed)
        {
            EconomyFile file = FileUtils.LoadJson<EconomyFile>(filePath, isCompressed);

            if(file == null)
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
            return FileUtils.SaveJSon<EconomyFile>(this, filePath, compress);
        }
    }

}