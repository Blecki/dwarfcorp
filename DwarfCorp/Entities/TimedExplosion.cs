// BearTrap.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Threading;

namespace DwarfCorp
{
    public class TimedExplosion : CraftedBody
    {
        [EntityFactory("Explosive")]
        private static GameComponent __factory03(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new TimedExplosion(Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null))
            {
                VoxelRadius = 3,
                FuseTime = 5.0f
            };
        }

        public float DamageAmount;
        public Timer DeathTimer;
        public int VoxelRadius = 5;
        public int VoxelsPerTick = 1;
        public float FuseTime = 2.0f;

        private Thread PrepThread;

        [JsonProperty]
        private List<VoxelHandle> OrderedExplosionList;
        
        public enum State
        {
            Initializing,
            Prep,
            Ready,
            Exploding,
            Done
        }

        [JsonProperty]
        private State _state = State.Initializing;

        public int ExplosionProgress = 0;


        public TimedExplosion()
        {

        }

        public TimedExplosion(ComponentManager manager, Vector3 pos, List<ResourceAmount> resources) :
            base(manager,
            "Explosion", Matrix.CreateTranslation(pos),
            new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, new DwarfCorp.CraftDetails(manager, "Explosive", resources))
        {
            DeathTimer = new Timer(FuseTime, true);
            DamageAmount = 200;
            CreateCosmeticChildren(manager);

            DeathTimer.Reset(DeathTimer.TargetTimeSeconds);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            AddChild(new Shadow(manager));

            var spriteSheet = new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32);
            AddChild(new SimpleSprite(Manager, "sprite", Matrix.Identity, spriteSheet, new Point(4, 5)))
                .SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(manager);
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

            if (Active)
            {

                DeathTimer.Update(gameTime);

                switch (_state)
                {
                    case State.Initializing:
                        if (PrepThread != null)
                            PrepThread.Abort();
                        PrepThread = new Thread(PrepareForExplosion) { IsBackground = true } ;
                        _state = State.Prep;
                        PrepThread.Start();

                            foreach (GameComponent body in Manager.World.EnumerateIntersectingObjects(
                                new BoundingBox(LocalPosition - new Vector3(VoxelRadius * 2.0f, VoxelRadius * 2.0f, VoxelRadius * 2.0f), LocalPosition + new Vector3(VoxelRadius * 2.0f, VoxelRadius * 2.0f, VoxelRadius * 2.0f)), CollisionType.Both))
                            {
                                var distance = (body.Position - LocalPosition).Length();
                                if (distance < (VoxelRadius * 2.0f))
                                {
                                    var creature = body.EnumerateAll().OfType<CreatureAI>().FirstOrDefault();
                                    if (creature != null)
                                        creature.ChangeTask(new FleeEntityTask(this, VoxelRadius * 2));
                                }
                            }
                        
                        break;

                    case State.Prep:
                        if (PrepThread == null) // Must have been saved mid-prep.
                            _state = State.Initializing;
                        break;

                    case State.Ready:
                        if (OrderedExplosionList == null) // Just a failsafe.
                            throw new InvalidOperationException();

                        float timeLeft = (float)(DeathTimer.TargetTimeSeconds - DeathTimer.CurrentTimeSeconds);
                        float pulseRate = 15 - 2 * timeLeft;
                        float pulse = (float)Math.Sin(timeLeft * pulseRate);
                        this.SetVertexColorRecursive(new Color(1.0f, 1.0f - pulse * pulse, 1.0f - pulse * pulse, 1.0f));
                        if (DeathTimer.HasTriggered)
                        {
                            _state = State.Exploding;
                            Manager.World.ParticleManager.Effects["explode"].Trigger(10, Position, Color.White);
                            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_trap_destroyed, 0.5f);

                            foreach (GameComponent body in Manager.World.EnumerateIntersectingObjects(
                                new BoundingBox(LocalPosition - new Vector3(VoxelRadius * 2.0f, VoxelRadius * 2.0f, VoxelRadius * 2.0f), LocalPosition + new Vector3(VoxelRadius * 2.0f, VoxelRadius * 2.0f, VoxelRadius * 2.0f)), CollisionType.Both))
                            {
                                var distance = (body.Position - LocalPosition).Length();
                                if (distance <= (VoxelRadius * 2.0f))
                                {
                                    var health = body.EnumerateAll().OfType<Health>().FirstOrDefault();
                                    if (health != null)
                                        health.Damage(DamageAmount * (1.0f - (distance / (VoxelRadius * 2.0f)))); // Linear fall off on damage.
                                }
                            }
                        }

                        break;

                    case State.Exploding:
                        SetFlagRecursive(Flag.Visible, false);
                        if (OrderedExplosionList == null)
                            throw new InvalidOperationException();

                        var voxelsExploded = 0;
                        while (true)
                        {
                            if (voxelsExploded >= VoxelsPerTick)
                                break;

                            if (ExplosionProgress >= OrderedExplosionList.Count)
                            {
                                GetRoot().Delete();
                                _state = State.Done;
                                foreach (var vox in OrderedExplosionList)
                                {
                                    if (!vox.IsValid) continue;
                                    var under = VoxelHelpers.GetVoxelBelow(vox);
                                    if (under.IsValid && !under.IsEmpty && MathFunctions.RandEvent(0.5f))
                                    {
                                        EntityFactory.CreateEntity<Fire>("Fire", vox.GetBoundingBox().Center());
                                    }
                                }
                                break;
                            }

                            var nextVoxel = OrderedExplosionList[ExplosionProgress];
                            ExplosionProgress += 1;

                            if (nextVoxel.IsValid)
                            {
                                if (!nextVoxel.Type.IsInvincible && !nextVoxel.IsEmpty)
                                {
                                    voxelsExploded += 1;
                                    nextVoxel.Type = Library.EmptyVoxelType;
                                    Manager.World.ParticleManager.Effects["explode"].Trigger(1, nextVoxel.Coordinate.ToVector3() + new Vector3(0.5f, 0.5f, 0.5f), Color.White);
                                    Manager.World.ParticleManager.Effects["dirt_particle"].Trigger(3, nextVoxel.Coordinate.ToVector3() + new Vector3(0.5f, 0.5f, 0.5f), Color.White);

                                    Manager.World.PlayerFaction.OnVoxelDestroyed(nextVoxel);
                                }
                            }
                        }

                        break;

                    case State.Done:
                    default:
                        if (PrepThread != null)
                            PrepThread.Abort();
                        break;
                }                
            }
        }

        private void PrepareForExplosion()
        {
            var pos = LocalPosition;
            var explodeList = new List<Tuple<GlobalVoxelCoordinate, float>>();

            for (var x = (int)(pos.X - VoxelRadius); x < (int)(pos.X + VoxelRadius); ++x)
                for (var y = (int)(pos.Y - VoxelRadius); y < (int)(pos.Y + VoxelRadius); ++y)
                    for (var z = (int)(pos.Z - VoxelRadius); z < (int)(pos.Z + VoxelRadius); ++z)
                    {
                        var voxelCenter = new Vector3(x, y, z) + new Vector3(0.5f, 0.5f, 0.5f);
                        var distance = (voxelCenter - pos).Length();
                        if (distance <= VoxelRadius)
                            explodeList.Add(Tuple.Create(new GlobalVoxelCoordinate(x, y, z), distance));
                    }

            OrderedExplosionList = explodeList.OrderBy(t => t.Item2).Select(t => new VoxelHandle(World.ChunkManager, t.Item1)).ToList();
            _state = State.Ready;
        }
    }
}
