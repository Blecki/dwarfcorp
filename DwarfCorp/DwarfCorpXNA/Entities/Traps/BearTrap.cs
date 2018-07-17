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

namespace DwarfCorp
{
    public class BearTrap : CraftedBody
    {
        [EntityFactory("Bear Trap")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            return new BearTrap(Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null));
        }

        public float DamageAmount { get; set; }
        public Faction Allies { get; set; }
        public bool ShouldDie = false;
        public Timer DeathTimer { get; set; }

        public BearTrap()
        {
            
        }

        public BearTrap(ComponentManager manager, Vector3 pos, List<ResourceAmount> resources) :
            base(manager,
            "BearTrap", Matrix.CreateTranslation(pos),
            new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, new DwarfCorp.CraftDetails(manager, "Bear Trap", resources))
        {
            Allies = manager.World.PlayerFaction;
            
            DeathTimer = new Timer(0.6f, true);

            DamageAmount = 200;

            CreateCosmeticChildren(manager);
        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            AddChild(new Shadow(manager));

            var spriteSheet = new SpriteSheet(ContentPaths.Entities.DwarfObjects.beartrap, 32);

            var sprite = AddChild(new AnimatedSprite(Manager, "Sprite", Matrix.Identity, false)) as AnimatedSprite;

            sprite.AddAnimation(AnimationLibrary.CreateAnimation(spriteSheet, new List<Point> { Point.Zero }, "BearTrapIdle"));

            var sprung = AnimationLibrary.CreateAnimation
                (spriteSheet, new List<Point>
                {
                    new Point(0,1),
                    new Point(1,1),
                    new Point(2,1),
                    new Point(3,1)
                }, "BearTrapTrigger");

            sprung.FrameHZ = 6.6f;

            sprite.AddAnimation(sprung);

            sprite.SetFlag(Flag.ShouldSerialize, false);
            sprite.SetCurrentAnimation("BearTrapIdle", false);

            AddChild(new GenericVoxelListener(manager, Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), new Vector3(0.0f, -1.0f, 0.0f), (changeEvent) =>
            {
                if (changeEvent.Type == VoxelChangeEventType.VoxelTypeChanged && changeEvent.NewVoxelType == 0)
                    Die();
            })).SetFlag(Flag.ShouldSerialize, false);


            var sensor = AddChild(new Sensor(manager, "Sensor", Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), Vector3.Zero)
            {
                FireTimer = new Timer(0.5f, false, Timer.TimerMode.Real)
            }) as Sensor;
            sensor.OnSensed += Sensor_OnSensed;
            sensor.SetFlag(Flag.ShouldSerialize, false);

            AddChild(new ParticleTrigger("explode", Manager, "DeathParticles",
                Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), Vector3.Zero)
            {
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_trap_destroyed
            }).SetFlag(Flag.ShouldSerialize, false);

            base.CreateCosmeticChildren(manager);
        }

        void Sensor_OnSensed(IEnumerable<Body> sensed)
        {
            if (!Active)
                return;

            if (ShouldDie)
            {
                return;
            }

            foreach (Body body in sensed)
            {
                var creature = body.EnumerateAll().OfType<CreatureAI>().FirstOrDefault();

                if (creature == null) continue;
                if (World.Diplomacy.GetPolitics(creature.Creature.Faction, Allies).GetCurrentRelationship() == Relationship.Loving) continue;

                creature.Creature.Damage(DamageAmount);
                creature.Creature.Physics.Velocity *= 0.0f;
                Trigger();
                break;
            }
        }

        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

            if (ShouldDie)
            {
                DeathTimer.Update(gameTime);

                if (DeathTimer.HasTriggered)
                {
                    Die();
                }
            }
        }

        public void Trigger()
        {
            EnumerateChildren().OfType<AnimatedSprite>().FirstOrDefault().SetCurrentAnimation("BearTrapTrigger", true);
            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_trap_trigger, GlobalTransform.Translation, false);
            ShouldDie = true;
            DeathTimer.Reset(DeathTimer.TargetTimeSeconds);
        }
    }
}
