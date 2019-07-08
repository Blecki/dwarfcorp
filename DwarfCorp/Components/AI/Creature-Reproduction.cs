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

            // Todo: Egg resource type and the baby made need to be in the species.
            if (!Library.DoesResourceTypeExist(Stats.CurrentClass.Name + " Egg") || !EntityFactory.EnumerateEntityTypes().Contains(Stats.CurrentClass.Name + " Egg Resource"))
            {
                var newEggResource = Library.CreateResourceType(Library.GetResourceType("Egg"));
                newEggResource.Name = Stats.CurrentClass.Name + " Egg";
                Library.AddResourceType(newEggResource);
            }

            var parent = EntityFactory.CreateEntity<GameComponent>(Stats.CurrentClass.Name + " Egg Resource", Physics.Position);
            parent.AddChild(new Egg(parent, Stats.Species.BabyType, Manager, Physics.Position, AI.PositionConstraint));
        }

        private void UpdatePregnancy()
        {
            if (IsPregnant && World.Time.CurrentDate > CurrentPregnancy.EndDate)
            {
                // Todo: This check really belongs before the creature becomes pregnant.
                if (World.GetSpeciesPopulation(Stats.Species) < Stats.Species.SpeciesLimit)
                {
                    if (EntityFactory.HasEntity(Stats.Species.BabyType))
                    {
                        var baby = EntityFactory.CreateEntity<GameComponent>(Stats.Species.BabyType, Physics.Position);
                        baby.GetRoot().GetComponent<CreatureAI>().PositionConstraint = AI.PositionConstraint;
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
                    EggTimer = new Timer(120f + MathFunctions.Rand(-120, 120), false);
                EggTimer.Update(gameTime);

                if (EggTimer.HasTriggered)
                {
                    if (World.GetSpeciesPopulation(Stats.Species) < Stats.Species.SpeciesLimit)
                    {
                        LayEgg(); // Todo: Egg rate in species
                        EggTimer = new Timer(120f + MathFunctions.Rand(-120, 120), false);
                    }
                }
            }
        }
    }
}
