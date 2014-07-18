using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        private bool isLeftPressed = false;
        private bool isRightPressed = false;
        private readonly Timer moveTimer = new Timer(0.25f, true);
        private float targetTheta = 0.0f;
        private float targetPhi = 0.0f;
        private bool shiftPressed = false;

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
        }


        public void SetTargetRotation(float theta, float phi)
        {
            targetTheta = theta;
            targetPhi = phi;
        }

        public override void Update(GameTime time, ChunkManager chunks)
        {
            MouseState mouse = Mouse.GetState();
            KeyboardState keys = Keyboard.GetState();

            int edgePadding = -10000;

            if(GameSettings.Default.EnableEdgeScroll)
            {
                edgePadding = 100;
            }

            bool stateChanged = false;
            float dt = (float) time.ElapsedGameTime.TotalSeconds;

            if(KeyManager.RotationEnabled()){
                if(!shiftPressed)
                {
                    shiftPressed = true;
                  
                    mouse = Mouse.GetState();
                    stateChanged = true;
                }
                if(!isLeftPressed && mouse.LeftButton == ButtonState.Pressed)
                {
                    isLeftPressed = true;
                    stateChanged = true;
                }
                else if(mouse.LeftButton == ButtonState.Released)
                {
                    isLeftPressed = false;
                }

                if(!isRightPressed && mouse.RightButton == ButtonState.Pressed)
                {
                    isRightPressed = true;
                    stateChanged = true;
                }
                else if(mouse.RightButton == ButtonState.Released)
                {
                    isRightPressed = false;
                }


                if(stateChanged)
                {
                    Mouse.SetPosition(GameState.Game.GraphicsDevice.Viewport.Width / 2, GameState.Game.GraphicsDevice.Viewport.Height / 2);
                    mouse = Mouse.GetState();
                }


                float diffX = mouse.X - GameState.Game.GraphicsDevice.Viewport.Width / 2;
                float diffY = mouse.Y - GameState.Game.GraphicsDevice.Viewport.Height / 2;
    

                float filterDiffX = (float) (diffX * dt);
                float filterDiffY = (float) (diffY * dt);
                if(Math.Abs(filterDiffX) > 1.0f)
                {
                    filterDiffX = 1.0f * Math.Sign(filterDiffX);
                }

                if(Math.Abs(filterDiffY) > 1.0f)
                {
                    filterDiffY = 1.0f * Math.Sign(filterDiffY);
                }

                targetTheta = Theta - (filterDiffX);
                targetPhi = Phi - (filterDiffY);
                Theta = targetTheta * 0.5f + Theta * 0.5f;
                Phi = targetPhi * 0.5f + Phi * 0.5f;


                if(Phi < -1.5f)
                {
                    Phi = -1.5f;
                }
                else if(Phi > 1.5f)
                {
                    Phi = 1.5f;
                }
            }
            else
            {
                shiftPressed = false;
            }

            bool goingBackward = false;
            Vector3 velocityToSet = Vector3.Zero;
            if(keys.IsKeyDown(ControlSettings.Default.Forward) || keys.IsKeyDown(Keys.Up))
            {
                Vector3 forward = (Target - Position);
                forward.Normalize();

                if(!KeyManager.RotationEnabled())
                {
                    forward.Y = 0;
                }
                forward.Normalize();

                velocityToSet += forward * CameraMoveSpeed * dt;
            }
            else if(keys.IsKeyDown(ControlSettings.Default.Back) || keys.IsKeyDown(Keys.Down))
            {
                Vector3 forward = (Target - Position);
                forward.Normalize();
                goingBackward = true;

                if(!KeyManager.RotationEnabled())
                {
                    forward.Y = 0;
                }

                forward.Normalize();

                velocityToSet += -forward * CameraMoveSpeed * dt;
            }

            if(keys.IsKeyDown(ControlSettings.Default.Left) || keys.IsKeyDown(Keys.Left))
            {
                Vector3 forward = (Target - Position);
                forward.Normalize();
                Vector3 right = Vector3.Cross(forward, UpVector);
                right.Normalize();
                if(goingBackward)
                {
                    //right *= -1;
                }

                velocityToSet += -right * CameraMoveSpeed * dt;
            }
            else if(keys.IsKeyDown(ControlSettings.Default.Right) || keys.IsKeyDown(Keys.Right))
            {
                Vector3 forward = (Target - Position);
                forward.Normalize();
                Vector3 right = Vector3.Cross(forward, UpVector);
                right.Normalize();
                if(goingBackward)
                {
                    //right *= -1;
                }
                velocityToSet += right * CameraMoveSpeed * dt;
            }

            if(velocityToSet.LengthSquared() > 0)
            {
                Velocity = velocityToSet;
            }


            if(!KeyManager.RotationEnabled())
            {
                if(mouse.X < edgePadding || mouse.X > GameState.Game.GraphicsDevice.Viewport.Width - edgePadding)
                {
                    moveTimer.Update(time);
                    if(moveTimer.HasTriggered)
                    {
                        float dir = 0.0f;

                        if(mouse.X < edgePadding)
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
                else if(mouse.Y < edgePadding || mouse.Y > GameState.Game.GraphicsDevice.Viewport.Height - edgePadding)
                {
                    moveTimer.Update(time);
                    if(moveTimer.HasTriggered)
                    {
                        float dir = 0.0f;

                        if(mouse.Y < edgePadding)
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

            if (mouse.ScrollWheelValue != LastWheel)
            {
                int change = mouse.ScrollWheelValue - LastWheel;

                if(!(keys.IsKeyDown(Keys.LeftAlt) || keys.IsKeyDown(Keys.RightAlt)))
                {
                    if(!keys.IsKeyDown(Keys.LeftControl))
                    {
                        Vector3 delta = new Vector3(0, change, 0);

                        if(GameSettings.Default.InvertZoom)
                        {
                            delta *= -1;
                        }

                        Velocity = delta * CameraZoomSpeed * dt;
                    }
                    else
                    {
                        chunks.ChunkData.SetMaxViewingLevel(chunks.ChunkData.MaxViewingLevel + (int) ((float) change * 0.01f), ChunkManager.SliceMode.Y);
                    }
                }

                LastWheel = mouse.ScrollWheelValue;
            }

            if (!CollidesWithChunks(PlayState.ChunkManager, Target + Velocity))
            {
                Target += Velocity;
            }
            Velocity *= 0.8f;
            UpdateBasisVectors();

            base.Update(time, chunks);
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

      

        public bool CollidesWithChunks(ChunkManager chunks, Vector3 pos)
        {
            BoundingBox box = new BoundingBox(pos - new Vector3(0.5f, 0.5f, 0.5f), pos + new Vector3(0.5f, 0.5f, 0.5f));
            VoxelRef currentVoxel = chunks.ChunkData.GetVoxelReferenceAtWorldLocation(null, pos);

            List<VoxelRef> vs = new List<VoxelRef>
            {
                currentVoxel
            };

            VoxelChunk chunk = chunks.ChunkData.GetVoxelChunkAtWorldLocation(pos);


            if (currentVoxel == null || chunk == null)
            {
                return false;
            }

            Vector3 grid = chunk.WorldToGrid(pos);

            List<VoxelRef> adjacencies = chunk.GetNeighborsEuclidean((int)grid.X, (int)grid.Y, (int)grid.Z);
            vs.AddRange(adjacencies);

            foreach (VoxelRef v in vs)
            {
                if (v == null || v.TypeName == "empty")
                {
                    continue;
                }

                BoundingBox voxAABB = v.GetBoundingBox();
                if (box.Intersects(voxAABB))
                {
                    return true;
                }
            }

            return false;
        }
    }

}