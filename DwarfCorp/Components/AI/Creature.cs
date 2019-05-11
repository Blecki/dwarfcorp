using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Linq;

namespace DwarfCorp
{
    public partial class Creature : Health
    {
        private DateTime LastHungerDamageTime = DateTime.Now;
        private CharacterMode _currentCharacterMode = CharacterMode.Idle;
        private bool _lastIsCloaked = false;
        public bool IsCloaked = false;
        [JsonIgnore] public CreatureAI AI => _get(ref _ai);
        private CreatureAI _ai = null;
        public Physics Physics { get; set; }
        private CharacterSprite _characterSprite = null;
        [JsonIgnore] public CharacterSprite Sprite => _get(ref _characterSprite);
        [JsonIgnore] public EnemySensor Sensor => _get(ref _sensor);
        private EnemySensor _sensor = null;
        [JsonIgnore] public NoiseMaker NoiseMaker { get; set; }
        [JsonIgnore] public Inventory Inventory => _get(ref _inventory);
        private Inventory _inventory = null;
        public Timer MigrationTimer { get; set; }
        [JsonIgnore] public List<Attack> Attacks;
        public Faction Faction { get; set; }
        public PIDController Controller { get; set; }
        public CreatureStats Stats { get; set; }
        public Timer DrawLifeTimer = new Timer(0.25f, true, Timer.TimerMode.Real);
        public bool IsOnGround { get; set; }
        public bool IsHeadClear { get; set; }
        /// <summary>
        /// If true, the character mode will not be updated automatically by the creature's movement.
        /// This is used to make the character animate in a certain way without interference.
        /// </summary> 
        public bool OverrideCharacterMode { get; set; }
        [JsonProperty] private bool FirstUpdate = true;


        public Creature()
        {
        }

        public Creature(
            ComponentManager Manager,
            CreatureStats stats,
            Faction faction,
            string name) :
            base(Manager, name, stats.MaxHealth, 0.0f, stats.MaxHealth)
        {
            Stats = stats;
            Stats.Gender = Mating.RandomGender();
            DrawLifeTimer.HasTriggered = true;
            IsOnGround = true;
            Faction = faction;
            Controller = new PIDController(Stats.MaxAcceleration, Stats.StoppingForce * 2, 0.0f);
            IsHeadClear = true;
            NoiseMaker = new NoiseMaker();
            NoiseMaker.BasePitch = stats.VoicePitch;
            OverrideCharacterMode = false;

            Attacks = stats.CurrentClass.Weapons.Select(a => new Attack(a)).ToList();
            for (var i = 0; i <= stats.LevelIndex && i < stats.CurrentClass.Levels.Count; ++i)
                Attacks.AddRange(stats.CurrentClass.Levels[i].ExtraWeapons.Select(w => new Attack(w)));
        }

        private T _get<T>(ref T cached) where T : GameComponent
        {
            if (cached == null)
                cached = Parent.EnumerateAll().OfType<T>().FirstOrDefault();
            //System.Diagnostics.Debug.Assert(cached != null, string.Format("No {0} created on creature.", typeof(T).Name));
            return cached;
        }



        /// <summary>
        /// Gets or sets the current character mode for animations.
        /// </summary>
        [JsonIgnore] public CharacterMode CurrentCharacterMode
        {
            get { return _currentCharacterMode; }
            set
            {
                if (OverrideCharacterMode) return;

                _currentCharacterMode = value;
                if (Parent != null && Sprite != null)
                {
                    if (Sprite.HasAnimation(_currentCharacterMode, OrientedAnimatedSprite.Orientation.Forward))
                        Sprite.SetCurrentAnimation(value.ToString());
                    else
                        Sprite.SetCurrentAnimation(_currentCharacterMode != CharacterMode.Walking ? CharacterMode.Walking.ToString() : CharacterMode.Idle.ToString());
                }
            }
        }


                /// <summary> Updates the creature </summary>
        override public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

            if (FirstUpdate)
            {
                FirstUpdate = false;
                Faction.Minions.Add(AI);
                Physics.AllowPhysicsSleep = false;
                World.AddToSpeciesTracking(Stats.CurrentClass);
            }

            if (AI == null)
            {
                Console.Out.WriteLine("Error: creature {0} {1} has no AI. Deleting it.", Name, GlobalID);
                GetRoot().Delete();
                return;
            }

            if (!Active) return;

            UpdateCloak();
            UpdateHealthBar(gameTime);
            CheckNeighborhood(chunks, (float)gameTime.ElapsedGameTime.TotalSeconds);
            UpdateAnimation(gameTime, chunks, camera);

