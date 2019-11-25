using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
    public class NonPlayerDwarf : Creature
    {
        [EntityFactory("Non-Player Dwarf Miner")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new NonPlayerDwarf(
                Manager,
                new CreatureStats("Dwarf", "Miner", 0)
                {
                    RandomSeed = MathFunctions.Random.Next(),
                },
                Manager.World.Factions.Factions["Dwarves"], "Dwarf", Position).Physics;
        }

        [EntityFactory("Non-Player Dwarf Soldier")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new NonPlayerDwarf(
                Manager,
                new CreatureStats("Dwarf", "Soldier", 0)
                {
                    RandomSeed = MathFunctions.Random.Next(),
                },
                Manager.World.Factions.Factions["Dwarves"], "Dwarf", Position).Physics;
        }

        [EntityFactory("Non-Player Dwarf Crafter")]
        private static GameComponent __factory2(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new NonPlayerDwarf(
                Manager,
                new CreatureStats("Dwarf", "Crafter", 0)
                {
                    RandomSeed = MathFunctions.Random.Next(),
                },
                Manager.World.Factions.Factions["Dwarves"], "Dwarf", Position).Physics; // Todo: Why are we adding them to a faction? Don't they just immediately get added to a different one?
        }

        public NonPlayerDwarf()
        {
            
        }

        public NonPlayerDwarf(ComponentManager manager, CreatureStats stats, Faction faction,  string name, Vector3 position) :
            base(manager, stats, faction, name)
        {
            Physics = new Physics(manager, "Dwarf", Matrix.CreateTranslation(position),
                        new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0));
            Physics.AddChild(this);

            Stats.Gender = Mating.RandomGender();
            Stats.VoicePitch = DwarfFactory.GetRandomVoicePitch(Stats.Gender);
            Physics.Orientation = Physics.OrientMode.RotateY;

            CreateCosmeticChildren(Manager);

            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(10, 5, 10), Vector3.Zero));
            Physics.AddChild(new CreatureAI(Manager, "Non Player Dwarf AI", Sensor));         
            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));

            Physics.Tags.Add("Dwarf");

            Physics.AddChild(new Flammable(Manager, "Flames"));

            Stats.FullName = TextGenerator.GenerateRandom("$firstname", " ", "$lastname");
            Stats.FindAdjustment("base stats").Size = 5;

            AI.Movement.CanClimbWalls = true; // Why isn't this a flag like the below?
            AI.Movement.SetCost(MoveType.ClimbWalls, 50.0f);
            AI.Movement.SetSpeed(MoveType.ClimbWalls, 0.15f);

            AI.Biography = Applicant.GenerateBiography(AI.Stats.FullName, Stats.Gender);
            Stats.Money = (decimal)MathFunctions.Rand(0, 150);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            CreateDwarfSprite(Stats.CurrentClass, manager);
            Physics.AddChild(Shadow.Create(0.75f, manager));
            Physics.AddChild(new VoxelRevealer(manager, Physics, 5)).SetFlag(Flag.ShouldSerialize, false);
            Physics.AddChild(new MinimapIcon(Manager, new NamedImageFrame(ContentPaths.GUI.map_icons, 16, 0, 0))).SetFlag(Flag.ShouldSerialize, false);

            NoiseMaker = new NoiseMaker();
            NoiseMaker.Noises["Hurt"] = new List<string>
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_hurt_1,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_hurt_2,
            };

            NoiseMaker.Noises["Ok"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_ok_1,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_ok_2,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_ok_3
            };

            NoiseMaker.Noises["Die"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_death
            };

            NoiseMaker.Noises["Pleased"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_pleased
            };

            NoiseMaker.Noises["Tantrum"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_tantrum_1,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_tantrum_2,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_tantrum_3,
            };
            NoiseMaker.Noises["Jump"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_jump
            };

            NoiseMaker.Noises["Climb"] = new List<string>()
            {
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_climb_1,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_climb_2,
                ContentPaths.Audio.Oscar.sfx_ic_dwarf_climb_3
            };

            Physics.AddChild(new ParticleTrigger("blood_particle", Manager, "Death Gibs", Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1,
                BoxTriggerTimes = 10,
                SoundToPlay = ContentPaths.Entities.Dwarf.Audio.dwarfhurt1,
            }).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(manager);
        }

        protected void CreateDwarfSprite(CreatureClass employeeClass, ComponentManager manager)
        {
            if (Physics == null)
            {
                if (GetRoot().GetComponent<Physics>().HasValue(out var physics))
                    Physics = physics;
                else
                    return;
            }

            Physics.AddChild(LayeredSprites.DwarfBuilder.CreateDwarfCharacterSprite(manager, Stats));
        }
    }
}
