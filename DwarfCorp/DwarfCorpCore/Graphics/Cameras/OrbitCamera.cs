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
        public float Theta { get; set; }
        public float Phi { get; set; }
        public float Radius { get; set; }

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

        public Physics PhysicsObject { get; set; }
        private bool isLeftPressed = false;
        private bool isRightPressed = false;
        private readonly Timer moveTimer = new Timer(0.25f, true);
        private float targetTheta = 0.0f;
        private float targetPhi = 0.0f;
        private bool shiftPressed = false;
        public Vector3 PushVelocity = Vector3.Zero;
        public ControlType Control = ControlType.Overhead;

        public List<Vector3> ZoomTargets { get; set; } 
       
        public OrbitCamera() : base()
        {
            
        }

        public OrbitCamera(float theta, float phi, float radius, Vector3 target, Vector3 position, float fov, float aspectRatio, float nearPlane, float farPlane) :
            base(target, position, fov, aspectRatio, nearPlane, farPlane)
        {
            Theta = theta;
            Phi = phi;
            Radius = radius;
            LastWheel = Mouse.GetState().ScrollWheelValue;
            targetTheta = theta;
            targetPhi = phi;
            ZoomTargets = new List<Vector3>();
        }


        public void SetTargetRotation(float theta, float phi)
        {
            targetTheta = theta;
            targetPhi = phi;
        }

        public override void Update(DwarfTime time, ChunkManager chunks)
        {
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

        private void WalkUpdate(DwarfTime time, ChunkManager chunks)
        {
            MouseState mouse = Mouse.GetState();
            KeyboardState keys = Keyboard.GetState();


            if (PhysicsObject == null)
            {
                PhysicsObject = new Physics("CameraPhysics", WorldManager.ComponentManager.RootComponent, Matrix.CreateTranslation(Target), new Vector3(0.5f, 0.5f, 0.5f), Vector3.Zero, 1.0f, 1.0f, 0.999f, 1.0f, Vector3.Down * 10);
                PhysicsObject.IsSleeping = false;
                PhysicsObject.Velocity = Vector3.Down*0.01f;
            }

            bool stateChanged = false;
            float dt = (float)time.ElapsedGameTime.TotalSeconds;

            if (KeyManager.RotationEnabled())
            {
                if (!shiftPressed)
                {
                    shiftPressed = true;

                    mouse = Mouse.GetState();
                    stateChanged = true;
                }
                if (!isLeftPressed && mouse.LeftButton == ButtonState.Pressed)
                {
                    isLeftPressed = true;
                    stateChanged = true;
                }
                else if (mouse.LeftButton == ButtonState.Released)
                {
                    isLeftPressed = false;
                }

                if (!isRightPressed && mouse.RightButton == ButtonState.Pressed)
                {
                    isRightPressed = true;
                    stateChanged = true;
                }
                else if (mouse.RightButton == ButtonState.Released)
                {
                    isRightPressed = false;
                }


                if (stateChanged)
                {
                    Mouse.SetPosition(GameState.Game.GraphicsDevice.Viewport.Width / 2, GameState.Game.GraphicsDevice.Viewport.Height / 2);
                    mouse = Mouse.GetState();
                }


                float diffX = mouse.X - GameState.Game.GraphicsDevice.Viewport.Width / 2;
                float diffY = mouse.Y - GameState.Game.GraphicsDevice.Viewport.Height / 2;


                float filterDiffX = (float)(diffX * dt);
                float filterDiffY = (float)(diffY * dt);
                if (Math.Abs(filterDiffX) > 1.0f)
                {
                    filterDiffX = 1.0f * Math.Sign(filterDiffX);
                }

                if (Math.Abs(filterDiffY) > 1.0f)
                {
                    filterDiffY = 1.0f * Math.Sign(filterDiffY);
                }

                targetTheta = Theta - (filterDiffX);
                targetPhi = Phi - (filterDiffY);
                Theta = targetTheta * 0.5f + Theta * 0.5f;
                Phi = targetPhi * 0.5f + Phi * 0.5f;


                if (Phi < -1.5f)
                {
                    Phi = -1.5f;
                }
                else if (Phi > 1.5f)
                {
                    Phi = 1.5f;
                }
            }
            else
            {
                shiftPressed = false;
            }

            Vector3 velocityToSet = Vector3.Zero;
            if (keys.IsKeyDown(ControlSettings.Mappings.Forward) || keys.IsKeyDown(Keys.Up))
            {
                Vector3 forward = (Target - Position);
                forward.Normalize();

                velocityToSet += forward * CameraMoveSpeed * dt;
            }
            else if (keys.IsKeyDown(ControlSettings.Mappings.Back) || keys.IsKeyDown(Keys.Down))
            {
                Vector3 forward = (Target - Position);
                forward.Normalize();
                velocityToSet += -forward * CameraMoveSpeed * dt;
            }

            if (keys.IsKeyDown(ControlSettings.Mappings.Left) || keys.IsKeyDown(Keys.Left))
            {
                Vector3 forward = (Target - Position);
                forward.Normalize();
                Vector3 right = Vector3.Cross(forward, UpVector);
                right.Normalize();
                velocityToSet += -right * CameraMoveSpeed * dt;
            }
            else if (keys.IsKeyDown(ControlSettings.Mappings.Right) || keys.IsKeyDown(Keys.Right))
            {
                Vector3 forward = (Target - Position);
                forward.Normalize();
                Vector3 right = Vector3.Cross(forward, UpVector);
                right.Normalize();
                velocityToSet += right * CameraMoveSpeed * dt;
            }

            if (velocityToSet.LengthSquared() > 0)
            {
                Velocity = Velocity * 0.5f + velocityToSet * 0.5f;
            }



            LastWheel = mouse.ScrollWheelValue;

            Velocity = new Vector3(Velocity.X, 0, Velocity.Z);


            if (keys.IsKeyDown(Keys.Space))
            {
                Velocity += Vector3.Up;
            }

            //CollidesWithChunks(PlayState.ChunkManager, Target, true);
            PhysicsObject.ApplyForce(Velocity * 20, dt);
         
            Target = PhysicsObject.GlobalTransform.Translation + Vector3.Up * 0.5f;
            Velocity *= 0.8f;
            UpdateBasisVectors();
        }

        public void ZoomTo(Vector3 pos)
        {
            ZoomTargets.Clear();
            ZoomTargets.Add(pos);
        }


        public void OverheadUpdate(DwarfTime time, ChunkManager chunks)
        {
            MouseState mouse = Mouse.GetState();
            KeyboardState keys = Keyboard.GetState();

            if (ZoomTargets.Count > 0)
            {
                Vector3 currTarget = ZoomTargets.First();
                if (Vector3.DistanceSquared(Target, currTarget) > 5)
                {
                    Target = 0.8f * Target + 0.2f * currTarget;
                }
                else
                {
                    ZoomTargets.RemoveAt(0);
                }
            }

            int edgePadding = -10000;

            if (GameSettings.Default.EnableEdgeScroll)
            {
                edgePadding = 100;
            }

            bool stateChanged = false;
            float dt = (float)time.ElapsedRealTime.TotalSeconds;
            SnapToBounds(new BoundingBox(WorldManager.ChunkManager.Bounds.Min, WorldManager.ChunkManager.Bounds.Max + Vector3.UnitY * 20));
            if (KeyManager.RotationEnabled())
            {
                if (!shiftPressed)
                {
                    shiftPressed = true;

                    mouse = Mouse.GetState();
                    stateChanged = true;
                }
                if (!isLeftPressed && mouse.LeftButton == ButtonState.Pressed)
                {
                    isLeftPressed = true;
                    stateChanged = true;
                }
                else if (mouse.LeftButton == ButtonState.Released)
                {
                    isLeftPressed = false;
                }

                if (!isRightPressed && mouse.RightButton == ButtonState.Pressed)
                {
                    isRightPressed = true;
                    stateChanged = true;
                }
                else if (mouse.RightButton == ButtonState.Released)
                {
                    isRightPressed = false;
                }


                if (stateChanged)
                {
                    Mouse.SetPosition(GameState.Game.GraphicsDevice.Viewport.Width / 2, GameState.Game.GraphicsDevice.Viewport.Height / 2);
                    mouse = Mouse.GetState();
                }


                float diffX = mouse.X - GameState.Game.GraphicsDevice.Viewport.Width / 2;
                float diffY = mouse.Y - GameState.Game.GraphicsDevice.Viewport.Height / 2;


                float filterDiffX = (float)(diffX * dt);
                float filterDiffY = (float)(diffY * dt);
                if (Math.Abs(filterDiffX) > 1.0f)
                {
                    filterDiffX = 1.0f * Math.Sign(filterDiffX);
                }

                if (Math.Abs(filterDiffY) > 1.0f)
                {
                    filterDiffY = 1.0f * Math.Sign(filterDiffY);
                }

                targetTheta = Theta - (filterDiffX);
                targetPhi = Phi - (filterDiffY);
                Theta = targetTheta * 0.5f + Theta * 0.5f;
                Phi = targetPhi * 0.5f + Phi * 0.5f;

                if (Phi < -MathHelper.PiOver2)
                {
                    Phi = -MathHelper.PiOver2;
                }
                else if (Phi > MathHelper.PiOver2)
                {
                    Phi = MathHelper.PiOver2;
                }
            }
            else
            {
                shiftPressed = false;
            }

            bool goingBackward = false;
            Vector3 velocityToSet = Vector3.Zero;
            if (keys.IsKeyDown(ControlSettings.Mappings.Forward) || keys.IsKeyDown(Keys.Up))
            {
                Vector3 forward = (Target - Position);
                forward.Normalize();

                if (!KeyManager.RotationEnabled())
                {
                    forward.Y = 0;
                }
                forward.Normalize();

                velocityToSet += forward * CameraMoveSpeed * dt;
            }
            else if (keys.IsKeyDown(ControlSettings.Mappings.Back) || keys.IsKeyDown(Keys.Down))
            {
                Vector3 forward = (Target - Position);
                forward.Normalize();
                goingBackward = true;

                if (!KeyManager.RotationEnabled())
                {
                    forward.Y = 0;
                }

                forward.Normalize();

                velocityToSet += -forward * CameraMoveSpeed * dt;
            }

            if (keys.IsKeyDown(ControlSettings.Mappings.Left) || keys.IsKeyDown(Keys.Left))
            {
                Vector3 forward = (Target - Position);
                forward.Normalize();
                Vector3 right = Vector3.Cross(forward, UpVector);
                right.Normalize();
                if (goingBackward)
                {
                    //right *= -1;
                }

                velocityToSet += -right * CameraMoveSpeed * dt;
            }
            else if (keys.IsKeyDown(ControlSettings.Mappings.Right) || keys.IsKeyDown(Keys.Right))
            {
                Vector3 forward = (Target - Position);
                forward.Normalize();
                Vector3 right = Vector3.Cross(forward, UpVector);
                right.Normalize();
                if (goingBackward)
                {
                    //right *= -1;
                }
                velocityToSet += right * CameraMoveSpeed * dt;
            }

            if (velocityToSet.LengthSquared() > 0)
            {
                Velocity = velocityToSet;
            }


            if (!KeyManager.RotationEnabled())
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

                        dir *= 0.05f;

                        Vector3 forward = (Target - Position);
                        forward.Normalize();
                        Vector3 right = Vector3.Cross(forward, UpVector);
                        Vector3 delta = right * CameraMoveSpeed * dir * dt;
                        delta.Y = 0;
                        Velocity = -delta;
                    }
                }
                else if (mouse.Y < edgePadding || mouse.Y > GameState.Game.GraphicsDevice.Viewport.Height - edgePadding)
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

                        dir *= 0.1f;

                        Vector3 forward = (Target - Position);
                        forward.Normalize();
                        Vector3 right = Vector3.Cross(forward, UpVector);
                        Vector3 up = Vector3.Cross(right, forward);
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

            if (mouse.ScrollWheelValue != LastWheel && !WorldManager.GUI.IsMouseOver())
            {
                int change = mouse.ScrollWheelValue - LastWheel;

                if (!(keys.IsKeyDown(Keys.LeftAlt) || keys.IsKeyDown(Keys.RightAlt)))
                {
                    if (!keys.IsKeyDown(Keys.LeftControl))
                    {
                        Vector3 delta = new Vector3(0, change, 0);

                        if (GameSettings.Default.InvertZoom)
                        {
                            delta *= -1;
                        }

                        Velocity = delta * CameraZoomSpeed * dt;
                    }
                    else
                    {
                        chunks.ChunkData.SetMaxViewingLevel(chunks.ChunkData.MaxViewingLevel + (int)((float)change * 0.01f), ChunkManager.SliceMode.Y);
                    }
                }
            }

            LastWheel = mouse.ScrollWheelValue;

            if (!CollidesWithChunks(WorldManager.ChunkManager, Target + Velocity, false))
            {
                Target += Velocity;
                PushVelocity = Vector3.Zero;
            }
            else
            {
                PushVelocity += Vector3.Up * 0.05f;
                Target += PushVelocity;
            }

            Velocity *= 0.8f;
            UpdateBasisVectors();
        }

        public void UpdateBasisVectors()
        {
            // Does nothing in this camera...
        }

        public override void UpdateViewMatrix()
        {

            Matrix cameraRotation = Matrix.CreateRotationX(Phi) * Matrix.CreateRotationY(Theta);

            Vector3 cameraOriginalTarget = new Vector3(0, 0, -1);
            Vector3 cameraRotatedTarget = Vector3.Transform(cameraOriginalTarget, cameraRotation);
            Vector3 cameraFinalTarget = Position + cameraRotatedTarget;

            Vector3 cameraOriginalUpVector = new Vector3(0, 1, 0);
            Vector3 cameraRotatedUpVector = Vector3.Transform(cameraOriginalUpVector, cameraRotation);

            ViewMatrix = Matrix.CreateLookAt(Position, cameraFinalTarget, cameraRotatedUpVector);
            Position = Target - cameraRotatedTarget;
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
            Target = MathFunctions.Clamp(Target, bounds.Expand(-2.0f));
        }

        public bool CollidesWithChunks(ChunkManager chunks, Vector3 pos, bool applyForce)
        {
            BoundingBox box = new BoundingBox(pos - new Vector3(0.5f, 0.5f, 0.5f), pos + new Vector3(0.5f, 0.5f, 0.5f));
            Voxel currentVoxel = new Voxel();
            bool success = chunks.ChunkData.GetVoxel(null, pos, ref currentVoxel);

            List<Voxel> vs = new List<Voxel>
            {
                currentVoxel
            };

            VoxelChunk chunk = chunks.ChunkData.GetVoxelChunkAtWorldLocation(pos);


            if (!success || currentVoxel == null || chunk == null)
            {
                return false;
            }

            Vector3 grid = chunk.WorldToGrid(pos);

            List<Voxel> adjacencies = chunk.GetNeighborsEuclidean((int)grid.X, (int)grid.Y, (int)grid.Z);
            vs.AddRange(adjacencies);

            bool gotCollision = false;
            foreach (Voxel v in vs)
            {
                if (v.IsEmpty || !v.IsVisible)
                {
                    continue;
                }

                BoundingBox voxAABB = v.GetBoundingBox();
                if (box.Intersects(voxAABB))
                {
                    gotCollision = true;
                    if (applyForce)
                    {
                        Collide(box, voxAABB);
                    }

                    else
                        return true;
                }
            }

            return gotCollision;
        }
    }

}