using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A creature randomly applies force at intervals to itself.
    /// </summary>
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
            WanderTime.Reset();
            TurnTime.Reset();
            while(!WanderTime.HasTriggered)
            {
                Creature.OverrideCharacterMode = false;
                Creature.Physics.Orientation = Physics.OrientMode.RotateY;
                Creature.CurrentCharacterMode = CharacterMode.Walking;
                WanderTime.Update(Agent.FrameDeltaTime);

                if (!Creature.IsOnGround)
                {
                    yield return Status.Success;
                    yield break;
                }
                if(TurnTime.Update(Agent.FrameDeltaTime) || TurnTime.HasTriggered || firstIter)
                {
                    int iters = 0;

                    while (iters < 100)
                    {
                        iters++;
                        Vector2 randTarget = MathFunctions.RandVector2Circle() * Radius;
                        LocalTarget = new Vector3(randTarget.X, 0, randTarget.Y) + oldPosition;
                        VoxelHandle voxel = new VoxelHandle(Agent.World.ChunkManager, GlobalVoxelCoordinate.FromVector3(LocalTarget));
                        bool foundLava = false;
                        foreach (VoxelHandle neighbor in VoxelHelpers.EnumerateAllNeighbors(voxel.Coordinate).Select(coord => new VoxelHandle(Agent.World.ChunkManager, coord)))
                        {
                            if (neighbor.IsValid && neighbor.Chunk != null && Library.GetLiquid("Lava").HasValue(out var liquid) && LiquidCellHelpers.AnyLiquidInVoxel(neighbor, liquid.ID))
                                foundLava = true;
                        }
                        if (!foundLava)
                            break;
                    }

                    if (iters == 100)
                    {
                        yield return Act.Status.Fail;
                        yield break;
                    }
                    firstIter = false;
                    TurnTime.Reset(TurnTime.TargetTimeSeconds + MathFunctions.Rand(-0.1f, 0.1f));
                }

                float dist = (LocalTarget - Agent.Position).Length();


                if (dist < 0.5f)
                {
                    Creature.Physics.Velocity *= 0.0f;
                    Creature.CurrentCharacterMode = CharacterMode.Idle;
                    yield return Status.Running;
                }
                else
                {
                    
                    Vector3 output =
                        Creature.Controller.GetOutput((float)Agent.FrameDeltaTime.ElapsedGameTime.TotalSeconds,
                            LocalTarget, Agent.Position);
                    output.Y = 0.0f;

                    Creature.Physics.ApplyForce(output * 0.5f, (float)Agent.FrameDeltaTime.ElapsedGameTime.TotalSeconds);
                    Creature.CurrentCharacterMode = CharacterMode.Walking;
                }

                yield return Status.Running;
            }
            Creature.CurrentCharacterMode = CharacterMode.Idle;
            yield return Status.Success;
        }
    }
}