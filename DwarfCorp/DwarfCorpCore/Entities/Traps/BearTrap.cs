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

using System.Collections.Generic;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     Trap that when triggered does damage to an enemy.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class BearTrap : Body
    {
        /// <summary>
        ///     When the trap is sitting around doing nothing, this is the animation to play.
        /// </summary>
        public static string IdleAnimation = "Idle";

        /// <summary>
        ///     When the trap is triggered, this is the animation to play.
        /// </summary>
        public static string TriggerAnimation = "Trigger";

        /// <summary>
        ///     If true, a death animation will be played.
        /// </summary>
        public bool ShouldDie = false;

        public BearTrap()
        {
        }

        public BearTrap(Vector3 pos) :
            base(
            "BearTrap", PlayState.ComponentManager.RootComponent, Matrix.CreateTranslation(pos),
            new Vector3(1.0f, 1.0f, 1.0f), Vector3.Zero, true)
        {
            Allies = PlayState.PlayerFaction;
            Sensor = new Sensor("Sensor", this, Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), Vector3.Zero)
            {
                FireTimer = new Timer(0.5f, false)
            };
            Sensor.OnSensed += Sensor_OnSensed;
            DeathTimer = new Timer(0.6f, true);
            DeathParticles = new ParticleTrigger("puff", PlayState.ComponentManager, "DeathParticles", this,
                Matrix.Identity, new Vector3(0.5f, 0.5f, 0.5f), Vector3.Zero)
            {
                SoundToPlay = ""
            };

            DamageAmount = 200;
            var voxUnder = new Voxel();
            PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(pos, ref voxUnder);
            VoxListener = new VoxelListener(PlayState.ComponentManager, this, PlayState.ChunkManager, voxUnder);
            Sprite = new Sprite(PlayState.ComponentManager, "Sprite", this, Matrix.Identity,
                new SpriteSheet(ContentPaths.Entities.DwarfObjects.beartrap), false);
            Sprite.AddAnimation(new Animation(0, ContentPaths.Entities.DwarfObjects.beartrap, 32, 32, 0)
            {
                Name = IdleAnimation
            });
            Sprite.AddAnimation(new Animation(1, ContentPaths.Entities.DwarfObjects.beartrap, 32, 32, 0, 1, 2, 3)
            {
                Name = TriggerAnimation,
                Speeds = new List<float> {6.6f},
                Loops = true
            });
        }

        /// <summary>
        ///     Sensor that fires whenever an enemy enteres it.
        /// </summary>
        public Sensor Sensor { get; set; }

        /// <summary>
        ///     The sprite of the trap.
        /// </summary>
        public Sprite Sprite { get; set; }

        /// <summary>
        ///     The trap gets destroyed whenever the voxel underneath it is destroyed
        /// </summary>
        public VoxelListener VoxListener { get; set; }

        /// <summary>
        ///     When the trap is destroyed, death particles are created there.
        /// </summary>
        public ParticleTrigger DeathParticles { get; set; }

        /// <summary>
        ///     Amount of damage (in HP) to apply to the enemy.
        /// </summary>
        public float DamageAmount { get; set; }

        /// <summary>
        ///     The trap is allied with this faction. It damages all enemies of this.
        /// </summary>
        public Faction Allies { get; set; }

        /// <summary>
        ///     Time between when death is triggered and the trap disappears.
        /// </summary>
        public Timer DeathTimer { get; set; }

        private void Sensor_OnSensed(List<Body> sensed)
        {
            if (ShouldDie)
            {
                return;
            }

            // Look for all the bodies that are enemies, and trigger if we found one!
            foreach (Body body in sensed)
            {
                CreatureAI creature = body.GetChildrenOfTypeRecursive<CreatureAI>().FirstOrDefault();

                if (creature == null) continue;
                // Only damage enemies. How does the trap know what the politics of the world are? Magic.
                if (
                    PlayState.ComponentManager.Diplomacy.GetPolitics(creature.Creature.Faction, Allies)
                        .GetCurrentRelationship() == Relationship.Loving) continue;

                // It damages the creature.
                creature.Creature.Damage(DamageAmount);
                // The trap stops the enemy creature in its tracks.
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
            Sprite.SetCurrentAnimation(TriggerAnimation);
            Sprite.CurrentAnimation.Play();
            SoundManager.PlaySound(ContentPaths.Audio.trap, GlobalTransform.Translation, false);
            ShouldDie = true;
            DeathTimer.Reset(DeathTimer.TargetTimeSeconds);
        }
    }
}