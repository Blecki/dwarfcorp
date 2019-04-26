using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DwarfCorp
{
    [Serializable]
    public class OverworldMetaData
    {
        public string Version;
        public string Name;
        public float SeaLevel;

        [Serializable]
        public struct FactionDescriptor
        {
            public string Name { get; set; }
            public byte Id { get; set; }
            public string Race { get; set; }
            public Color PrimaryColory { get; set; }
            public Color SecondaryColor { get; set; }
            public int CenterX { get; set; }
            public int CenterY { get; set; }
            public float GoodWill { get; set; }
        }

        public List<FactionDescriptor> FactionList;
        public Dictionary<int, String> BiomeTypeMap;

        public OverworldMetaData()
        {
        }

        public OverworldMetaData(GraphicsDevice device, Overworld Overworld, string name, float seaLevel)
        {
            int sizeX = Overworld.Map.GetLength(0);
            int sizeY = Overworld.Map.GetLength(1);

            Name = name;
            SeaLevel = seaLevel;

            FactionList = new List<FactionDescriptor>();
            byte id = 0;
            foreach (Faction f in Overworld.NativeFactions)
            {
                FactionList.Add(new FactionDescriptor()
                {
                    Name = f.Name,
                    PrimaryColory = f.PrimaryColor,
                    SecondaryColor = f.SecondaryColor,
                    Id = id,
                    Race = f.Race.Name,
                    CenterX = f.Center.X,
                    CenterY = f.Center.Y,
                    GoodWill = f.GoodWill
                });
                id++;
            }

            BiomeTypeMap = BiomeLibrary.GetBiomeTypeMap();
        }
    }
}