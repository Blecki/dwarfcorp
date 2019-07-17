using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using LibNoise.Models;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class RadiusSensor : GameComponent
    {
        public List<Creature> Creatures = new List<Creature>();
        private Timer SenseTimer = new Timer(0.5f, false, Timer.TimerMode.Real);
        public float SenseRadius = 15 * 15;
        public bool CheckLineOfSight = true;

        public RadiusSensor() : base()
        {
            CollisionType = CollisionType.None;
        }

        public RadiusSensor(ComponentManager manager, string name, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
            base(manager, name, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            // Todo: Calculate bounding box from radius.
            Tags.Add("Sensor");
            CollisionType = CollisionType.None;
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

            if (!Active) return;

            SenseTimer.Update(gameTime);

            if (SenseTimer.HasTriggered)
            {
                Creatures.Clear();

                var myRoot = GetRoot();

                foreach (var body in Manager.World.EnumerateIntersectingObjects(BoundingBox, b => b.Active && !Object.ReferenceEquals(b, myRoot) && b.IsRoot()))
                {
                    if (body.GetComponent<Creature>().HasValue(out var minion))
                    {
                        float dist = (body.Position - GlobalTransform.Translation).LengthSquared();

                        if (dist > SenseRadius)
                            continue;

                        if (CheckLineOfSight && VoxelHelpers.DoesRayHitSolidVoxel(Manager.World.ChunkManager, Position, body.Position))
                            continue;

                        Creatures.Add(minion);
                    }
                }
            }

            Creatures.RemoveAll(ai => ai.IsDead);
        }
    }
}