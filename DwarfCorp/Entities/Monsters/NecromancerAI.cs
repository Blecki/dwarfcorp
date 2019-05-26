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
        public Timer SummonTimer { get; set; }
        public Timer AttackTimer { get; set; }
        public float AttackRange { get; set; }

        public NecromancerAI()
        {
            Skeletons = new List<Skeleton>();
            MaxSkeletons = 3;
            SummonTimer = new Timer(5, false);
            WanderTimer = new Timer(1, false);
            AttackTimer = new Timer(3, false);
            SummonTimer.HasTriggered = true;
            AttackRange = 10;
        }

        public NecromancerAI(ComponentManager Manager, string name, EnemySensor sensor) :
            base(Manager, name, sensor)
        {
            Skeletons = new List<Skeleton>();
            MaxSkeletons = 3;
            SummonTimer = new Timer(5, false);
            WanderTimer = new Timer(1, false);
            AttackTimer = new Timer(3, false);
            SummonTimer.HasTriggered = true;
            AttackRange = 10;
        }
        
        public override Task ActOnIdle()
        {
            return new ActWrapperTask(SummonFromGraves())
            {
                Priority = Task.PriorityType.High
            };
        }

        public void SummonSkeleton()
        {
            SummonSkeleton(Position + MathFunctions.RandVector3Box(-1.0f, 1.0f, 0.0f, 0.0f, -1.0f, 1.0f));
        }


        public void SummonSkeleton(Vector3 pos)
        {
            Skeleton skeleton = EntityFactory.CreateEntity<Physics>("Skeleton", pos).GetRoot().GetComponent<Skeleton>();
            if (skeleton.Faction != null)
            {
                skeleton.Faction.Minions.Remove(skeleton.AI);
            }
            skeleton.Faction = this.Faction;
            this.Faction.AddMinion(skeleton.AI);
            Skeletons.Add(skeleton);
            Matrix animatePosition = skeleton.Sprite.LocalTransform;
            animatePosition.Translation = animatePosition.Translation - new Vector3(0, 1, 0);
            skeleton.Sprite.AnimationQueue.Add(new EaseMotion(1.0f, animatePosition, skeleton.Sprite.LocalTransform.Translation));
            Manager.World.ParticleManager.Trigger("green_flame", pos, Color.White, 10);
            Manager.World.ParticleManager.Trigger("dirt_particle", pos, Color.White, 10);

            var myEnvoy = Faction.TradeEnvoys.Where(envoy => envoy.Creatures.Contains(this)).FirstOrDefault();
            
            if (myEnvoy != null)
            {
                myEnvoy.Creatures.Add(skeleton.AI);
            }

            var myWarParty = Faction.WarParties.Where(party => party.Creatures.Contains(this)).FirstOrDefault();
            
            if (myWarParty != null)
            {
                myWarParty.Creatures.Add(skeleton.AI);
            }
        }

        public IEnumerable<Act.Status> SummonSkeleton(GameComponent grave)
        {
            if (grave.IsDead)
            {
                SetMessage("Failed to summon skeleton: grave is nonexistent.");
                yield return Act.Status.Fail;
            }
            SummonSkeleton(grave.Position);
            grave.Die();
            yield return Act.Status.Success;
        }

        public void GatherSkeletons()
        {
            foreach (Skeleton skeleton in Skeletons)
            {
                Vector3 offset = Position - skeleton.AI.Position;
                float dist = (offset).Length();
                if (dist > 4 && skeleton.AI.Tasks.Count <= 1)
                {
                    Task goToTask = new ActWrapperTask(new GoToEntityAct(Physics, skeleton.AI))
                    {
                        Priority = Task.PriorityType.High
                    };
                    if (!skeleton.AI.Tasks.Contains(goToTask))
                    {
                        skeleton.AI.AssignTask(goToTask);
                    }
                }
            }
        }

        public void OrderSkeletonsToAttack()
        {
            IEnumerable<CreatureAI> enemies = (from faction in Creature.Manager.World.Factions.Factions
                where
                    Manager.World.Diplomacy.GetPolitics(Creature.Faction, faction.Value)
                        .GetCurrentRelationship() == Relationship.Hateful
                from minion in faction.Value.Minions
                let dist = (minion.Position - Creature.AI.Position).Length()
                where dist < AttackRange
                select minion);

            List<Task> attackTasks = enemies.Select(enemy => new KillEntityTask(enemy.Physics, KillEntityTask.KillType.Auto)).Cast<Task>().ToList();
            List<CreatureAI> skeletonAis = Skeletons.Select(skeleton => skeleton.AI).ToList();
            if (attackTasks.Count > 0)
            {
                TaskManager.AssignTasksGreedy(attackTasks, skeletonAis);
            }
        }

        public Act SummonFromGraves()
        {
            List<GameComponent> graves = (from faction in Creature.Manager.World.Factions.Factions
                where
                    Manager.World.Diplomacy.GetPolitics(Creature.Faction, faction.Value)
                        .GetCurrentRelationship() == Relationship.Hateful
                from zone in faction.Value.EnumerateZones()
                from body in zone.ZoneBodies
                where body.Tags.Contains("Grave")
                select body).ToList();

            if (graves.Count > 0)
            {
                var grave = graves.FirstOrDefault();
                if (grave != null && !grave.IsDead)
                {
                    return new Sequence(new GoToEntityAct(grave, this),
                        new Wrap(() => SummonSkeleton(grave)));
                }
            }
            return new Wrap(SummonSkeletons);
        }

        public IEnumerable<Act.Status> SummonSkeletons()
        {
            while (true)
            {
                WanderAct wander = new WanderAct(this, WanderTimer.TargetTimeSeconds, 1.0f, 1.0f);
                Skeletons.RemoveAll(skeleton => skeleton.IsDead);
                if (SummonTimer.HasTriggered && Skeletons.Count < MaxSkeletons)
                {
                    Creature.CurrentCharacterMode = Creature.Stats.CurrentClass.AttackMode;
                    Creature.OverrideCharacterMode = true;
                    Creature.Sprite.ReloopAnimations(Creature.Stats.CurrentClass.AttackMode);
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_ic_necromancer_summon, Position, true);
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
                SummonTimer.Update(DwarfTime.LastTime);
                Creature.CurrentCharacterMode = CharacterMode.Idle;
                Creature.OverrideCharacterMode = false;
                if (WanderTimer.HasTriggered)
                {
                    foreach (Act.Status status in wander.Run())
                    {
                        GatherSkeletons();
                        yield return Act.Status.Running;
                    }
                }
                WanderTimer.Update(DwarfTime.LastTime);

                if (AttackTimer.HasTriggered)
                {
                    OrderSkeletonsToAttack();
                }
                AttackTimer.Update(DwarfTime.LastTime);
                yield return Act.Status.Running;
            }
        }
    }
}
