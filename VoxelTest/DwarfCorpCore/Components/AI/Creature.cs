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
    /// <summary>
    /// Component which keeps track of a large number of other components (AI, physics, sprites, etc.) 
    /// related to creatures (such as dwarves and goblins). 
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Creature : GameComponent
    {

       
        public CreatureAI AI { get; set; }
        public Physics Physics { get; set; }
        public CharacterSprite Sprite { get; set; }
        public SelectionCircle SelectionCircle { get; set; }
        public EnemySensor Sensors { get; set; }
        public Flammable Flames { get; set; }
        public Health Health { get; set; }
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
            get { return CharacterMode.Sleeping == CurrentCharacterMode; }
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
                base(name, parent)
        {
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

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            CheckNeighborhood(chunks, (float) gameTime.ElapsedGameTime.TotalSeconds);
            
            UpdateAnimation(gameTime, chunks, camera);

            Status.Update(this, gameTime, chunks, camera);

            JumpTimer.Update(gameTime);

            base.Update(gameTime, chunks, camera);
        }

        public void CheckNeighborhood(ChunkManager chunks, float dt)
        {
            Voxel voxelBelow = new Voxel();
            bool belowExists = chunks.ChunkData.GetVoxelerenceAtWorldLocation(Physics.GlobalTransform.Translation - Vector3.UnitY * 0.8f, ref voxelBelow);
            Voxel voxelAbove = new Voxel();
            bool aboveExists = chunks.ChunkData.GetVoxelerenceAtWorldLocation(Physics.GlobalTransform.Translation + Vector3.UnitY, ref voxelAbove);

            if (aboveExists)
            {
                IsHeadClear = voxelAbove.IsEmpty;
            }

            if(belowExists && voxelBelow.Water.WaterLevel > 5)
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
                    if(Physics.Velocity.Y > 0.1)
                    {
                        if (CurrentCharacterMode == CharacterMode.Walking || CurrentCharacterMode == CharacterMode.Idle)
                        {
                            CurrentCharacterMode = CharacterMode.Jumping;
                        }
                    }
                    else if(Physics.Velocity.Y < -0.1)
                    {
                        if (CurrentCharacterMode == CharacterMode.Walking || CurrentCharacterMode == CharacterMode.Idle)
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
                    PlayState.ParticleManager.Trigger(DeathParticleTrigger.EmitterName, AI.Position, Color.White, 5);
                    break;
            }

            
            base.ReceiveMessageRecursive(messageToReceive);
        }

        public void UpdateAnimation(GameTime gameTime, ChunkManager chunks, Camera camera)
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

            if(CurrentCharacterMode == CharacterMode.Attacking)
            {
                return;
            }

            if(!IsOnGround)
            {
                return;
            }

            if(veloNorm < 0.3f || Physics.IsSleeping)
            {
                if(CurrentCharacterMode == CharacterMode.Walking)
                {
                    CurrentCharacterMode = CharacterMode.Idle;
                }
            }
            else
            {
                if(CurrentCharacterMode == CharacterMode.Idle)
                {
                    CurrentCharacterMode = CharacterMode.Walking;
                }
            }
        }

        public IEnumerable<Act.Status> HitAndWait(float f, bool loadBar)
        {
            Timer waitTimer = new Timer(f, true);

            CurrentCharacterMode = CharacterMode.Attacking;
            
            while(!waitTimer.HasTriggered)
            {
                waitTimer.Update(Act.LastTime);

                if(loadBar)
                {
                    Drawer2D.DrawLoadBar(AI.Position + Vector3.Up, Color.White, Color.Black, 100, 16, waitTimer.CurrentTimeSeconds / waitTimer.TargetTimeSeconds);
                }

                Attacks[0].PerformNoDamage(Act.LastTime, AI.Position);

                yield return Act.Status.Running;
            }

            CurrentCharacterMode = CharacterMode.Idle;
            yield return Act.Status.Success;
        }
    }

}