using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{

    public class TrailerCameraController
    {
        public Vector3 Velocity = Vector3.Zero;
        public float Spin = 0.0f;
        public float Zoom = 0.0f;
        public Timer Duration = new Timer(10.0f, true, Timer.TimerMode.Real);
        private bool _lightActive = false;
        public bool Active = true;

        public void Activate(WorldManager world, OrbitCamera camera)
        {
            Active = true;
            _lightActive = GameSettings.Default.CursorLightEnabled;
            GameSettings.Default.CursorLightEnabled = false;
            world.UserInterface.Gui.RootItem.Hidden = true;
            world.UserInterface.Gui.RootItem.Invalidate();
            
            camera.Control = OrbitCamera.ControlType.Overhead;
            camera.EnableControl = false;
        }

        public void Deactivate(WorldManager world, OrbitCamera camera)
        {
            Active = false;
            GameSettings.Default.CursorLightEnabled = _lightActive;
            world.UserInterface.Gui.RootItem.Hidden = false;
            world.UserInterface.Gui.RootItem.Invalidate();
            camera.Control = OrbitCamera.ControlType.Overhead;
            camera.EnableControl = true;
        }

        public void Update(WorldManager world, OrbitCamera camera)
        {
            float dt = (float)DwarfTime.LastTime.ElapsedRealTime.TotalSeconds;
            Duration.Update(DwarfTime.LastTime);
            Vector3 forward = (camera.Target - camera.Position);
            forward.Normalize();
            Vector3 right = Vector3.Cross(forward, camera.UpVector);
            Vector3 up = Vector3.Cross(right, forward);
            right.Normalize();
            up.Normalize();
            Vector3 vel = Velocity.X * right + Velocity.Y * up - Velocity.Z * forward;
            float l = vel.Length();
            vel.Y = 0;
            
            if (vel.Length() > 0.001f)
            {
                vel.Normalize();
                vel *= l;
            }
            camera.Position += vel * dt;
            camera.Target += vel * dt;
            camera.OverheadUpdate(DwarfTime.LastTime, world.ChunkManager, 0.0f, Spin * dt, Zoom * dt);

            if (Duration.HasTriggered)
            {
                Deactivate(world, camera);
            }
        }
    }

    /// <summary>
    /// This is a particular instantiation of the camera class which can rotate and 
    /// translate around with the mouse and keyboard.
    /// </summary>
    public class OrbitCamera : Camera
    {
        //public float Theta { get; set; }
        //public float Phi { get; set; }
        //public float Radius { get; set; }

        public float CameraMoveSpeed
        {
            get { return GameSettings.Default.CameraScrollSpeed; }
            set { GameSettings.Default.CameraScrollSpeed = value; }
        }

        public float CameraZoomSpeed
        {
            get { return GameSettings.Default.CameraZoomSpeed; }
            set { GameSettings.Default.CameraZoomSpeed = value; }
        }

        public enum ControlType
        {
            Overhead,
            Walk
        }

        public TrailerCameraController TrailerCommand = null;
        private bool isLeftPressed = false;
        private bool isRightPressed = false;
        private readonly Timer moveTimer = new Timer(0.25f, true, Timer.TimerMode.Real);
        private bool shiftPressed = false;
        public Vector3 PushVelocity = Vector3.Zero;
        public ControlType Control = ControlType.Overhead;
        private Point mouseOnRotate = new Point(0, 0);
        public List<Vector3> ZoomTargets { get; set; }


        public bool EnableControl = true;
        public Vector3 AutoTarget { get; set; }
        public bool FollowAutoTarget { get; set; }

        public OrbitCamera() : base()
        {
     
        }

        public OrbitCamera(WorldManager world, Vector3 target, Vector3 position, float fov, float aspectRatio, float nearPlane, float farPlane) :
            base(world, target, position, fov, aspectRatio, nearPlane, farPlane)
        {
            LastWheel = Mouse.GetState().ScrollWheelValue;
            ZoomTargets = new List<Vector3>();
         
        }

        public void Trailer(Vector3 velocity, float spin, float zoom)
        {
            TrailerCommand = new TrailerCameraController()
            {
                Velocity = velocity,
                Spin = spin,
                Zoom = zoom
            };

            TrailerCommand.Activate(World, this);
        }

        public override void Update(DwarfTime time, ChunkManager chunks)
        {
            if (TrailerCommand != null)
            {
                TrailerCommand.Update(World, this);
                if (!TrailerCommand.Active)
                {
                    TrailerCommand = null;
                }
            }

            switch (Control)
            {
              case ControlType.Overhead:
                    OverheadUpdate(time, chunks);
                    break;

                case ControlType.Walk:
                    WalkUpdate(time, chunks);
                    break;
            }
            base.Update(time, chunks);
        }

      
        public void ZoomTo(Vector3 pos)
        {
            ZoomTargets.Clear();
            ZoomTargets.Add(pos);
        }


        private Vector3 ProjectToSurface(Vector3 pos)
        {
            var vox = VoxelHelpers.FindFirstVisibleVoxelOnRay(World.ChunkManager,
                new Vector3(pos.X, World.WorldSizeInVoxels.Y - 1, pos.Z),
                new Vector3(pos.X, 0, pos.Z));

            if (!vox.IsValid) return pos;

            var diffY = (vox.WorldPosition.Y + 0.5f) - pos.Y;
            if (Math.Abs(diffY) > 10)
                diffY = Math.Sign(diffY) * 10;

            return new Vector3(pos.X, pos.Y + diffY, pos.Z);
        }

        private Point mousePrerotate = new Point(0, 0);
        private bool crouched = false;
        public Vector3 Gravity = Vector3.Down * 20;
        private bool flying = false;
        private bool flyKeyPressed = false;
        private bool mouseActiveInWalk = true;

        private GlobalVoxelCoordinate _prevVoxelCoord = new GlobalVoxelCoordinate(0, 0, 0);

        public bool IsMouseActiveInWalk()
        {
            return mouseActiveInWalk;
        }

        public void WalkUpdate(DwarfTime time, ChunkManager chunks)
        {

            {
                var mouseState = Mouse.GetState();
                if (!GameState.Game.GraphicsDevice.Viewport.Bounds.Contains(mouseState.X, mouseState.Y))
                {
                    return;
                }
            }
            // Don't attempt any camera control if the user is trying to type intoa focus item.
            if (World.UserInterface.Gui.FocusItem != null && !World.UserInterface.Gui.FocusItem.IsAnyParentTransparent() && !World.UserInterface.Gui.FocusItem.IsAnyParentHidden())
            {
                return;
            }

            if (GameSettings.Default.FogofWar)
            {
                var currentCoordinate = GlobalVoxelCoordinate.FromVector3(Position);
                if (currentCoordinate != _prevVoxelCoord)
                {
                    VoxelHelpers.RadiusReveal(chunks, new VoxelHandle(chunks, currentCoordinate), 10);
                    _prevVoxelCoord = currentCoordinate;
                }
            }

            float diffPhi = 0;
            float diffTheta = 0;
            Vector3 forward = (Target - Position);
            forward.Normalize();
            Vector3 right = Vector3.Cross(forward, UpVector);
            Vector3 up = Vector3.Cross(right, forward);
            right.Normalize();
            up.Normalize();
            MouseState mouse = Mouse.GetState();
            KeyboardState keys = Keyboard.GetState();
            var bounds = new BoundingBox(World.ChunkManager.Bounds.Min, World.ChunkManager.Bounds.Max + Vector3.UnitY * 20);

            ZoomTargets.Clear();

            Target = MathFunctions.Clamp(Target, bounds);

            float diffX, diffY = 0;
            float dt = (float)time.ElapsedRealTime.TotalSeconds;
            SnapToBounds(new BoundingBox(World.ChunkManager.Bounds.Min, World.ChunkManager.Bounds.Max + Vector3.UnitY * 20));

            bool switchState = false;

            bool isAnyRotationKeyActive = keys.IsKeyDown(ControlSettings.Mappings.CameraMode) ||
                                       keys.IsKeyDown(Keys.RightShift) || Mouse.GetState().MiddleButton == ButtonState.Pressed;
            if (isAnyRotationKeyActive && !shiftPressed)
            {
                shiftPressed = true;
                mouseOnRotate = GameState.Game.GraphicsDevice.Viewport.Bounds.Center;
                mousePrerotate = new Point(mouse.X, mouse.Y);
                switchState = true;
                mouseActiveInWalk = !mouseActiveInWalk;
            }
            else if (!isAnyRotationKeyActive && shiftPressed)
            {
                shiftPressed = false;
            }

            if (shiftPressed)
            {
                Mouse.SetPosition(mousePrerotate.X, mousePrerotate.Y);
                KeyManager.TrueMousePos = new Point(mousePrerotate.X, mousePrerotate.Y);
            }
            else
            {
                KeyManager.TrueMousePos = new Point(mouse.X, mouse.Y);
            }

            if (KeyManager.RotationEnabled(this))
            {
                World.UserInterface.Gui.MouseVisible = false;
                Mouse.SetPosition(mouseOnRotate.X, mouseOnRotate.Y);

                if (!switchState)
                {
                    diffX = mouse.X - mouseOnRotate.X;
                    diffY = mouse.Y - mouseOnRotate.Y;
                }
                else
                {
                    diffX = 0;
                    diffY = 0;
                }

                if (!isLeftPressed && mouse.LeftButton == ButtonState.Pressed)
                {
                    isLeftPressed = true;
                }
                else if (mouse.LeftButton == ButtonState.Released)
                {
                    isLeftPressed = false;
                }

                if (!isRightPressed && mouse.RightButton == ButtonState.Pressed)
                {
                    isRightPressed = true;
                }
                else if (mouse.RightButton == ButtonState.Released)
                {
                    isRightPressed = false;
                }



                if (!isRightPressed)
                {

                    float filterDiffX = (float)(diffX * dt);
                    float filterDiffY = (float)(diffY * dt);

                    diffTheta = (filterDiffX);
                    diffPhi = -(filterDiffY);
                }
                KeyManager.TrueMousePos = mousePrerotate;
            }
            else
            {
                World.UserInterface.Gui.MouseVisible = true;
            }

            Vector3 velocityToSet = Vector3.Zero;

            //if (EnableControl)
            {
                if (keys.IsKeyDown(ControlSettings.Mappings.Forward) || keys.IsKeyDown(Keys.Up))
                {
                    Vector3 mov = forward;
                    mov.Normalize();
                    velocityToSet += mov * CameraMoveSpeed;
                }
                else if (keys.IsKeyDown(ControlSettings.Mappings.Back) || keys.IsKeyDown(Keys.Down))
                {
                    Vector3 mov = forward;
                    mov.Normalize();
                    velocityToSet += -mov * CameraMoveSpeed;
                }

                if (keys.IsKeyDown(ControlSettings.Mappings.Left) || keys.IsKeyDown(Keys.Left))
                {
                    Vector3 mov = right;
                    mov.Normalize();
                    velocityToSet += -mov * CameraMoveSpeed;
                }
                else if (keys.IsKeyDown(ControlSettings.Mappings.Right) || keys.IsKeyDown(Keys.Right))
                {
                    Vector3 mov = right;
                    mov.Normalize();
                    velocityToSet += mov * CameraMoveSpeed;
                }
            }

            if (keys.IsKeyDown(ControlSettings.Mappings.Fly))
            {
                flyKeyPressed = true;
            }
            else
            {
                if (flyKeyPressed)
                {
                    flying = !flying;
                }
                flyKeyPressed = false;
            }

            if (velocityToSet.LengthSquared() > 0)
            {
                if (!flying)
                {
                    float y = Velocity.Y;
                    Velocity = Velocity * 0.5f + 0.5f * velocityToSet;
                    Velocity = new Vector3(Velocity.X, y, Velocity.Z);
                }
                else
                {
                    Velocity = Velocity * 0.5f + 0.5f * velocityToSet;
                }
            }


            LastWheel = mouse.ScrollWheelValue;
            float ymult = flying ? 0.9f : 1.0f;
            Velocity = new Vector3(Velocity.X * 0.9f, Velocity.Y * ymult, Velocity.Z * 0.9f);

            float subSteps = 10.0f;
            float subStepLength = 1.0f / subSteps;

            crouched = false;
            for (int i = 0; i < subSteps; i++)
            {
                VoxelHandle currentVoxel = new VoxelHandle(World.ChunkManager, GlobalVoxelCoordinate.FromVector3(Position));

                var below = VoxelHelpers.GetNeighbor(currentVoxel, new GlobalVoxelOffset(0, -1, 0));
                var above = VoxelHelpers.GetNeighbor(currentVoxel, new GlobalVoxelOffset(0, 1, 0));
                if (above.IsValid && !above.IsEmpty)
                {
                    crouched = true;
                }

                if (!flying)
                {
                    if (!below.IsValid || below.IsEmpty)
                    {
                        Velocity += dt * Gravity * subStepLength;
                    }
                    else if (keys.IsKeyDown(ControlSettings.Mappings.Jump))
                    {
                        Velocity += -dt * Gravity * subStepLength * 4;
                    }

                    if (currentVoxel.IsValid && currentVoxel.LiquidLevel > 0)
                    {
                        Velocity += -dt * Gravity * subStepLength * 0.999f;

                        if (keys.IsKeyDown(ControlSettings.Mappings.Jump))
                        {
                            Velocity += -dt * Gravity * subStepLength * 0.5f;
                        }
                        Velocity *= 0.99f;
                    }
                }

                if (!CollidesWithChunks(World.ChunkManager, Position, true, true, 0.4f, 0.9f))
                {
                    MoveTarget(Velocity * dt * subStepLength);
                    PushVelocity = Vector3.Zero;
                }
                else
                {
                    MoveTarget(Velocity * dt * subStepLength);
                }
            }
            VoxelHandle voxelAfterMove = new VoxelHandle(World.ChunkManager, GlobalVoxelCoordinate.FromVector3(Position));
            if (voxelAfterMove.IsValid && !voxelAfterMove.IsEmpty)
            {
                float distCenter = (voxelAfterMove.GetBoundingBox().Center() - Position).Length();
                if (distCenter < 0.5f)
                {
                    float closest = float.MaxValue;
                    VoxelHandle closestVoxel = VoxelHandle.InvalidHandle;
                    foreach (var voxel in VoxelHelpers.EnumerateAllNeighbors(voxelAfterMove.Coordinate).Select(c => new VoxelHandle(World.ChunkManager, c)).Where(v => v.IsEmpty))
                    {
                        float d = (voxel.GetBoundingBox().Center() - Position).Length();
                        if (d < closest)
                        {
                            closest = d;
                            closestVoxel = voxel;
                        }
                    }

                    if (closestVoxel.IsValid)
                    {
                        var newPosition = closestVoxel.GetBoundingBox().Center();
                        var diff = (newPosition - Position);
                        MoveTarget(diff);
                    }
                }
            }

            Target += right * diffTheta * 0.1f;
            var newTarget = up * diffPhi * 0.1f + Target;
            var newForward = (Target - Position);
            if (Math.Abs(Vector3.Dot(newForward, UpVector)) < 0.99f)
            {
                Target = newTarget;
            }
            var diffTarget = Target - Position;
            diffTarget.Normalize();
            Target = Position + diffTarget * 1.0f;


            UpdateBasisVectors();
            
            UpdateViewMatrix();
        }

        private float _zoomTime = 0;

        public void OverheadUpdate(DwarfTime time, ChunkManager chunks)
        {
            // Don't attempt any camera control if the user is trying to type into a focus item.
            if (World.UserInterface.Gui.FocusItem != null && !World.UserInterface.Gui.FocusItem.IsAnyParentTransparent() && !World.UserInterface.Gui.FocusItem.IsAnyParentHidden())
            {
                return;
            }
            float diffPhi = 0;
            float diffTheta = 0;
            float diffRadius = 0;
            OverheadUpdate(time, chunks, diffPhi, diffTheta, diffRadius);
        }

        public void OverheadUpdate(DwarfTime time, ChunkManager chunks, float diffPhi, float diffTheta, float diffRadius)
        {
            Vector3 forward = (Target - Position);
            forward.Normalize();
            Vector3 right = Vector3.Cross(forward, UpVector);
            Vector3 up = Vector3.Cross(right, forward);
            right.Normalize();
            up.Normalize();
            MouseState mouse = Mouse.GetState();
            KeyboardState keys = Keyboard.GetState();
            var bounds = new BoundingBox(World.ChunkManager.Bounds.Min, World.ChunkManager.Bounds.Max + Vector3.UnitY * 20).Expand(VoxelConstants.ChunkSizeX * 8);
            if (ZoomTargets.Count > 0)
            {
                Vector3 currTarget = MathFunctions.Clamp(ProjectToSurface(ZoomTargets.First()), bounds);
                if (MathFunctions.Dist2D(Target, currTarget) > 5 && _zoomTime < 3)
                {
                    Vector3 newTarget = 0.8f * Target + 0.2f * currTarget;
                    Vector3 d = newTarget - Target;
                    if (bounds.Contains(Target + d) != ContainmentType.Contains)
                    {
                        _zoomTime = 0;
                        ZoomTargets.RemoveAt(0);
                    }
                    else
                    {
                        Target += d;
                        Position += d;
                        _zoomTime += (float)time.ElapsedRealTime.TotalSeconds;
                    }
                }
                else
                {
                    _zoomTime = 0;
                    ZoomTargets.RemoveAt(0);
                }
            }

            Target = MathFunctions.Clamp(Target, bounds);
            int edgePadding = -10000;

            if (GameSettings.Default.EnableEdgeScroll)
            {
                edgePadding = 100;
            }

            float diffX, diffY = 0;
            float dt = (float)time.ElapsedRealTime.TotalSeconds;
            SnapToBounds(bounds);
            if (KeyManager.RotationEnabled(this))
            {
                World.UserInterface.Gui.MouseVisible = false;
                if (!shiftPressed)
                {
                    shiftPressed = true;
                    mouseOnRotate = new Point(mouse.X, mouse.Y);
                    mousePrerotate = new Point(mouse.X, mouse.Y);
                }

                if (!isLeftPressed && mouse.LeftButton == ButtonState.Pressed)
                {
                    isLeftPressed = true;
                }
                else if (mouse.LeftButton == ButtonState.Released)
                {
                    isLeftPressed = false;
                }

                if (!isRightPressed && mouse.RightButton == ButtonState.Pressed)
                {
                    isRightPressed = true;
                }
                else if (mouse.RightButton == ButtonState.Released)
                {
                    isRightPressed = false;
                }

                Mouse.SetPosition(mouseOnRotate.X, mouseOnRotate.Y);

                diffX = mouse.X - mouseOnRotate.X;
                diffY = mouse.Y - mouseOnRotate.Y;


                if (!isRightPressed)
                {

                    float filterDiffX = (float)(diffX * dt);
                    float filterDiffY = (float)(diffY * dt);

                    diffTheta = (filterDiffX);
                    diffPhi = -(filterDiffY);
                }
                KeyManager.TrueMousePos = mousePrerotate;
            }
            else
            {
                if (shiftPressed)
                {
                    Mouse.SetPosition(mousePrerotate.X, mousePrerotate.Y);
                    KeyManager.TrueMousePos = new Point(mousePrerotate.X, mousePrerotate.Y);
                }
                else
                {
                    KeyManager.TrueMousePos = new Point(mouse.X, mouse.Y);
                }
                shiftPressed = false;
                World.UserInterface.Gui.MouseVisible = true;
            }

            Vector3 velocityToSet = Vector3.Zero;

            //if (EnableControl)
            {
                if (keys.IsKeyDown(ControlSettings.Mappings.Forward) || keys.IsKeyDown(Keys.Up))
                {
                    Vector3 mov = forward;
                    mov.Y = 0;
                    mov.Normalize();
                    velocityToSet += mov * CameraMoveSpeed * dt;
                }
                else if (keys.IsKeyDown(ControlSettings.Mappings.Back) || keys.IsKeyDown(Keys.Down))
                {
                    Vector3 mov = forward;
                    mov.Y = 0;
                    mov.Normalize();
                    velocityToSet += -mov * CameraMoveSpeed * dt;
                }

                if (keys.IsKeyDown(ControlSettings.Mappings.Left) || keys.IsKeyDown(Keys.Left))
                {
                    Vector3 mov = right;
                    mov.Y = 0;
                    mov.Normalize();
                    velocityToSet += -mov * CameraMoveSpeed * dt;
                }
                else if (keys.IsKeyDown(ControlSettings.Mappings.Right) || keys.IsKeyDown(Keys.Right))
                {
                    Vector3 mov = right;
                    mov.Y = 0;
                    mov.Normalize();
                    velocityToSet += mov * CameraMoveSpeed * dt;
                }
            }
            //else 
            if (FollowAutoTarget)
            {
                Vector3 prevTarget = Target;
                float damper = MathFunctions.Clamp((Target - AutoTarget).Length() - 5, 0, 1);
                float smooth = 0.1f * damper;
                Target = AutoTarget * (smooth) + Target * (1.0f - smooth);
                Position += (Target - prevTarget);
            }

            if (velocityToSet.LengthSquared() > 0)
            {
                World.Tutorial("camera");
                Velocity = velocityToSet;
            }


            if (!KeyManager.RotationEnabled(this))
            {
                if (!World.UserInterface.IsMouseOverGui)
                {

                    if (mouse.X < edgePadding || mouse.X > GameState.Game.GraphicsDevice.Viewport.Width - edgePadding)
                    {
                        moveTimer.Update(time);
                        if (moveTimer.HasTriggered)
                        {
                            float dir = 0.0f;

                            if (mouse.X < edgePadding)
                            {
                                dir = edgePadding - mouse.X;
                            }
                            else
                            {
                                dir = (GameState.Game.GraphicsDevice.Viewport.Width - edgePadding) - mouse.X;
                            }

                            dir *= 0.01f;
                            Vector3 delta = right * CameraMoveSpeed * dir * dt;
                            delta.Y = 0;
                            Velocity = -delta;
                        }
                    }
                    else if (mouse.Y < edgePadding ||
                             mouse.Y > GameState.Game.GraphicsDevice.Viewport.Height - edgePadding)
                    {
                        moveTimer.Update(time);
                        if (moveTimer.HasTriggered)
                        {
                            float dir = 0.0f;

                            if (mouse.Y < edgePadding)
                            {
                                dir = -(edgePadding - mouse.Y);
                            }
                            else
                            {
                                dir = -((GameState.Game.GraphicsDevice.Viewport.Height - edgePadding) - mouse.Y);
                            }

                            dir *= 0.01f;

                            Vector3 delta = up * CameraMoveSpeed * dir * dt;
                            delta.Y = 0;
                            Velocity = -delta;
                        }
                    }
                    else
                    {
                        moveTimer.Reset(moveTimer.TargetTimeSeconds);
                    }
                }
            }

            int scroll = mouse.ScrollWheelValue;

            if (isRightPressed && KeyManager.RotationEnabled(this))
            {
                scroll = (int)(diffY * 10) + LastWheel;
            }

            if (scroll != LastWheel && !World.UserInterface.IsMouseOverGui)
            {
                int change = scroll - LastWheel;

                if (!(keys.IsKeyDown(Keys.LeftAlt) || keys.IsKeyDown(Keys.RightAlt)))
                {
                    if (!keys.IsKeyDown(Keys.LeftControl))
                    {
                        var delta = change * -1;

                        if (GameSettings.Default.InvertZoom)
                        {
                            delta *= -1;
                        }

                        diffRadius = delta * CameraZoomSpeed * dt;

                        if (diffRadius < 0 && !FollowAutoTarget && GameSettings.Default.ZoomCameraTowardMouse && !shiftPressed)
                        {
                            float diffxy =
                                (new Vector3(Target.X, 0, Target.Z) -
                                 new Vector3(World.Renderer.CursorLightPos.X, 0, World.Renderer.CursorLightPos.Z)).Length();

                            if (diffxy > 5)
                            {
                                Vector3 slewTarget = Target * 0.9f + World.Renderer.CursorLightPos * 0.1f;
                                Vector3 slewDiff = slewTarget - Target;
                                Target += slewDiff;
                                Position += slewDiff;
                            }
                        }
                    }
                    else
                    {
                        World.Renderer.SetMaxViewingLevel(World.Renderer.PersistentSettings.MaxViewingLevel + (int)((float)change * 0.01f));
                    }
                }
            }

            LastWheel = mouse.ScrollWheelValue;

            if (!CollidesWithChunks(World.ChunkManager, Position + Velocity, false, false, 0.5f, 1.0f))
            {
                MoveTarget(Velocity);
                PushVelocity = Vector3.Zero;
            }
            else
            {
                PushVelocity += Vector3.Up * 0.1f;
                Position += PushVelocity;
            }


            Velocity *= 0.8f;
            UpdateBasisVectors();

            bool projectTarget = GameSettings.Default.CameraFollowSurface || (!GameSettings.Default.CameraFollowSurface && (keys.IsKeyDown(Keys.LeftControl) || keys.IsKeyDown(Keys.RightControl)));
            Vector3 projectedTarget = projectTarget ? ProjectToSurface(Target) : Target;
            Vector3 diffTarget = projectedTarget - Target;
            if (diffTarget.LengthSquared() > 25)
            {
                diffTarget.Normalize();
                diffTarget *= 5;
            }
            Position = (Position + diffTarget) * 0.05f + Position * 0.95f;
            Target = (Target + diffTarget) * 0.05f + Target * 0.95f;
            float currRadius = (Position - Target).Length();
            float newRadius = Math.Max(currRadius + diffRadius, 3.0f);
            Position = MathFunctions.ProjectOutOfHalfPlane(MathFunctions.ProjectOutOfCylinder(MathFunctions.ProjectToSphere(Position - right * diffTheta * 2 - up * diffPhi * 2, newRadius, Target), Target, 3.0f), Target, 2.0f);
            UpdateViewMatrix();
        }

        public void MoveTarget(Vector3 delta)
        {
            Target += delta;
            Position += delta;
        }

        public void UpdateBasisVectors()
        {
            // Does nothing in this camera...
        }
        private Vector3 Noise = Vector3.Zero;
        private float HeightOffset = 0.0f;
        public override void UpdateViewMatrix()
        {
            if (this.Control == ControlType.Walk)
            {
                float heightOffset = crouched ? 0.0f : 0.5f;
                HeightOffset = heightOffset * 0.1f + HeightOffset * 0.9f;
                var undistorted = Position + Vector3.UnitY * heightOffset;
                var distorted = VertexNoise.Warp(undistorted);
                var noise = distorted - undistorted;
                Noise = noise * 0.1f + Noise * 0.9f;
                ViewMatrix = Matrix.CreateLookAt(Position + Vector3.UnitY * HeightOffset + Noise, FollowAutoTarget ? (AutoTarget * 0.5f + Target * 0.5f) : Target + Vector3.UnitY * HeightOffset + Noise, Vector3.UnitY);
            }
            else
            {
                ViewMatrix = Matrix.CreateLookAt(Position, FollowAutoTarget ? (AutoTarget * 0.5f + Target * 0.5f) : Target, Vector3.UnitY);
            }
        }

        private bool IsNeighborOccupied(VoxelHandle voxel, int x, int y, int z)
        {
            VoxelHandle neighbor = VoxelHelpers.GetNeighbor(voxel, new GlobalVoxelOffset(x, y, z));
            return (neighbor.IsValid && !(neighbor.IsEmpty || !neighbor.IsVisible));
        }

        public bool Collide(VoxelHandle voxel, BoundingBox myBox, BoundingBox box)
        {
            if (!myBox.Intersects(box))
            {
                return false;
            }

            Physics.Contact contact = new Physics.Contact();

            bool testX = !IsNeighborOccupied(voxel, -1, 0, 0) || !IsNeighborOccupied(voxel, 1, 0, 0);
            bool testY = !IsNeighborOccupied(voxel, 0, -1, 0) || !IsNeighborOccupied(voxel, 0, 1, 0);
            bool testZ = !IsNeighborOccupied(voxel, 0, 0, -1) || !IsNeighborOccupied(voxel, 0, 0, 1);
            if (!Physics.TestStaticAABBAABB(myBox, box, ref contact, testX, testY, testZ))
            {
                return false;
            }

            Vector3 p = Target;
            p += contact.NEnter * contact.Penetration;
            Velocity = Velocity - Vector3.Dot(Velocity, contact.NEnter) * contact.NEnter;
            Target = p;
            Position = Position + contact.NEnter * contact.Penetration;

            return true;
        }

        public void SnapToBounds(BoundingBox bounds)
        {
            Vector3 clampTarget = MathFunctions.Clamp(Target, bounds.Expand(-2.0f));
            Vector3 clampPosition = MathFunctions.Clamp(Position, bounds.Expand(-2.0f));
            Vector3 dTarget = clampTarget - Target;
            Vector3 dPosition = clampPosition - Position;
            var newTarget = Target + dTarget + dPosition;
            var newPosition = Position + dTarget + dPosition;
            Target = 0.95f * Target + 0.05f * newTarget;
            Position = 0.95f * Position + 0.05f * newPosition;
        }

        public bool CollidesWithChunks(ChunkManager chunks, Vector3 pos, bool applyForce, bool allowInvisible, float size=0.5f, float height=2.0f)
        {
            var box = new BoundingBox(pos - new Vector3(size, height * 0.5f, size), pos + new Vector3(size, height * 0.5f, size));
            bool gotCollision = false;
            
            foreach (var v in VoxelHelpers.EnumerateCube(GlobalVoxelCoordinate.FromVector3(pos))
                .Select(n => new VoxelHandle(chunks, n)))                
            {
                if (!v.IsValid) continue;
                if (v.IsEmpty) continue;
                if (!allowInvisible && !v.IsVisible) continue;

                var voxAABB = v.GetBoundingBox();
                if (box.Intersects(voxAABB))
                {
                    gotCollision = true;
                    if (applyForce)
                    {
                        Collide(v, box, voxAABB);
                    }
                    else
                        return true;
                }
            }

            return gotCollision;
        }



        public Vector3 GetForwardVector()
        {
            Vector3 forward = (Target - Position);
            forward.Normalize();
            return forward;
        }

        public Vector3 GetRightVector()
        {
            Vector3 forward = (Target - Position);
            forward.Normalize();
            Vector3 right = Vector3.Cross(forward, UpVector);
            right.Normalize();
            return right;
        }
    }

}