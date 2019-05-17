using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// Special kind of buff that gets cured under certain conditions and does damage.
    /// May also spread.
    /// </summary>
    public class Disease : StatusEffect
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
        public StatAdjustment StatDamage { get; set; }
        private float LastHunger = 0.0f;
        private float TotalDamage = 0.0f;
        public int DamageEveryNSeconds { get; set; }
        public bool IsInjury { get; set; }
        public Timer SpreadTimer = new Timer(1.0f, false);

        public override void OnApply(Creature creature)
        {
            creature.World.Tutorial("disease");

            if (creature.Faction == creature.World.PlayerFaction)
            {
                creature.World.UserInterface.MakeWorldPopup(creature.Stats.FullName + " got " + Name + "!", creature.Physics, -10, 10);
                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic, 0.15f);
            }

            creature.Stats.AddStatAdjustment(StatDamage.Clone());
            base.OnApply(creature);
        }

        public override void OnEnd(Creature creature)
        {
            creature.Stats.RemoveStatAdjustment(StatDamage.Name);

            if (creature.Faction == creature.Faction.World.PlayerFaction)
            {
                creature.World.UserInterface.MakeWorldPopup(creature.Stats.FullName + " recovered from  " + Name + "!", creature.Physics, -10, 10);
                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_positive_generic, 0.15f);
            }
            base.OnEnd(creature);
        }

        public override void Update(DwarfTime time, Creature creature)
        {
            float hungerChange = creature.Stats.Hunger.CurrentValue - LastHunger;
            LastHunger = creature.Stats.Hunger.CurrentValue;
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
                    if (!creature.Stats.IsAsleep)
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
                SpreadTimer.Update(time);
                if (SpreadTimer.HasTriggered && MathFunctions.RandEvent(LikelihoodOfSpread))
                {
                    foreach (CreatureAI other in creature.Faction.Minions)
                    {
                        if (other == creature.AI) continue;
                        if ((other.Position - creature.AI.Position).LengthSquared() > 2) continue;
                        other.Creature.Stats.AcquireDisease(DiseaseLibrary.GetDisease(Name));
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

        public override StatusEffect Clone()
        {
            return new Disease()
            {
                EffectTime = Timer.Clone(EffectTime),
                Particles = Particles,
                ParticleTimer = Timer.Clone(ParticleTimer),
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
