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
            return GenerateDwarf(Position, Manager, "Player", "Miner", 0, Mating.RandomGender(), MathFunctions.Random.Next());
        }

        [EntityFactory("Dwarf Soldier")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return GenerateDwarf(Position, Manager, "Player", "Soldier", 0, Mating.RandomGender(), MathFunctions.Random.Next());
        }

        [EntityFactory("Dwarf Crafter")]
        private static GameComponent __factory2(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return GenerateDwarf(Position, Manager, "Player", "Crafter", 0, Mating.RandomGender(), MathFunctions.Random.Next());
        }

        [EntityFactory("Dwarf Wizard")]
        private static GameComponent __factory3(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return GenerateDwarf(Position, Manager, "Player", "Wizard", 0, Mating.RandomGender(), MathFunctions.Random.Next());
        }

        [EntityFactory("Dwarf Musketeer")]
        private static GameComponent __factory4(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return GenerateDwarf(Position, Manager, "Player", "Musketeer", 0, Mating.RandomGender(), MathFunctions.Random.Next());
        }

        public static GameComponent GenerateDwarf(
            Vector3 Position,
            ComponentManager Manager,
            string Allies, 
            String DwarfClass, 
            int Level, Gender gender, int seed)
        {
            Dwarf toReturn = new Dwarf(Manager, new CreatureStats("Dwarf", DwarfClass, Level) { Gender = gender, RandomSeed = seed, VoicePitch  = GetRandomVoicePitch(gender) }, Allies, Manager.World.PlayerFaction, "Dwarf", Position);
            toReturn.AddThought(Thought.CreateStandardThought(Thought.ThoughtType.JustArrived, Manager.World.Time.CurrentDate), false);
            return toReturn.Physics;
        }

        private static float GetRandomVoicePitch(Gender gender)
        {
            switch (gender)
            {
                case Gender.Female:
                    return MathFunctions.Rand(0.2f, 1.0f);
                case Gender.Male:
                    return MathFunctions.Rand(-1.0f, 0.3f);
                case Gender.Nonbinary:
                    return MathFunctions.Rand(-1.0f, 1.0f);
            }
            return 1.0f;
        }
    }
}