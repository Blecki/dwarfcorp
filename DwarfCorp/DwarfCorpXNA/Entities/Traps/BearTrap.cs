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
    [JsonObject(IsReference = true)]
    public class BearTrap : Body, IUpdateableComponent
    {
        public Sensor Sensor { get; set; }
        public AnimatedSprite Sprite { get; set; }
        public VoxelListener VoxListener { get; set; }
        public ParticleTrigger DeathParticles { get; set; }
        public float DamageAmount { get; set; }
        public static string IdleAnimation = "Idle";
        public static string TriggerAnimation = "Trigger";
        public Faction Allies { get; set; }
        public bool ShouldDie = false;
        public Timer DeathTimer { get; set; }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext ctx)
        {
            Sensor.OnSensed += Sensor_OnSensed;
        }

        public BearTrap()
        {
            
        }

        public BearTrap(ComponentManager manager, Vector3 pos) :
            base(manager,
            "BearTrap", Matrix.CreateTranslation(pos),
            new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, true)
        {
            Allies = manager.World.PlayerFaction;
            Sensor = AddChild(new Sensor(manager, "Sensor", Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), Vector3.Zero)
            {
                FireTimer = new Timer(0.5f, false)
            }) as Sensor;
            Sensor.OnSensed += Sensor_OnSensed;
            DeathTimer = new Timer(0.6f, true);
            DeathParticles = AddChild(new ParticleTrigger("explode", Manager, "DeathParticles", 
                Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), Vector3.Zero)
            {
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_trap_destroyed
            }) as ParticleTrigger;

            DamageAmount = 200;

            var voxelUnder = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(
                manager.World.ChunkManager.ChunkData,
                GlobalVoxelCoordinate.FromVector3(pos)));
            VoxListener = AddChild(new VoxelListener(manager, manager.World.ChunkManager, voxelUnder))
                    as VoxelListener;

            

            CreateCosmeticChildren(manager);

        }

        public override void CreateCosmeticChildren(ComponentManager manager)
        {
            AddChild(new Shadow(manager));

            var spriteSheet = new SpriteSheet(ContentPaths.Entities.DwarfObjects.beartrap);

            Sprite = AddChild(new AnimatedSprite(Manager, "Sprite", Matrix.Identity, false)) as AnimatedSprite;

            Sprite.AddAnimation(AnimationLibrary.CreateAnimation(spriteSheet, new List<Point> { Point.Zero }, IdleAnimation));

            var sprung = AnimationLibrary.CreateAnimation
                (spriteSheet, new List<Point>
                {
                    new Point(0,1),
                    new Point(1,1),
                    new Point(2,1),
                    new Point(3,1)
                }, TriggerAnimation);

            sprung.FrameHZ = 6.6f;

            Sprite.AddAnimation(sprung);

            Sprite.SetFlag(Flag.ShouldSerialize, false);
            base.CreateCosmeticChildren(manager);
        }

        void Sensor_OnSensed(IEnumerable<Body> sensed)
        {
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

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {

            if (ShouldDie)
            {
                DeathTimer.Update(gameTime);

                if (DeathTimer.HasTriggered)
                {
                    Die();
                }
            }
            base.Update(gameTime, chunks, camera);
        }

        public void Trigger()
        {
            Sprite.SetCurrentAnimation(TriggerAnimation, true);
            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_trap_trigger, GlobalTransform.Translation, false);
            ShouldDie = true;
            DeathTimer.Reset(DeathTimer.TargetTimeSeconds);
        }
    }
}
