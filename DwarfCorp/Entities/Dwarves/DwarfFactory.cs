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
            return GenerateDwarf(Position, Manager, "Miner", 0, Mating.RandomGender(), MathFunctions.Random.Next());
        }

        [EntityFactory("Dwarf Soldier")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return GenerateDwarf(Position, Manager, "Soldier", 0, Mating.RandomGender(), MathFunctions.Random.Next());
        }

        [EntityFactory("Dwarf Crafter")]
        private static GameComponent __factory2(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return GenerateDwarf(Position, Manager, "Crafter", 0, Mating.RandomGender(), MathFunctions.Random.Next());
        }

        [EntityFactory("Dwarf Wizard")]
        private static GameComponent __factory3(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return GenerateDwarf(Position, Manager, "Wizard", 0, Mating.RandomGender(), MathFunctions.Random.Next());
        }

        [EntityFactory("Dwarf Musketeer")]
        private static GameComponent __factory4(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return GenerateDwarf(Position, Manager, "Musketeer", 0, Mating.RandomGender(), MathFunctions.Random.Next());
        }

        [EntityFactory("Dwarf Manager")]
        private static GameComponent __factory5(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return GenerateDwarf(Position, Manager, "Manager", 0, Mating.RandomGender(), MathFunctions.Random.Next());
        }

        public static GameComponent GenerateDwarf(
            Vector3 Position,
            ComponentManager Manager,
            String DwarfClass, 
            int Level, Gender gender, int seed)
        {
            var toReturn = new Dwarf(Manager, new CreatureStats("Dwarf", DwarfClass, Level) { Gender = gender, RandomSeed = seed, VoicePitch  = GetRandomVoicePitch(gender) }, Manager.World.PlayerFaction, "Dwarf", Position);
            toReturn.AddThought("I just arrived to this new land.", new TimeSpan(3, 0, 0, 0), 100.0f);

            if (toReturn.Equipment.HasValue(out var equipment))
                foreach (var equippedItem in toReturn.Stats.CurrentClass.StartingEquipment)
                    equipment.EquipItem(new Resource(equippedItem.TypeName));

            return toReturn.Physics;
        }

        public static float GetRandomVoicePitch(Gender gender)
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