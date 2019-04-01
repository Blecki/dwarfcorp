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
                /*
                case MoveType.WaitForElevator:
                    if (action.SourceState.Elevator == null || action.SourceState.Elevator.IsDead)
                        yield return Status.Fail;

                    action.SourceState.Elevator.EnterQueue(Agent);

                    while (!action.SourceState.Elevator.ReadyToBoard(Agent))
                        yield return Status.Running;

                    break;

                case MoveType.EnterElevator:
                    if (action.SourceState.Elevator == null || action.SourceState.Elevator.IsDead)
                        yield return Status.Fail;
                    
                    if (t < 0.5f)
                        Creature.NoiseMaker.MakeNoise("Jump", Agent.Position, false);

                    Creature.OverrideCharacterMode = false;
                    Creature.CurrentCharacterMode = Creature.Physics.Velocity.Y > 0 ? CharacterMode.Jumping  : CharacterMode.Falling;
                    if (hasNextAction)
                    {
                        float z = Easing.Ballistic(t, 1.0f, 1.0f);
                        Vector3 start = currPosition;
                        Vector3 end = nextPosition + Vector3.Up * 0.5f;
                        Vector3 dx = (end - start) * t + start;
                        dx.Y = start.Y * (1 - t) + end.Y * (t) + z;
                        transform.Translation = dx;
                        Agent.Physics.Velocity = new Vector3(diff.X, (dx.Y - Agent.Physics.Position.Y), diff.Z);
                    }
                    else
                        transform.Translation = currPosition;

                    action.SourceState.Elevator.Board(Agent);

                    break;

                case MoveType.ExitElevator:
                    if (action.SourceState.Elevator != null)
                        action.SourceState.Elevator.Disembark(Agent);
                    transform.Translation = currPosition;

                    break;

                case MoveType.RideElevator:

                    CleanupMinecart();
                    Creature.OverrideCharacterMode = false;
                    Creature.CurrentCharacterMode = CharacterMode.Walking;
                    if (hasNextAction)
                    {
                        transform.Translation = diff * t + currPosition;
                        Agent.Physics.Velocity = diff;
                    }
                    else
                    {
                        transform.Translation = currPosition;
                    }

                    break;
                    */

                case MoveType.EnterVehicle:

                    Creature.NoiseMaker.MakeNoise("Jump", Agent.Position, false);
                    Creature.OverrideCharacterMode = false;

                    foreach (var bit in Jump(Step.SourceVoxel.Center, Step.DestinationVoxel.Center, Step.DestinationVoxel.Center - Step.SourceVoxel.Center, actionSpeed))
                    {
                        Creature.CurrentCharacterMode = Creature.Physics.Velocity.Y > 0 ? CharacterMode.Jumping : CharacterMode.Falling;
                        yield return Status.Running;
                    }

                    SetupMinecart();

                    break;

                case MoveType.ExitVehicle:
                    CleanupMinecart();
                    {
                        var transform = Agent.Physics.LocalTransform;
                        transform.Translation = Step.DestinationVoxel.Center;
                        Agent.Physics.LocalTransform = transform;
                    }
                    break;

                case MoveType.RideVehicle:
                    SetupMinecart();
                    Creature.CurrentCharacterMode = CharacterMode.Minecart;
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
                        yield return Status.Running;
                    }

                    break;

                case MoveType.Walk:
                    CleanupMinecart();
                    Creature.OverrideCharacterMode = false;
                    Creature.CurrentCharacterMode = CharacterMode.Walking;
                    foreach (var bit in Translate(Agent.Position, Step.DestinationVoxel.Center, actionSpeed))
                        yield return Status.Running;
                    break;

                case MoveType.Swim:
                    CleanupMinecart();
                    Creature.OverrideCharacterMode = false;
                    Creature.CurrentCharacterMode = CharacterMode.Swimming;
                    foreach (var bit in Translate(Step.SourceVoxel.Center, Step.DestinationVoxel.Center + (0.5f * Vector3.Up * Agent.Physics.BoundingBox.Extents().Y), actionSpeed))
                    {
                        Creature.NoiseMaker.MakeNoise("Swim", Agent.Position, true);

                        yield return Status.Running;
                    }

                    break;

                case MoveType.Jump:
                    CleanupMinecart();
                    Creature.NoiseMaker.MakeNoise("Jump", Agent.Position, false);
                    Creature.OverrideCharacterMode = false;

                    foreach (var bit in Jump(Step.SourceVoxel.Center, Step.DestinationVoxel.Center, Step.DestinationVoxel.Center - Step.SourceVoxel.Center, actionSpeed))
                    {
                        Creature.CurrentCharacterMode = Creature.Physics.Velocity.Y > 0 ? CharacterMode.Jumping : CharacterMode.Falling;
                        yield return Status.Running;
                    }

                    break;

                case MoveType.Fall:
                    CleanupMinecart();
                    Creature.OverrideCharacterMode = false;
                    Creature.CurrentCharacterMode = CharacterMode.Falling;
                    foreach (var bit in Translate(Step.SourceVoxel.Center, Step.DestinationVoxel.Center, actionSpeed))
                        yield return Status.Running;
                    break;

                case MoveType.Climb:
                    CleanupMinecart();

                    foreach (var bit in Translate(Step.SourceVoxel.Center, Step.DestinationVoxel.Center, actionSpeed))
                    {
                        if (Step.InteractObject == null || Step.InteractObject.IsDead)
                            yield return Status.Fail;
                        Creature.NoiseMaker.MakeNoise("Climb", Agent.Position, false);
                        Creature.OverrideCharacterMode = false;
                        Creature.CurrentCharacterMode = CharacterMode.Climbing;
                        yield return Status.Running;
                    }

                    break;

                case MoveType.ClimbWalls:

                    CleanupMinecart();

                    foreach (var bit in Translate(Step.SourceVoxel.Center, Step.DestinationVoxel.Center, actionSpeed))
                    {
                        //if (Step.InteractObject == null || Step.InteractObject.IsDead)
                        //    yield return Status.Fail;
                        Creature.NoiseMaker.MakeNoise("Climb", Agent.Position, false);
                        Creature.OverrideCharacterMode = false;
                        Creature.CurrentCharacterMode = CharacterMode.Climbing;
                        yield return Status.Running;
                    }

                    break;
                case MoveType.Fly:

                    CleanupMinecart();
                    Creature.OverrideCharacterMode = false;
                    Creature.CurrentCharacterMode = CharacterMode.Flying;
                    foreach (var bit in Translate(Step.SourceVoxel.Center, Step.DestinationVoxel.Center, actionSpeed))
                    {
                        if ((int)(DeltaTime * 100) % 2 == 0)
                            Creature.NoiseMaker.MakeNoise("Flap", Agent.Position, false);
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
                //DeltaTime = (float)DwarfTime.LastTime.ElapsedGameTime.TotalSeconds;

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