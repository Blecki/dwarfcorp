using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Linq;

namespace DwarfCorp
{
    public partial class Creature : Health
    {
        [JsonIgnore] public bool IsPregnant => CurrentPregnancy != null;
        public Pregnancy CurrentPregnancy = null;
        public Timer EggTimer;

        public void LayEgg()
        {
            NoiseMaker.MakeNoise("Lay Egg", AI.Position, true, 1.0f);
            var eggResource = new Resource("Egg") { DisplayName = (Stats.CurrentClass.HasValue(out var c) ? c.Name + " Egg" : "Egg") };
            var resourceEntity = Manager.RootComponent.AddChild(new ResourceEntity(Manager, eggResource, Physics.Position));
            resourceEntity.AddChild(new Egg(resourceEntity, Stats.Species.BabyType, Manager, Physics.Position, AI.PositionConstraint));
        }

        private void UpdatePregnancy()
        {
            if (IsPregnant && World.Time.CurrentDate > CurrentPregnancy.EndDate)
            {
                // Todo: This check really belongs before the creature becomes pregnant.
                if (World.CanSpawnWithoutExceedingSpeciesLimit(Stats.Species))
                {
                    if (EntityFactory.HasEntity(Stats.Species.BabyType))
                    {
                        var baby = EntityFactory.CreateEntity<GameComponent>(Stats.Species.BabyType, Physics.Position);

                        if (baby.GetRoot().GetComponent<CreatureAI>().HasValue(out var ai)) // Set position constraint so baby stays inside pen.
                            ai.PositionConstraint = AI.PositionConstraint;
                    }
                }
                CurrentPregnancy = null;
            }
        }

        private void UpdateEggs(DwarfTime gameTime)
        {
            if (Stats.Species.LaysEggs)
            {
                if (EggTimer == null)
                    EggTimer = new Timer(Stats.Species.EggTime + MathFunctions.Rand(-120, 120), false);

                EggTimer.Update(gameTime);

                if (EggTimer.HasTriggered)
                {
                    if (World.CanSpawnWithoutExceedingSpeciesLimit(Stats.Species))
                        LayEgg(); // Todo: Egg rate in species

                    EggTimer = new Timer(Stats.Species.EggTime + MathFunctions.Rand(-120, 120), false);
                }
            }
        }
    }
}
