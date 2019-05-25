using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature attacks a target until the target is dead.
    /// </summary>
    public class MeleeAct : CreatureAct
    {
        public float EnergyLoss { get; set; }
        public Attack CurrentAttack { get; set; }
        public GameComponent Target { get; set; }
        public bool Training { get; set; }
        public Timer Timeout { get; set; }
        public string TargetName { get; set; }
        public Timer FailTimer { get; set; }
        public GameComponent DefensiveStructure { get; set; }

        public MeleeAct(CreatureAI agent, string target) :
            base(agent)
        {
            FailTimer = new Timer(5.0f, false, Timer.TimerMode.Real);
            Timeout = new Timer(100.0f, false, Timer.TimerMode.Real);
            Training = false;
            Name = "Attack!";
            EnergyLoss = 200.0f;
            TargetName = target;
            CurrentAttack = Datastructures.SelectRandom(agent.Creature.Attacks);
        }

        public float LastHp = 0.0f;

        public MeleeAct(CreatureAI agent, GameComponent target) :
            base(agent)
        {
            FailTimer = new Timer(5.0f, false, Timer.TimerMode.Real);
            Timeout = new Timer(100.0f, false, Timer.TimerMode.Real);
            Training = false;
            Name = "Attack!";
            EnergyLoss = 200.0f;
            Target = target;
            CurrentAttack = Datastructures.SelectRandom(agent.Creature.Attacks);
        }

        public override void OnCanceled()
        {
            Creature.Physics.Orientation = Physics.OrientMode.RotateY;
            Creature.OverrideCharacterMode = false;
            Creature.CurrentCharacterMode = CharacterMode.Walking;
            if (DefensiveStructure != null)
            {
                DefensiveStructure.ReservedFor = null;
            }
            base.OnCanceled();
        }

        public IEnumerable<Status> AvoidTarget(float range, float time)
        {
            if (Target == null)
            {
                yield return Status.Fail;
                yield break;
            }

            Timer avoidTimer = new Timer(time, true, Timer.TimerMode.Game);
            var bodies = Agent.World.PlayerFaction.OwnedObjects.Where(o => o.Tags.Contains("Teleporter")).ToList();
            while (true)
            {
                avoidTimer.Update(DwarfTime.LastTime);

                if (avoidTimer.HasTriggered)
                    yield return Status.Success;
            
                float dist = (Target.Position - Agent.Position).Length();

                if (dist > range)
                {
                    yield return Status.Success;
                    yield break;
                }

                List<MoveAction> neighbors = Agent.Movement.GetMoveActions(Agent.Position, bodies).ToList();
                neighbors.Sort((a, b) =>
                {
                    if (a.Equals(b)) return 0;

                    float da = (a.DestinationVoxel.WorldPosition - Target.Position).LengthSquared() * Agent.Movement.Cost(a.MoveType);
                    float db = (b.DestinationVoxel.WorldPosition - Target.Position).LengthSquared() * Agent.Movement.Cost(a.MoveType);

                    return da.CompareTo(db);
                });

                neighbors.RemoveAll(
                    a => a.MoveType == MoveType.Jump || a.MoveType == MoveType.Climb);

                if (neighbors.Count == 0)
                {
                    yield return Status.Fail;
                    yield break;
                }

                MoveAction furthest = neighbors.Last();
                bool reachedTarget = false;
                Timer timeout = new Timer(2.0f, true, Timer.TimerMode.Real);
                while (!reachedTarget)
                {
                    Vector3 output = Creature.Controller.GetOutput(DwarfTime.Dt, furthest.DestinationVoxel.WorldPosition + Vector3.One*0.5f,
                        Agent.Position);
                    Creature.Physics.ApplyForce(output, DwarfTime.Dt);

                    if (Creature.AI.Movement.CanFly)
                    {
                        Creature.Physics.ApplyForce(Vector3.Up * 10, DwarfTime.Dt);
                    }

                    timeout.Update(DwarfTime.LastTime);

                    yield return Status.Running;
                    if (timeout.HasTriggered || (furthest.DestinationVoxel.WorldPosition - Agent.Position).Length() < 1)
                        reachedTarget = true;

                    Agent.Creature.CurrentCharacterMode = CharacterMode.Walking;
                }

                yield return Status.Success;
                yield break;
            }
        }

        public override IEnumerable<Status> Run()
        {
            Creature.IsCloaked = false;

            if (CurrentAttack == null)
            {
                yield return Status.Fail;
                yield break;
            }

            Timeout.Reset();
            FailTimer.Reset();
            if (Target == null && TargetName != null)
            {
                Target = Agent.Blackboard.GetData<GameComponent>(TargetName);

                if (Target == null)
                {
                    yield return Status.Fail;
                    yield break;
                }
            }

            if (Agent.Faction.Race.IsIntelligent)
            {
                var targetInventory = Target.GetRoot().GetComponent<Inventory>();
                if (targetInventory != null)
                    targetInventory.SetLastAttacker(Agent);
            }

            CharacterMode defaultCharachterMode = Creature.AI.Movement.CanFly
                ? CharacterMode.Flying
                : CharacterMode.Walking;

            bool avoided = false;
            while(true)
            {
                Timeout.Update(DwarfTime.LastTime);
                FailTimer.Update(DwarfTime.LastTime);
                if (FailTimer.HasTriggered)
                {
                    Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                    Creature.OverrideCharacterMode = false;
                    Creature.CurrentCharacterMode = defaultCharachterMode;
                    yield return Status.Fail;
                    yield break;
                }

                if (Timeout.HasTriggered)
                {
                    if (Training)
                    {
                        Agent.AddXP(1);
                        Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                        Creature.OverrideCharacterMode = false;
                        Creature.CurrentCharacterMode = defaultCharachterMode;
                        yield return Status.Success;
                        yield break;
                    }
                    else
                    {
                        Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                        Creature.OverrideCharacterMode = false;
                        Creature.CurrentCharacterMode = defaultCharachterMode;
                        yield return Status.Fail;
                        yield break;
                    }
                }

                if (Target == null || Target.IsDead)
                {
                    Creature.CurrentCharacterMode = defaultCharachterMode;
                    Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                    yield return Status.Success;
                }

                // Find the location of the melee target
                Vector3 targetPos = new Vector3(Target.GlobalTransform.Translation.X,
                    Target.GetBoundingBox().Min.Y,
                    Target.GlobalTransform.Translation.Z);

                Vector2 diff = new Vector2(targetPos.X, targetPos.Z) - new Vector2(Creature.AI.Position.X, Creature.AI.Position.Z);

                Creature.Physics.Face(targetPos);

                bool intersectsbounds = Creature.Physics.BoundingBox.Intersects(Target.BoundingBox);
                float dist = diff.Length();
                // If we are really far from the target, something must have gone wrong.
                if (DefensiveStructure == null && !intersectsbounds && dist > CurrentAttack.Weapon.Range * 4)
                {
                    Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                    Creature.OverrideCharacterMode = false;
                    Creature.CurrentCharacterMode = defaultCharachterMode;
                    yield return Status.Fail;
                    yield break;
                }

                if (DefensiveStructure != null)
                {
                 
                    if (Creature.Hp < LastHp)
                    {
                        float damage = LastHp - Creature.Hp;
                        Creature.Heal(Math.Min(5.0f, damage));
                        var health = DefensiveStructure.GetRoot().GetComponent<Health>();
                        if (health != null)
                        {
                            health.Damage(damage);
                            Drawer2D.DrawLoadBar(health.World.Renderer.Camera, DefensiveStructure.Position, Color.White, Color.Black, 32, 1, health.Hp / health.MaxHealth, 0.1f);
                        }
                        LastHp = Creature.Hp;
                    }

                    if (dist > CurrentAttack.Weapon.Range)
                    {
                        float sqrDist = dist * dist;
                        foreach(var threat in Creature.AI.Faction.Threats)
                        {
                            float threatDist = (threat.AI.Position - Creature.AI.Position).LengthSquared();
                            if (threatDist < sqrDist)
                            {
                                sqrDist = threatDist;
                                Target = threat.Physics;
                                break;
                            }
                        }
                        dist = (float)Math.Sqrt(sqrDist);
                    }

                    if (dist > CurrentAttack.Weapon.Range * 4)
                    {
                        yield return Status.Fail;
                        yield break;
                    }

                    if (DefensiveStructure.IsDead)
                    {
                        DefensiveStructure = null;
                    }
                }

                LastHp = Creature.Hp;

               
                // If we're out of attack range, run toward the target.
                if(DefensiveStructure == null && !Creature.AI.Movement.IsSessile && !intersectsbounds && diff.Length() > CurrentAttack.Weapon.Range)
                {
                    Creature.CurrentCharacterMode = defaultCharachterMode;
                    /*
                    Vector3 output = Creature.Controller.GetOutput(DwarfTime.Dt, targetPos, Creature.Physics.GlobalTransform.Translation) * 0.9f;
                    output.Y = 0.0f;
                    if (Creature.AI.Movement.CanFly)
                    {
                        Creature.Physics.ApplyForce(-Creature.Physics.Gravity, DwarfTime.Dt);
                    }
                    if (Creature.AI.Movement.IsSessile)
                    {
                        output *= 0.0f;
                    }
                    Creature.Physics.ApplyForce(output, DwarfTime.Dt);
                    Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                     */
                    GreedyPathAct greedyPath = new GreedyPathAct(Creature.AI, Target, CurrentAttack.Weapon.Range * 0.75f) {PathLength = 5};
                    greedyPath.Initialize();

                    foreach (Act.Status stat in greedyPath.Run())
                    {
                        if (stat == Act.Status.Running)
                        {
                            yield return Status.Running;
                        }
                        else break;
                    }
                }
                // If we have a ranged weapon, try avoiding the target for a few seconds to get within range.
                else if (DefensiveStructure == null && !Creature.AI.Movement.IsSessile && !intersectsbounds && !avoided && (CurrentAttack.Weapon.Mode == Weapon.AttackMode.Ranged &&
                    dist < CurrentAttack.Weapon.Range *0.15f))
                {
                    FailTimer.Reset();
                    foreach (Act.Status stat in AvoidTarget(CurrentAttack.Weapon.Range, 3.0f))
                        yield return Status.Running;
                    avoided = true;
                }
                // Else, stop and attack
                else if ((DefensiveStructure == null && dist < CurrentAttack.Weapon.Range) ||
                         (DefensiveStructure != null && dist < CurrentAttack.Weapon.Range * 2.0))
                {
                    if (CurrentAttack.Weapon.Mode == Weapon.AttackMode.Ranged 
                        && VoxelHelpers.DoesRayHitSolidVoxel(Creature.World.ChunkManager, Creature.AI.Position, Target.Position))
                    {
                        yield return Status.Fail;
                        yield break;
                    }

                    FailTimer.Reset();
                    avoided = false;
                    Creature.Physics.Orientation = Physics.OrientMode.Fixed;
                    Creature.Physics.Velocity = new Vector3(Creature.Physics.Velocity.X * 0.9f, Creature.Physics.Velocity.Y, Creature.Physics.Velocity.Z * 0.9f);
                    CurrentAttack.RechargeTimer.Reset(CurrentAttack.Weapon.RechargeRate);

                    Creature.Sprite.ResetAnimations(Creature.Stats.CurrentClass.AttackMode);
                    Creature.Sprite.PlayAnimations(Creature.Stats.CurrentClass.AttackMode);
                    Creature.CurrentCharacterMode = Creature.Stats.CurrentClass.AttackMode;
                    Creature.OverrideCharacterMode = true;
                    Timer timeout = new Timer(10.0f, true);
                    while (!CurrentAttack.Perform(Creature, Target, DwarfTime.LastTime, Creature.Stats.Strength + Creature.Stats.Size,
                            Creature.AI.Position, Creature.Faction.ParentFaction.Name))
                    {
                        timeout.Update(DwarfTime.LastTime);
                        if (timeout.HasTriggered)
                            break;

                        Creature.Physics.Velocity = new Vector3(Creature.Physics.Velocity.X * 0.9f, Creature.Physics.Velocity.Y, Creature.Physics.Velocity.Z * 0.9f);
                        if (Creature.AI.Movement.CanFly)
                            Creature.Physics.ApplyForce(-Creature.Physics.Gravity * 0.1f, DwarfTime.Dt);
                        yield return Status.Running;
                    }

                    timeout.Reset();
                    while (!Agent.Creature.Sprite.AnimPlayer.IsDone())
                    {
                        timeout.Update(DwarfTime.LastTime);
                        if (timeout.HasTriggered)
                            break;
                        if (Creature.AI.Movement.CanFly)
                            Creature.Physics.ApplyForce(-Creature.Physics.Gravity * 0.1f, DwarfTime.Dt);
                        yield return Status.Running;
                    }

                    var targetCreature = Target.GetRoot().GetComponent<CreatureAI>();
                    if (targetCreature != null && !Creature.AI.FightOrFlight(targetCreature))
                    {
                        yield return Act.Status.Fail;
                        yield break;
                    }
                    Creature.CurrentCharacterMode = CharacterMode.Attacking;

                    Vector3 dogfightTarget = Vector3.Zero;
                    while (!CurrentAttack.RechargeTimer.HasTriggered && !Target.IsDead)
                    {
                        CurrentAttack.RechargeTimer.Update(DwarfTime.LastTime);
                        if (CurrentAttack.Weapon.Mode == Weapon.AttackMode.Dogfight)
                        {
                            dogfightTarget += MathFunctions.RandVector3Cube()*0.1f;
                            Vector3 output = Creature.Controller.GetOutput(DwarfTime.Dt, dogfightTarget + Target.Position, Creature.Physics.GlobalTransform.Translation) * 0.9f;
                            Creature.Physics.ApplyForce(output - Creature.Physics.Gravity, DwarfTime.Dt);
                        }
                        else
                        {
                            Creature.Physics.Velocity = Vector3.Zero;
                            if (Creature.AI.Movement.CanFly)
                            {
                                Creature.Physics.ApplyForce(-Creature.Physics.Gravity, DwarfTime.Dt);
                            }
                        }
                        yield return Status.Running;
                    }

                    Creature.CurrentCharacterMode = defaultCharachterMode;
                    Creature.Physics.Orientation = Physics.OrientMode.RotateY;

                    if (Target.IsDead)
                    {
                        Target = null;
                        Agent.AddXP(10);
                        Creature.Physics.Face(Creature.Physics.Velocity + Creature.Physics.GlobalTransform.Translation);
                        Creature.Stats.NumThingsKilled++;
                        Creature.AddThought(Thought.ThoughtType.KilledThing);
                        Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                        Creature.OverrideCharacterMode = false;
                        Creature.CurrentCharacterMode = defaultCharachterMode;
                        yield return Status.Success;
                        break;
                    }
                
                }

                yield return Status.Running;
            }
        }
    }

    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class GreedyPathAct : CompoundCreatureAct
    {
        public int PathLength { get; set; }
        public bool Is2D { get; set; }
        public GameComponent Target { get; set; }
        public float Threshold { get; set; }

        public GreedyPathAct()
        {

        }

        public GreedyPathAct(CreatureAI creature, GameComponent target, float threshold)
            : base(creature)
        {
            Target = target;
            Threshold = threshold;
        }

        public IEnumerable<Status> FindGreedyPath()
        {
            Vector3 target = Target.Position;

            if (Is2D) target.Y = Creature.AI.Position.Y;
            List<MoveAction> path = new List<MoveAction>();
            var curr = Creature.Physics.CurrentVoxel;
            var bodies = Agent.World.PlayerFaction.OwnedObjects.Where(o => o.Tags.Contains("Teleporter")).ToList();
            var storage = new MoveActionTempStorage();
            for (int i = 0; i < PathLength; i++)
            {
                IEnumerable<MoveAction> actions =
                    Creature.AI.Movement.GetMoveActions(new MoveState() { Voxel = curr }, bodies, storage);

                MoveAction? bestAction = null;
                float bestDist = float.MaxValue;

                foreach (MoveAction action in actions)
                {
                    // Prevents a stack overflow due to "DestroyObject" task creating a FollowPathAct!
                    if (action.MoveType == MoveType.DestroyObject)
                    {
                        continue;
                    }
                    float dist = (action.DestinationVoxel.WorldPosition - target).LengthSquared();

                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestAction = action;
                    }
                }

                Vector3 half = Vector3.One*0.5f;
                if (bestAction.HasValue &&
                    !path.Any(p => p.DestinationVoxel.Equals(bestAction.Value.DestinationVoxel) && p.MoveType == bestAction.Value.MoveType))
                {
                    path.Add(bestAction.Value);
                    MoveAction action = bestAction.Value;
                    action.DestinationVoxel = curr;
                    curr = bestAction.Value.DestinationVoxel;
                    bestAction = action;

                    if (((bestAction.Value.DestinationVoxel.WorldPosition + half) - target).Length() < Threshold)
                    {
                        break;
                    }
                }

            }

            if (path.Count > 0)
            {
                Creature.AI.Blackboard.SetData("RandomPath", path);
                yield return Status.Success;
            }
            else
            {
                yield return Status.Fail;
            }
        }

        public override void Initialize()
        {
            Tree = new Sequence(new Wrap(FindGreedyPath), new FollowPathAct(Creature.AI, "RandomPath"));
            base.Initialize();
        }
    }
}