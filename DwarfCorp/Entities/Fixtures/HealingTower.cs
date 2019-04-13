using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class HealingTower : CraftedFixture
    {
        public float HealIncrease = 10;
        public float HealRadius = 10;
        public Timer HealTimer = new Timer(5.0f, false);

        [EntityFactory("Tower of Health")]
        private static GameComponent __factory00(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new HealingTower(Manager, Position, Data);
        }

        public HealingTower()
        {

        }

        public HealingTower(ComponentManager Manager, Vector3 Position, Blackboard Data) :
            base("Tower of Health", new String[] { "Tower of Health" },
                Manager, Position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32), new Point(6, 2),
                Data.GetData<List<ResourceAmount>>("Resources", null))
        {
            OrientMode = SimpleSprite.OrientMode.YAxis;
            AddChild(new MagicalObject(Manager));
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);
            var sprite = GetComponent<SimpleSprite>();
            sprite.OrientationType = SimpleSprite.OrientMode.YAxis;
            sprite.LightsWithVoxels = false;
        }

        public override void Update(DwarfTime Time, ChunkManager Chunks, Camera Camera)
        {
            if (Active)
            {
                // Prevent towers from healing when they have no charge.
                var magicalObject = GetComponent<MagicalObject>();
                if (magicalObject != null && magicalObject.CurrentCharges == 0) return;

                HealTimer.Update(Time);
                if (HealTimer.HasTriggered)
                {
                    var objects = World.EnumerateIntersectingObjects(new BoundingBox(-Vector3.One * HealRadius + Position, Vector3.One * HealRadius + Position), CollisionType.Dynamic);
                    foreach (var obj in objects)
                    {
                        var creature = obj.GetRoot().GetComponent<Creature>();
                        if (creature == null || creature.AI == null || creature.AI.Faction != creature.World.PlayerFaction || creature.Hp == creature.MaxHealth)
                            continue;
                        if (MathFunctions.RandEvent(0.5f))
                        {
                            creature.Heal(HealIncrease);
                            IndicatorManager.DrawIndicator((HealIncrease).ToString() + " HP",
                                creature.Physics.Position, 1.0f,
                                    GameSettings.Default.Colors.GetColor("Positive", Microsoft.Xna.Framework.Color.Green));
                            creature.DrawLifeTimer.Reset();
                            World.ParticleManager.Trigger("star_particle", obj.Position, Color.Red, 10);
                            World.ParticleManager.TriggerRay("star_particle", Position, obj.Position);
                            SoundManager.PlaySound(ContentPaths.Audio.tinkle, obj.Position, true, 1.0f);

                            if (magicalObject != null)
                            {
                                magicalObject.CurrentCharges -= 1;
                                if (magicalObject.CurrentCharges == 0)
                                    return;
                            }
                        }
                    }
                }
            }

            base.Update(Time, Chunks, Camera);
        }
    }
}