            #region Update Status Stat Effects

            var statAdjustments = Stats.FindAdjustment("status");
            Stats.RemoveStatAdjustment("status");
            if (statAdjustments == null)
                statAdjustments = new StatAdjustment() { Name = "status" };
            statAdjustments.Reset();

            if (!Stats.IsAsleep)
                Stats.Hunger.CurrentValue -= (float)gameTime.ElapsedGameTime.TotalSeconds * Stats.HungerGrowth;
            else
                Hp += (float)gameTime.ElapsedGameTime.TotalSeconds * 0.1f;

            Stats.Health.CurrentValue = (Hp - MinHealth) / (MaxHealth - MinHealth); // Todo: MinHealth always 0?

            // Todo: Why is energy just tied to time of day? Lets make them actually recover at night and spend it during the day.
            if (Stats.Species.CanSleep)
                Stats.Energy.CurrentValue = (float)(100 * Math.Sin(Manager.World.Time.GetTotalHours() * Math.PI / 24.0f));
            else
                Stats.Energy.CurrentValue = 100.0f;

            if (Stats.Energy.IsDissatisfied())
            {
                DrawIndicator(IndicatorManager.StandardIndicators.Sleepy);
                statAdjustments.Strength += -2.0f;
                statAdjustments.Intelligence += -2.0f;
                statAdjustments.Dexterity += -2.0f;
            }

            if (Stats.CanEat && Stats.Hunger.IsDissatisfied() && !Stats.IsAsleep)
            {
                DrawIndicator(IndicatorManager.StandardIndicators.Hungry);

                statAdjustments.Intelligence += -1.0f;
                statAdjustments.Dexterity += -1.0f;

                if (Stats.Hunger.CurrentValue <= 1e-12 && (DateTime.Now - LastHungerDamageTime).TotalSeconds > Stats.HungerDamageRate)
                {
                    Damage(1.0f / (Stats.HungerResistance) * Stats.HungerDamageRate);
                    LastHungerDamageTime = DateTime.Now;
                }
            }

            if (!statAdjustments.IsAllZero)
                Stats.AddStatAdjustment(statAdjustments);

            #endregion
            
            Stats.HandleBuffs(this, gameTime);
            UpdateMigration(gameTime);
            UpdateEggs(gameTime);
            UpdatePregnancy();
            MakeNoises();
        }

        private void UpdateHealthBar(DwarfTime gameTime)
        {
            DrawLifeTimer.Update(gameTime);

            if (!DrawLifeTimer.HasTriggered)
            {
                float val = Hp / MaxHealth;
                Color color = val < 0.75f ? (val < 0.5f ? GameSettings.Default.Colors.GetColor("Low Health", Color.Red) : GameSettings.Default.Colors.GetColor("Medium Health", Color.Orange)) : GameSettings.Default.Colors.GetColor("High Health", Color.LightGreen);
                Drawer2D.DrawLoadBar(Manager.World.Camera, AI.Position - Vector3.Up * 0.5f, color, Color.Black, 32, 2, Hp / MaxHealth);
            }
        }

        private void MakeNoises()
        {
            if (AI == null)
                return;

            if (MathFunctions.RandEvent(0.0001f))
            {
                NoiseMaker.MakeNoise("Chirp", AI.Position, true, 0.25f);
            }
        }

        private void UpdateMigration(DwarfTime gameTime)
        {
            if (Stats.Species.IsMigratory && !AI.IsPositionConstrained())
            {
                if (MigrationTimer == null)
                    MigrationTimer = new Timer(3600f + MathFunctions.Rand(-120, 120), false);

                MigrationTimer.Update(gameTime);
                if (MigrationTimer.HasTriggered)
                    AI.LeaveWorld();
            }
        }

        private void UpdateCloak()
        {
            if (IsCloaked != _lastIsCloaked)
            {
                foreach (var tinter in Physics.EnumerateAll().OfType<Tinter>())
                {
                    tinter.Stipple = IsCloaked;
                }
                _lastIsCloaked = IsCloaked;
            }

            if (IsCloaked)
            {
                Sensor.Active = false;
            }
            else
            {
                Sensor.Active = true;
            }
        }

