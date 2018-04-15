// OrbitCamera.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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

        private bool isLeftPressed = false;
        private bool isRightPressed = false;
        private readonly Timer moveTimer = new Timer(0.25f, true, Timer.TimerMode.Real);
        private bool shiftPressed = false;
        public Vector3 PushVelocity = Vector3.Zero;
        public ControlType Control = ControlType.Overhead;
        private Point mouseOnRotate = new Point(0, 0);
        public List<Vector3> ZoomTargets { get; set; }


        public bool EnableControl { get; set; }
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


        public override void Update(DwarfTime time, ChunkManager chunks)
        {
            switch (Control)
            {
              case ControlType.Overhead:
                    OverheadUpdate(time, chunks);
                    break;

                case ControlType.Walk:
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
            var vox = VoxelHelpers.FindFirstVisibleVoxelOnRay(
                World.ChunkManager.ChunkData,
                new Vector3(pos.X, VoxelConstants.ChunkSizeY - 1, pos.Z),
                new Vector3(pos.X, 0, pos.Z));
            if (!vox.IsValid) return pos;
            return new Vector3(pos.X, vox.WorldPosition.Y + 0.5f, pos.Z);
        }

        private Point mousePrerotate = new Point(0, 0);

        public void OverheadUpdate(DwarfTime time, ChunkManager chunks)
        {
            // Don't attempt any camera control if the user is trying to type intoa focus item.
            if (World.Gui.FocusItem != null && !World.Gui.FocusItem.IsAnyParentTransparent() && !World.Gui.FocusItem.IsAnyParentHidden())
            {
                return;
            }
            float diffPhi = 0;
            float diffTheta = 0;
            float diffRadius = 0;
            Vector3 forward = (Target - Position);
            forward.Normalize();
            Vector3 right = Vector3.Cross(forward, UpVector);
            Vector3 up = Vector3.Cross(right, forward);
            right.Normalize();
            up.Normalize();
            MouseState mouse = Mouse.GetState();
            KeyboardState keys = Keyboard.GetState();
            var bounds = new BoundingBox(World.ChunkManager.Bounds.Min, World.ChunkManager.Bounds.Max + Vector3.UnitY * 20);
            if (ZoomTargets.Count > 0)
            {
                Vector3 currTarget = MathFunctions.Clamp(ProjectToSurface(ZoomTargets.First()), bounds);
                if (Vector3.DistanceSquared(Target, currTarget) > 5)
                {
                    Vector3 newTarget = 0.8f * Target + 0.2f * currTarget;
                    Vector3 d = newTarget - Target;
                    Target += d;
                    Position += d;
                }
                else
                {
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
            SnapToBounds(new BoundingBox(World.ChunkManager.Bounds.Min, World.ChunkManager.Bounds.Max + Vector3.UnitY * 20));
            if (KeyManager.RotationEnabled())
            {
                World.Gui.MouseVisible = false;
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

                    float filterDiffX = (float) (diffX*dt);
                    float filterDiffY = (float) (diffY*dt);

                    diffTheta = (filterDiffX);
                    diffPhi = - (filterDiffY);
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
                World.Gui.MouseVisible = true;
            }

            Vector3 velocityToSet = Vector3.Zero;

            if (EnableControl)
            {
                if (keys.IsKeyDown(ControlSettings.Mappings.Forward) || keys.IsKeyDown(Keys.Up))
                {
                    Vector3 mov = forward;
                    mov.Y = 0;
                    mov.Normalize();
                    velocityToSet += mov*CameraMoveSpeed*dt;
                }
                else if (keys.IsKeyDown(ControlSettings.Mappings.Back) || keys.IsKeyDown(Keys.Down))
                {
                    Vector3 mov = forward;
                    mov.Y = 0;
                    mov.Normalize();
                    velocityToSet += -mov*CameraMoveSpeed*dt;
                }

                if (keys.IsKeyDown(ControlSettings.Mappings.Left) || keys.IsKeyDown(Keys.Left))
                {
                    Vector3 mov = right;
                    mov.Y = 0;
                    mov.Normalize();
                    velocityToSet += -mov*CameraMoveSpeed*dt;
                }
                else if (keys.IsKeyDown(ControlSettings.Mappings.Right) || keys.IsKeyDown(Keys.Right))
                {
                    Vector3 mov = right;
                    mov.Y = 0;
                    mov.Normalize();
                    velocityToSet += mov*CameraMoveSpeed*dt;
                }
            }
            else if (FollowAutoTarget)
            {
                Vector3 prevTarget = Target;
                float damper = MathFunctions.Clamp((Target - AutoTarget).Length() - 5, 0, 1);
                float smooth = 0.1f*damper;
                Target = AutoTarget * (smooth) + Target * (1.0f - smooth);
                Position += (Target - prevTarget);
            }

            if (velocityToSet.LengthSquared() > 0)
            {
                World.Tutorial("camera");
                Velocity = velocityToSet;
            }


            if (!KeyManager.RotationEnabled())
            {
                if (!World.IsMouseOverGui)
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
                            Vector3 delta = right*CameraMoveSpeed*dir*dt;
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

                            Vector3 delta = up*CameraMoveSpeed*dir*dt;
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

            if (isRightPressed && KeyManager.RotationEnabled())
            {
                scroll = (int)(diffY * 10) + LastWheel;
            }

            if (scroll != LastWheel && !World.IsMouseOverGui)
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
                                 new Vector3(World.CursorLightPos.X, 0, World.CursorLightPos.Z)).Length();

                            if (diffxy > 5)
                            {
                                Vector3 slewTarget = Target*0.9f + World.CursorLightPos*0.1f;
                                Vector3 slewDiff = slewTarget - Target;
                                Target += slewDiff;
                                Position += slewDiff;
                            }
                        }
                    }
                    else
                    {
                        World.Master.SetMaxViewingLevel(World.Master.MaxViewingLevel + (int)((float)change * 0.01f), ChunkManager.SliceMode.Y);
                    }
                }
            }

            LastWheel = mouse.ScrollWheelValue;

            if (!CollidesWithChunks(World.ChunkManager, Position + Velocity, false))
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
            Vector3 projectedTarget = GameSettings.Default.CameraFollowSurface ? ProjectToSurface(Target) : Target;
            if (!GameSettings.Default.CameraFollowSurface && (keys.IsKeyDown(Keys.LeftControl) || keys.IsKeyDown(Keys.RightControl)))
            {
                projectedTarget = ProjectToSurface(Target);
            }
            Vector3 diffTarget = projectedTarget - Target;
            Position = (Position + diffTarget) * 0.05f + Position * 0.95f;
            Target = projectedTarget * 0.05f + Target * 0.95f;
            float currRadius = (Position - Target).Length();
            float newRadius = Math.Max(currRadius + diffRadius, 3.0f);
            Position = MathFunctions.ProjectOutOfHalfPlane(MathFunctions.ProjectOutOfCylinder(MathFunctions.ProjectToSphere(Position - right*diffTheta * 2 - up*diffPhi * 2, newRadius, Target), Target, 3.0f), Target, 2.0f);
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

        public override void UpdateViewMatrix()
        {
            ViewMatrix = Matrix.CreateLookAt(Position, FollowAutoTarget ? (AutoTarget * 0.5f + Target * 0.5f) : Target, Vector3.UnitY);
        }

        public bool Collide(BoundingBox myBox, BoundingBox box)
        {
            if (!myBox.Intersects(box))
            {
                return false;
            }

            Physics.Contact contact = new Physics.Contact();

            if (!Physics.TestStaticAABBAABB(box, box, ref contact))
            {
                return false;
            }

            Vector3 p = Target;
            p += contact.NEnter * contact.Penetration;

            Vector3 newVelocity = (contact.NEnter * Vector3.Dot(Velocity, contact.NEnter));
            Velocity = (Velocity - newVelocity);

            Target = p;

            return true;
        }

        public void SnapToBounds(BoundingBox bounds)
        {
            Vector3 clampTarget = MathFunctions.Clamp(Target, bounds.Expand(-2.0f));
            Vector3 clampPosition = MathFunctions.Clamp(Position, bounds.Expand(-2.0f));
            Vector3 dTarget = clampTarget - Target;
            Vector3 dPosition = clampPosition - Position;
            Position += dTarget + dPosition;
            Target += dTarget + dPosition;
        }

        public bool CollidesWithChunks(ChunkManager chunks, Vector3 pos, bool applyForce)
        {
            var box = new BoundingBox(pos - new Vector3(0.5f, 0.5f, 0.5f), pos + new Vector3(0.5f, 0.5f, 0.5f));
            bool gotCollision = false;

            foreach (var v in VoxelHelpers.EnumerateCube(GlobalVoxelCoordinate.FromVector3(pos))
                .Select(n => new VoxelHandle(chunks.ChunkData, n)))                
            {
                if (!v.IsValid) continue;
                if (v.IsEmpty) continue;
                if (!v.IsVisible) continue;

                var voxAABB = v.GetBoundingBox();
                if (box.Intersects(voxAABB))
                {
                    gotCollision = true;
                    if (applyForce)
                        Collide(box, voxAABB);
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