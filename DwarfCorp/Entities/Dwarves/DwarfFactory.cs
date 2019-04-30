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
        [EntityFactory("Dwarf Miner")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return GenerateDwarf(Position, Manager, "Player", JobLibrary.Classes["Miner"], 0, Mating.RandomGender(), MathFunctions.Random.Next());
        }

        [EntityFactory("Dwarf Soldier")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return GenerateDwarf(Position, Manager, "Player", JobLibrary.Classes["Soldier"], 0, Mating.RandomGender(), MathFunctions.Random.Next());
        }

        [EntityFactory("Dwarf Crafter")]
        private static GameComponent __factory2(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return GenerateDwarf(Position, Manager, "Player", JobLibrary.Classes["Crafter"], 0, Mating.RandomGender(), MathFunctions.Random.Next());
        }

        [EntityFactory("Dwarf Wizard")]
        private static GameComponent __factory3(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return GenerateDwarf(Position, Manager, "Player", JobLibrary.Classes["Wizard"], 0, Mating.RandomGender(), MathFunctions.Random.Next());
        }

        [EntityFactory("Dwarf Musketeer")]
        private static GameComponent __factory4(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return GenerateDwarf(Position, Manager, "Player", JobLibrary.Classes["Musketeer"], 0, Mating.RandomGender(), MathFunctions.Random.Next());
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