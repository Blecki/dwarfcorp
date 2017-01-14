// Creature.cs
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
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    ///<summary>
    /// CreatureDef defines a creature to be loaded from JSON files. When
    /// deserialized, it can be converted into a creature directly.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class CreatureDef
    {
        /// <summary> Name of the creature used for spawning </summary>
        public string Name { get; set; }
        /// <summary> Description of the creature displayed when the player mouses over it </summary>
        public string Description { get; set; }
        /// <summary> Race that the creature belongs to </summary>
        public string Race { get; set; }
        /// <summary> Size of the creature's bounding box in voxels </summary>
        public Vector3 Size { get; set; }
        /// <summary> If true, a shadow will be rendered under the creature </summary>
        public bool HasShadow { get; set; }
        /// <summary> If true, the creature takes fire damage and lights on fire from lava </summary>
        public bool IsFlammable { get; set; }
        /// <summary> Name of the particle effect to trigger when the creature gets hurt </summary>
        public string BloodParticle { get; set; }
        /// <summary> Sound the creature makes when it dies </summary>
        public string DeathSound { get; set; }
        /// <summary> Sounds the creature makes when hurt </summary>
        public List<string> HurtSounds { get; set; }
        /// <summary> Sound the creature makes when chewing food </summary>
        public string ChewSound { get; set; }
        /// <summary> Sound the creature makes when jumping </summary>
        public string JumpSound { get; set; }
        /// <summary> If true, when the creature dies, all of the other members of its race will mourn </summary>
        public bool TriggersMourning { get; set; }
        /// <summary> The size of the creature's shadow in voxels </summary>
        public float ShadowScale { get; set; }
        /// <summary> How much the creature resists external forces </summary>
        public float Mass { get; set; }
        /// <summary> The number of objects in the creature's inventory </summary>
        public int InventorySize { get; set; }
        /// <summary> Offset between the creature's origin and its sprite </summary>
        public Vector3 SpriteOffset { get; set; }
        /// <summary> The icon to draw on the minimap for the creature </summary>
        public NamedImageFrame MinimapIcon { get; set; }
        /// <summary> Bounding box in which the creature can see </summary>
        public Vector3 SenseRange { get; set; }
        /// <summary> Identifier path to a JSON file containing all the creature's classes </summary>
        public string Classes { get; set; }
        /// <summary> If true, the creature will sleep when tired. </summary>
        public bool CanSleep { get; set; }
        /// <summary> If true, the creature will eat when hungry </summary>
        public bool CanEat { get; set; }
        /// <summary> Arbitrary tags assigned to the creature </summary>
        public List<string> Tags { get; set; }

        /// <summary>
        /// Called when the creature definition is deserialized from JSON.
        /// </summary>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            EmployeeClass.AddClasses(Classes);
        }
    }

    /// <summary>
    ///     Component which keeps track of a large number of other components (AI, physics, sprites, etc.)
    ///     related to creatures (such as dwarves and goblins).
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Creature : Health
    {
        /// <summary> Enum describing the character's current action (used for animation) </summary>
        public enum CharacterMode
        {
            Walking,
            Idle,
            Falling,
            Jumping,
            Attacking,
            Hurt,
            Sleeping,
            Swimming,
            Flying,
            Sitting
        }

        /// <summary> Describes the way in which a creature can move from one location to another </summary>
        public enum MoveType
        {
            /// <summary> Move along a horizontal surface </summary>
            Walk,
            /// <summary> Jump from one voxel to another. </summary>
            Jump,
            /// <summary> Climb up a climbable object </summary>
            Climb,
            /// <summary> Move through water </summary>
            Swim,
            /// <summary> Fall vertically through space </summary>
            Fall,
            /// <summary> Move from one empty voxel to another </summary>
            Fly,
            /// <summary> Attack a blocking object until it is destroyed </summary>
            DestroyObject,
            /// <summary> Move along a vertical surface. </summary>
            ClimbWalls
        }

        /// <summary> 
        /// Creatures can draw indicators showing the user what they're thinking.
        /// This is the minimum time in seconds between which indicators will be drawn.
        /// </summary>
        private float IndicatorRateLimit = 2.0f;

        /// <summary>
        /// This is the last time that the creature produced an indicator.
        /// This is compared against the rate limit to determine if a new indicator
        /// can be drawn.
        /// </summary>
        private DateTime LastIndicatorTime = DateTime.Now;

        /// <summary> This is what the character is currently doing (used for animation) </summary>
        protected CharacterMode currentCharacterMode = CharacterMode.Idle;

        public Creature()
        {
            CurrentCharacterMode = CharacterMode.Idle;

            OverrideCharacterMode = false;
            Buffs = new List<Buff>();
            HasMeat = true;
            HasBones = true;
        }

        public Creature(Vector3 pos, CreatureDef def, string creatureClass, int creatureLevel, string faction) :
            this(new CreatureStats(EmployeeClass.Classes[creatureClass], creatureLevel),
                faction,
                PlayState.PlanService,
                PlayState.ComponentManager.Factions.Factions[faction],
                new Physics(def.Name, PlayState.ComponentManager.RootComponent, Matrix.CreateTranslation(pos), def.Size,
                    new Vector3(0, -def.Size.Y * 0.5f, 0), def.Mass, 1.0f, 0.999f, 0.999f, Vector3.UnitY * -10,
                    Physics.OrientMode.RotateY),
                PlayState.ChunkManager,
                GameState.Game.GraphicsDevice,
                GameState.Game.Content,
                def.Name)
        {
            HasMeat = true;
            HasBones = true;
            EmployeeClass employeeClass = EmployeeClass.Classes[creatureClass];
            Physics.Orientation = Physics.OrientMode.RotateY;
            Sprite = new CharacterSprite(Graphics, Manager, "Sprite", Physics,
                Matrix.CreateTranslation(def.SpriteOffset));

            foreach (Animation animation in employeeClass.Animations)
            {
                Sprite.AddAnimation(animation.Clone());
            }

            Hands = new Grabber("hands", Physics, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero);

            Sensors = new EnemySensor(Manager, "EnemySensor", Physics, Matrix.Identity, def.SenseRange, Vector3.Zero);

            AI = new CreatureAI(this, "AI", Sensors, PlanService);

            Attacks = new List<Attack>();

            foreach (Attack attack in employeeClass.Attacks)
            {
                Attacks.Add(new Attack(attack));
            }


            Inventory = new Inventory("Inventory", Physics)
            {
                Resources = new ResourceContainer
                {
                    MaxResources = def.InventorySize
                }
            };

            if (def.HasShadow)
            {
                Matrix shadowTransform = Matrix.CreateRotationX((float)Math.PI * 0.5f);
                shadowTransform.Translation = new Vector3(0.0f, -0.5f, 0.0f);

                Shadow = new Shadow(Manager, "Shadow", Physics, shadowTransform,
                    new SpriteSheet(ContentPaths.Effects.shadowcircle))
                {
                    GlobalScale = def.ShadowScale
                };
                var shP = new List<Point>
                {
                    new Point(0, 0)
                };
                var shadowAnimation = new Animation(Graphics, new SpriteSheet(ContentPaths.Effects.shadowcircle),
                    "sh", 32, 32, shP, false, Color.Black, 1, 0.7f, 0.7f, false);
                Shadow.AddAnimation(shadowAnimation);
                shadowAnimation.Play();
                Shadow.SetCurrentAnimation("sh");
            }
            Physics.Tags.AddRange(def.Tags);

            DeathParticleTrigger = new ParticleTrigger(def.BloodParticle, Manager, "Death Gibs", Physics,
                Matrix.Identity, Vector3.One, Vector3.Zero)
            {
                TriggerOnDeath = true,
                TriggerAmount = 1,
                BoxTriggerTimes = 10,
                SoundToPlay = ContentPaths.Entities.Dwarf.Audio.dwarfhurt1,
            };

            if (def.IsFlammable)
            {
                Flames = new Flammable(Manager, "Flames", Physics, this);
            }

            NoiseMaker.Noises["Hurt"] = def.HurtSounds;
            NoiseMaker.Noises["Chew"] = new List<string> { def.ChewSound };
            NoiseMaker.Noises["Jump"] = new List<string> { def.JumpSound };

            var minimapIcon = new MinimapIcon(Physics, def.MinimapIcon);
            Stats.FullName =
                TextGenerator.GenerateRandom(PlayState.ComponentManager.Factions.Races[def.Race].NameTemplates);
            Stats.CanSleep = def.CanSleep;
            Stats.CanEat = def.CanEat;
            AI.TriggersMourning = def.TriggersMourning;
        }

        public Creature(CreatureStats stats,
            string allies,
            PlanService planService,
            Faction faction,
            Physics parent,
            ChunkManager chunks,
            GraphicsDevice graphics,
            ContentManager content,
            string name) :
            base(parent.Manager, name, parent, stats.MaxHealth, 0.0f, stats.MaxHealth)
        {
            HasMeat = true;
            HasBones = true;
            Buffs = new List<Buff>();
            IsOnGround = true;
            Physics = parent;
            Stats = stats;
            Chunks = chunks;
            Graphics = graphics;
            Content = content;
            Faction = faction;
            PlanService = planService;
            Allies = allies;
            Controller = new PIDController(Stats.MaxAcceleration, Stats.StoppingForce * 2, 0.0f);
            JumpTimer = new Timer(0.2f, true);
            Status = new CreatureStatus();
            IsHeadClear = true;
            NoiseMaker = new NoiseMaker();
            OverrideCharacterMode = false;
            SelectionCircle = new SelectionCircle(Manager, Physics)
            {
                IsVisible = false
            };
        }

        /// <summary> The creature's AI determines how it will behave. </summary>
        public CreatureAI AI { get; set; }
        /// <summary> The crature's physics determines how it moves around </summary>
        public Physics Physics { get; set; }
        /// <summary> The sprite draws the character and handles animations </summary>
        public CharacterSprite Sprite { get; set; }
        /// <summary> The selection circle is drawn when the character is selected </summary>
        public SelectionCircle SelectionCircle { get; set; }
        /// <summary> Finds enemies nearby and triggers when it sees them </summary>
        public EnemySensor Sensors { get; set; }
        /// <summary> Spawns fire and kills the creature when it is damaged </summary>
        public Flammable Flames { get; set; }
        /// <summary> Creates particles when the creature dies. </summary>
        public ParticleTrigger DeathParticleTrigger { get; set; }
        /// <summary> Allows the creature to grab other objects </summary>
        public Grabber Hands { get; set; }
        /// <summary> Drawn beneath the creature </summary>
        public Shadow Shadow { get; set; }
        /// <summary> If true, the creature will generate meat when it dies. </summary>
        public bool HasMeat { get; set; }
        /// <summary> If true, the creature will generate bones when it dies. </summary>
        public bool HasBones { get; set; }
        /// <summary> Used to make sounds for the creature </summary>
        public NoiseMaker NoiseMaker { get; set; }
        /// <summary> The creature can hold objects in its inventory </summary>
        public Inventory Inventory { get; set; }

        /// <summary> Reference to the graphics device. </summary>
        [JsonIgnore]
        public GraphicsDevice Graphics { get; set; }

        /// <summary> Reference to the chunk manager. </summary>
        [JsonIgnore]
        public ChunkManager Chunks { get; set; }

        /// <summary> List of attacks the creature can perform. </summary>
        public List<Attack> Attacks { get; set; }

        /// <summary> Reference to the content manager </summary>
        [JsonIgnore]
        public ContentManager Content { get; set; }

        /// <summary> Faction that the creature belongs to </summary>
        public Faction Faction { get; set; }

        /// <summary> Reference to the planning service for path planning </summary>
        public PlanService PlanService { get; set; }

        /// <summary> DEPRECATED. TODO(mklingen): DELETE </summary>
        public string Allies { get; set; }

        /// <summary> Used to smoothly apply forces to the creature </summary>
        public PIDController Controller { get; set; }
        /// <summary> The creature's stat numbers (WIS, DEX, STR etc.) </summary>
        public CreatureStats Stats { get; set; }
        /// <summary> The creature's current status (energy, hunger, happiness, etc.) </summary>
        public CreatureStatus Status { get; set; }

        /// <summary> Timer that rate-limits how quickly the creature can jump. DEPRECATED TODO(mklingen): DELETE </summary>
        public Timer JumpTimer { get; set; }

        /// <summary>
        /// If true, the character mode will not be updated automatically by the creature's movement.
        /// This is used to make the character animate in a certain way without interference.
        /// </summary> 
        public bool OverrideCharacterMode { get; set; }

        /// <summary>
        /// Gets or sets the current character mode for animations.
        /// </summary>
        public CharacterMode CurrentCharacterMode
        {
            get { return currentCharacterMode; }
            set
            {
                if (OverrideCharacterMode) return;

                currentCharacterMode = value;
                if (Sprite != null)
                {
                    Sprite.SetCurrentAnimation(value.ToString());
                }
            }
        }

        /// <summary> Convenience wrapper around Status.IsAsleep </summary>
        public bool IsAsleep
        {
            get { return Status.IsAsleep; }
        }

        /// <summary> If true there is a filled voxel immediately beneath this creature </summary>
        public bool IsOnGround { get; set; }
        /// <summary> If true there is an empty voxel immediately above this creature </summary>
        public bool IsHeadClear { get; set; }


        /// <summary> List of ongoing effects the creature is sustaining </summary>
        public List<Buff> Buffs { get; set; }

        /// <summary> Called when the creature is deserialized from JSON </summary>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Graphics = PlayState.ChunkManager.Graphics;
            Content = PlayState.ChunkManager.Content;
            Chunks = PlayState.ChunkManager;
        }

        /// <summary> Adds the specified ongoing effect. </summary>
        /// <param name="buff"> The onging effect to add </param>
        public void AddBuff(Buff buff)
        {
            buff.OnApply(this);
            Buffs.Add(buff);
        }

        /// <summary> Updates the creature's ongoing effects </summary>
        public void HandleBuffs(DwarfTime time)
        {
            foreach (Buff buff in Buffs)
            {
                buff.Update(time, this);
            }

            List<Buff> doneBuffs = Buffs.FindAll(buff => !buff.IsInEffect);
            foreach (Buff buff in doneBuffs)
            {
                buff.OnEnd(this);
                Buffs.Remove(buff);
            }
        }

        /// <summary> Updates the creature </summary>
        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (!IsActive) return;

            CheckNeighborhood(chunks, (float)gameTime.ElapsedGameTime.TotalSeconds);
            UpdateAnimation(gameTime, chunks, camera);
            Status.Update(this, gameTime, chunks, camera);
            JumpTimer.Update(gameTime);
            HandleBuffs(gameTime);

            base.Update(gameTime, chunks, camera);
        }

        /// <summary> 
        /// Checks the voxels around the creature and reacts to changes in its immediate environment.
        /// For example this function determines when the creature is standing on solid ground.
        /// </summary>
        public void CheckNeighborhood(ChunkManager chunks, float dt)
        {
            var voxelBelow = new Voxel();
            bool belowExists = chunks.ChunkData.GetVoxel(Physics.GlobalTransform.Translation - Vector3.UnitY * 0.8f,
                ref voxelBelow);
            var voxelAbove = new Voxel();
            bool aboveExists = chunks.ChunkData.GetVoxel(Physics.GlobalTransform.Translation + Vector3.UnitY,
                ref voxelAbove);

            if (aboveExists)
            {
                IsHeadClear = voxelAbove.IsEmpty;
            }
            if (belowExists && Physics.IsInLiquid)
            {
                IsOnGround = false;
            }
            else if (belowExists)
            {
                IsOnGround = !voxelBelow.IsEmpty;
            }
            else
            {
                if (IsOnGround)
                {
                    IsOnGround = false;
                }
            }

            if (!IsOnGround)
            {
                if (CurrentCharacterMode != CharacterMode.Flying)
                {
                    if (Physics.Velocity.Y > 0.05)
                    {
                        CurrentCharacterMode = CharacterMode.Jumping;
                    }
                    else if (Physics.Velocity.Y < -0.05)
                    {
                        CurrentCharacterMode = CharacterMode.Falling;
                    }
                }

                if (Physics.IsInLiquid)
                {
                    CurrentCharacterMode = CharacterMode.Swimming;
                }
            }

            if (CurrentCharacterMode == CharacterMode.Falling && IsOnGround)
            {
                CurrentCharacterMode = CharacterMode.Idle;
            }

            if (Status.IsAsleep)
            {
                CurrentCharacterMode = CharacterMode.Sleeping;
            }
            else if (currentCharacterMode == CharacterMode.Sleeping)
            {
                CurrentCharacterMode = CharacterMode.Idle;
            }

            if (!Status.Energy.IsUnhappy())
            {
                Status.IsAsleep = false;
            }
        }


        /// <summary>
        /// Kills the creature and releases its resources.
        /// </summary>
        public override void Die()
        {
            // This is just a silly hack to make sure that creatures
            // carrying resources to a trade depot release their resources
            // when they die.
            Inventory.Resources.MaxResources = 99999;
            CreateMeatAndBones();
            base.Die();
        }

        /// <summary>
        /// If the creature has meat or bones, creates resources
        /// which get released when the creature dies.
        /// </summary>
        public virtual void CreateMeatAndBones()
        {
            if (HasMeat)
            {
                ResourceLibrary.ResourceType type = Name + " " + ResourceLibrary.ResourceType.Meat;

                if (!ResourceLibrary.Resources.ContainsKey(type))
                {
                    ResourceLibrary.Add(new Resource(ResourceLibrary.Resources[ResourceLibrary.ResourceType.Meat])
                    {
                        Type = type,
                        ShortName = type
                    });
                }

                Inventory.Resources.AddResource(new ResourceAmount(type, 1));
            }

            if (HasBones)
            {
                ResourceLibrary.ResourceType type = Name + " " + ResourceLibrary.ResourceType.Bones;

                if (!ResourceLibrary.Resources.ContainsKey(type))
                {
                    ResourceLibrary.Add(new Resource(ResourceLibrary.Resources[ResourceLibrary.ResourceType.Bones])
                    {
                        Type = type,
                        ShortName = type
                    });
                }

                Inventory.Resources.AddResource(new ResourceAmount(type, 1));
            }
        }


        /// <summary>
        /// Draws an indicator image over the creature telling us what its thinking.
        /// </summary>
        public void DrawIndicator(ImageFrame image, Color tint)
        {
            if (!((DateTime.Now - LastIndicatorTime).TotalSeconds >= IndicatorRateLimit))
            {
                return;
            }

            IndicatorManager.DrawIndicator(image, AI.Position + new Vector3(0, 0.5f, 0), 1, 1.5f,
                new Vector2(image.SourceRect.Width / 2.0f, -image.SourceRect.Height / 2.0f), tint);
            LastIndicatorTime = DateTime.Now;
        }


        /// <summary>
        /// Draws an indicator above the creature from the list of standard indicators.
        /// </summary>
        public void DrawIndicator(IndicatorManager.StandardIndicators indicator)
        {
            if (!((DateTime.Now - LastIndicatorTime).TotalSeconds >= IndicatorRateLimit))
            {
                return;
            }

            IndicatorManager.DrawIndicator(indicator, AI.Position + new Vector3(0, 0.5f, 0), 1, 2, new Vector2(16, -16));
            LastIndicatorTime = DateTime.Now;
        }


        /// <summary>
        /// Called when the creature receives an event message from another source.
        /// This somewhat janky messaging system is rarely used anymore and should
        /// probably be removed for clarity.
        /// </summary>
        public override void ReceiveMessageRecursive(Message messageToReceive)
        {
            switch (messageToReceive.Type)
            {
                case Message.MessageType.OnChunkModified:
                    break;

                case Message.MessageType.OnHurt:
                    NoiseMaker.MakeNoise("Hurt", AI.Position);
                    Sprite.Blink(0.5f);
                    AI.AddThought(Thought.ThoughtType.TookDamage);
                    PlayState.ParticleManager.Trigger(DeathParticleTrigger.EmitterName, AI.Position, Color.White, 2);
                    break;
            }


            base.ReceiveMessageRecursive(messageToReceive);
        }

        /// <summary>
        /// Updates the creature's animation based on its current state.
        /// </summary>
        public void UpdateAnimation(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (CurrentCharacterMode == CharacterMode.Attacking)
            {
                return;
            }

            float veloNorm = Physics.Velocity.Length();
            if (veloNorm > Stats.MaxSpeed)
            {
                Physics.Velocity = (Physics.Velocity / veloNorm) * Stats.MaxSpeed;
            }
        }

        /// <summary>
        /// Basic Act that causes the creature to wait for the specified time.
        /// Also draws a loading bar above the creature's head when relevant.
        /// </summary>
        public IEnumerable<Act.Status> HitAndWait(float f, bool loadBar)
        {
            var waitTimer = new Timer(f, true);

            CurrentCharacterMode = CharacterMode.Attacking;
            Sprite.ResetAnimations(CharacterMode.Attacking);
            Sprite.PlayAnimations(CharacterMode.Attacking);

            CurrentCharacterMode = CharacterMode.Attacking;

            while (!waitTimer.HasTriggered)
            {
                waitTimer.Update(DwarfTime.LastTime);

                if (loadBar)
                {
                    Drawer2D.DrawLoadBar(AI.Position + Vector3.Up, Color.White, Color.Black, 100, 16,
                        waitTimer.CurrentTimeSeconds / waitTimer.TargetTimeSeconds);
                }

                Attacks[0].PerformNoDamage(this, DwarfTime.LastTime, AI.Position);
                Physics.Velocity = Vector3.Zero;
                Sprite.ReloopAnimations(CharacterMode.Attacking);
                yield return Act.Status.Running;
            }
            Sprite.PauseAnimations(CharacterMode.Attacking);
            CurrentCharacterMode = CharacterMode.Idle;
            yield return Act.Status.Success;
        }

        /// <summary>
        /// Called whenever the creature takes damage.
        /// </summary>
        public override float Damage(float amount, DamageType type = DamageType.Normal)
        {
            float damage = base.Damage(amount, type);

            string prefix = damage > 0 ? "-" : "+";
            Color color = damage > 0 ? Color.Red : Color.Green;

            IndicatorManager.DrawIndicator(prefix + (int)amount + " HP",
                AI.Position + Vector3.Up + MathFunctions.RandVector3Cube() * 0.5f, 0.5f, color,
                Indicator.IndicatorMode.Indicator3D);

            if (damage > 0)
            {
                NoiseMaker.MakeNoise("Hurt", AI.Position);
                Sprite.Blink(0.5f);
                AI.AddThought(Thought.ThoughtType.TookDamage);
                PlayState.ParticleManager.Trigger(DeathParticleTrigger.EmitterName, AI.Position, Color.White, 2);
            }

            return damage;
        }

        /// <summary>
        /// Adds a body to the creature's list of gather designations.
        /// </summary>
        public void Gather(Body item)
        {
            var gatherTask = new GatherItemTask(item)
            {
                Priority = Task.PriorityType.High
            };

            if (!AI.Tasks.Contains(gatherTask))
            {
                if (!AI.Faction.GatherDesignations.Contains(item))
                {
                    AI.Faction.GatherDesignations.Add(item);
                }
                AI.Tasks.Add(gatherTask);
            }
        }

        /// <summary>
        /// A buff is an ongoing effect applied to a creature. This can heal the creature,
        /// damage it, or apply any other kind of effect.
        /// </summary>
        public class Buff
        {
            public Buff()
            {
            }

            /// <summary>
            /// Create a buff which persists for the specified time.
            /// </summary>
            public Buff(float time)
            {
                EffectTime = new Timer(time, true);
                ParticleTimer = new Timer(0.25f, false);
            }

            /// <summary> Time that the effect persists for </summary>
            public Timer EffectTime { get; set; }

            /// <summary> If true, the buff is active. </summary>
            public bool IsInEffect
            {
                get { return !EffectTime.HasTriggered; }
            }

            /// <summary> Particles to generate during the buff. </summary>
            public string Particles { get; set; }
            /// <summary> Every time this triggers, a particle gets released </summary>
            public Timer ParticleTimer { get; set; }
            /// <summary> Sound to play when the buff starts </summary>
            public string SoundOnStart { get; set; }
            /// <summary> Sound to play when the buff ends </summary>
            public string SoundOnEnd { get; set; }


            /// <summary> Called when the Buff is added to a Creature </summary>
            public virtual void OnApply(Creature creature)
            {
                if (!string.IsNullOrEmpty(SoundOnStart))
                {
                    SoundManager.PlaySound(SoundOnStart, creature.Physics.Position, true, 1.0f);
                }
            }

            /// <summary> Called when the Buff is removed from a Creature </summary>
            public virtual void OnEnd(Creature creature)
            {
                if (!string.IsNullOrEmpty(SoundOnEnd))
                {
                    SoundManager.PlaySound(SoundOnEnd, creature.Physics.Position, true, 1.0f);
                }
            }

            /// <summary> Updates the Buff </summary>
            public virtual void Update(DwarfTime time, Creature creature)
            {
                EffectTime.Update(time);
                ParticleTimer.Update(time);

                if (ParticleTimer.HasTriggered && !string.IsNullOrEmpty(Particles))
                {
                    PlayState.ParticleManager.Trigger(Particles, creature.Physics.Position, Color.White, 1);
                }
            }

            /// <summary> Creates a new Buff that is a deep copy of this one. </summary>
            public virtual Buff Clone()
            {
                return new Buff
                {
                    EffectTime = new Timer(EffectTime.TargetTimeSeconds, EffectTime.TriggerOnce, EffectTime.Mode),
                    Particles = Particles,
                    ParticleTimer =
                        new Timer(ParticleTimer.TargetTimeSeconds, ParticleTimer.TriggerOnce, ParticleTimer.Mode),
                    SoundOnEnd = SoundOnEnd,
                    SoundOnStart = SoundOnStart
                };
            }
        }
        ///<summary> A Buff which allows the creature to resist some amount of damage of a specific kind </summary>
        public class DamageResistBuff : Buff
        {
            public DamageResistBuff()
            {
                DamageType = DamageType.Normal;
                Bonus = 0.0f;
            }

            /// <summary> The kind of damage to ignore </summary>
            public DamageType DamageType { get; set; }
            /// <summary> The amount of damage to ignore. </summary>
            public float Bonus { get; set; }

            public override Buff Clone()
            {
                return new DamageResistBuff
                {
                    EffectTime = new Timer(EffectTime.TargetTimeSeconds, EffectTime.TriggerOnce, EffectTime.Mode),
                    Particles = Particles,
                    ParticleTimer =
                        new Timer(ParticleTimer.TargetTimeSeconds, ParticleTimer.TriggerOnce, ParticleTimer.Mode),
                    SoundOnEnd = SoundOnEnd,
                    SoundOnStart = SoundOnStart,
                    DamageType = DamageType,
                    Bonus = Bonus
                };
            }

            public override void OnApply(Creature creature)
            {
                creature.Resistances[DamageType] += Bonus;
                base.OnApply(creature);
            }

            public override void OnEnd(Creature creature)
            {
                creature.Resistances[DamageType] -= Bonus;
                base.OnEnd(creature);
            }
        }

        /// <summary>
        /// A move action is a link between two voxels and a type of motion
        /// used to get between them.
        /// </summary>
        public struct MoveAction
        {
            /// <summary> The destination voxel of the motion </summary>
            public Voxel Voxel { get; set; }
            /// <summary> The type of motion applied to get to the voxel </summary>
            public MoveType MoveType { get; set; }
            /// <summary> The offset between the start and destination </summary>
            public Vector3 Diff { get; set; }
            /// <summary> And object to interact with to get between the start and destination </summary>
            public GameComponent InteractObject { get; set; }
        }

        /// <summary>
        /// Applies damage to the creature over time.
        /// </summary>
        public class OngoingDamageBuff : Buff
        {
            /// <summary> The type of damage to apply </summary>
            public DamageType DamageType { get; set; }
            /// <summary> The amount of damage to take in HP per second </summary>
            public float DamagePerSecond { get; set; }

            public override void Update(DwarfTime time, Creature creature)
            {
                var dt = (float)time.ElapsedGameTime.TotalSeconds;
                creature.Damage(DamagePerSecond * dt, DamageType);
                base.Update(time, creature);
            }

            public override Buff Clone()
            {
                return new OngoingDamageBuff
                {
                    EffectTime = new Timer(EffectTime.TargetTimeSeconds, EffectTime.TriggerOnce, EffectTime.Mode),
                    Particles = Particles,
                    ParticleTimer =
                        new Timer(ParticleTimer.TargetTimeSeconds, ParticleTimer.TriggerOnce, ParticleTimer.Mode),
                    SoundOnEnd = SoundOnEnd,
                    SoundOnStart = SoundOnStart,
                    DamageType = DamageType,
                    DamagePerSecond = DamagePerSecond
                };
            }
        }

        /// <summary>
        /// Heals the creature continuously over time.
        /// </summary>
        public class OngoingHealBuff : Buff
        {
            public OngoingHealBuff()
            {
            }

            public OngoingHealBuff(float dps, float time) :
                base(time)
            {
                DamagePerSecond = dps;
            }

            /// <summary> Amount to heal the creature in HP per second </summary>
            public float DamagePerSecond { get; set; }

            public override void Update(DwarfTime time, Creature creature)
            {
                var dt = (float)time.ElapsedGameTime.TotalSeconds;
                creature.Heal(dt * DamagePerSecond);

                base.Update(time, creature);
            }

            public override Buff Clone()
            {
                return new OngoingHealBuff
                {
                    EffectTime = new Timer(EffectTime.TargetTimeSeconds, EffectTime.TriggerOnce, EffectTime.Mode),
                    Particles = Particles,
                    ParticleTimer =
                        new Timer(ParticleTimer.TargetTimeSeconds, ParticleTimer.TriggerOnce, ParticleTimer.Mode),
                    SoundOnEnd = SoundOnEnd,
                    SoundOnStart = SoundOnStart,
                    DamagePerSecond = DamagePerSecond
                };
            }
        }

        /// <summary> Increases the creature's stats for a time </summary>
        public class StatBuff : Buff
        {
            public StatBuff()
            {
                Buffs = new CreatureStats.StatNums();
            }

            public StatBuff(float time, CreatureStats.StatNums buffs) :
                base(time)
            {
                Buffs = buffs;
            }

            /// <summary> The amount to add to the creature's stats </summary>
            public CreatureStats.StatNums Buffs { get; set; }

            public override Buff Clone()
            {
                return new StatBuff
                {
                    EffectTime = new Timer(EffectTime.TargetTimeSeconds, EffectTime.TriggerOnce, EffectTime.Mode),
                    Particles = Particles,
                    ParticleTimer =
                        new Timer(ParticleTimer.TargetTimeSeconds, ParticleTimer.TriggerOnce, ParticleTimer.Mode),
                    SoundOnEnd = SoundOnEnd,
                    SoundOnStart = SoundOnStart,
                    Buffs = Buffs
                };
            }

            public override void Update(DwarfTime time, Creature creature)
            {
                base.Update(time, creature);
            }

            public override void OnApply(Creature creature)
            {
                creature.Stats.StatBuffs += Buffs;
                base.OnApply(creature);
            }

            public override void OnEnd(Creature creature)
            {
                creature.Stats.StatBuffs -= Buffs;
                base.OnEnd(creature);
            }
        }

        /// <summary> Causes the creature to have a Thought for a specified time </summary>
        public class ThoughtBuff : Buff
        {
            public ThoughtBuff()
            {
            }

            public ThoughtBuff(float time, Thought.ThoughtType type) :
                base(time)
            {
                ThoughtType = type;
            }

            /// <summary> The Thought the creature has during the buff </summary>
            public Thought.ThoughtType ThoughtType { get; set; }

            public override void OnApply(Creature creature)
            {
                creature.AI.AddThought(ThoughtType);
                base.OnApply(creature);
            }

            public override void OnEnd(Creature creature)
            {
                creature.AI.RemoveThought(ThoughtType);
                base.OnApply(creature);
            }

            public override Buff Clone()
            {
                return new ThoughtBuff
                {
                    EffectTime = new Timer(EffectTime.TargetTimeSeconds, EffectTime.TriggerOnce, EffectTime.Mode),
                    Particles = Particles,
                    ParticleTimer =
                        new Timer(ParticleTimer.TargetTimeSeconds, ParticleTimer.TriggerOnce, ParticleTimer.Mode),
                    SoundOnEnd = SoundOnEnd,
                    SoundOnStart = SoundOnStart,
                    ThoughtType = ThoughtType
                };
            }
        }
    }
}
