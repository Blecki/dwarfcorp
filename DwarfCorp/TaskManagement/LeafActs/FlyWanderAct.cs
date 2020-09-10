using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    /// A creature randomly applies force at intervals to itself.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class FlyWanderAct : CreatureAct
    {
        public Timer WanderTime { get; set; }
        public Timer TurnTime { get; set; }
        public Timer PerchTime { get; set; }
        public float Radius { get; set; }
        public float Altitude { get; set; }
        public float YRadius { get; set; }
        public float GravityCompensation { get; set; }
        public float Damping { get; set; }
        public float TurnThreshold { get; set; }
        public Vector3 LocalTarget { get; set; }
        public Vector3 OriginalGravity { get; set; }

        public bool CanPerchOnWalls { get; set; }
        public bool CanPerchOnGround { get; set; }
        public bool CanPerchOnObjects { get; set; }

        public FlyState State { get; set; }

        public enum FlyState
        {
            Wandering,
            SearchingForPerch,
            Perching
        }

        public FlyWanderAct()
        {

        }

        public FlyWanderAct(CreatureAI creature, float seconds, float turnTime, float radius, float altitude, float perchTime) :
            base(creature)
        {
            Altitude = altitude;
            Name = "FlyWander " + seconds;
            WanderTime = new Timer(seconds, false);
            TurnTime = new Timer(turnTime, false);
            PerchTime = new Timer(perchTime, false);
            Radius = radius;
            YRadius = 2.0f;
            GravityCompensation = 0.1f;
            Damping = 0.25f;
            TurnThreshold = 2.0f;
            OriginalGravity = Vector3.Down * 10;
            CanPerchOnWalls = false;
            CanPerchOnGround = true;
            CanPerchOnObjects = true;
            State = FlyState.Wandering;
            
        }

        public override void Initialize()
        {
            WanderTime.Reset(WanderTime.TargetTimeSeconds);
            TurnTime.Reset(TurnTime.TargetTimeSeconds);
            base.Initialize();
        }

        public override void OnCanceled()
        {
            Agent.Creature.OverrideCharacterMode = false;
            Agent.Creature.Physics.Gravity = Vector3.Down * 10;
            Agent.Creature.CurrentCharacterMode = CharacterMode.Idle;
            base.OnCanceled();
        }

        public override IEnumerable<Status> Run()
        {
            PerchTime.Reset();
            WanderTime.Reset();
            TurnTime.Reset();
            while (true)
            {
                if (State == FlyState.Perching)
                {
                    Agent.Creature.OverrideCharacterMode = false;
                    PerchTime.Reset();
                    Agent.Creature.Physics.Gravity = Vector3.Down * 10;
                    while (!PerchTime.HasTriggered)
                    {
                        Agent.Creature.Physics.Velocity = Vector3.Zero;
                        Agent.Creature.CurrentCharacterMode = CharacterMode.Idle;
                        PerchTime.Update(Agent.FrameDeltaTime);
                        yield return Act.Status.Running;
                    }
                    // When we're done flying, go back to walking and just fall.
                    Agent.Creature.CurrentCharacterMode = CharacterMode.Walking;
                    Agent.Creature.Physics.Gravity = Vector3.Down * 10;
                    yield return Act.Status.Success;
                }

                Agent.Creature.Physics.Gravity = Vector3.Zero;
                // Store the last position of the bird to sample from
                Vector3 oldPosition = Agent.Position;

                // Get the height of the terrain beneath the bird.
                var surfaceHeight = VoxelHelpers.FindFirstVoxelBelow(new VoxelHandle(
                    Agent.World.ChunkManager, GlobalVoxelCoordinate.FromVector3(oldPosition)))
                    .Coordinate.Y + 1;

                // Immediately start flying.
                Agent.Creature.OverrideCharacterMode = false;
                Agent.Creature.CurrentCharacterMode = CharacterMode.Flying;
                Agent.Creature.OverrideCharacterMode = true;

                // Use this to determine when to start turning.
                float currentDistance = 999;

                {
                    // Pick a target within a box floating some distance above the surface.
                    float randomX = MathFunctions.Rand()*Radius - Radius/2.0f;
                    float randomZ = MathFunctions.Rand()*Radius - Radius/2.0f;
                    float randomY = (float) MathFunctions.Random.NextDouble()*YRadius + Altitude + surfaceHeight;

                    // Set the target to that random location.
                    LocalTarget = new Vector3(randomX + oldPosition.X, randomY, randomZ + oldPosition.Z);
                }

                
                // Keep flying until a timer has trigerred.
                while ((!WanderTime.HasTriggered && State == FlyState.Wandering) || (State == FlyState.SearchingForPerch))
                {
                    // If we hit the ground, switch to walking, otherwise switch to flying.
                    Agent.Creature.CurrentCharacterMode = CharacterMode.Flying;

                    WanderTime.Update(Agent.FrameDeltaTime);

                    // If we're near a target, or a timeout occured, pick a new ranodm target.
                    if (TurnTime.Update(Agent.FrameDeltaTime) || TurnTime.HasTriggered || currentDistance < TurnThreshold)
                    {
                        // Pick a target within a box floating some distance above the surface.
                        float randomX = MathFunctions.Rand()*Radius - Radius/2.0f;
                        float randomZ = MathFunctions.Rand()*Radius - Radius/2.0f;
                        float randomY = (float) MathFunctions.Random.NextDouble()*YRadius + Altitude + surfaceHeight;

                        // Set the target to that random location.
                        LocalTarget = new Vector3(randomX + oldPosition.X, randomY, randomZ + oldPosition.Z);
                    }

                  
                    // Set the current distance to the target so we know when to go to a new target.
                    currentDistance = (Agent.Position - LocalTarget).Length();

                    // Output from the force controller.
                    Vector3 output =
                        Creature.Controller.GetOutput((float) Agent.FrameDeltaTime.ElapsedGameTime.TotalSeconds,
                            LocalTarget, Creature.Physics.GlobalTransform.Translation);

                    // We apply a linear combination of the force controller and the 
                    // feed forward force to the bird to make it lazily turn around and fly.
                    Creature.Physics.ApplyForce(output*Damping + Vector3.Up * GravityCompensation,
                        (float)Agent.FrameDeltaTime.ElapsedGameTime.TotalSeconds);


                    if (State == FlyState.Wandering && WanderTime.HasTriggered)
                    {
                        State = FlyState.SearchingForPerch;
                    }

                    if (State == FlyState.SearchingForPerch)
                    {
                        oldPosition = Agent.Position;
                        var vox = Creature.Physics.CurrentVoxel;
                        if (!vox.IsValid)
                        {
                            yield return Act.Status.Running;
                            continue;
                        }
                        if (vox.IsValid && vox.LiquidLevel > 0)
                        {
                            LocalTarget += Vector3.Up * 0.1f;
                            yield return Act.Status.Running;
                            continue;
                        }

                        if (CanPerchOnGround)
                        {
                            Creature.Physics.ApplyForce(OriginalGravity * 2, (float)Agent.FrameDeltaTime.ElapsedGameTime.TotalSeconds);
                            var below = new VoxelHandle(Creature.World.ChunkManager,
                                vox.Coordinate + new GlobalVoxelOffset(0, -1, 0));

                            if (below.IsValid && !below.IsEmpty && below.LiquidLevel == 0)
                            {
                                State = FlyState.Perching;
                                continue;
                            }
                        }

                        if (CanPerchOnWalls)
                        {
                            foreach (var n in VoxelHelpers.EnumerateManhattanNeighbors(Creature.Physics.CurrentVoxel.Coordinate)
                                .Select(c => new VoxelHandle(Creature.World.ChunkManager, c)))
                            {
                                if (n.IsValid && n.Coordinate.Y >= vox.Coordinate.Y && !n.IsEmpty)
                                    State = FlyState.Perching;
                            }
                        }

                        /*
                        if (CanPerchOnObjects)
                        {
                            List<Body> objetcs = new List<Body>();
                            PlayState.ComponentManager.GetBodiesIntersecting(Creature.Physics.BoundingBox, objetcs, CollisionManager.CollisionType.Static);

                            if (objetcs.Count > 0)
                            {
                                State = FlyState.Perching;
                                continue;
                            }
                        }
                         */
                        
                    }

                    yield return Status.Running;

                }

                yield return Status.Running;
            }
        }
    }

}
