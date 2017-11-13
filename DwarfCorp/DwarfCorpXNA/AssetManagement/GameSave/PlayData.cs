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
//    [Saving.SaveableObject(0)]
    public class PlayData //: Saving.ISaveableObject
    {
        public static string Extension = "save";

        public OrbitCamera Camera;
        public ComponentManager.ComponentSaveData Components;
        public List<Goals.Goal> Goals;
        public Tutorial.TutorialSaveData TutorialSaveData;
        public Diplomacy Diplomacy;
        public FactionLibrary Factions;
        public Dictionary<ResourceLibrary.ResourceType, Resource> Resources;
        public DesignationDrawer Designations;
        public SpellTree Spells;
        public TaskManager Tasks;

        public static PlayData CreateFromWorld(WorldManager World)
        {
            return new PlayData()
            {
                Camera = World.Camera,
                Components = World.ComponentManager.GetSaveData(),
                Goals = World.GoalManager.EnumerateGoals().ToList(),
                TutorialSaveData = World.TutorialManager.GetSaveData(),
                Diplomacy = World.Diplomacy,
                Factions = World.Factions,
                Resources = ResourceLibrary.Resources,
                Designations = World.DesignationDrawer,
                Spells = World.Master.Spells,
                Tasks = World.Master.TaskManager
            };
        }

        /*Nugget ISaveableObject.SaveToNugget(Saver SaveSystem)
        {
            return new PlayDataNugget
            {
                Camera = SaveSystem.SaveObject(Camera),
                Components = SaveSystem.SaveObject(Components),
                Goals = SaveSystem.SaveObject(Goals),
                Tutorial = SaveSystem.SaveObject(TutorialSaveData),
                Diplomacy = SaveSystem.SaveObject(Diplomacy),
                Factions = SaveSystem.SaveObject(Factions),
                Resources = SaveSystem.SaveObject(Resources),
                Designations = SaveSystem.SaveObject(Designations),
                Spells = SaveSystem.SaveObject(Spells)
            };
        }

        void ISaveableObject.LoadFromNugget(Loader SaveSystem, Nugget From)
        {
            var nug = From as PlayDataNugget;
            Camera = SaveSystem.LoadObject(nug.Camera) as OrbitCamera;
            // Etc
            throw new NotImplementedException();
        }*/
    }

    /*public class PlayDataNugget : Saving.Nugget
    {
        public Nugget Camera;
        public Nugget Components;
        public Nugget Goals;
        public Nugget Tutorial;
        public Nugget Diplomacy;
        public Nugget Factions;
        public Nugget Resources;
        public Nugget Designations;
        public Nugget Spells;
    }*/
}