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

    public class PlayerFile : SaveData
    {
        public new static string Extension = "player";
        public new static string CompressedExtension = "zplayer";

        public class PlayerData
        {
            public List<ZoneData> Rooms { get; set; }
            public List<ZoneData> Stockpiles { get; set; }
            public List<EntityPtr> Minions { get; set; }
            public List<VoxelPtr> DigDesignations { get; set; }
            public List<VoxelPtr> GuardDesignations { get; set; }
            public List<EntityPtr> ChopDesignations { get; set; }

            public List<EntityPtr> GatherDesignations { get; set; } 
            

            public PlayerData()
            {
                Rooms = new List<ZoneData>();
                Stockpiles = new List<ZoneData>();
                Minions = new List<EntityPtr>();
                DigDesignations = new List<VoxelPtr>();
                GuardDesignations = new List<VoxelPtr>();
                ChopDesignations = new List<EntityPtr>();
                GatherDesignations = new List<EntityPtr>();
            }

            public PlayerData(GameMaster player)
            {
                Rooms = new List<ZoneData>();

                foreach(ZoneData zoneData in player.RoomDesignator.DesignatedRooms.Select(r => new ZoneData(r)))
                {
                    Rooms.Add(zoneData);
                }

                Stockpiles = new List<ZoneData>();

                foreach(ZoneData zoneData in player.Stockpiles.Select(s => new ZoneData(s)))
                {
                    Stockpiles.Add(zoneData);
                }

                Minions = new List<EntityPtr>();

                foreach(EntityPtr ent in player.Minions.Select(minion => new EntityPtr(minion.Parent.GlobalID)))
                {
                    Minions.Add(ent);
                }

                DigDesignations = new List<VoxelPtr>();

                foreach(GameMaster.Designation designation in player.DigDesignations)
                {
                    DigDesignations.Add(new VoxelPtr(designation.vox));
                }

                GuardDesignations = new List<VoxelPtr>();

                foreach(GameMaster.Designation designation in player.GuardDesignations)
                {
                    GuardDesignations.Add(new VoxelPtr(designation.vox));
                }

                ChopDesignations = new List<EntityPtr>();

                foreach(LocatableComponent designation in player.ChopDesignations)
                {
                    ChopDesignations.Add(new EntityPtr(designation.GlobalID));
                }

                GatherDesignations = new List<EntityPtr>();
                foreach (LocatableComponent designation in player.GatherDesignations)
                {
                    GatherDesignations.Add(new EntityPtr(designation.GlobalID));
                }

            }
        }


        public PlayerData Data { get; set; }


        public virtual string GetExtension()
        {
            return "player";
        }

        public virtual string GetCompressedExtension()
        {
            return "zplayer";
        }

        public PlayerFile()
        {
            Data = new PlayerData();
        }

        public PlayerFile(string filePath, bool isCompressed)
        {
            ReadFile(filePath, isCompressed);
        }


        public PlayerFile(GameMaster player)
        {
            Data = new PlayerData(player);
        }

        public void CopyFrom(PlayerFile file)
        {
            Data = file.Data;
        }

        public override bool ReadFile(string filePath, bool isCompressed)
        {
            PlayerFile file = FileUtils.LoadJson<PlayerFile>(filePath, isCompressed);

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
            return FileUtils.SaveJSon<PlayerFile>(this, filePath, compress);
        }
    }

}