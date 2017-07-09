using System;
using System.Collections.Generic;
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
                    Type = Disease.HealType.Food,
                    Name = "Tuberculosis",
                    Description = "The dwarf will move very slowly and get damaged until eating enough food",
                    DamageEveryNSeconds = 20,
                    DamagePerSecond = 1,
                    EffectTime = new Timer(9999, true),
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
                    SoundOnStart = ContentPaths.Audio.Oscar.sfx_ic_dwarf_tantrum_1
                }
            };
        }

        public static Disease GetDisease(string name)
        {
            return Diseases.Where(d => d.Name == name).FirstOrDefault();
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
        public override void OnApply(Creature creature)
        {
            creature.Faction.World.Tutorial("disease");
            creature.Faction.World.MakeAnnouncement(creature.Stats.FullName + " got " + Name + "!", creature.AI.ZoomToMe, ContentPaths.Audio.Oscar.sfx_gui_negative_generic);
            creature.Stats.StatBuffs += StatDamage;
            base.OnApply(creature);
        }

        public override void OnEnd(Creature creature)
        {
            creature.Stats.StatBuffs -= StatDamage;
            creature.Faction.World.MakeAnnouncement(creature.Stats.FullName + " was cured of  " + Name + "!", creature.AI.ZoomToMe, ContentPaths.Audio.Oscar.sfx_gui_negative_generic);
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
            TotalDamage += dt*DamagePerSecond;

            if (TotalDamage > DamageEveryNSeconds)
            {
                creature.Damage(1, Health.DamageType.Poison);
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
                Name = Name
            };
        }
    }
}