        /// <summary> 
        /// Checks the voxels around the creature and reacts to changes in its immediate environment.
        /// For example this function determines when the creature is standing on solid ground.
        /// </summary>
        public void CheckNeighborhood(ChunkManager chunks, float dt)
        {
            var below = new VoxelHandle(chunks, GlobalVoxelCoordinate.FromVector3(Physics.GlobalTransform.Translation - Vector3.UnitY * 0.8f));
            var above = new VoxelHandle(chunks, GlobalVoxelCoordinate.FromVector3(Physics.GlobalTransform.Translation + Vector3.UnitY * 0.8f));

            if (above.IsValid)
                IsHeadClear = above.IsEmpty;

            if (below.IsValid && Physics.IsInLiquid)
                IsOnGround = false;
            else if (below.IsValid)
                IsOnGround = !below.IsEmpty;
            else
                IsOnGround = false;

            if (!IsOnGround)
            {
                if (CurrentCharacterMode != CharacterMode.Flying)
                {
                    if (Physics.Velocity.Y > 0.05)
                        CurrentCharacterMode = CharacterMode.Jumping;
                    else if (Physics.Velocity.Y < -0.05)
                        CurrentCharacterMode = CharacterMode.Falling;
                }

                if (Physics.IsInLiquid)
                    CurrentCharacterMode = CharacterMode.Swimming;
            }

            if (CurrentCharacterMode == CharacterMode.Falling && IsOnGround)
                CurrentCharacterMode = CharacterMode.Idle;

            if (Stats.IsAsleep)
            {
                CurrentCharacterMode = CharacterMode.Sleeping;

                if (MathFunctions.RandEvent(0.01f))
                    NoiseMaker.MakeNoise("Sleep", AI.Position, true);
            }
            else if (CurrentCharacterMode == CharacterMode.Sleeping)
                CurrentCharacterMode = CharacterMode.Idle;

            if (World.Time.IsDay() && Stats.IsAsleep && !Stats.Energy.IsDissatisfied() && !Stats.Health.IsCritical())
                Stats.IsAsleep = false;
        }


        public override void Delete()
        {
            World.RemoveFromSpeciesTracking(Stats.CurrentClass);

            base.Delete();
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
                case Message.MessageType.OnHurt:
                    NoiseMaker.MakeNoise("Hurt", AI.Position);
                    Sprite.Blink(0.5f);
                    AddThought(Thought.ThoughtType.TookDamage);

                    var deathParticleTriggers = Parent.EnumerateAll().OfType<ParticleTrigger>().Where(p => p.Name == "Death Gibs");

                    foreach (var trigger in deathParticleTriggers)
                        Manager.World.ParticleManager.Trigger(trigger.EmitterName, AI.Position, Color.White, 2);
                    break;
            }


