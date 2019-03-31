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

        [EntityFactory("PlayerElf")]
        private static GameComponent __factory5(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            var toReturn = new Elf(new CreatureStats(new ElfClass(true), 0), Manager.World.PlayerFaction.Name, Manager.World.PlanService, Manager.World.PlayerFaction, Manager, "elf", Position);
            return toReturn.Physics;
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