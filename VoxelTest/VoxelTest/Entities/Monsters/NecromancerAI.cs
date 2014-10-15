using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class NecromancerAI : CreatureAI
    {
        public List<Skeleton> Skeletons { get; set; }
        public int MaxSkeletons { get; set; }
        public Timer WanderTimer { get; set; }
        public Timer SummonTimer { get; set; }
        public Timer AttackTimer { get; set; }
        public float AttackRange { get; set; }

        public NecromancerAI()
        {
            Skeletons = new List<Skeleton>();
            MaxSkeletons = 5;
            SummonTimer = new Timer(5, false);
            WanderTimer = new Timer(1, false);
            AttackTimer = new Timer(3, false);
            SummonTimer.HasTriggered = true;
            AttackRange = 10;
        }

        public NecromancerAI(Creature creature, string name, EnemySensor sensor, PlanService planService) :
            base(creature, name, sensor, planService)
        {
            Skeletons = new List<Skeleton>();
            MaxSkeletons = 5;
            SummonTimer = new Timer(5, false);
            WanderTimer = new Timer(1, false);
            AttackTimer = new Timer(3, false);
            SummonTimer.HasTriggered = true;
            AttackRange = 10;
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);
        }

        public override Task ActOnIdle()
        {
            return new ActWrapperTask(new Wrap(SummonSkeletons));
        }

        public void SummonSkeleton()
        {
            Vector3 pos = Position + MathFunctions.RandVector3Box(-1.0f, 1.0f, 0.0f, 0.0f, -1.0f, 1.0f);
            Skeleton skeleton = EntityFactory.GenerateSkeleton(pos, Manager, GameState.Game.Content,
                GameState.Game.GraphicsDevice, Chunks, PlayState.Camera, Faction, PlayState.PlanService,
                this.Creature.Allies).GetChildrenOfType<Skeleton>().FirstOrDefault();

            Skeletons.Add(skeleton);
            Matrix animatePosition = skeleton.Sprite.LocalTransform;
            animatePosition.Translation = animatePosition.Translation - new Vector3(0, 1, 0);
            skeleton.Sprite.AnimationQueue.Add(new EaseMotion(1.0f, animatePosition, skeleton.Sprite.LocalTransform.Translation));
            PlayState.ParticleManager.Trigger("green_flame", pos, Color.White, 10);
            SoundManager.PlaySound(ContentPaths.Audio.fire, pos, true);
        }

        public void GatherSkeletons()
        {
            foreach (Skeleton skeleton in Skeletons)
            {
                Vector3 offset = Position - skeleton.AI.Position;
                float dist = (offset).Length();
                if (dist > 4 && skeleton.AI.Tasks.Count <= 1)
                {
                    Task goToTask = new ActWrapperTask(new GoToEntityAct(Physics, skeleton.AI));
                    if (!skeleton.AI.Tasks.Contains(goToTask))
                    {
                        skeleton.AI.Tasks.Add(goToTask);
                    }
                }
            }
        }

        public void OrderSkeletonsToAttack()
        {
            List<CreatureAI> enemies = (from faction in Creature.Manager.Factions.Factions 
                                        where Alliance.GetRelationship(Creature.Allies, faction.Value.Alliance) == Relationship.Hates 
                                        from minion in faction.Value.Minions 
                                        let dist = (minion.Position - Creature.AI.Position).Length() 
                                        where dist < AttackRange 
                                        select minion).ToList();

            List<Task> attackTasks = enemies.Select(enemy => new KillEntityTask(enemy.Physics)).Cast<Task>().ToList();
            List<CreatureAI> skeletonAis = Skeletons.Select(skeleton => skeleton.AI).ToList();
            if (attackTasks.Count > 0)
            {
                TaskManager.AssignTasks(attackTasks, skeletonAis);
            }
        }

        public IEnumerable<Act.Status> SummonSkeletons()
        {
            while (true)
            {
                Skeletons.RemoveAll(skeleton => skeleton.IsDead);
                if (SummonTimer.HasTriggered && Skeletons.Count < MaxSkeletons)
                {
                    SummonTimer.Reset(SummonTimer.TargetTimeSeconds);
                    for (int i = Skeletons.Count; i < MaxSkeletons; i+=2)
                    {
                        SummonSkeleton();
                    }
                    yield return Act.Status.Success;
                }
                else if (SummonTimer.HasTriggered)
                {
                    yield return Act.Status.Success;
                }
                SummonTimer.Update(Act.LastTime);

                if (WanderTimer.HasTriggered)
                {
                    Physics.ApplyForce(MathFunctions.RandVector3Box(-5f, 5f, 0.01f, 0.01f, -5f, 5f), 1f);
                    GatherSkeletons();
                }
                WanderTimer.Update(Act.LastTime);

                if (AttackTimer.HasTriggered)
                {
                    OrderSkeletonsToAttack();
                }
                AttackTimer.Update(Act.LastTime);
                yield return Act.Status.Running;
            }
        }
    }
}
