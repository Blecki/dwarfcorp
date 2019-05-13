using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class MetaData
    {
        public string OverworldFile { get; set; }
        public Vector2 WorldOrigin { get; set; }
        public float TimeOfDay { get; set; }
        public int GameID { get; set; }
        public int Slice { get; set; }
        public WorldTime Time { get; set; }
        public Point3 NumChunks { get; set; }
        public String Version;
        public String Commit;
        
        public static string Extension = "meta";
        public static string CompressedExtension = "zmeta";

        public static MetaData CreateFromWorld(WorldManager World)
        {
            return new MetaData
            {
                OverworldFile = World.GenerationSettings.Overworld.Name,
                WorldOrigin = World.WorldOrigin,
                TimeOfDay = World.Sky.TimeOfDay,
                GameID = World.GameID,
                Time = World.Time,
                Slice = (int)World.Master.MaxViewingLevel,
                NumChunks = World.ChunkManager.WorldSize,
                Version = Program.Version,
                Commit = Program.Commit,
            };
        }
    }
}