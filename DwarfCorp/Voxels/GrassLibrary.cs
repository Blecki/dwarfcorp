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
        private static Dictionary<string, GrassType> GrassTypes = new Dictionary<string, GrassType>();
        private static List<GrassType> GrassTypeList;
        private static bool GrassLibraryInitialized = false;

        public static IEnumerable<GrassType> EnumerateGrassTypes()
        {
            InitializeGrassLibrary();
            return GrassTypeList;
        }

        private static GrassType.FringeTileUV[] CreateFringeUVs(Point[] Tiles)
        {
            global::System.Diagnostics.Debug.Assert(Tiles.Length == 3);

            var r = new GrassType.FringeTileUV[8];

            // North
            r[0] = new GrassType.FringeTileUV(Tiles[0].X, (Tiles[0].Y * 2) + 1, 16, 32);
            // East
            r[1] = new GrassType.FringeTileUV((Tiles[1].X * 2) + 1, Tiles[1].Y, 32, 16);
            // South
            r[2] = new GrassType.FringeTileUV(Tiles[0].X, (Tiles[0].Y * 2), 16, 32);
            // West
            r[3] = new GrassType.FringeTileUV(Tiles[1].X * 2, Tiles[1].Y, 32, 16);

            // NW
            r[4] = new GrassType.FringeTileUV((Tiles[2].X * 2) + 1, (Tiles[2].Y * 2) + 1, 32, 32);
            // NE
            r[5] = new GrassType.FringeTileUV((Tiles[2].X * 2), (Tiles[2].Y * 2) + 1, 32, 32);
            // SE
            r[6] = new GrassType.FringeTileUV((Tiles[2].X * 2), (Tiles[2].Y * 2), 32, 32);
            // SW
            r[7] = new GrassType.FringeTileUV((Tiles[2].X * 2) + 1, (Tiles[2].Y * 2), 32, 32);

            return r;
        }

        private static void InitializeGrassLibrary()
        {
            if (GrassLibraryInitialized) return;
            GrassLibraryInitialized = true;

            GrassTypeList = FileUtils.LoadJsonListFromDirectory<GrassType>(ContentPaths.grass_types, null, g => g.Name);

            byte ID = 1;
            foreach (var type in GrassTypeList)
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

                GrassTypes[type.Name] = type;

                if (type.FringeTiles != null)
                    type.FringeTransitionUVs = CreateFringeUVs(type.FringeTiles);

                if (type.InitialDecayValue > VoxelConstants.MaximumGrassDecay)
                {
                    type.InitialDecayValue = VoxelConstants.MaximumGrassDecay;
                    Console.WriteLine("Grass type " + type.Name + " with invalid InitialDecayValue");
                }
            }

            if (ID > VoxelConstants.MaximumGrassTypes)
                Console.WriteLine("Allowed number of grass types exceeded. Limit is " + VoxelConstants.MaximumGrassTypes);

            GrassTypeList = GrassTypeList.OrderBy(v => v.ID).ToList();

            Console.WriteLine("Loaded Grass Library.");
        }

        public static GrassType GetGrassType(byte id)
        {
            InitializeGrassLibrary();
            return GrassTypeList[id];
        }

        public static GrassType GetGrassType(string name)
        {
            InitializeGrassLibrary();
            if (name == null)
            {
                return null;
            }
            GrassType r = null;
            GrassTypes.TryGetValue(name, out r);
            return r;
        }

        public static Dictionary<int, String> GetGrassTypeMap()
        {
            InitializeGrassLibrary();
            var r = new Dictionary<int, String>();
            for (var i = 0; i < GrassTypeList.Count; ++i)
                r.Add(i, GrassTypeList[i].Name);
            return r;
        }
    }
}