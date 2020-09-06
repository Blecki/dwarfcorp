using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
    public class Dwarf : Creature
    {
        private DateTime LastHungerDamageTime = DateTime.Now;

        public Dwarf()
        {
            
        }

        public Dwarf(ComponentManager manager, CreatureStats stats, Faction faction,  string name, Vector3 position) :
            base(manager, stats, faction, name)
        {
            Physics = new Physics(manager, "Dwarf", Matrix.CreateTranslation(position),
                        new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -0.25f, 0.0f), 1.0f, 1.0f, 0.999f, 0.999f, new Vector3(0, -10, 0));

            Physics.AddChild(this);

            Stats.Gender = Mating.RandomGender();
            Physics.Orientation = Physics.OrientMode.RotateY;

            CreateCosmeticChildren(Manager);
            
            Physics.AddChild(new EnemySensor(Manager, "EnemySensor", Matrix.Identity, new Vector3(10, 5, 10), Vector3.Zero));
            Physics.AddChild(new DwarfAI(Manager, "Dwarf AI", Sensor));         
            Physics.AddChild(new Inventory(Manager, "Inventory", Physics.BoundingBox.Extents(), Physics.LocalBoundingBoxOffset));
            Physics.AddChild(new Equipment(Manager));

            Physics.Tags.Add("Dwarf");

            Physics.AddChild(new Flammable(Manager, "Flames"));

            Stats.FullName = TextGenerator.GenerateRandom("$firstname", " ", "$lastname");
            Stats.FindAdjustment("base stats").Size = 5;
            Stats.CanEat = true;
            AI.Movement.CanClimbWalls = true; // Why isn't this a flag like the below?
            AI.Movement.SetCan(MoveType.Teleport, true);
            AI.Movement.SetCost(MoveType.Teleport, 1.0f);
            AI.Movement.SetSpeed(MoveType.Teleport, 10.0f);
            AI.Movement.SetCost(MoveType.ClimbWalls, 50.0f);
            AI.Movement.SetSpeed(MoveType.ClimbWalls, 0.15f);
            AI.Movement.SetCan(MoveType.EnterVehicle, true);
            AI.Movement.SetCan(MoveType.ExitVehicle, true);
            AI.Movement.SetCan(MoveType.RideVehicle, true);
            AI.Movement.SetCost(MoveType.EnterVehicle, 0.01f);
            AI.Movement.SetCost(MoveType.ExitVehicle, 0.01f);
            AI.Movement.SetCost(MoveType.RideVehicle, 0.01f);
            AI.Movement.SetSpeed(MoveType.RideVehicle, 3.0f);
            AI.Movement.SetSpeed(MoveType.EnterVehicle, 1.0f);
            AI.Movement.SetSpeed(MoveType.ExitVehicle, 1.0f);
            if (AI.Stats.IsTaskAllowed(TaskCategory.Dig))
                AI.Movement.SetCan(MoveType.Dig, true);
            AI.Biography = Applicant.GenerateBiography(AI.Stats.FullName, Stats.Gender);
            Stats.Money = (decimal)MathFunctions.Rand(0, 150);

            Physics.AddChild(new DwarfThoughts(Manager, "Thoughts"));
        }

        public override void Die()
        {
            Inventory.AddResource(new Resource("Corpse") { DisplayName = AI.Stats.FullName + "'s Corpse" });
            base.Die();
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            CreateDwarfSprite(manager);
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

            if (Equipment.HasValue(out var equipment) && GetRoot().GetComponent<DwarfSprites.DwarfCharacterSprite>().HasValue(out var sprite))
                equipment.AddLayersToSprite(sprite);

            base.CreateCosmeticChildren(manager);
        }

        protected void CreateDwarfSprite(ComponentManager manager)
        {
            if (Physics == null)
            {
                if (GetRoot().GetComponent<Physics>().HasValue(out var physics))
                    Physics = physics;
                else
                    return;
            }

            Physics.AddChild(DwarfSprites.DwarfBuilder.CreateDwarfCharacterSprite(manager, Stats));
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

            if (!Active) return;

            #region Update Status Stat Effects 

            var statAdjustments = Stats.FindAdjustment("status");
            Stats.RemoveStatAdjustment("status");
            if (statAdjustments == null)
                statAdjustments = new StatAdjustment() { Name = "status" };
            statAdjustments.Reset();

            if (!Stats.IsAsleep)
                Stats.Hunger.CurrentValue -= (float)gameTime.ElapsedGameTime.TotalSeconds * Stats.HungerGrowth;
            else
                Hp += (float)gameTime.ElapsedGameTime.TotalSeconds * 0.2f;

            Stats.Health.CurrentValue = (Hp - MinHealth) / (MaxHealth - MinHealth); // Todo: MinHealth always 0?

            if (Stats.Energy.IsDissatisfied())
            {
                DrawIndicator(IndicatorManager.StandardIndicators.Sleepy);
                statAdjustments.Strength += -2;
                statAdjustments.Intelligence += -2;
                statAdjustments.Dexterity += -2;
            }

            if (Stats.CanEat && Stats.Hunger.IsDissatisfied() && !Stats.IsAsleep)
            {
                DrawIndicator(IndicatorManager.StandardIndicators.Hungry);

                statAdjustments.Intelligence += -1;
                statAdjustments.Dexterity += -1;

                if (Stats.Hunger.CurrentValue <= 1e-12 && (DateTime.Now - LastHungerDamageTime).TotalSeconds > Stats.HungerDamageRate)
                {
                    Damage(1.0f / (Stats.HungerResistance) * Stats.HungerDamageRate);
                    LastHungerDamageTime = DateTime.Now;
                }
            }

            if (!statAdjustments.IsAllZero)
                Stats.AddStatAdjustment(statAdjustments);

            #endregion
        }
    }
}
