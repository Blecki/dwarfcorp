using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Newtonsoft.Json.Serialization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.IO.Compression;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class PlayData
    {
        public static string Extension = "save";

        public OrbitCamera Camera;
        public ComponentManager.ComponentSaveData Components;
        public Tutorial.TutorialSaveData TutorialSaveData;
        public Diplomacy Diplomacy;
        public FactionSet Factions;
        //public RoomBuilder RoomBuilder;
        public DesignationDrawer Designations;
        public TaskManager Tasks;
        public Yarn.MemoryVariableStore ConversationMemory;
        public DwarfCorp.Gui.Widgets.StatsTracker Stats;
        public RoomBuilder RoomBuilder;
        public PersistentWorldData PersistentData;

        public static PlayData CreateFromWorld(WorldManager World)
        {
            return new PlayData()
            {
                Camera = World.Renderer.Camera,
                Components = World.ComponentManager.GetSaveData(),
                TutorialSaveData = World.TutorialManager.GetSaveData(),
                Diplomacy = World.Diplomacy,
                Factions = World.Factions,
                Designations = World.Renderer.DesignationDrawer,
                Tasks = World.TaskManager,
                ConversationMemory = World.ConversationMemory,
                Stats = World.Stats,
                RoomBuilder = World.RoomBuilder,
                PersistentData = World.PersistentData
            };
        }
    }
}