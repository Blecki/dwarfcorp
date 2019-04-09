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

        public IEnumerable<Status> PerformStep(MoveAction Step)
        {
            var actionSpeed = GetAgentSpeed(Step.MoveType);

            switch (Step.MoveType)
            {
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
                    foreach (var bit in Translate(Agent.Position, shafts.Entrance.Position, actionSpeed))
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
                    foreach (var bit in Translate(Agent.Physics.LocalPosition, Step.DestinationVoxel.Center, actionSpeed))
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

                case MoveType.EnterVehicle:

                    Creature.NoiseMaker.MakeNoise("Jump", Agent.Position, false);

                    foreach (var bit in Jump(Step.SourceVoxel.Center, Step.DestinationVoxel.Center, Step.DestinationVoxel.Center - Step.SourceVoxel.Center, actionSpeed))
                    {
                        SetCharacterMode(Creature.Physics.Velocity.Y > 0 ? CharacterMode.Jumping : CharacterMode.Falling);
                        yield return Status.Running;
                    }

                    DeltaTime = 0.0f;
                    SetupMinecart();

                    break;

                case MoveType.ExitVehicle:

                    CleanupMinecart();
                    SetAgentTranslation(Step.DestinationVoxel.Center);
                    break;

                case MoveType.RideVehicle:

                    SetupMinecart();
                    var rail = Step.SourceState.Rail;
                    if (rail == null)
                        yield return Status.Fail;

                    while (DeltaTime < 1.0f / actionSpeed)
                    {
                        var pos = rail.InterpolateSpline(DeltaTime, Step.SourceVoxel.Center, Step.DestinationVoxel.Center);
                        var transform = Agent.Physics.LocalTransform;
                        transform.Translation = pos + Vector3.Up * 0.5f;
                        Agent.Physics.LocalTransform = transform;
                        Agent.Physics.Velocity = Step.DestinationVoxel.Center - Step.SourceVoxel.Center;

                        SetCharacterMode(CharacterMode.Minecart);
                        yield return Status.Running;
                    }
                    DeltaTime -= (1.0f / actionSpeed);

                    break;

                case MoveType.Walk:

                    // Todo: Fail if distance is too great.

                    CleanupMinecart();

                    foreach (var bit in Translate(Agent.Position, Step.DestinationVoxel.Center, actionSpeed))
                    {
                        SetCharacterMode(CharacterMode.Walking);
                        yield return Status.Running;
                    }
                    break;

                case MoveType.Swim:

                    CleanupMinecart();

                    foreach (var bit in Translate(Step.SourceVoxel.Center, Step.DestinationVoxel.Center + (0.5f * Vector3.Up * Agent.Physics.BoundingBox.Extents().Y), actionSpeed))
                    {
                        Creature.NoiseMaker.MakeNoise("Swim", Agent.Position, true);
                        SetCharacterMode(CharacterMode.Swimming);
                        yield return Status.Running;
                    }

                    break;

                case MoveType.Jump:

                    CleanupMinecart();
                    Creature.NoiseMaker.MakeNoise("Jump", Agent.Position, false);

                    foreach (var bit in Jump(Step.SourceVoxel.Center, Step.DestinationVoxel.Center + new Vector3(0.0f, 0.5f, 0.0f), Step.DestinationVoxel.Center - Step.SourceVoxel.Center, actionSpeed))
                    {
                        Creature.OverrideCharacterMode = false;
                        SetCharacterMode(Creature.Physics.Velocity.Y > 0 ? CharacterMode.Jumping : CharacterMode.Falling);
                        yield return Status.Running;
                    }

                    SetAgentTranslation(Step.DestinationVoxel.Center);

                    break;

                case MoveType.Fall:

                    CleanupMinecart();

                    foreach (var bit in Translate(Step.SourceVoxel.Center, Step.DestinationVoxel.Center, actionSpeed))
                    {
                        SetCharacterMode(CharacterMode.Falling);
                        yield return Status.Running;
                    }

                    break;

                case MoveType.Climb:

                    CleanupMinecart();

                    DeltaTime = 0.0f;
                    foreach (var bit in Translate(Step.SourceVoxel.Center, Step.DestinationVoxel.Center, actionSpeed))
                    {
                        if (Step.InteractObject == null || Step.InteractObject.IsDead)
                            yield return Status.Fail;
                        Creature.NoiseMaker.MakeNoise("Climb", Agent.Position, false);
                        SetCharacterMode(CharacterMode.Climbing);
                        yield return Status.Running;
                    }

                    break;

                case MoveType.ClimbWalls:

                    CleanupMinecart();

                    DeltaTime = 0.0f;
                    foreach (var bit in Translate(Step.SourceVoxel.Center, Step.DestinationVoxel.Center, actionSpeed))
                    {
                        Creature.NoiseMaker.MakeNoise("Climb", Agent.Position, false);
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
                    foreach (var bit in Translate(Step.SourceVoxel.Center, Step.DestinationVoxel.Center, actionSpeed))
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

                    var melee = new MeleeAct(Creature.AI, (Body) Step.InteractObject);
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

                    var teleporter = Step.InteractObject.GetComponent<MagicalObject>();
                    if (teleporter != null)
                        teleporter.CurrentCharges--;

                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_ic_dwarf_magic_research, Agent.Position, true, 1.0f);
                    Agent.World.ParticleManager.Trigger("green_flame", (Step.InteractObject as Body).Position, Color.White, 1);
                    Agent.GetRoot().SetFlagRecursive(GameComponent.Flag.Visible, false);

                    foreach (var bit in Translate(Step.SourceVoxel.Center, Step.DestinationVoxel.Center, actionSpeed))
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
            var layers = Agent.GetRoot().GetComponent<LayeredSprites.LayeredCharacterSprite>();
            if (layers != null && layers.GetLayers().GetLayer("minecart") == null)
                    layers.AddLayer(LayeredSprites.LayerLibrary.EnumerateLayers("minecart").FirstOrDefault(), LayeredSprites.LayerLibrary.BaseDwarfPalette);
        }

        private void CleanupMinecart()
        {
            var layers = Agent.GetRoot().GetComponent<LayeredSprites.LayeredCharacterSprite>();
            if (layers != null)
                layers.RemoveLayer("minecart");
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
                    DeltaTime += (float)DwarfTime.LastTime.ElapsedGameTime.TotalSeconds;

                    if (status == Status.Fail)
                    {
                        CleanupMinecart();
                        yield return Status.Fail;
                    }
                    else
                        yield return Status.Running;
                }
            }

            Creature.OverrideCharacterMode = false;
            Path = null;
            Agent.GetRoot().SetFlagRecursive(GameComponent.Flag.Visible, true);
            CleanupMinecart();
            yield return Status.Success;
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
                float z = Easing.Ballistic(DeltaTime, jumpTime, 1.0f);
                Vector3 dx = (End - Start) * DeltaTime + Start;
                dx.Y = Start.Y * (1.0f - jumpProgress) + End.Y * jumpProgress + z;
                SetAgentTranslation(dx);
                Agent.Physics.Velocity = new Vector3(JumpDelta.X, (dx.Y - Agent.Physics.Position.Y), JumpDelta.Z);
                yield return Status.Running;
            }

            DeltaTime -= jumpTime;
        }

        private IEnumerable<Status> Translate(Vector3 Start, Vector3 End, float MovementSpeed)
        {
            DeltaTime = 0.0f;
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
        }
    }
}