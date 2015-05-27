using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature moves along a planned path until the path is completed, or
    /// it detects failure.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class FollowPathAct : CreatureAct
    {
        private string PathName { get; set; }

        public float EnergyLoss { get; set; }

        public Timer ValidPathTimer { get; set; }

        public FollowPathAct(CreatureAI agent, string pathName) :
            base(agent)
        {
            Name = "Follow path";
            PathName = pathName;
            EnergyLoss = 10.0f;
            ValidPathTimer = new Timer(.75f, false);
        }

        public List<Creature.MoveAction> GetPath()
        {
            return Agent.Blackboard.GetData<List<Creature.MoveAction>>(PathName);
        }

        public void SetPath(List<Creature.MoveAction> path)
        {
            Agent.Blackboard.SetData(PathName, path);
        }

        public bool IsPathValid(List<Creature.MoveAction> path)
        {
            for (int i = 0; i < path.Count - 1; i++)
            {
                if (!path[i].Voxel.IsEmpty) return false;
                List<Creature.MoveAction> neighbors = Agent.Movement.GetMoveActions(path[i].Voxel);
                bool valid = false;
                foreach (Creature.MoveAction vr in neighbors)
                {
                    Vector3 dif = vr.Voxel.Position - path[i + 1].Voxel.Position;
                    if (dif.Length() < .1)
                    {
                        valid = true;
                    }
                }
                if (!valid) return false;
            }
            return true;
        }

        public override IEnumerable<Status> Run()
        {
            Physics.CollisionMode collisionMode = Agent.Physics.CollideMode;
            Creature.CurrentCharacterMode = Creature.CharacterMode.Walking;
            Creature.OverrideCharacterMode = false;
            while(true)
            {
                // ERROR CHECKS / INITIALIZING
                Agent.Physics.Orientation = Physics.OrientMode.RotateY;
                //Agent.Physics.CollideMode = Physics.CollisionMode.UpDown;

                List<Creature.MoveAction> path = GetPath();

                if(path == null)
                {
                    SetPath(null);
                    Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                    Agent.Physics.CollideMode = collisionMode;
                    yield return Status.Fail;
                    break;
                }
                else if(path.Count > 0)
                {
                   TargetVoxel = path.ElementAt(0).Voxel;
                }
                else
                {
                    Agent.Physics.CollideMode = collisionMode;
                    yield return Status.Success;
                    break;
                }

                Creature.MoveAction currentAction = path.ElementAt(0);

                // IF WE ARE MOVING
                if(TargetVoxel != null)
                {
                    Agent.Physics.IsSleeping = false;
                    Agent.LocalControlTimeout.Update(DwarfTime.LastTime);
                    ValidPathTimer.Update(DwarfTime.LastTime);

                    // Check if the path has been made invalid
                    if (ValidPathTimer.HasTriggered && !IsPathValid(path))
                    {
                        Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                        Agent.Physics.CollideMode = collisionMode;
                        yield return Status.Fail;
                    }

                    if(Agent.LocalControlTimeout.HasTriggered)
                    {
                        Vector3 target = TargetVoxel.Position + new Vector3(.5f, .5f, .5f);
                        float distToTarget = (target - Agent.Position).Length();
                        while (distToTarget > 0.1f)
                        {
                            Agent.Position = 0.9f*Agent.Position + 0.1f * target;
                            distToTarget = (target - Agent.Position).Length();
                            yield return Status.Running;
                        }
                        Agent.LocalControlTimeout.Reset(Agent.LocalControlTimeout.TargetTimeSeconds);
                        Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                        yield return Status.Running;
                    }


                    if(PreviousTargetVoxel == null)
                    {
                        LocalTarget = TargetVoxel.Position + new Vector3(0.5f, 0.5f, 0.5f);
                    }
                    else
                    {
                        LocalTarget = MathFunctions.ClosestPointToLineSegment(Agent.Position,
                            PreviousTargetVoxel.Position,
                            TargetVoxel.Position, 0.25f) + new Vector3(0.5f, 0.5f, 0.5f);
                    }

                    Vector3 desiredVelocity = (LocalTarget - Agent.Position);
                    desiredVelocity.Normalize();
                    desiredVelocity *= Creature.Stats.MaxSpeed;

                    Vector3 output = Agent.Creature.Controller.GetOutput((float) DwarfTime.LastTime.ElapsedGameTime.TotalSeconds,
                        desiredVelocity,
                        Creature.Physics.Velocity);

                    output.Y = 0.0f;
                    Agent.Creature.Physics.ApplyForce(output * 0.25f, (float)DwarfTime.LastTime.ElapsedGameTime.TotalSeconds);

                    float yDifference = (LocalTarget - Agent.Position).Y;

                    if(yDifference > 0.25f)
                    {
                        Agent.Jump(DwarfTime.LastTime);
                    }


                    if(Agent.DrawPath)
                    {
                        Drawer3D.DrawLineList(new List<Vector3>{LocalTarget, Agent.Creature.Physics.GlobalTransform.Translation}, Color.White, 0.01f );

                        List<Vector3> points = path.Select(v => v.Voxel.Position + new Vector3(0.5f, 0.5f, 0.5f)).ToList();

                        Drawer3D.DrawLineList(points, Color.Red, 0.1f);
                    }

                    bool goToNextNode;
                    if(path.Count > 1)
                    {
                        goToNextNode = (yDifference < 0.05 && (LocalTarget - Agent.Creature.Physics.GlobalTransform.Translation).Length() < 0.5f);
                    }
                    else
                    {
                        goToNextNode = (LocalTarget - Agent.Creature.Physics.GlobalTransform.Translation).Length() < 0.25f;
                    }

                    if(goToNextNode)
                    {
                        if(path.Count > 0)
                        {
                            PreviousTargetVoxel = TargetVoxel;
                            Agent.LocalControlTimeout.Reset(Agent.LocalControlTimeout.TargetTimeSeconds);
                            TargetVoxel = path[0].Voxel;
                            path.RemoveAt(0);
                        }
                        else
                        {
                            PreviousTargetVoxel = null;
                            SetPath(null);
                            Agent.Physics.CollideMode = collisionMode;
                            yield return Status.Success;
                            break;
                        }
                    }
                }
                else
                {
                    PreviousTargetVoxel = null;
                    Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                    Agent.Physics.CollideMode = collisionMode;
                    yield return Status.Fail;
                    break;
                }

                yield return Status.Running;
            }
        }

        public Voxel TargetVoxel { get; set; }
        public Voxel PreviousTargetVoxel { get; set; }
        public Vector3 LocalTarget { get; set; }
    }

}