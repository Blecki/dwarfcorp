using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class FollowPathAct : CreatureAct
    {
        public FollowPathAct(CreatureAI agent, string pathName) : base(agent)
        {
            Name = "Follow path";
            PathName = pathName;
        }

        private string PathName;
        private float DeltaTime = 0.0f;
        private float LastNoiseTime = 0.0f;
        private Cart Minecart = null;
        private Vector3? LastCartPos = null;

        private float GetAgentSpeed(MoveType Action)
        {
            var speed = Agent.Movement.Speed(Action);

            return GameSettings.Default.CreatureMovementAdjust * Agent.Stats.Dexterity * speed;
        }

        public List<MoveAction> Path
        {
            get
            {
                return Agent.Blackboard.GetData<List<MoveAction>>(PathName);
            }

            set
            {
                Agent.Blackboard.SetData(PathName, value);
            }
        }

        private Vector3 GetPathPoint(VoxelHandle Voxel)
        {
            return Voxel.WorldPosition + new Vector3(0.5f, Agent.Physics.BoundingBoxSize.Y, 0.5f);
        }

        public IEnumerable<Status> PerformStep(MoveAction Step)
        {
            var actionSpeed = GetAgentSpeed(Step.MoveType);

            switch (Step.MoveType)
            {
                #region Ride Elevator
                case MoveType.RideElevator:
                 
                    var shafts = Step.DestinationState.Tag as Elevators.ElevatorMoveState;
                    if (shafts == null || shafts.Entrance == null || shafts.Entrance.IsDead || shafts.Exit == null || shafts.Exit.IsDead)
                        yield return Status.Fail;

                    var shaft = shafts.Entrance.Shaft;
                    if (shaft == null || shaft.Invalid)
                        yield return Status.Fail;

                    if (!shaft.EnqueuDwarf(Agent, shafts))
                        yield return Status.Fail;

                    while (!shaft.ReadyToBoard(Agent))
                    {
                        if (DeltaTime > 30.0f)
                            yield return Status.Fail; // We waited too long.

                        if (shaft.Invalid)
                            yield return Status.Fail;

                        SetCharacterMode(CharacterMode.Idle);

                        if (Debugger.Switches.DebugElevators)
                        {
                            Drawer3D.DrawBox(shafts.Entrance.GetBoundingBox(), Color.Red, 0.1f, false);
                            Drawer3D.DrawBox(shafts.Exit.GetBoundingBox(), Color.Red, 0.1f, false);
                            Drawer3D.DrawBox(Step.DestinationVoxel.GetBoundingBox(), Color.Red, 0.1f, false);
                        }

                        yield return Status.Running;
                    }

                    DeltaTime = 0;
                    foreach (var bit in Translate(Agent.Position, GetPathPoint(shafts.Entrance.GetContainingVoxel()), actionSpeed))
                    {
                        if (shaft.Invalid)
                            yield return Status.Fail;

                        shaft.WaitForMe(Agent);

                        SetCharacterMode(CharacterMode.Walking);

                        if (Debugger.Switches.DebugElevators)
                        {
                            Drawer3D.DrawBox(shafts.Entrance.GetBoundingBox(), Color.Green, 0.1f, false);
                            Drawer3D.DrawBox(shafts.Exit.GetBoundingBox(), Color.Green, 0.1f, false);
                            Drawer3D.DrawBox(Step.DestinationVoxel.GetBoundingBox(), Color.Green, 0.1f, false);
                        }
                        yield return Status.Running;
                    }

                    shaft.StartMotion(Agent);

                    var grav = Creature.Physics.Gravity;
                    //Creature.Physics.Gravity = Vector3.Zero;
                    while (!shaft.AtDestination(Agent))
                    {
                        if (shaft.Invalid)
                            yield return Status.Fail;

                        SetCharacterMode(CharacterMode.Idle);
                        
                        if (Debugger.Switches.DebugElevators)
                        {
                            Drawer3D.DrawBox(shafts.Entrance.GetBoundingBox(), Color.Red, 0.1f, false);
                            Drawer3D.DrawBox(shafts.Exit.GetBoundingBox(), Color.Red, 0.1f, false);
                            Drawer3D.DrawBox(Step.DestinationVoxel.GetBoundingBox(), Color.Red, 0.1f, false);
                        }

                        yield return Status.Running;
                    }

                    DeltaTime = 0;
                    foreach (var bit in Translate(Agent.Physics.LocalPosition, GetPathPoint(Step.DestinationVoxel), actionSpeed))
                    {
                        if (shaft.Invalid)
                            yield return Status.Fail;

                        shaft.WaitForMe(Agent);

                        SetCharacterMode(CharacterMode.Walking);

                        if (Debugger.Switches.DebugElevators)
                        {
                            Drawer3D.DrawBox(shafts.Entrance.GetBoundingBox(), Color.Green, 0.1f, false);
                            Drawer3D.DrawBox(shafts.Exit.GetBoundingBox(), Color.Green, 0.1f, false);
                            Drawer3D.DrawBox(Step.DestinationVoxel.GetBoundingBox(), Color.Green, 0.1f, false);
                        }

                        yield return Status.Running;
                    }
                    Creature.Physics.Gravity = grav;

                    shaft.Done(Agent);

                    break;
                #endregion
                case MoveType.EnterVehicle:

                    Creature.NoiseMaker.MakeNoise("Jump", Agent.Position, false);

                    foreach (var bit in Jump(GetPathPoint(Step.SourceVoxel), GetPathPoint(Step.DestinationVoxel), Step.DestinationVoxel.Center - Step.SourceVoxel.Center, actionSpeed))
                    {
                        SetCharacterMode(Creature.Physics.Velocity.Y > 0 ? CharacterMode.Jumping : CharacterMode.Falling);
                        yield return Status.Running;
                    }

                    DeltaTime = 0.0f;
                    SetupMinecart();

                    break;

                case MoveType.ExitVehicle:

                    CleanupMinecart();
                    SetAgentTranslation(GetPathPoint(Step.DestinationVoxel));
                    break;

                case MoveType.RideVehicle:

                    SetupMinecart();
                    var rail = Step.SourceState.Rail;
                    if (rail == null)
                        yield return Status.Fail;

                    var rideTime = 1.0f / actionSpeed;
                    while (DeltaTime < rideTime)
                    {
                        var pos = rail.InterpolateSpline(DeltaTime / rideTime, Step.SourceVoxel.WorldPosition, Step.DestinationVoxel.WorldPosition);

                        var transform = Agent.Physics.LocalTransform;
                        transform.Translation = pos + Vector3.Up * 0.5f;
                        Agent.Physics.LocalTransform = transform;
                        Agent.Physics.Velocity = GetPathPoint(Step.DestinationVoxel) - GetPathPoint(Step.SourceVoxel);
                        
                        SetCharacterMode(CharacterMode.Minecart);

                        transform.Translation = pos + Vector3.Up * -0.1f;

                        //if (!LastCartPos.HasValue)
                        //    LastCartPos = transform.Translation;
                        //else
                        //    transform = Matrix.Invert(Matrix.CreateLookAt(LastCartPos.Value, transform.Translation, Vector3.Up));

                        Minecart.LocalTransform = transform;
                        LastCartPos = transform.Translation;
                        yield return Status.Running;
                    }

                    DeltaTime -= rideTime;

                    break;

                case MoveType.Walk:

                    // Todo: Fail if distance is too great.

                    CleanupMinecart();

                    foreach (var bit in Translate(Agent.Position, GetPathPoint(Step.DestinationVoxel), actionSpeed))
                    {
                        SetCharacterMode(CharacterMode.Walking);
                        yield return Status.Running;
                    }
                    break;

                case MoveType.Swim:

                    CleanupMinecart();

                    foreach (var bit in Translate(GetPathPoint(Step.SourceVoxel), GetPathPoint(Step.DestinationVoxel) + (0.5f * Vector3.Up * Agent.Physics.BoundingBox.Extents().Y), actionSpeed))
                    {
                        Creature.NoiseMaker.MakeNoise("Swim", Agent.Position, true);
                        SetCharacterMode(CharacterMode.Swimming);
                        yield return Status.Running;
                    }

                    break;

                case MoveType.Jump:

                    CleanupMinecart();
                    Creature.NoiseMaker.MakeNoise("Jump", Agent.Position, false);

                    var dest = GetPathPoint(Step.DestinationVoxel);
                    foreach (var bit in Jump(Agent.Position, dest, dest - Agent.Position, actionSpeed / 2.0f))
                    {
                        Creature.OverrideCharacterMode = false;
                        SetCharacterMode(Creature.Physics.Velocity.Y > 0 ? CharacterMode.Jumping : CharacterMode.Falling);
                        yield return Status.Running;
                    }

                    SetAgentTranslation(dest);

                    break;

                case MoveType.Fall:

                    CleanupMinecart();

                    foreach (var bit in Translate(Agent.Position, GetPathPoint(Step.DestinationVoxel), actionSpeed))
                    {
                        SetCharacterMode(CharacterMode.Falling);
                        yield return Status.Running;
                    }

                    break;

                case MoveType.Climb:

                    CleanupMinecart();

                    DeltaTime = 0.0f;
                    foreach (var bit in Translate(GetPathPoint(Step.SourceVoxel), GetPathPoint(Step.DestinationVoxel), actionSpeed))
                    {
                        if (Step.InteractObject == null || Step.InteractObject.IsDead)
                            yield return Status.Fail;

                        if (DeltaTime - LastNoiseTime > 1.0f)
                        {
                            Creature.NoiseMaker.MakeNoise("Climb", Agent.Position, false);
                            LastNoiseTime = DeltaTime;
                        }

                        SetCharacterMode(CharacterMode.Climbing);
                        yield return Status.Running;
                    }

                    break;

                case MoveType.ClimbWalls:

                    CleanupMinecart();

                    DeltaTime = 0.0f;
                    foreach (var bit in Translate(GetPathPoint(Step.SourceVoxel), GetPathPoint(Step.DestinationVoxel), actionSpeed))
                    {
                        if (DeltaTime - LastNoiseTime > 1.0f)
                        {
                            Creature.NoiseMaker.MakeNoise("Climb", Agent.Position, false);
                            LastNoiseTime = DeltaTime;
                        }

                        SetCharacterMode(CharacterMode.Climbing);

                        if (Step.ActionVoxel.IsValid)
                        {
                            var voxelVector = new Vector3(Step.ActionVoxel.Coordinate.X + 0.5f, Agent.Physics.Position.Y, Step.ActionVoxel.Coordinate.Z + 0.5f);
                            Agent.Physics.Velocity = Vector3.Normalize(voxelVector - Agent.Physics.Position) * actionSpeed;
                        }

                        yield return Status.Running;
                    }

                    break;
                case MoveType.Fly:

                    CleanupMinecart();

                    DeltaTime = 0.0f;
                    foreach (var bit in Translate(GetPathPoint(Step.SourceVoxel), GetPathPoint(Step.DestinationVoxel), actionSpeed))
                    {
                        if ((int)(DeltaTime * 100) % 2 == 0)
                            Creature.NoiseMaker.MakeNoise("Flap", Agent.Position, false);
                        SetCharacterMode(CharacterMode.Flying);

                        yield return Status.Running;
                    }

                    break;

                case MoveType.Dig:

                    CleanupMinecart();

                    var destroy = new DigAct(Creature.AI, new KillVoxelTask(Step.DestinationVoxel)) { CheckOwnership = false } ;
                    destroy.Initialize();
                    foreach (var status in destroy.Run())
                    {
                        if (status == Act.Status.Fail)
                            yield return Act.Status.Fail;
                        yield return Act.Status.Running;
                    }
                    yield return Act.Status.Fail; // Abort the path so that they stop digging if a path has opened.
                    break;

                case MoveType.DestroyObject:

                    CleanupMinecart();

                    var melee = new MeleeAct(Creature.AI, (GameComponent) Step.InteractObject);
                    melee.Initialize();
                    foreach (var status in melee.Run())
                    {
                        if (status == Act.Status.Fail)
                            yield return Act.Status.Fail;
                        yield return Act.Status.Running;
                    }
                    yield return Act.Status.Fail; // Abort the path so that they stop destroying things if a path has opened.
                    break;

                case MoveType.Teleport:

                    if (Step.InteractObject == null || Step.InteractObject.IsDead)
                        yield return Status.Fail;

                    if (Step.InteractObject.GetComponent<MagicalObject>().HasValue(out var teleporter))
                        teleporter.CurrentCharges--;

                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_ic_dwarf_magic_research, Agent.Position, true, 1.0f);
                    Agent.World.ParticleManager.Trigger("green_flame", (Step.InteractObject as GameComponent).Position, Color.White, 1);
                    Agent.GetRoot().SetFlagRecursive(GameComponent.Flag.Visible, false);

                    foreach (var bit in Translate(GetPathPoint(Step.SourceVoxel), GetPathPoint(Step.DestinationVoxel), actionSpeed))
                    {
                        Agent.World.ParticleManager.Trigger("star_particle", Agent.Position, Color.White, 1);
                        yield return Status.Running;
                    }

                    Agent.GetRoot().SetFlagRecursive(GameComponent.Flag.Visible, true);
                        break;
            }
        }

        private void SetCharacterMode(CharacterMode Mode)
        {
            Creature.OverrideCharacterMode = false;
            Creature.CurrentCharacterMode = Mode;
        }

        private void SetupMinecart()
        {
            if (Minecart == null)
            {
                Minecart = EntityFactory.CreateEntity<Cart>("Cart", Agent.Position);
                LastCartPos = null;
            }

            //if (Agent.GetRoot().GetComponent<LayeredSprites.LayeredCharacterSprite>().HasValue(out var layers) && layers.GetLayers().GetLayer("minecart") == null)
            //    layers.AddLayer(LayeredSprites.LayerLibrary.EnumerateLayers("minecart").FirstOrDefault(), LayeredSprites.LayerLibrary.BaseDwarfPalette);
        }

        private void CleanupMinecart()
        {
            if (Minecart != null)
                Minecart.Delete();
            //if (Agent.GetRoot().GetComponent<LayeredSprites.LayeredCharacterSprite>().HasValue(out var layers))
            //    layers.RemoveLayer("minecart");
        }

        public override IEnumerable<Status> Run()
        {
            // Todo: Re-add random offsets?
            Creature.CurrentCharacterMode = CharacterMode.Walking;
            Creature.OverrideCharacterMode = false;
            Agent.Physics.Orientation = Physics.OrientMode.RotateY;
            if (Path == null || Path.Count == 0)
            {
                Path = null;
                Creature.DrawIndicator(IndicatorManager.StandardIndicators.Question);
                yield return Act.Status.Success;
            }

            foreach (var step in Path)
            {
                foreach (var status in PerformStep(step))
                {
                    //Agent.Physics.PropogateTransforms();
                    DeltaTime += (float)DwarfTime.LastTime.ElapsedGameTime.TotalSeconds;

                    if (status == Status.Fail)
                    {
                        CleanupMinecart();
                        yield return Status.Fail;
                    }
                    else
                    {
                        DrawDebugPath();
                        yield return Status.Running;
                    }
                }
            }

            Creature.OverrideCharacterMode = false;
            Path = null;
            Agent.GetRoot().SetFlagRecursive(GameComponent.Flag.Visible, true);
            CleanupMinecart();
            yield return Status.Success;
        }

        private void DrawDebugPath()
        {
            if (Debugger.Switches.DrawPaths)
                for (var i = 0; i < Path.Count; ++i)
                {
                    if (Path[i].MoveType == MoveType.Jump)
                        Drawer3D.DrawLine(GetPathPoint(Path[i].SourceVoxel), GetPathPoint(Path[i].DestinationVoxel), Color.Red, 0.1f);
                    else
                        Drawer3D.DrawLine(GetPathPoint(Path[i].SourceVoxel), GetPathPoint(Path[i].DestinationVoxel), Color.Blue, 0.1f);
                }
        }

        public override void OnCanceled()
        {
            Creature.OverrideCharacterMode = false;
            CleanupMinecart();
            Path = null;
            Agent.GetRoot().SetFlagRecursive(GameComponent.Flag.Visible, true);

            base.OnCanceled();
        }

        private IEnumerable<Status> Jump(Vector3 Start, Vector3 End, Vector3 JumpDelta, float MovementSpeed)
        {
            DeltaTime = 0.0f;

            var jumpTime = 1.0f / MovementSpeed;
            while (DeltaTime < jumpTime)
            {
                var jumpProgress = DeltaTime / jumpTime;
                var dx = ((End - Start) * jumpProgress) + Start;
                dx.Y += Easing.Ballistic(DeltaTime, jumpTime, 1.5f);
                SetAgentTranslation(dx);
                Agent.Physics.Velocity = JumpDelta;
                yield return Status.Running;
            }

            DeltaTime -= jumpTime;
        }

        private IEnumerable<Status> Translate(Vector3 Start, Vector3 End, float MovementSpeed)
        {
            //DeltaTime = 0.0f;
            var targetDeltaTime = (End - Start).Length() / MovementSpeed;

            while (DeltaTime < targetDeltaTime)
            {
                Vector3 dx = (End - Start) * (DeltaTime / targetDeltaTime) + Start;
                SetAgentTranslation(dx);
                Agent.Physics.Velocity = Vector3.Normalize(End - Start) * MovementSpeed;
                yield return Status.Running;
            }

            //SetAgentTranslation(End);
            DeltaTime -= targetDeltaTime;
        }

        private void SetAgentTranslation(Vector3 T)
        {
            var transform = Agent.Physics.LocalTransform;
            transform.Translation = T;
            Agent.Physics.LocalTransform = transform;
            //Agent.Physics.PropogateTransforms();
        }
    }
}