using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DwarfCorp
{

    public class DiseaseLibrary
    {
        public static List<Disease> Diseases { get; set; }

        static DiseaseLibrary()
        {
            Diseases = new List<Disease>()
            {
                new Disease()
                {
                    Type = Disease.HealType.Time,
                    Name = "Tuberculosis",
                    Description = "The dwarf will move very slowly and get damaged until healed.",
                    DamageEveryNSeconds = 20,
                    DamagePerSecond = 0.1f,
                    EffectTime = new Timer(120, true),
                    FoodValueUntilHealed = 100,
                    IsContagious = true,
                    LikelihoodOfSpread = 0.001f,
                    StatDamage = new CreatureStats.StatNums()
                    {
                        Charisma = -1,
                        Constitution = -1,
                        Dexterity = -3,
                        Intelligence = -1,
                        Size = 0,
                        Strength = -3,
                        Wisdom = -1
                    },
                    Particles = "blood_particle",
                    ParticleTimer = new Timer(5.0f, false),
                    SoundOnStart = ContentPaths.Audio.Oscar.sfx_ic_dwarf_tantrum_1,
                    AcquiredRandomly = true,
                    ChanceofRandomAcquisitionPerDay = 0.01f
                },
                new Disease()
                {
                    Type = Disease.HealType.Time,
                    Name = "Scarlet Fever",
                    Description = "The dwarf will move very slowly and get damaged until healed.",
                    DamageEveryNSeconds = 20,
                    DamagePerSecond = 0.2f,
                    EffectTime = new Timer(120, true),
                    FoodValueUntilHealed = 100,
                    IsContagious = true,
                    LikelihoodOfSpread = 0.01f,
                    StatDamage = new CreatureStats.StatNums()
                    {
                        Charisma = -1,
                        Constitution = -1,
                        Dexterity = -3,
                        Intelligence = -1,
                        Size = 0,
                        Strength = -3,
                        Wisdom = -1
                    },
                    Particles = "blood_particle",
                    ParticleTimer = new Timer(5.0f, false),
                    SoundOnStart = ContentPaths.Audio.Oscar.sfx_ic_dwarf_tantrum_1,
                    AcquiredRandomly = true,
                    ChanceofRandomAcquisitionPerDay = 0.001f
                },
                new Disease()
                {
                    Type = Disease.HealType.Time,
                    Name = "Yellow Fever",
                    Description = "The dwarf will move very slowly and get damaged until healed.",
                    DamageEveryNSeconds = 20,
                    DamagePerSecond = 0.2f,
                    EffectTime = new Timer(120, true),
                    FoodValueUntilHealed = 100,
                    IsContagious = true,
                    LikelihoodOfSpread = 0.1f,
                    StatDamage = new CreatureStats.StatNums()
                    {
                        Charisma = -1,
                        Constitution = -1,
                        Dexterity = -3,
                        Intelligence = -1,
                        Size = 0,
                        Strength = -3,
                        Wisdom = -1
                    },
                    Particles = "blood_particle",
                    ParticleTimer = new Timer(5.0f, false),
                    SoundOnStart = ContentPaths.Audio.Oscar.sfx_ic_dwarf_tantrum_1,
                    AcquiredRandomly = true,
                    ChanceofRandomAcquisitionPerDay = 0.001f
                },
                new Disease()
                {
                    Type = Disease.HealType.Time,
                    Name = "Cholera",
                    Description = "The dwarf will move very slowly and get damaged until healed.",
                    DamageEveryNSeconds = 20,
                    DamagePerSecond = 0.2f,
                    EffectTime = new Timer(120, true),
                    FoodValueUntilHealed = 100,
                    IsContagious = true,
                    LikelihoodOfSpread = 0.1f,
                    StatDamage = new CreatureStats.StatNums()
                    {
                        Charisma = -1,
                        Constitution = -1,
                        Dexterity = -3,
                        Intelligence = -1,
                        Size = 0,
                        Strength = -3,
                        Wisdom = -1
                    },
                    Particles = "blood_particle",
                    ParticleTimer = new Timer(5.0f, false),
                    SoundOnStart = ContentPaths.Audio.Oscar.sfx_ic_dwarf_tantrum_1,
                    AcquiredRandomly = true,
                    ChanceofRandomAcquisitionPerDay = 0.01f
                },
                new Disease()
                {
                    Type = Disease.HealType.Time,
                    Name = "Dysentery",
                    Description = "The dwarf will move very slowly and get damaged until healed.",
                    DamageEveryNSeconds = 20,
                    DamagePerSecond = 0.2f,
                    EffectTime = new Timer(120, true),
                    FoodValueUntilHealed = 100,
                    IsContagious = true,
                    LikelihoodOfSpread = 0.001f,
                    StatDamage = new CreatureStats.StatNums()
                    {
                        Charisma = -1,
                        Constitution = -1,
                        Dexterity = -3,
                        Intelligence = -1,
                        Size = 0,
                        Strength = -3,
                        Wisdom = -1
                    },
                    Particles = "blood_particle",
                    ParticleTimer = new Timer(5.0f, false),
                    SoundOnStart = ContentPaths.Audio.Oscar.sfx_ic_dwarf_tantrum_1,
                    AcquiredRandomly = true,
                    ChanceofRandomAcquisitionPerDay = 0.01f
                },
                new Disease()
                {
                    Type = Disease.HealType.Time,
                    Name = "Rabies",
                    Description = "The dwarf will move very slowly and get damaged until healed.",
                    DamageEveryNSeconds = 20,
                    DamagePerSecond = 0.1f,
                    EffectTime = new Timer(120, true),
                    FoodValueUntilHealed = 100,
                    IsContagious = false,
                    LikelihoodOfSpread = 0.1f,
                    StatDamage = new CreatureStats.StatNums()
                    {
                        Charisma = -1,
                        Constitution = -1,
                        Dexterity = -3,
                        Intelligence = -1,
                        Size = 0,
                        Strength = -3,
                        Wisdom = -1
                    },
                    Particles = "blood_particle",
                    ParticleTimer = new Timer(5.0f, false),
                    SoundOnStart = ContentPaths.Audio.Oscar.sfx_ic_dwarf_tantrum_1,
                    AcquiredRandomly = false,
                },
                new Disease()
                {
                    Type = Disease.HealType.Time,
                    Name = "Necrorot",
                    Description = "The dwarf will move very slowly and get damaged until healed.",
                    DamageEveryNSeconds = 20,
                    DamagePerSecond = 0.5f,
                    EffectTime = new Timer(120, true),
                    FoodValueUntilHealed = 100,
                    IsContagious = false,
                    LikelihoodOfSpread = 0.1f,
                    StatDamage = new CreatureStats.StatNums()
                    {
                        Charisma = -1,
                        Constitution = -1,
                        Dexterity = -3,
                        Intelligence = -1,
                        Size = 0,
                        Strength = -3,
                        Wisdom = -1
                    },
                    Particles = "blood_particle",
                    ParticleTimer = new Timer(5.0f, false),
                    SoundOnStart = ContentPaths.Audio.Oscar.sfx_ic_dwarf_tantrum_1,
                    AcquiredRandomly = false,
                },
            };

            var bodyParts = TextGenerator.TextAtoms["$bodypart"];
            string[] injuries =
            {
                "Blunt force trauma to the ", "A deep cut on the ", "A broken ", "A scraped ", "A dislocated ", "An injured ", "A bruised "
            };
            string[] locations =
            {
                "left ", "right "
            };
            foreach (var part in bodyParts.Terms)
            {
                foreach (var injury in injuries)
                {
                    foreach (var location in locations)
                    {
                        Diseases.Add(new Disease()
                        {
                            Name = injury + location + part,
                            LikelihoodOfSpread = 0.05f,
                            AcquiredRandomly = false,
                            DamageEveryNSeconds = 99999,
                            Description = "An injury.",
                            StatDamage = new CreatureStats.StatNums()
                            {
                                Dexterity = -1,
                                Constitution = -1
                            },
                            EffectTime = new Timer(240, false),
                            Type = Disease.HealType.Time,
                            Particles = "",
                            ParticleTimer = new Timer(99999, false),
                            IsInjury = true
                        });   
                    }
                }
            }
        }

        public static Disease GetRandomInjury()
        {
            return Datastructures.SelectRandom(Diseases.Where(disease => disease.IsInjury));
        }

        public static Disease GetDisease(string name)
        {
            return Diseases.Where(d => d.Name == name).FirstOrDefault();
        }

        public static void SpreadRandomDiseases(IEnumerable<CreatureAI> creatures)
        {
            foreach (Disease disease in Diseases.Where(disease => disease.AcquiredRandomly))
            {
                foreach (var creature in creatures)
                {
                    if (MathFunctions.RandEvent(disease.ChanceofRandomAcquisitionPerDay))
                    {
                        creature.Creature.AcquireDisease(disease.Name);
                    }
                }
            }
        }
    }
    /// <summary>
    /// Special kind of buff that gets cured under certain conditions and does damage.
    /// May also spread.
    /// </summary>
    public class Disease : Buff
    {
        public enum HealType
        {
            Time,
            Food,
            Sleep
        }

        public bool AcquiredRandomly { get; set; }
        public float ChanceofRandomAcquisitionPerDay { get; set; }
        public HealType Type { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsContagious { get; set; }
        public float LikelihoodOfSpread { get; set; }
        public float SecondsUntilHealed { get; set; }
        public float DamagePerSecond { get; set; }
        public float FoodValueUntilHealed { get; set; }
        public CreatureStats.StatNums StatDamage { get; set; }
        private float LastHunger = 0.0f;
        private float TotalDamage = 0.0f;
        public int DamageEveryNSeconds { get; set; }
        public bool IsInjury { get; set; }
        public override void OnApply(Creature creature)
        {
            creature.Faction.World.Tutorial("disease");
            if (creature.Faction == creature.Faction.World.PlayerFaction)
                creature.Faction.World.MakeAnnouncement(creature.Stats.FullName + " got " + Name + "!", creature.AI.ZoomToMe, ContentPaths.Audio.Oscar.sfx_gui_negative_generic);
            creature.Stats.StatBuffs += StatDamage;
            base.OnApply(creature);
        }

        public override void OnEnd(Creature creature)
        {
            creature.Stats.StatBuffs -= StatDamage;
            if (creature.Faction == creature.Faction.World.PlayerFaction)
                creature.Faction.World.MakeAnnouncement(creature.Stats.FullName + " recovered from  " + Name + "!", creature.AI.ZoomToMe, ContentPaths.Audio.Oscar.sfx_gui_negative_generic);
            base.OnEnd(creature);
        }

        public override void Update(DwarfTime time, Creature creature)
        {
            float hungerChange = creature.Status.Hunger.CurrentValue - LastHunger;
            LastHunger = creature.Status.Hunger.CurrentValue;
            switch (Type)
            {
                case HealType.Food:
                    FoodValueUntilHealed -= hungerChange;
                    if (FoodValueUntilHealed > 0)
                    {
                       DoDamage(DwarfTime.Dt, creature);
                    }
                    else
                    {
                        EffectTime.Reset(0);
                    }
                    break;
                case HealType.Sleep:
                    if (!creature.Status.IsAsleep)
                    {
                        DoDamage(DwarfTime.Dt, creature);
                    }
                    else
                        EffectTime.Reset(0);
                    break;
                case HealType.Time:
                    DoDamage(DwarfTime.Dt, creature);
                    break;

            }

            if (IsContagious)
            {
                if (MathFunctions.RandEvent(LikelihoodOfSpread))
                {
                    foreach (CreatureAI other in creature.Faction.Minions)
                    {
                        if (other == creature.AI) continue;
                        if ((other.Position - creature.AI.Position).LengthSquared() > 2) continue;
                        other.Creature.AcquireDisease(Name);
                    }
                }
            }
            base.Update(time, creature);
        }

        private void DoDamage(float dt, Creature creature)
        {
            TotalDamage += dt;

            if (TotalDamage > DamageEveryNSeconds)
            {
                creature.Damage(DamageEveryNSeconds * DamagePerSecond, Health.DamageType.Poison);
                TotalDamage = 0;
            }
        }

        public override Buff Clone()
        {
            return new Disease()
            {
                EffectTime = new Timer(EffectTime.TargetTimeSeconds, EffectTime.TriggerOnce, EffectTime.Mode),
                Particles = Particles,
                ParticleTimer =
                    new Timer(ParticleTimer.TargetTimeSeconds, ParticleTimer.TriggerOnce, ParticleTimer.Mode),
                SoundOnEnd = SoundOnEnd,
                SoundOnStart = SoundOnStart,
                DamagePerSecond = DamagePerSecond,
                Description = Description,
                IsContagious = IsContagious,
                LikelihoodOfSpread = LikelihoodOfSpread,
                SecondsUntilHealed = SecondsUntilHealed,
                StatDamage = StatDamage,
                FoodValueUntilHealed = FoodValueUntilHealed,
                Type = Type,
                DamageEveryNSeconds = DamageEveryNSeconds,
                Name = Name,
                AcquiredRandomly = AcquiredRandomly,
                ChanceofRandomAcquisitionPerDay = ChanceofRandomAcquisitionPerDay,
                IsInjury = IsInjury
            };
        }
    }
}
