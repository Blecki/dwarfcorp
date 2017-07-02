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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Linq;

namespace DwarfCorp
{
    /// <summary>
    ///     Component which keeps track of a large number of other components (AI, physics, sprites, etc.)
    ///     related to creatures (such as dwarves and goblins).
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Creature : Health, IUpdateableComponent
    {
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


        public Gender Gender { get; set; }

        public bool CanReproduce = false;

        public bool IsPregnant
        {
            get { return CurrentPregnancy != null; }
        }

        public int PregnancyLengthHours = 24;
        public string Species = "";
        public string BabyType = "";
        
        public Pregnancy CurrentPregnancy = null;
        
        public Creature()
        {
            CurrentCharacterMode = CharacterMode.Idle;

            OverrideCharacterMode = false;
            Buffs = new List<Buff>();
            HasMeat = true;
            HasBones = true;
            HasCorpse = false;
            DrawLifeTimer.HasTriggered = true;
            Gender = Mating.RandomGender();
        }

        public Creature(
            ComponentManager Manager,
            CreatureStats stats,
            string allies,
            PlanService planService,
            Faction faction,
            string name) :
            base(Manager, name, stats.MaxHealth, 0.0f, stats.MaxHealth)
        {
            Gender = Mating.RandomGender();
            DrawLifeTimer.HasTriggered = true;
            HasMeat = true;
            HasBones = true;
            HasCorpse = false;
            Buffs = new List<Buff>();
            IsOnGround = true;
            Stats = stats;
            Faction = faction;
            PlanService = planService;
            Allies = allies;
            Controller = new PIDController(Stats.MaxAcceleration, Stats.StoppingForce * 2, 0.0f);
            JumpTimer = new Timer(0.2f, true);
            Status = new CreatureStatus();
            IsHeadClear = true;
            NoiseMaker = new NoiseMaker();
            OverrideCharacterMode = false;

        }

        public void LayEgg()
        {
            NoiseMaker.MakeNoise("Lay Egg", AI.Position, true, 1.0f);
            Manager.RootComponent.AddChild(new Egg(this.Name, Manager, Physics.Position, AI.PositionConstraint));
        }

        /// <summary> The creature's AI determines how it will behave. </summary>
        public CreatureAI AI { get; set; }
        /// <summary> The crature's physics determines how it moves around </summary>
        public Physics Physics { get; set; }
        /// <summary> The selection circle is drawn when the character is selected </summary>
        private CharacterSprite _characterSprite = null;
        [JsonIgnore]
        public CharacterSprite Sprite
        {
            get
            {
                if (_characterSprite == null)
                    _characterSprite = Parent.EnumerateAll().OfType<CharacterSprite>().FirstOrDefault();
                System.Diagnostics.Debug.Assert(_selectionCircle != null, "No selection circle created on creature.");
                return _characterSprite;
            }
        }


        /// <summary> The selection circle is drawn when the character is selected </summary>
        private SelectionCircle _selectionCircle = null;
        [JsonIgnore] public SelectionCircle SelectionCircle
        {
            get
            {
                if (_selectionCircle == null)
                    _selectionCircle = Parent.EnumerateAll().OfType<SelectionCircle>().FirstOrDefault();
                System.Diagnostics.Debug.Assert(_selectionCircle != null, "No selection circle created on creature.");
                return _selectionCircle;
            }
        }

        /// <summary> Finds enemies nearby and triggers when it sees them </summary>
        public EnemySensor Sensors { get; set; }
        /// <summary> Allows the creature to grab other objects </summary>
        public Grabber Hands { get; set; }
        /// <summary> If true, the creature will generate meat when it dies. </summary>
        public bool HasMeat { get; set; }
        /// <summary> If true, the creature will generate bones when it dies. </summary>
        public bool HasBones { get; set; }
        /// <summary>
        /// If true, the creature will generate a corpse.
        /// </summary>
        public bool HasCorpse { get; set; }
        /// <summary> Used to make sounds for the creature </summary>
        public NoiseMaker NoiseMaker { get; set; }
        /// <summary> The creature can hold objects in its inventory </summary>
        public Inventory Inventory { get; set; }
        public Timer EggTimer { get; set; }
        /// <summary> Reference to the graphics device. </summary>
        [JsonIgnore]
        public GraphicsDevice Graphics { get { return Manager.World.GraphicsDevice; } }

        /// <summary> List of attacks the creature can perform. </summary>
        public List<Attack> Attacks { get; set; }

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

        public bool FirstUpdate = true;

        /// <summary>
        /// Gets or sets the current character mode for animations.
        /// </summary>
        [JsonIgnore]public CharacterMode CurrentCharacterMode
        {
            get { return currentCharacterMode; }
            set
            {
                if (OverrideCharacterMode) return;

                currentCharacterMode = value;
                if (Parent != null && Sprite != null)
                {
                    if (Sprite.HasAnimation(currentCharacterMode, OrientedAnimation.Orientation.Forward))
                    {
                        Sprite.SetCurrentAnimation(value.ToString());
                    }
                    else
                    {
                        Sprite.SetCurrentAnimation(currentCharacterMode != CharacterMode.Walking
                            ? CharacterMode.Walking.ToString()
                            : CharacterMode.Idle.ToString());
                    }
                }
            }
        }

        public Timer DrawLifeTimer = new Timer(0.25f, true);

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
        public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (FirstUpdate)
            {
                FirstUpdate = false;
                Faction.Minions.Add(AI);
            }

            if (!IsActive) return;
            DrawLifeTimer.Update(gameTime);

            if (!DrawLifeTimer.HasTriggered)
            {
                float val = Hp / MaxHealth;
                Color color = val < 0.75f ? (val < 0.5f ? Color.Red : Color.Orange) : Color.LightGreen;
                Drawer2D.DrawLoadBar(Manager.World.Camera, AI.Position - Vector3.Up * 0.5f, color, Color.Black, 32, 2, Hp / MaxHealth);
            }
            CheckNeighborhood(chunks, (float)gameTime.ElapsedGameTime.TotalSeconds);
            UpdateAnimation(gameTime, chunks, camera);
            Status.Update(this, gameTime, chunks, camera);
            JumpTimer.Update(gameTime);
            HandleBuffs(gameTime);

            if (Stats.LaysEggs)
            {
                if (EggTimer == null)
                {
                    EggTimer = new Timer(1200.0f, false);
                }
                EggTimer.Update(gameTime);

                if (EggTimer.HasTriggered)
                {
                    LayEgg();
                    EggTimer = new Timer(1200.0f + MathFunctions.Rand(-30.0f, 30.0f), false);
                }
            }

            if (IsPregnant && World.Time.CurrentDate > CurrentPregnancy.EndDate)
            {
                var baby = EntityFactory.CreateEntity<GameComponent>(BabyType, Physics.Position);
                if (AI.PositionConstraint.HasValue)
                    baby.GetComponent<CreatureAI>().PositionConstraint = AI.PositionConstraint.Value;
                CurrentPregnancy = null;
            }

            if (MathFunctions.RandEvent(0.0001f))
            {
                NoiseMaker.MakeNoise("Chirp", AI.Position, true, 0.25f);
            }
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

                if (MathFunctions.RandEvent(0.01f))
                {
                    NoiseMaker.MakeNoise("Sleep", AI.Position, true);
                }
            }
            else if (currentCharacterMode == CharacterMode.Sleeping)
            {
                CurrentCharacterMode = CharacterMode.Idle;
            }

