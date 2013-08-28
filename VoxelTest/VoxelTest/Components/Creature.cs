using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{
    public class Creature : GameComponent
    {
        public CreatureAIComponent AI { get; set; }
        public PhysicsComponent Physics { get; set; }
        public CharacterSprite Sprite { get; set; }
        public EnemySensor Sensors { get; set; }
        public FlammableComponent Flames { get; set; }
        public HealthComponent Health { get; set; }
        public EmitterComponent DeathEmitter { get; set; }
        public Grabber Hands { get; set; }
        public ShadowComponent Shadow { get; set; }
        public GraphicsDevice Graphics { get; set; }
        public ChunkManager Chunks { get; set; }
        public Weapon Weapon { get; set; }
        public ContentManager Content { get; set; }
        public GameMaster Master { get; set; }
        public PlanService PlanService { get; set; }
        public string Allies { get; set; }
        public Vector3 LocalTarget { get; set; }
        public PIDController Controller { get; set; }
        public CreatureStats Stats { get; set; }
        public Timer JumpTimer { get; set; }
        public CreatureStatus Status { get; set; }
        protected CharacterMode currentCharacterMode = CharacterMode.Idle;
        public CharacterMode CurrentCharacterMode { get { return currentCharacterMode; } set { currentCharacterMode = value; if (Sprite != null) { Sprite.SetCurrentAnimation(value.ToString()); } } }
        public bool IsOnGround { get; set; }

        public enum CharacterMode
        {
            Walking,
            Idle,
            Falling,
            Jumping,
            Attacking,
            Hurt,
            Sleeping,
            Swimming
        }


        public Creature(CreatureStats stats,
                        string allies,
                        PlanService planService,
                        GameMaster master,
                        PhysicsComponent parent,
                        ComponentManager manager,
                        ChunkManager chunks,
                        GraphicsDevice graphics,
                        ContentManager content,
                        string name) :
            base(parent.Manager, name, parent)
        {
            IsOnGround = true;
            Physics = parent;
            Stats = stats;
            Chunks = chunks;
            Graphics = graphics;
            Content = content;
            Master = master;
            PlanService = planService;
            Allies = allies;
            Controller = new PIDController(Stats.MaxAcceleration, Stats.StoppingForce, 0.0f);
            LocalTarget = parent.LocalTransform.Translation;
            JumpTimer = new Timer(0.2f, true);
            Status = new CreatureStatus();
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            CheckGrounded(chunks, (float)gameTime.ElapsedGameTime.TotalSeconds);
            UpdateAnimation(gameTime, chunks, camera);
            Status.Hunger += (float)gameTime.ElapsedGameTime.TotalSeconds * Stats.HungerIncrease;
            Status.Energy = Math.Max(Status.Energy - (float)gameTime.ElapsedGameTime.TotalSeconds * Stats.EnergyLoss, 0.0f);
            JumpTimer.Update(gameTime);
            Weapon.Update(gameTime);
            base.Update(gameTime, chunks, camera);
        }

        public void CheckGrounded(ChunkManager chunks, float dt)
        {
            List<VoxelRef> voxelsBelow = chunks.GetVoxelReferencesAtWorldLocation(Physics.GlobalTransform.Translation - Vector3.UnitY * 0.8f);
            VoxelRef voxelBelow = null;

            if (voxelsBelow.Count > 0)
            {
                voxelBelow = voxelsBelow[0];
            }


            if (voxelBelow != null && voxelBelow.GetWaterLevel(chunks) > 5)
            {
                IsOnGround = false;
                CurrentCharacterMode = CharacterMode.Swimming;
            }
            else if (voxelBelow != null)
            {
                if (voxelBelow.TypeName!= "empty")
                {
                    IsOnGround = true;

                    if(CurrentCharacterMode != CharacterMode.Attacking)
                        CurrentCharacterMode = CharacterMode.Idle;
                }
                else
                {
                    IsOnGround = false;
                    if (Physics.Velocity.Y > 0.1)
                    {
                        if (CurrentCharacterMode != CharacterMode.Attacking) CurrentCharacterMode = CharacterMode.Jumping;
                    }
                    else if (Physics.Velocity.Y < -0.1)
                    {
                        if (CurrentCharacterMode != CharacterMode.Attacking) CurrentCharacterMode = CharacterMode.Falling;
                    }
                    else
                    {
                        if (CurrentCharacterMode != CharacterMode.Attacking) currentCharacterMode = CharacterMode.Idle;
                    }
                }
            }
            else
            {
                if (IsOnGround)
                {
                    IsOnGround = false;
                    if (Physics.Velocity.Y > 0)
                    {
                        CurrentCharacterMode = CharacterMode.Jumping;
                    }
                    else
                    {
                        CurrentCharacterMode = CharacterMode.Falling;
                    }
                }
            }

        }


        public void UpdateAnimation(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            float veloNorm = Physics.Velocity.Length();
            if (veloNorm > Stats.MaxSpeed)
            {
                Physics.Velocity = (Physics.Velocity / veloNorm) * Stats.MaxSpeed;
                if(IsOnGround && CurrentCharacterMode == CharacterMode.Idle) CurrentCharacterMode = CharacterMode.Walking;
            }

            if (CurrentCharacterMode != CharacterMode.Attacking)
            {
                if (IsOnGround)
                {
                    if (veloNorm < 0.3f || Physics.IsSleeping)
                    {
                        if (CurrentCharacterMode == CharacterMode.Walking) CurrentCharacterMode = CharacterMode.Idle;
                    }
                    else
                    {
                        if (CurrentCharacterMode == CharacterMode.Idle) CurrentCharacterMode = CharacterMode.Walking;
                    }
                }
            }
        }

    }
}
