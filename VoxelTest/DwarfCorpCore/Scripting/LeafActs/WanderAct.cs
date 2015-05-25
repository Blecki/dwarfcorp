using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A creature randomly applies force at intervals to itself.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class WanderAct : CreatureAct
    {
        public Timer WanderTime { get; set; }
        public Timer TurnTime { get; set; }
        public float Radius { get; set; }
        public Vector3 LocalTarget { get; set; }
        public WanderAct()
        {
            
        }

        public WanderAct(CreatureAI creature, float seconds, float turnTime, float radius) :
            base(creature)
        {
            Name = "Wander";
            WanderTime = new Timer(seconds, false);
            TurnTime = new Timer(turnTime, false);
            Radius = radius;
        }

        public override void Initialize()
        {
            WanderTime.Reset(WanderTime.TargetTimeSeconds);
            TurnTime.Reset(TurnTime.TargetTimeSeconds);
            LocalTarget = Agent.Position;
            base.Initialize();
        }


        public override IEnumerable<Status> Run()
        {
            Vector3 oldPosition = Agent.Position;
            bool firstIter = true;
            Creature.Controller.Reset();
            while(!WanderTime.HasTriggered)
            {
                Creature.OverrideCharacterMode = false;
                Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                Creature.CurrentCharacterMode = Creature.CharacterMode.Idle;
                WanderTime.Update(DwarfTime.LastTime);

                if (!Creature.IsOnGround)
                {
                    yield return Status.Running;
                    continue;
                }
                if(TurnTime.Update(DwarfTime.LastTime) || TurnTime.HasTriggered || firstIter)
                {
                    
                    LocalTarget = new Vector3(MathFunctions.Rand() * Radius - Radius / 2.0f, 0.0f, MathFunctions.Rand() * Radius - Radius / 2.0f) + oldPosition;
                     

                    /*
                    List<Creature.MoveAction> neighbors = Agent.Chunks.ChunkData.GetMovableNeighbors(Agent.Position);
                    neighbors.RemoveAll(
                    a => a.MoveType == Creature.MoveType.Jump || a.MoveType == DwarfCorp.Creature.MoveType.Climb);

                    if (neighbors.Count > 0)
                    {
                        LocalTarget = neighbors[PlayState.Random.Next(0, neighbors.Count)].Voxel.Position +
                                      Vector3.One*0.5f;
                    }
                     */
                    firstIter = false;
                }

                float origDist = (oldPosition - LocalTarget).Length();

                if (origDist > Radius)
                {
                    Creature.Physics.Velocity *= 0.9f;
                    yield return Status.Running;
                    continue;
                }

                float dist = (LocalTarget - Agent.Position).Length();

                if (dist < Radius*0.25f)
                {
                    Creature.Physics.Velocity *= 0.9f;
                }
                else
                {
                    
                    Vector3 output =
                        Creature.Controller.GetOutput((float) DwarfTime.LastTime.ElapsedGameTime.TotalSeconds,
                            LocalTarget, Agent.Position);
                    output.Y = 0.0f;

                    Creature.Physics.ApplyForce(output * 0.5f, (float) DwarfTime.LastTime.ElapsedGameTime.TotalSeconds);
                }

                yield return Status.Running;
            }

            yield return Status.Success;
        }
    }

}