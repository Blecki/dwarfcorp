using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class CreatureDef
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Race { get; set; }
        public Vector3 Size { get; set; }
        public bool HasShadow { get; set; }
        public bool IsFlammable { get; set; }
        public string BloodParticle { get; set; }
        public string DeathSound { get; set; }
        public List<string> HurtSounds { get; set; }
        public string ChewSound { get; set; }
        public string JumpSound { get; set; }
        public bool TriggersMourning { get; set; }
        public float ShadowScale { get; set; }
        public float Mass { get; set; }
        public int InventorySize { get; set; }
        public Vector3 SpriteOffset { get; set; }
        public NamedImageFrame MinimapIcon { get; set; }
        public Vector3 SenseRange { get; set; }
        public string Classes { get; set; }
        public bool CanSleep { get; set; }
        public bool CanEat { get; set; }
        public List<string> Tags { get; set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
           EmployeeClass.AddClasses(Classes);
        }
    }

    /// <summary>
    /// Component which keeps track of a large number of other components (AI, physics, sprites, etc.) 
    /// related to creatures (such as dwarves and goblins). 
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Creature : Health
    {
        public CreatureAI AI { get; set; }
        public Physics Physics { get; set; }
        public CharacterSprite Sprite { get; set; }
        public SelectionCircle SelectionCircle { get; set; }
        public EnemySensor Sensors { get; set; }
        public Flammable Flames { get; set; }
        public ParticleTrigger DeathParticleTrigger { get; set; }
        public Grabber Hands { get; set; }
        public Shadow Shadow { get; set; }

        public NoiseMaker NoiseMaker { get; set; }

        public Inventory Inventory { get; set; }

        [JsonIgnore]
        public GraphicsDevice Graphics { get; set; }

        [JsonIgnore]
        public ChunkManager Chunks { get; set; }

        public List<Attack> Attacks { get; set; }
            
        [JsonIgnore]
        public ContentManager Content { get; set; }

        public Faction Faction { get; set; }

        public PlanService PlanService { get; set; }

        public string Allies { get; set; }

        public PIDController Controller { get; set; }
        public CreatureStats Stats { get; set; }
        public CreatureStatus Status { get; set; }

        public Timer JumpTimer { get; set; }

        public bool OverrideCharacterMode { get; set; }

        protected CharacterMode currentCharacterMode = CharacterMode.Idle;

        public CharacterMode CurrentCharacterMode
        {
            get { return currentCharacterMode; }
            set
            {
                if (OverrideCharacterMode) return;
                
                currentCharacterMode = value;
                if(Sprite != null)
                {
                    Sprite.SetCurrentAnimation(value.ToString());
                }
            }
        }

        public bool IsAsleep
        {
            get { return Status.IsAsleep; }
        }

        public bool IsOnGround { get; set; }
        public bool IsHeadClear { get; set; }


        private float IndicatorRateLimit = 2.0f;
        private DateTime LastIndicatorTime = DateTime.Now;

        public struct MoveAction
        {
            public Voxel Voxel { get; set; }
            public MoveType MoveType { get; set; }
            public Vector3 Diff { get; set; }
        }

        public List<Buff> Buffs { get; set; } 

        public enum MoveType
        {
            Walk,
            Jump,
            Climb
        }

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

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Graphics = PlayState.ChunkManager.Graphics;
            Content = PlayState.ChunkManager.Content;
            Chunks = PlayState.ChunkManager;
        }

        public Creature()
        {
            OverrideCharacterMode = false;
            Buffs = new List<Buff>();
        }

        public Creature(Vector3 pos, CreatureDef def, string creatureClass, int creatureLevel, string faction) :
            this(new CreatureStats(EmployeeClass.Classes[creatureClass], creatureLevel), 
                faction, 
                PlayState.PlanService, 
                PlayState.ComponentManager.Factions.Factions[faction], 
                new Physics(def.Name, PlayState.ComponentManager.RootComponent, Matrix.CreateTranslation(pos), def.Size, new Vector3(0, -def.Size.Y * 0.5f, 0), def.Mass, 1.0f, 0.999f, 0.999f, Vector3.UnitY * -10, Physics.OrientMode.RotateY),
                PlayState.ChunkManager,
                GameState.Game.GraphicsDevice, 
                GameState.Game.Content,
                def.Name)
        {
            EmployeeClass employeeClass = EmployeeClass.Classes[creatureClass];
            Physics.Orientation = Physics.OrientMode.RotateY;
            Sprite = new CharacterSprite(Graphics, Manager, "Sprite", Physics, Matrix.CreateTranslation(def.SpriteOffset));

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
                Matrix shadowTransform = Matrix.CreateRotationX((float) Math.PI*0.5f);
                shadowTransform.Translation = new Vector3(0.0f, -0.5f, 0.0f);

                Shadow = new Shadow(Manager, "Shadow", Physics, shadowTransform,
                    new SpriteSheet(ContentPaths.Effects.shadowcircle))
                {
                    GlobalScale = def.ShadowScale
                };
                List<Point> shP = new List<Point>
                {
                    new Point(0, 0)
                };
                Animation shadowAnimation = new Animation(Graphics, new SpriteSheet(ContentPaths.Effects.shadowcircle),
                    "sh", 32, 32, shP, false, Color.Black, 1, 0.7f, 0.7f, false);
                Shadow.AddAnimation(shadowAnimation);
                shadowAnimation.Play();
                Shadow.SetCurrentAnimation("sh");
            }
            Physics.Tags.AddRange(def.Tags);

            DeathParticleTrigger = new ParticleTrigger(def.BloodParticle, Manager, "Death Gibs", Physics, Matrix.Identity, Vector3.One, Vector3.Zero)
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
            NoiseMaker.Noises["Chew"] = new List<string>() {def.ChewSound};
            NoiseMaker.Noises["Jump"] = new List<string>() {def.JumpSound};

            MinimapIcon minimapIcon = new MinimapIcon(Physics, def.MinimapIcon);
            Stats.FullName = TextGenerator.GenerateRandom(PlayState.ComponentManager.Factions.Races[def.Race].NameTemplates);
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

        public void AddBuff(Buff buff)
        {
            buff.OnApply(this);
            Buffs.Add(buff);
        }

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

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            CheckNeighborhood(chunks, (float)gameTime.ElapsedGameTime.TotalSeconds);
            UpdateAnimation(gameTime, chunks, camera);
            Status.Update(this, gameTime, chunks, camera);
            JumpTimer.Update(gameTime);
            HandleBuffs(gameTime);

            base.Update(gameTime, chunks, camera);
        }

        public void CheckNeighborhood(ChunkManager chunks, float dt)
        {
            Voxel voxelBelow = new Voxel();
            bool belowExists = chunks.ChunkData.GetVoxel(Physics.GlobalTransform.Translation - Vector3.UnitY * 0.8f, ref voxelBelow);
            Voxel voxelAbove = new Voxel();
            bool aboveExists = chunks.ChunkData.GetVoxel(Physics.GlobalTransform.Translation + Vector3.UnitY, ref voxelAbove);

            if (aboveExists)
            {
                IsHeadClear = voxelAbove.IsEmpty;
            }

            if (!Physics.IsInLiquid && CurrentCharacterMode == CharacterMode.Swimming)
            {
                CurrentCharacterMode = CharacterMode.Idle;
            }

            if(belowExists && Physics.IsInLiquid)
            {
                IsOnGround = false;
                CurrentCharacterMode = CharacterMode.Swimming;
            }
            else if(belowExists)
            {
                if(!voxelBelow.IsEmpty)
                {
                    IsOnGround = true;

                    if(CurrentCharacterMode != CharacterMode.Attacking) CurrentCharacterMode = CharacterMode.Idle;
                }
                else
                {
                    IsOnGround = false;
                    if(Physics.Velocity.Y > 0.05)
                    {
                        if (CurrentCharacterMode == CharacterMode.Walking || CurrentCharacterMode == CharacterMode.Idle || CurrentCharacterMode == CharacterMode.Falling)
                        {
                            CurrentCharacterMode = CharacterMode.Jumping;
                        }
                    }
                    else if(Physics.Velocity.Y < -0.05)
                    {
                        if (CurrentCharacterMode == CharacterMode.Walking || CurrentCharacterMode == CharacterMode.Idle || CurrentCharacterMode == CharacterMode.Jumping)
                        {
                            CurrentCharacterMode = CharacterMode.Falling;
                        }
                    }
                    else
                    {
                        if(CurrentCharacterMode == CharacterMode.Walking || CurrentCharacterMode == CharacterMode.Idle)
                        {
                            currentCharacterMode = CharacterMode.Idle;
                        }
                    }
                }
            }
            else
            {
                if(IsOnGround)
                {
                    IsOnGround = false;
                    if(CurrentCharacterMode != CharacterMode.Flying)
                    {
                        CurrentCharacterMode = Physics.Velocity.Y > 0 ? CharacterMode.Jumping : CharacterMode.Falling;
                    }
                }
            }

            if(Status.IsAsleep)
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


        public void DrawIndicator(ImageFrame image)
        {
            if (!((DateTime.Now - LastIndicatorTime).TotalSeconds >= IndicatorRateLimit))
            {
                return;
            }

            IndicatorManager.DrawIndicator(image, AI.Position + new Vector3(0, 0.5f, 0), 1, 1.5f, new Vector2(image.SourceRect.Width / 2.0f, -image.SourceRect.Height / 2.0f));
            LastIndicatorTime = DateTime.Now;
        }


        public void DrawIndicator(IndicatorManager.StandardIndicators indicator)
        {
            if(!((DateTime.Now - LastIndicatorTime).TotalSeconds >= IndicatorRateLimit))
            {
                return;
            }

            IndicatorManager.DrawIndicator(indicator, AI.Position + new Vector3(0, 0.5f, 0), 1, 2, new Vector2(16, -16));
            LastIndicatorTime = DateTime.Now;
        }

        

        public override void ReceiveMessageRecursive(Message messageToReceive)
        {
            switch(messageToReceive.Type)
            {
                case Message.MessageType.OnChunkModified:
                    break;
                
                case Message.MessageType.OnHurt:
                    NoiseMaker.MakeNoise("Hurt", AI.Position);
                    this.Sprite.Blink(0.5f);
                    AI.AddThought(Thought.ThoughtType.TookDamage);
                    PlayState.ParticleManager.Trigger(DeathParticleTrigger.EmitterName, AI.Position, Color.White, 2);
                    break;
            }

            
            base.ReceiveMessageRecursive(messageToReceive);
        }

        public void UpdateAnimation(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
           
            float veloNorm = Physics.Velocity.Length();
            if(veloNorm > Stats.MaxSpeed)
            {
                Physics.Velocity = (Physics.Velocity / veloNorm) * Stats.MaxSpeed;
                if(IsOnGround && CurrentCharacterMode == CharacterMode.Idle)
                {
                    CurrentCharacterMode = CharacterMode.Walking;
                }
            }

            if (veloNorm > 0.25f)
            {
                if (IsOnGround && CurrentCharacterMode == CharacterMode.Idle)
                {
                    CurrentCharacterMode = CharacterMode.Walking;
                }
            }

            if(CurrentCharacterMode == CharacterMode.Attacking)
            {
                return;
            }

            if(!IsOnGround)
            {
                return;
            }

            if(veloNorm < 0.25f || Physics.IsSleeping)
            {
                if(CurrentCharacterMode == CharacterMode.Walking)
                {
                    CurrentCharacterMode = CharacterMode.Idle;
                }
            }
            else
            {
                if (CurrentCharacterMode == CharacterMode.Idle)
                {
                    CurrentCharacterMode = CharacterMode.Walking;
                    Animation walk = Sprite.GetAnimation(CharacterMode.Walking, Sprite.CurrentOrientation);
                    if (walk != null)
                    {
                        walk.SpeedMultiplier = MathFunctions.Clamp(veloNorm/Stats.MaxSpeed*5.0f, 0.5f, 3.0f);
                    }
                }
            }
        }

        public IEnumerable<Act.Status> HitAndWait(float f, bool loadBar)
        {
            Timer waitTimer = new Timer(f, true);

            CurrentCharacterMode = CharacterMode.Attacking;
            
            while(!waitTimer.HasTriggered)
            {
                waitTimer.Update(DwarfTime.LastTime);

                if(loadBar)
                {
                    Drawer2D.DrawLoadBar(AI.Position + Vector3.Up, Color.White, Color.Black, 100, 16, waitTimer.CurrentTimeSeconds / waitTimer.TargetTimeSeconds);
                }

                Attacks[0].PerformNoDamage(DwarfTime.LastTime, AI.Position);

                yield return Act.Status.Running;
            }

            CurrentCharacterMode = CharacterMode.Idle;
            yield return Act.Status.Success;
        }

        public override float Damage(float amount, DamageType type = DamageType.Normal)
        {
            float damage = base.Damage(amount, type);

            string prefix = damage > 0 ? "-" : "+";
            Color color = damage > 0 ? Color.Red : Color.Green;

            IndicatorManager.DrawIndicator(prefix + (int)amount + " HP", AI.Position + Vector3.Up + MathFunctions.RandVector3Cube() * 0.5f, 0.5f, color, Indicator.IndicatorMode.Indicator3D);

            if (damage > 0)
            {
                NoiseMaker.MakeNoise("Hurt", AI.Position);
                this.Sprite.Blink(0.5f);
                AI.AddThought(Thought.ThoughtType.TookDamage);
                PlayState.ParticleManager.Trigger(DeathParticleTrigger.EmitterName, AI.Position, Color.White, 2);
            }

            return damage;
        }

        public class Buff 
        {
            public Timer EffectTime { get; set; }
            public bool IsInEffect { get { return !EffectTime.HasTriggered; } }
            public string Particles { get; set; }
            public Timer ParticleTimer { get; set; }
            public string SoundOnStart { get; set; }
            public string SoundOnEnd { get; set; }

            

            public Buff()
            {

            }

            public Buff(float time)
            {
                EffectTime = new Timer(time, true);
                ParticleTimer = new Timer(0.25f, false);
            }

            public virtual void OnApply(Creature creature)
            {
                if (!string.IsNullOrEmpty(SoundOnStart))
                {
                    SoundManager.PlaySound(SoundOnStart, creature.Physics.Position, true, 1.0f);
                }
            }

            public virtual void OnEnd(Creature creature)
            {
                if (!string.IsNullOrEmpty(SoundOnEnd))
                {
                    SoundManager.PlaySound(SoundOnEnd, creature.Physics.Position, true, 1.0f);
                }
            }

            public virtual void Update(DwarfTime time, Creature creature)
            {
                EffectTime.Update(time);
                ParticleTimer.Update(time);

                if (ParticleTimer.HasTriggered && !string.IsNullOrEmpty(Particles))
                {
                    PlayState.ParticleManager.Trigger(Particles, creature.Physics.Position, Color.White, 1);
                }
            }

            public virtual Buff Clone()
            {
                return new Buff()
                {
                    EffectTime = new Timer(EffectTime.TargetTimeSeconds, EffectTime.TriggerOnce, EffectTime.Mode),
                    Particles = Particles,
                    ParticleTimer = new Timer(ParticleTimer.TargetTimeSeconds, ParticleTimer.TriggerOnce, ParticleTimer.Mode),
                    SoundOnEnd = SoundOnEnd,
                    SoundOnStart = SoundOnStart
                };
            }
        }

        public class DamageResistBuff : Buff
        {
            public Health.DamageType DamageType { get; set; }
            public float Bonus { get; set; }

            public override Buff Clone()
            {
                return new DamageResistBuff()
                {
                    EffectTime = new Timer(EffectTime.TargetTimeSeconds, EffectTime.TriggerOnce, EffectTime.Mode),
                    Particles = Particles,
                    ParticleTimer = new Timer(ParticleTimer.TargetTimeSeconds, ParticleTimer.TriggerOnce, ParticleTimer.Mode),
                    SoundOnEnd = SoundOnEnd,
                    SoundOnStart = SoundOnStart,
                    DamageType = DamageType,
                    Bonus = Bonus
                };
            }

            public DamageResistBuff()
            {
                DamageType = DamageType.Normal;
                Bonus = 0.0f;
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

        public class StatBuff : Buff
        {
            public CreatureStats.StatNums Buffs { get; set; }
            public StatBuff()
            {
                Buffs = new CreatureStats.StatNums();
            }

            public StatBuff(float time, CreatureStats.StatNums buffs) :
                base(time)
            {
                Buffs = buffs;
            }

            public override Buff Clone()
            {
                return new StatBuff()
                {
                    EffectTime = new Timer(EffectTime.TargetTimeSeconds, EffectTime.TriggerOnce, EffectTime.Mode),
                    Particles = Particles,
                    ParticleTimer = new Timer(ParticleTimer.TargetTimeSeconds, ParticleTimer.TriggerOnce, ParticleTimer.Mode),
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

        public class OngoingDamageBuff : Buff
        {
            public DamageType DamageType { get; set; }
            public float DamagePerSecond { get; set; }

            public OngoingDamageBuff()
            {
                
            }

            public override void Update(DwarfTime time, Creature creature)
            {
                float dt = (float)time.ElapsedGameTime.TotalSeconds;
                creature.Damage(DamagePerSecond*dt, DamageType);
                base.Update(time, creature);
            }

            public override Buff Clone()
            {
                return new OngoingDamageBuff()
                {
                    EffectTime = new Timer(EffectTime.TargetTimeSeconds, EffectTime.TriggerOnce, EffectTime.Mode),
                    Particles = Particles,
                    ParticleTimer = new Timer(ParticleTimer.TargetTimeSeconds, ParticleTimer.TriggerOnce, ParticleTimer.Mode),
                    SoundOnEnd = SoundOnEnd,
                    SoundOnStart = SoundOnStart,
                    DamageType = DamageType,
                    DamagePerSecond = DamagePerSecond
                };
            }
        }

        public class OngoingHealBuff : Buff
        {
            public float DamagePerSecond { get; set; }

            public OngoingHealBuff()
            {
                
            }

            public OngoingHealBuff(float dps, float time) :
                base(time)
            {
                DamagePerSecond = dps;
            }

            public override void Update(DwarfTime time, Creature creature)
            {
                float dt = (float)time.ElapsedGameTime.TotalSeconds;
                creature.Heal(dt * DamagePerSecond);

                base.Update(time, creature);
            }

            public override Buff Clone()
            {
                return new OngoingHealBuff()
                {
                    EffectTime = new Timer(EffectTime.TargetTimeSeconds, EffectTime.TriggerOnce, EffectTime.Mode),
                    Particles = Particles,
                    ParticleTimer = new Timer(ParticleTimer.TargetTimeSeconds, ParticleTimer.TriggerOnce, ParticleTimer.Mode),
                    SoundOnEnd = SoundOnEnd,
                    SoundOnStart = SoundOnStart,
                    DamagePerSecond = DamagePerSecond
                };
            }
        }

        public class ThoughtBuff : Buff
        {
            public Thought.ThoughtType ThoughtType { get; set; }

            public ThoughtBuff()
            {
                
            }

            public ThoughtBuff(float time, Thought.ThoughtType type) :
                base(time)
            {
                ThoughtType = type;
            }

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
                return new ThoughtBuff()
                {
                    EffectTime = new Timer(EffectTime.TargetTimeSeconds, EffectTime.TriggerOnce, EffectTime.Mode),
                    Particles = Particles,
                    ParticleTimer = new Timer(ParticleTimer.TargetTimeSeconds, ParticleTimer.TriggerOnce, ParticleTimer.Mode),
                    SoundOnEnd = SoundOnEnd,
                    SoundOnStart = SoundOnStart,
                    ThoughtType = ThoughtType
                };
            }
        }
    }

}