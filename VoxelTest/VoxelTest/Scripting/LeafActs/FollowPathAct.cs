using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{

    public class FollowPathAct : CreatureAct
    {
        private string PathName { get; set; }
        public FollowPathAct(CreatureAIComponent agent, string pathName) :
            base(agent)
        {
            Name = "Follow path";
            PathName = pathName;
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
            bool pathFinished = false;

            while(true)
            {
                List<VoxelRef> path = GetPath();

                ChunkManager chunks = Agent.Creature.Master.Chunks;
                if(path == null)
                {
                    SetPath(null);
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
                        yield return Status.Fail;
                        break;
                    }


                    if(Agent.PreviousTargetVoxel == null)
                    {
                        Agent.Creature.LocalTarget = Agent.TargetVoxel.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f);
                    }
                    else
                    {
                        Agent.Creature.LocalTarget = LinearMathHelpers.ClosestPointToLineSegment(Agent.Position,
                            Agent.PreviousTargetVoxel.WorldPosition,
                            Agent.TargetVoxel.WorldPosition, 0.25f) + new Vector3(0.5f, 0.5f, 0.5f);
                    }

                    Vector3 output = Agent.Creature.Controller.GetOutput((float) Act.LastTime.ElapsedGameTime.TotalSeconds,
                        Agent.Creature.LocalTarget,
                        Agent.Creature.Physics.GlobalTransform.Translation);
                    Agent.Creature.Physics.ApplyForce(output, (float) Act.LastTime.ElapsedGameTime.TotalSeconds);

                    output.Y = 0.0f;

                    if((Agent.Creature.LocalTarget - Agent.Creature.Physics.GlobalTransform.Translation).Y > 0.1)
                    {
                        Agent.Jump(LastTime);
                    }


                    if(Agent.DrawPath)
                    {
                        List<Vector3> points = path.Select(v => v.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f)).ToList();

                        SimpleDrawing.DrawLineList(points, Color.Red, 0.1f);
                    }

                    if((Agent.Creature.LocalTarget - Agent.Creature.Physics.GlobalTransform.Translation).Length() < 0.8f || path.Count < 1)
                    {
                        if(path.Count > 1)
                        {
                            Agent.PreviousTargetVoxel = Agent.TargetVoxel;
                            path.RemoveAt(0);
                            Agent.LocalControlTimeout.Reset(Agent.LocalControlTimeout.TargetTimeSeconds);
                            Agent.TargetVoxel = path[0];
                        }
                        else
                        {
                            Agent.PreviousTargetVoxel = null;
                            SetPath(null);
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