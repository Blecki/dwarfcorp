// Dwarf.cs
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
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public static class DwarfFactory
    {
        [EntityFactory("Dwarf")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return GenerateDwarf(Position, Manager, "Player", JobLibrary.Classes[JobLibrary.JobType.Worker], 0, Mating.RandomGender(), MathFunctions.Random.Next());
        }

        [EntityFactory("AxeDwarf")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return GenerateDwarf(Position, Manager, "Player", JobLibrary.Classes[JobLibrary.JobType.AxeDwarf], 0, Mating.RandomGender(), MathFunctions.Random.Next());
        }

        [EntityFactory("CraftsDwarf")]
        private static GameComponent __factory2(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return GenerateDwarf(Position, Manager, "Player", JobLibrary.Classes[JobLibrary.JobType.CraftsDwarf], 0, Mating.RandomGender(), MathFunctions.Random.Next());
        }

        [EntityFactory("Wizard")]
        private static GameComponent __factory3(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return GenerateDwarf(Position, Manager, "Player", JobLibrary.Classes[JobLibrary.JobType.Wizard], 0, Mating.RandomGender(), MathFunctions.Random.Next());
        }

        [EntityFactory("MusketDwarf")]
        private static GameComponent __factory4(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return GenerateDwarf(Position, Manager, "Player", JobLibrary.Classes[JobLibrary.JobType.MusketDwarf], 0, Mating.RandomGender(), MathFunctions.Random.Next());
        }

        public static GameComponent GenerateDwarf(
            Vector3 Position,
            ComponentManager Manager,
            string Allies, 
            EmployeeClass DwarfClass, 
            int Level, Gender gender, int seed)
        {
            Dwarf toReturn = new Dwarf(Manager, new CreatureStats(DwarfClass, Level) { Gender = gender, RandomSeed = seed, VoicePitch  = CreatureStats.GetRandomVoicePitch(gender) }, Allies, Manager.World.PlanService, Manager.World.PlayerFaction, "Dwarf", DwarfClass, Position);
            toReturn.AddThought(Thought.CreateStandardThought(Thought.ThoughtType.JustArrived, Manager.World.Time.CurrentDate), false);
            return toReturn.Physics;
        }
    }
}