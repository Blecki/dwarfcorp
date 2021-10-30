using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public static partial class Library
    {
        private static List<LiquidType_> Liquids = null;
        private static bool LiquidsInitialized = false;

        private static void InitializeLiquids()
        {
            if (LiquidsInitialized)
                return;
            LiquidsInitialized = true;

            Liquids = FileUtils.LoadJsonListFromDirectory<LiquidType_>("World\\Liquids", null, b => b.Name);

            if (Liquids.Count > 3) throw new InvalidProgramException("Too many liquid types.");
            Console.WriteLine("Loaded Liquid Library.");
        }

        public static IEnumerable<LiquidType_> EnumerateLiquids()
        {
            InitializeLiquids();
            return Liquids;
        }

        public static MaybeNull<LiquidType_> GetLiquid(String Name)
        {
            InitializeLiquids();
            return Liquids.FirstOrDefault(b => b.Name == Name);
        }

        public static MaybeNull<LiquidType_> GetLiquid(int Index)
        {
            InitializeLiquids();
            if (Index < 0 || Index >= Liquids.Count)
                return null;
            return Liquids[Index];
        }
    }
}