            base.ReceiveMessageRecursive(messageToReceive);
        }

        public void AddThought(Thought.ThoughtType ThoughtType)
        {
            Physics.GetComponent<DwarfThoughts>()?.AddThought(ThoughtType);
        }

        public void AddThought(Thought thought, bool allowDuplicates)
        {
            Physics.GetComponent<DwarfThoughts>()?.AddThought(thought, allowDuplicates);
        }

        /// <summary>
        /// Updates the creature's animation based on its current state.
        /// </summary>
        public void UpdateAnimation(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (CurrentCharacterMode == Stats.CurrentClass.AttackMode)
                return;

            Physics.Velocity = MathFunctions.ClampXZ(Physics.Velocity, Stats.MaxSpeed);
        }

        /// <summary>
        /// Basic Act that causes the creature to wait for the specified time.
        /// Also draws a loading bar above the creature's head when relevant.
        /// </summary>
        public IEnumerable<Act.Status> HitAndWait(float f, bool loadBar, Func<Vector3> pos)
        {
            var waitTimer = new Timer(f, true);

            CurrentCharacterMode = Stats.CurrentClass.AttackMode;
            Sprite.ResetAnimations(CurrentCharacterMode);
            Sprite.PlayAnimations(CurrentCharacterMode);

            while (!waitTimer.HasTriggered)
            {
                waitTimer.Update(DwarfTime.LastTime);

                if (loadBar)
                    Drawer2D.DrawLoadBar(Manager.World.Camera, AI.Position + Vector3.Up, Color.LightGreen, Color.Black, 64, 4, waitTimer.CurrentTimeSeconds / waitTimer.TargetTimeSeconds);

                Physics.Active = false;

                Attacks[0].PerformNoDamage(this, DwarfTime.LastTime, pos());
                Physics.Velocity = Vector3.Zero;
                Sprite.ReloopAnimations(Stats.CurrentClass.AttackMode);

                yield return Act.Status.Running;
            }

            Sprite.PauseAnimations(Stats.CurrentClass.AttackMode);
            CurrentCharacterMode = CharacterMode.Idle;
            Physics.Active = true;

            yield return Act.Status.Success;
            yield break;
        }

        public IEnumerable<Act.Status> HitAndWait(bool loadBar, Func<float> maxProgress, 
            Func<float> progress, Action incrementProgress, 
            Func<Vector3> pos, string playSound = "", Func<bool> continueHitting = null, bool maintainPos = true)
        {
            Vector3 currentPos = Physics.LocalTransform.Translation;
            CurrentCharacterMode = Stats.CurrentClass.AttackMode;
            Sprite.ResetAnimations(CurrentCharacterMode);
            Sprite.PlayAnimations(CurrentCharacterMode);
            var p_current = pos();
            Timer incrementTimer = new Timer(1.0f, false);
            while (progress() < maxProgress())
            {
                if (continueHitting != null && !continueHitting())
                {
                    yield break;
                }

                if (loadBar)
                {
                    Drawer2D.DrawLoadBar(Manager.World.Camera, AI.Position + Vector3.Up, Color.LightGreen, Color.Black, 64, 4,
                        progress() / maxProgress());
                }
                Physics.Active = false;
                Physics.Face(p_current);
                if(Attacks[0].PerformNoDamage(this, DwarfTime.LastTime, p_current))
                {
                    p_current = pos();
                }
                Physics.Velocity = Vector3.Zero;

                if (!String.IsNullOrEmpty(playSound))
                {
                    NoiseMaker.MakeNoise(playSound, AI.Position, true);
                }

                incrementTimer.Update(DwarfTime.LastTime);
                if (incrementTimer.HasTriggered)
                {
                    Sprite.ReloopAnimations(Stats.CurrentClass.AttackMode);
                    incrementProgress();
                }

                if (maintainPos)
                {
                    var matrix = Physics.LocalTransform;
                    matrix.Translation = currentPos;
                    Physics.LocalTransform = matrix;
                }

                yield return Act.Status.Running;
            }
            Sprite.PauseAnimations(Stats.CurrentClass.AttackMode);
            CurrentCharacterMode = CharacterMode.Idle;
            Physics.Active = true;
            yield return Act.Status.Success;
            yield break;
        }

       

        /// <summary>
        /// Called whenever the creature takes damage.
        /// </summary>
        public override float Damage(float amount, DamageType type = DamageType.Normal)
        {
            IsCloaked = false;
            float damage = base.Damage(amount, type);

            string prefix = damage > 0 ? "-" : "+";
            Color color = damage > 0 ? GameSettings.Default.Colors.GetColor("Negative", Color.Red) : GameSettings.Default.Colors.GetColor("Positive", Color.Green);

            if (AI != null)
            {
                IndicatorManager.DrawIndicator(prefix + (int)amount + " HP",
                AI.Position + Vector3.Up + MathFunctions.RandVector3Cube() * 0.5f, 0.5f, color);
                NoiseMaker.MakeNoise("Hurt", AI.Position);
                Sprite.Blink(0.5f);
                AddThought(Thought.ThoughtType.TookDamage);

                var deathParticleTriggers = Parent.EnumerateAll().OfType<ParticleTrigger>().Where(p => p.Name == "Death Gibs");

                foreach (var trigger in deathParticleTriggers)
                    Manager.World.ParticleManager.Trigger(trigger.EmitterName, AI.Position, Color.White, 2);
                DrawLifeTimer.Reset();
            }

            return damage;
        }

        public void GatherImmediately(GameComponent item, Inventory.RestockType restockType = Inventory.RestockType.None)
        {
            if (item is CoinPile)
            {
                var money = (item as CoinPile).Money;
                AI.AddMoney(money);
                item.Die();

                AI.GatherManager.StockMoneyOrders.Add(new GatherManager.StockMoneyOrder()
                {
                    Destination = null,
                    Money = money
                });
            }
            else
                Inventory.Pickup(item, restockType);
        }

        public void Gather(GameComponent item)
        {

            var task = new GatherItemTask(item) { Priority = Task.PriorityType.High };
            if (AI.Faction == World.PlayerFaction)
                World.Master.TaskManager.AddTask(task);
            else
                AI.AssignTask(task);
        }

        protected CharacterSprite CreateSprite(string animations, ComponentManager manager, float VerticalOffset)
        {
            var sprite = new CharacterSprite(manager, "Sprite", Matrix.CreateTranslation(0, VerticalOffset, 0));

            Physics.AddChild(sprite);
            sprite.SetAnimations(Library.LoadCompositeAnimationSet(animations, Name));
            sprite.SetFlag(Flag.ShouldSerialize, false);

            return sprite;
        }

    }
}
