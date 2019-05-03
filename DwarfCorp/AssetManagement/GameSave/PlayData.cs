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
        //public List<Goals.Goal> Goals;
        public Tutorial.TutorialSaveData TutorialSaveData;
        public Diplomacy Diplomacy;
        public FactionLibrary Factions;
        public List<Resource> Resources;
        public DesignationDrawer Designations;
        public TaskManager Tasks;
        public Embarkment InitialEmbark;
        public Yarn.MemoryVariableStore ConversationMemory;
        public List<GameMaster.ApplicantArrival> NewArrivals;
        public DwarfCorp.Gui.Widgets.StatsTracker Stats;

        public static PlayData CreateFromWorld(WorldManager World)
        {
            return new PlayData()
            {
                Camera = World.Camera,
                Components = World.ComponentManager.GetSaveData(),
                //Goals = World.GoalManager.EnumerateGoals().ToList(),
                TutorialSaveData = World.TutorialManager.GetSaveData(),
                Diplomacy = World.Diplomacy,
                Factions = World.Factions,
                Resources = ResourceLibrary.Enumerate().ToList(),
                Designations = World.DesignationDrawer,
                Tasks = World.Master.TaskManager,
                InitialEmbark = World.InitialEmbark,
                ConversationMemory = World.ConversationMemory,
                NewArrivals = World.Master.NewArrivals,
                Stats = World.Stats
            };
        }
    }
}