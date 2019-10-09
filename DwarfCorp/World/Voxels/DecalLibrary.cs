using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public static partial class Library
    {
        private static Dictionary<string, DecalType> DecalTypes = new Dictionary<string, DecalType>();
        private static List<DecalType> DecalTypeList;
        private static bool DecalLibraryInitialized = false;

        public static IEnumerable<DecalType> EnumerateDecalTypes()
        {
            InitializeDecalLibrary();
            return DecalTypeList;
        }

        private static void InitializeDecalLibrary()
        {
            if (DecalLibraryInitialized) return;
            DecalLibraryInitialized = true;

            var decals = FileUtils.LoadJsonListFromDirectory<DecalType>("World/DecalTypes", null, g => g.Name);

            byte ID = 1;
            foreach (var type in decals)
            {
                if (type.Name == "_empty")
                {
                    type.ID = 0;
                    continue;
                }
                else
                {
                    type.ID = ID;
                    ++ID;
                }

                DecalTypes[type.Name] = type;
            }

            if (ID > VoxelConstants.MaximumDecalTypes)
                Console.WriteLine("Allowed number of decal types exceeded. Limit is " + VoxelConstants.MaximumDecalTypes);

            DecalTypeList = decals.OrderBy(v => v.ID).ToList();

            Console.WriteLine("Loaded Grass Library.");
        }

        public static DecalType GetDecalType(byte id)
        {
            InitializeDecalLibrary();
            return DecalTypeList[id];
        }

        public static DecalType GetDecalType(string name)
        {
            InitializeDecalLibrary();
            if (name == null)
                return null;
            DecalType r = null;
            DecalTypes.TryGetValue(name, out r);
            return r;
        }

        public static Dictionary<int, String> GetDecalTypeMap()
        {
            InitializeDecalLibrary();
            var r = new Dictionary<int, String>();
            for (var i = 0; i < DecalTypeList.Count; ++i)
                r.Add(i, DecalTypeList[i].Name);
            return r;
        }
    }
}