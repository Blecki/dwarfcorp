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
    public class RadiusSensor : Body
    {
        public List<CreatureAI> Creatures = new List<CreatureAI>();
        private Timer SenseTimer = new Timer(0.5f, false, Timer.TimerMode.Real);
        public float SenseRadius = 15 * 15;
        public bool DetectCloaked = false;

        public RadiusSensor() : base()
        {
            UpdateRate = 10;
            CollisionType = CollisionType.None;
        }

        public RadiusSensor(ComponentManager manager, string name, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos) :
            base(manager, name, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            UpdateRate = 10;
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

                foreach (var body in Manager.World.EnumerateIntersectingObjects(BoundingBox, b => !Object.ReferenceEquals(b, myRoot) && b.IsRoot()))
                {
                    var minion = body.GetComponent<CreatureAI>();
                    if (minion == null || !minion.Active)
                        continue;

                    if (!DetectCloaked && minion.Creature.IsCloaked)
                        continue;

                    float dist = (minion.Position - GlobalTransform.Translation).LengthSquared();

                    if (dist < SenseRadius && !VoxelHelpers.DoesRayHitSolidVoxel(
                        Manager.World.ChunkManager.ChunkData, Position, minion.Position))
                    {
                        Creatures.Add(minion);
                    }
                }
            }

            Creatures.RemoveAll(ai => ai.IsDead);
        }
    }
}