using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    public class OrbitCamera : Camera
    {
        public float Theta { get; set; }
        public float Phi { get; set; }
        public float Radius { get; set; }
        public float CameraMoveSpeed { get { return GameSettings.Default.CameraScrollSpeed; } set { GameSettings.Default.CameraScrollSpeed = value; } }
        public float CameraZoomSpeed { get { return GameSettings.Default.CameraZoomSpeed; } set { GameSettings.Default.CameraZoomSpeed = value; } }

        private float m_thetaOnClick = 0.0f;
        private float m_phiOnClick = 0.0f;
        private float m_radiusOnClick = 0.0f;
        private Vector3 m_targetOnClick = Vector3.Zero;
        private bool m_isLeftPressed = false;
        private bool m_isRightPressed = false;
        private Point m_mouseCoordsOnClick;
        private PlayState m_playState = null;
        private int m_lastWheel = 1;
        private Timer m_moveTimer = new Timer(0.25f, true);
        private float m_targetTheta = 0.0f;
        private float m_targetPhi = 0.0f;
        private bool shiftPressed = false;

        public OrbitCamera(PlayState playState, float theta, float phi, float radius, Vector3 target, Vector3 position, float fov, float aspectRatio, float nearPlane, float farPlane) :
            base(playState.Game.GraphicsDevice, target, position, fov, aspectRatio, nearPlane, farPlane)
        {
            Theta = theta;
            Phi = phi;
            Radius = radius;
            m_playState = playState;
            m_lastWheel = Mouse.GetState().ScrollWheelValue;
            m_targetTheta = theta;
            m_targetPhi = phi;
        }


        public override void Update(GameTime time, ChunkManager chunks)
        {
            MouseState mouse = Mouse.GetState();
            KeyboardState keys = Keyboard.GetState();

            int edgePadding = -10000;

            if (GameSettings.Default.EnableEdgeScroll)
            {
                edgePadding = 100;
            }

            bool stateChanged = false;
            float dt = (float)time.ElapsedGameTime.TotalSeconds;

            if (keys.IsKeyDown(ControlSettings.Default.CameraMode))
            {
                if (!shiftPressed)
                {
                    shiftPressed = true;
                    Mouse.SetPosition(Graphics.Viewport.Width / 2, Graphics.Viewport.Height / 2);
                    mouse = Mouse.GetState();
                }
                if (!m_isLeftPressed && mouse.LeftButton == ButtonState.Pressed)
                {
                    m_mouseCoordsOnClick = new Point(mouse.X, mouse.Y);
                    m_isLeftPressed = true;
                    stateChanged = true;
                }
                else if (mouse.LeftButton == ButtonState.Released)
                {
                    m_isLeftPressed = false;
                }

                if (!m_isRightPressed && mouse.RightButton == ButtonState.Pressed)
                {
                    m_isRightPressed = true;
                    stateChanged = true;
                }
                else if (mouse.RightButton == ButtonState.Released)
                {
                    m_isRightPressed = false;
                }


                if (stateChanged)
                {
                    m_radiusOnClick = Radius;
                    m_targetOnClick = Target;
                }


                float diffX = mouse.X - Graphics.Viewport.Width / 2;
                float diffY = mouse.Y - Graphics.Viewport.Height / 2;
                m_thetaOnClick = Theta;
                m_phiOnClick = Phi;
                Mouse.SetPosition(Graphics.Viewport.Width / 2, Graphics.Viewport.Height / 2);



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

                m_targetTheta = Theta - (filterDiffX);
                m_targetPhi = Phi - (filterDiffY);
                Theta = m_targetTheta * 0.5f + Theta * 0.5f;
                Phi = m_targetPhi * 0.5f + Phi * 0.5f;


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

            bool goingBackward = false;
            Vector3 velocityToSet = Vector3.Zero;
            if (keys.IsKeyDown(ControlSettings.Default.Forward) || keys.IsKeyDown(Keys.Up))
            {
                Vector3 forward = (Target - Position);
                forward.Normalize();

                if (!keys.IsKeyDown(ControlSettings.Default.CameraMode))
                {
                    forward.Y = 0;
                }
                forward.Normalize();

                velocityToSet += forward * CameraMoveSpeed * dt;
            }
            else if (keys.IsKeyDown(ControlSettings.Default.Back) || keys.IsKeyDown(Keys.Down))
            {
                Vector3 forward = (Target - Position);
                forward.Normalize();
                goingBackward = true;

                if (!keys.IsKeyDown(ControlSettings.Default.CameraMode))
                {
                    forward.Y = 0;
                }

                forward.Normalize();

                velocityToSet += -forward * CameraMoveSpeed * dt;
            }

            if (keys.IsKeyDown(ControlSettings.Default.Left) || keys.IsKeyDown(Keys.Left))
            {
                Vector3 forward =  (Target - Position);
                forward.Normalize();
                Vector3 right = Vector3.Cross(forward, UpVector);
                right.Normalize();
                if (goingBackward)
                {
                    //right *= -1;
                }

                velocityToSet += -right * CameraMoveSpeed * dt;
            }
            else if (keys.IsKeyDown(ControlSettings.Default.Right) || keys.IsKeyDown(Keys.Right))
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



            if (!keys.IsKeyDown(ControlSettings.Default.CameraMode))
            {
                if (mouse.X < edgePadding || mouse.X > m_playState.Game.GraphicsDevice.Viewport.Width - edgePadding)
                {
                    if (m_moveTimer.HasTriggered)
                    {
                        float dir = 0.0f;

                        if (mouse.X < edgePadding)
                        {
                            dir = edgePadding - mouse.X;
                        }
                        else
                        {
                            dir = (m_playState.Game.GraphicsDevice.Viewport.Width - edgePadding) - mouse.X;
                        }

                        dir *= 0.05f;

                        Vector3 forward = (Target - Position);
                        forward.Normalize();
                        Vector3 right = Vector3.Cross(forward, UpVector);
                        Vector3 delta = right * CameraMoveSpeed * dir * dt;
                        delta.Y = 0;
                        Velocity = -delta;
                    }
                    else
                    {
                        m_moveTimer.Update(time);
                    }
                }
                else if (mouse.Y < edgePadding || mouse.Y > m_playState.Game.GraphicsDevice.Viewport.Height - edgePadding)
                {
                    if (m_moveTimer.HasTriggered)
                    {
                        float dir = 0.0f;

                        if (mouse.Y < edgePadding)
                        {
                            dir = -(edgePadding - mouse.Y);
                        }
                        else
                        {
                            dir = -((m_playState.Game.GraphicsDevice.Viewport.Height - edgePadding) - mouse.Y);
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
                    else
                    {
                        m_moveTimer.Update(time);
                    }
                }
                else
                {
                    m_moveTimer.Reset(m_moveTimer.TargetTimeSeconds);
                        
                }
            }

            if (mouse.ScrollWheelValue != m_lastWheel)
            {
                int change = mouse.ScrollWheelValue - m_lastWheel;

                if(!(keys.IsKeyDown(Keys.LeftAlt) || keys.IsKeyDown(Keys.RightAlt)))
                {
                    if (!keys.IsKeyDown(Keys.LeftControl))
                    {
                        Vector3 delta = new Vector3(0, change, 0);
                        Velocity = delta * CameraZoomSpeed * dt;
                    }
                    else
                    {
                        chunks.SetMaxViewingLevel(chunks.MaxViewingLevel + (int)((float)change * 0.01f), ChunkManager.SliceMode.Y);
                    }
                }

                m_lastWheel = mouse.ScrollWheelValue;
            }

            Target += Velocity;
            Velocity *= 0.8f;
            UpdateBasisVectors();
           
            base.Update(time, chunks);
        }

        public void UpdateBasisVectors()
        {
            //Vector3 p = new Vector3();
            //p.Z = (float)(Radius * Math.Cos(Theta) * Math.Sin(Phi));
            //p.Y = (float)(Radius * Math.Cos(Phi));
            //p.X = (float)(Radius * Math.Sin(Theta) * Math.Sin(Phi));

            //Position = Vector3.Transform(Vector3.Backward, Matrix.CreateFromYawPitchRoll(Theta, Phi, 0)) * Radius + Target;

            /*
            Vector3 u = new Vector3();
            u.Y = (float)(-Math.Cos(Theta) * Math.Cos(Phi));
            u.Z = (float)(Math.Sin(Phi));
            u.X = (float)(-Math.Sin(Theta) * Math.Cos(Phi));
            
            UpVector = u;
             */
             
        }

        public  override void UpdateViewMatrix()
        {

            //float height = (float)(30.0f * Math.Tan(FOV / 2));
            //float width = (float)(AspectRatio * 30.0f * Math.Tan(FOV / 2));
            //ProjectionMatrix = Matrix.CreateOrthographic(width, height, NearPlane, FarPlane);

            Matrix cameraRotation = Matrix.CreateRotationX(Phi) * Matrix.CreateRotationY(Theta);

            Vector3 cameraOriginalTarget = new Vector3(0, 0, -1);
            Vector3 cameraRotatedTarget = Vector3.Transform(cameraOriginalTarget, cameraRotation);
            Vector3 cameraFinalTarget = Position + cameraRotatedTarget;

            Vector3 cameraOriginalUpVector = new Vector3(0, 1, 0);
            Vector3 cameraRotatedUpVector = Vector3.Transform(cameraOriginalUpVector, cameraRotation);

            ViewMatrix = Matrix.CreateLookAt(Position, cameraFinalTarget, cameraRotatedUpVector);
            Position = Target - cameraRotatedTarget;
        }
    }
}
