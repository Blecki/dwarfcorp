﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class PlantGrowthTower : CraftedFixture
    {
        public float GrowthTime = 2;
        public float GrowthRadius = 10;
        public Timer GrowthTimer = new Timer(5.0f, false);

        [EntityFactory("Tower of Life")]
        private static GameComponent __factory00(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new PlantGrowthTower(Manager, Position, Data);
        }

        public PlantGrowthTower()
        {

        }

        public PlantGrowthTower(ComponentManager Manager, Vector3 Position, Blackboard Data) :
            base("Tower of Life", new String[] { "Tower of Life" },
                Manager, Position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32), new Point(5, 2),
                Data.GetData<Resource>("Resource", null)) // Todo: The crafted fixture constructor should be extracting this. Pass Data down instead?
        {
            OrientMode = SimpleSprite.OrientMode.YAxis;
            AddChild(new MagicalObject(Manager));
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            base.CreateCosmeticChildren(manager);

            if (GetComponent<SimpleSprite>().HasValue(out var sprite))
            {
                sprite.OrientationType = SimpleSprite.OrientMode.YAxis;
                sprite.LightsWithVoxels = false;
            }
        }

        public override void Update(DwarfTime Time, ChunkManager Chunks, Camera Camera)
        {
            if (Active)
            {
                GrowthTimer.Update(Time);
                if (GrowthTimer.HasTriggered)
                {
                    var objects = World.EnumerateIntersectingRootObjects(new BoundingBox(-Vector3.One * GrowthRadius + Position, Vector3.One * GrowthRadius + Position), CollisionType.Static);
                    var seeds = objects.OfType<Seedling>().ToList();
                    foreach (var obj in seeds)
                    {
                        if (MathFunctions.RandEvent(1.0f / seeds.Count))
                        {
                            obj.GrowthTime += GrowthTime;
                            World.ParticleManager.Trigger("green_flame", obj.Position, Color.White, 10);
                            World.ParticleManager.TriggerRay("green_flame", Position, obj.Position);
                            SoundManager.PlaySound(ContentPaths.Audio.tinkle, obj.Position, true, 1.0f);

                            if (GetComponent<MagicalObject>().HasValue(out var magicalObject))
                                magicalObject.CurrentCharges -= 1;

                            break;
                        }
                    }
                }
            }

            base.Update(Time, Chunks, Camera);
        }
    }
}
