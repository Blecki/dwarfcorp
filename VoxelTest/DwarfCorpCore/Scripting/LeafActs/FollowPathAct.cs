using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            for (int i = 0; i < path.Count - 2; i++)
            {
                List<Creature.MoveAction> neighbors = Agent.Chunks.ChunkData.ChunkMap[path[i].Voxel.ChunkID].GetMovableNeighbors(path[i].Voxel);
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
            while(true)
            {
                // ERROR CHECKS / INITIALIZING

                List<Creature.MoveAction> path = GetPath();

                if(path == null)
                {
                    SetPath(null);
                    Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                    yield return Status.Fail;
                    break;
                }
                else if(path.Count > 0)
                {
                   TargetVoxel = path.ElementAt(0).Voxel;
                }
                else
                {
                    yield return Status.Success;
                    break;
                }

                Creature.MoveAction currentAction = path.ElementAt(0);

                // IF WE ARE MOVING
                if(TargetVoxel != null)
                {
                    Agent.LocalControlTimeout.Update(LastTime);
                    ValidPathTimer.Update(LastTime);

                    // Check if the path has been made invalid
                    if (ValidPathTimer.HasTriggered && !IsPathValid(path))
                    {
                        Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                        yield return Status.Fail;
                    }

                    if(Agent.LocalControlTimeout.HasTriggered)
                    {
                        Agent.Position = TargetVoxel.Position + new Vector3(.5f, .5f, .5f);
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

                    Vector3 output = Agent.Creature.Controller.GetOutput((float) Act.LastTime.ElapsedGameTime.TotalSeconds,
                       LocalTarget,
                        Agent.Creature.Physics.GlobalTransform.Translation);

                    Agent.Creature.Physics.ApplyForce(output, (float) Act.LastTime.ElapsedGameTime.TotalSeconds);

                    output.Y = 0.0f;

                    float yDifference = (LocalTarget - Agent.Creature.Physics.GlobalTransform.Translation).Y;

                    if(yDifference > 0.1)
                    {
                        Agent.Jump(LastTime);
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
                        goToNextNode = (yDifference < 0.05 && (LocalTarget - Agent.Creature.Physics.GlobalTransform.Translation).Length() < 0.7f);
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
                            yield return Status.Success;
                            break;
                        }
                    }
                }
                else
                {
                    PreviousTargetVoxel = null;
                    Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
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