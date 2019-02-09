// GameData.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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
using DwarfCorp.Saving;

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
        public Dictionary<ResourceType, Resource> Resources;
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
                Resources = ResourceLibrary.Resources,
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