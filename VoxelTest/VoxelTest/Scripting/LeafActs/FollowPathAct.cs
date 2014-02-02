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

        public FollowPathAct(CreatureAIComponent agent, string pathName) :
            base(agent)
        {
            Name = "Follow path";
            PathName = pathName;
            EnergyLoss = 10.0f;
        }

        public List<VoxelRef> GetPath()
        {
            return Agent.Blackboard.GetData<List<VoxelRef>>(PathName);
        }

        public void SetPath(List<VoxelRef> path)
        {
            Agent.Blackboard.SetData(PathName, path);
        }

        public override IEnumerable<Status> Run()
        {
            while(true)
            {
                List<VoxelRef> path = GetPath();

                if(path == null)
                {
                    SetPath(null);
                    Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                    yield return Status.Fail;
                    break;
                }
                else if(path.Count > 0)
                {
                    Agent.TargetVoxel = path.ElementAt(0);
                }
                else
                {
                    yield return Status.Success;
                    break;
                }

                if(Agent.TargetVoxel != null)
                {
                    Agent.LocalControlTimeout.Update(LastTime);

                    if(Agent.LocalControlTimeout.HasTriggered)
                    {
                        Agent.LocalControlTimeout.Reset(Agent.LocalControlTimeout.TargetTimeSeconds);
                        Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                        yield return Status.Fail;
                        break;
                    }


                    if(Agent.PreviousTargetVoxel == null)
                    {
                        Agent.Creature.LocalTarget = Agent.TargetVoxel.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f);
                    }
                    else
                    {
                        Agent.Creature.LocalTarget = MathFunctions.ClosestPointToLineSegment(Agent.Position,
                            Agent.PreviousTargetVoxel.WorldPosition,
                            Agent.TargetVoxel.WorldPosition, 0.25f) + new Vector3(0.5f, 0.5f, 0.5f);
                    }

                    Vector3 output = Agent.Creature.Controller.GetOutput((float) Act.LastTime.ElapsedGameTime.TotalSeconds,
                        Agent.Creature.LocalTarget,
                        Agent.Creature.Physics.GlobalTransform.Translation);

                    Agent.Creature.Physics.ApplyForce(output, (float) Act.LastTime.ElapsedGameTime.TotalSeconds);

                    output.Y = 0.0f;

                    float yDifference = (Agent.Creature.LocalTarget - Agent.Creature.Physics.GlobalTransform.Translation).Y;

                    if(yDifference > 0.1)
                    {
                        Agent.Jump(LastTime);
                    }


                    if(Agent.DrawPath)
                    {
                        Drawer3D.DrawLineList(new List<Vector3>{Agent.Creature.LocalTarget, Agent.Creature.Physics.GlobalTransform.Translation}, Color.White, 0.01f );

                        List<Vector3> points = path.Select(v => v.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f)).ToList();

                        Drawer3D.DrawLineList(points, Color.Red, 0.1f);
                    }

                    bool goToNextNode;
                    if(path.Count > 1)
                    {
                        goToNextNode = (yDifference < 0.05 && (Agent.Creature.LocalTarget - Agent.Creature.Physics.GlobalTransform.Translation).Length() < 0.7f);
                    }
                    else
                    {
                        goToNextNode = (Agent.Creature.LocalTarget - Agent.Creature.Physics.GlobalTransform.Translation).Length() < 0.25f;
                    }

                    if(goToNextNode)
                    {
                        if(path.Count > 0)
                        {
                            Agent.PreviousTargetVoxel = Agent.TargetVoxel;
                            Agent.LocalControlTimeout.Reset(Agent.LocalControlTimeout.TargetTimeSeconds);
                            Agent.TargetVoxel = path[0];
                            path.RemoveAt(0);
                        }
                        else
                        {
                            Agent.PreviousTargetVoxel = null;
                            SetPath(null);
                            yield return Status.Success;
                            break;
                        }
                    }
                }
                else
                {
                    Agent.PreviousTargetVoxel = null;
                    Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                    yield return Status.Fail;
                    break;
                }

                Creature.Status.Energy.CurrentValue -= EnergyLoss * Dt * Creature.Stats.Tiredness;
                yield return Status.Running;
            }
        }
    }

}