            if (!Status.Energy.IsDissatisfied())
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
            NoiseMaker.MakeNoise("Die", Physics.Position, true);

            if (AI.Status.Money > 0)
            {
                EntityFactory.CreateEntity<CoinPile>("Coins Resource", AI.Position, Blackboard.Create("Money", AI.Status.Money));
            }

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

            if (HasCorpse)
            {
                ResourceLibrary.ResourceType type = AI.Stats.FullName + "'s " + "Corpse";

                if (!ResourceLibrary.Resources.ContainsKey(type))
                {
                    ResourceLibrary.Add(new Resource(ResourceLibrary.Resources["Corpse"])
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

                    var deathParticleTrigger = Parent.EnumerateAll().OfType<ParticleTrigger>().Where(p => p.Name == "Death Gibs").FirstOrDefault();

                    if (deathParticleTrigger != null)
                        Manager.World.ParticleManager.Trigger(deathParticleTrigger.EmitterName, AI.Position, Color.White, 2);
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

            Physics.Velocity = MathFunctions.ClampXZ(Physics.Velocity, Stats.MaxSpeed);
        }

        /// <summary>
        /// Basic Act that causes the creature to wait for the specified time.
        /// Also draws a loading bar above the creature's head when relevant.
        /// </summary>
        public IEnumerable<Act.Status> HitAndWait(float f, bool loadBar, Func<Vector3> pos, string playSound = "", Func<bool> continueHitting = null)
        {
            var waitTimer = new Timer(f, true);

            CurrentCharacterMode = CharacterMode.Attacking;
            Sprite.ResetAnimations(CharacterMode.Attacking);
            Sprite.PlayAnimations(CharacterMode.Attacking);

            CurrentCharacterMode = CharacterMode.Attacking;

            while (!waitTimer.HasTriggered)
            {
                waitTimer.Update(DwarfTime.LastTime);

                if (continueHitting != null && !continueHitting())
                {
                    yield break;
                }

                if (loadBar)
                {
                    Drawer2D.DrawLoadBar(Manager.World.Camera, AI.Position + Vector3.Up, Color.LightGreen, Color.Black, 64, 4,
                        waitTimer.CurrentTimeSeconds / waitTimer.TargetTimeSeconds);
                }

                Attacks[0].PerformNoDamage(this, DwarfTime.LastTime, pos());
                Physics.Velocity = Vector3.Zero;
                Sprite.ReloopAnimations(CharacterMode.Attacking);

                if (!String.IsNullOrEmpty(playSound))
                {
                    NoiseMaker.MakeNoise(playSound, AI.Position, true);
                }

                yield return Act.Status.Running;
            }
            Sprite.PauseAnimations(CharacterMode.Attacking);
            CurrentCharacterMode = CharacterMode.Idle;
            yield return Act.Status.Success;
            yield break;
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
                AI.Position + Vector3.Up + MathFunctions.RandVector3Cube() * 0.5f, 0.5f, color);

            if (damage > 0)
            {
                NoiseMaker.MakeNoise("Hurt", AI.Position);
                Sprite.Blink(0.5f);
                AI.AddThought(Thought.ThoughtType.TookDamage);

                var deathParticleTrigger = Parent.EnumerateAll().OfType<ParticleTrigger>().Where(p => p.Name == "Death Gibs").FirstOrDefault();

                if (deathParticleTrigger != null)
                    Manager.World.ParticleManager.Trigger(deathParticleTrigger.EmitterName, AI.Position, Color.White, 2);
                DrawLifeTimer.Reset();
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

        protected void CreateSprite(EmployeeClass employeeClass, ComponentManager manager)
        {
            var sprite = Physics.AddChild(new CharacterSprite(manager.World.GraphicsDevice, manager, "Sprite", Matrix.CreateTranslation(new Vector3(0, 0.15f, 0)))) as CharacterSprite;
            foreach (Animation animation in employeeClass.Animations)
            {
                sprite.AddAnimation(animation.Clone());
            }
            sprite.SpriteSheet = Sprite.Animations.First().Value.SpriteSheet;
            sprite.CurrentAnimation = Sprite.Animations.First().Value;
            sprite.CurrentAnimation.NextFrame();
            sprite.SetFlag(Flag.ShouldSerialize, false);
        }

        protected void CreateSprite(string animations, ComponentManager manager)
        {
            // Create the sprite component for the bird.
            var sprite = Physics.AddChild(new CharacterSprite
                                  (manager.World.GraphicsDevice,
                                  manager,
                                  "Sprite",
                                  Matrix.CreateTranslation(0, 0.5f, 0)
                                  )) as CharacterSprite;

            CompositeAnimation.Descriptor descriptor =
                FileUtils.LoadJsonFromString<CompositeAnimation.Descriptor>(
                    ContentPaths.GetFileAsString(animations));

            List<CompositeAnimation> animations_list = descriptor.GenerateAnimations(Name);

            foreach (CompositeAnimation animation in animations_list)
            {
                sprite.AddAnimation(animation);
            }
            sprite.SetFlag(Flag.ShouldSerialize, false);
        }

    }
}
