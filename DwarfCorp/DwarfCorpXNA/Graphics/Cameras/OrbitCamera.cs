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

        public Physics PhysicsObject { get; set; }
        private bool isLeftPressed = false;
        private bool isRightPressed = false;
        private readonly Timer moveTimer = new Timer(0.25f, true, Timer.TimerMode.Real);
        private bool shiftPressed = false;
        public Vector3 PushVelocity = Vector3.Zero;
        public ControlType Control = ControlType.Overhead;

        public List<Vector3> ZoomTargets { get; set; }

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
            Drawer3D.DrawAxes(Matrix.CreateTranslation(Target), 1.0f);
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
            Voxel vox =  World.ChunkManager.ChunkData.GetFirstVisibleBlockHitByRay(new Vector3(pos.X, World.ChunkHeight - 1, pos.Z), new Vector3(pos.X, 0, pos.Z));
            if (vox == null)
            {
                return pos;
            }
            else return new Vector3(pos.X, vox.Position.Y + 0.5f, pos.Z);
        }

        public void OverheadUpdate(DwarfTime time, ChunkManager chunks)
        {
            Voxel currentVoxel = new Voxel();
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

            if (ZoomTargets.Count > 0)
            {
                Vector3 currTarget = ProjectToSurface(ZoomTargets.First());
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

            int edgePadding = -10000;

            if (GameSettings.Default.EnableEdgeScroll)
            {
                edgePadding = 100;
            }

            bool stateChanged = false;
            float dt = (float)time.ElapsedRealTime.TotalSeconds;
            SnapToBounds(new BoundingBox(World.ChunkManager.Bounds.Min, World.ChunkManager.Bounds.Max + Vector3.UnitY * 20));
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

                diffTheta = - (filterDiffX);
                diffPhi =  - (filterDiffY);

            }
            else
            {
                shiftPressed = false;
            }

            bool goingBackward = false;
            Vector3 velocityToSet = Vector3.Zero;
            if (keys.IsKeyDown(ControlSettings.Mappings.Forward) || keys.IsKeyDown(Keys.Up))
            {
                Vector3 mov = forward;
                mov.Y = 0;
                mov.Normalize();
                velocityToSet += mov * CameraMoveSpeed * dt;
            }
            else if (keys.IsKeyDown(ControlSettings.Mappings.Back) || keys.IsKeyDown(Keys.Down))
            {
                goingBackward = true;

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

            if (velocityToSet.LengthSquared() > 0)
            {
                Velocity = velocityToSet;
            }


            if (!KeyManager.RotationEnabled())
            {
                DwarfGame.World.GUI.IsMouseVisible = true;

                if (!DwarfGame.World.IsMouseOverGui)
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
            else
            {
                DwarfGame.World.GUI.IsMouseVisible = false;
            }

            if (mouse.ScrollWheelValue != LastWheel && !World.IsMouseOverGui)
            {
                int change = mouse.ScrollWheelValue - LastWheel;

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

                        if (diffRadius < 0)
                        {
                            float diffxy =
                                (new Vector3(Target.X, 0, Target.Z) -
                                 new Vector3(DwarfGame.World.CursorPos.X, 0, DwarfGame.World.CursorPos.Z)).Length();

                            if (diffxy > 5)
                            {
                                Vector3 slewTarget = Target*0.9f + DwarfGame.World.CursorPos*0.1f;
                                Vector3 slewDiff = slewTarget - Target;
                                Target += slewDiff;
                                Position += slewDiff;
                            }
                        }
                    }
                    else
                    {
                        chunks.ChunkData.SetMaxViewingLevel(chunks.ChunkData.MaxViewingLevel + (int)((float)change * 0.01f), ChunkManager.SliceMode.Y);
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
            Vector3 projectedTarget = ProjectToSurface(Target);
            Vector3 diffTarget = projectedTarget - Target;
            Position = (Position + diffTarget) * 0.05f + Position * 0.95f;
            Target = projectedTarget * 0.05f + Target * 0.95f;
            float currRadius = (Position - Target).Length();
            float newRadius = Math.Max(currRadius + diffRadius, 1.0f);
            Position = MathFunctions.ProjectToSphere(Position - right*diffTheta * 2 - up*diffPhi * 2, newRadius, Target);
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
            ViewMatrix = Matrix.CreateLookAt(Position, Target, Vector3.UnitY);
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
            Position = MathFunctions.Clamp(Position, bounds.Expand(-2.0f));
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