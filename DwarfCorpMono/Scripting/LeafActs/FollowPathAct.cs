using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class FollowPathAct : CreatureAct
    {
        public FollowPathAct(CreatureAIComponent agent) :
            base(agent)
        {
            Name = "Follow path";
        }


        public override IEnumerable<Status> Run()
        {
            bool pathFinished = false;

            while (!pathFinished)
            {
                ChunkManager chunks = Agent.Creature.Master.Chunks;
                if (Agent.CurrentPath == null)
                {
                    Agent.CurrentPath = null;
                    yield return Status.Fail;
                    break;
                }

                if (Agent.TargetVoxel != null)
                {
                    Agent.LocalControlTimeout.Update(Act.LastTime);

                    if (Agent.LocalControlTimeout.HasTriggered)
                    {
                        yield return Status.Fail;
                        break;
                    }


                    if (Agent.PreviousTargetVoxel == null)
                    {
                        Agent.Creature.LocalTarget = Agent.TargetVoxel.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f);
                    }
                    else
                    {
                        Agent.Creature.LocalTarget = LinearMathHelpers.ClosestPointToLineSegment(Agent.Position, 
                                                                                                 Agent.PreviousTargetVoxel.WorldPosition, 
                                                                                                 Agent.TargetVoxel.WorldPosition, 0.25f) + new Vector3(0.5f, 0.5f, 0.5f);
                    }

                    Vector3 output = Agent.Creature.Controller.GetOutput((float)Act.LastTime.ElapsedGameTime.TotalSeconds,
                                                                               Agent.Creature.LocalTarget, 
                                                                               Agent.Creature.Physics.GlobalTransform.Translation);
                    Agent.Creature.Physics.ApplyForce(output, (float)Act.LastTime.ElapsedGameTime.TotalSeconds);

                    output.Y = 0.0f;

                    if ((Agent.Creature.LocalTarget - Agent.Creature.Physics.GlobalTransform.Translation).Y > 0.3)
                    {
                        Agent.Jump(Act.LastTime);
                    }


                    if (Agent.DrawPath)
                    {
                        List<Vector3> points = new List<Vector3>();
                        foreach (VoxelRef v in Agent.CurrentPath)
                        {
                            points.Add(v.WorldPosition + new Vector3(0.5f, 0.5f, 0.2f));
                        }

                        SimpleDrawing.DrawLineList(points, Color.Red, 0.1f);
                    }

                    if ((Agent.Creature.LocalTarget - Agent.Creature.Physics.GlobalTransform.Translation).Length() < 0.8f || Agent.CurrentPath.Count < 1)
                    {
                        if (Agent.CurrentPath != null && Agent.CurrentPath.Count > 1)
                        {
                            Agent.PreviousTargetVoxel = Agent.TargetVoxel;
                            Agent.CurrentPath.RemoveAt(0);
                            Agent.LocalControlTimeout.Reset(Agent.LocalControlTimeout.TargetTimeSeconds);
                            Agent.TargetVoxel = Agent.CurrentPath[0];
                        }
                        else
                        {
                            Agent.PreviousTargetVoxel = null;
                            Agent.CurrentPath = null;
                            yield return Status.Success;
                            pathFinished = true;
                            break;
                        }
                    }
                }
                else
                {
                    Agent.PreviousTargetVoxel = null;
                    yield return Status.Fail;
                    break;
                }

                yield return Status.Running;
            }
        }

    }
}
