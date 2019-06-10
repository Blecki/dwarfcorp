using System.Collections.Generic;
using System.Linq;
using LibNoise;
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DwarfCorp.Gui;

namespace DwarfCorp.GameStates
{
    public class SimpleOrbitCamera
    {
        private float phi = 1.2f;
        private float theta = -0.25f;
        private float zoom = 0.9f;
        private Vector3 Focus = new Vector3(0.5f, 0.0f, 0.5f);
        private Vector3 GoalFocus = new Vector3(0.5f, 0, 0.5f);
        private Point PreviousMousePosition;
        public Rectangle Rect;
        public OverworldGenerationSettings Overworld;

        public void SetGoalFocus(Vector3 GoalFocus)
        {
            this.GoalFocus = GoalFocus;
        }

        public Matrix CameraRotation
        {
            get
            {
                return Matrix.CreateRotationX(phi) * Matrix.CreateRotationY(theta);
            }
        }

        public Matrix ViewMatrix
        {
            get
            {
                return Matrix.CreateLookAt(CameraPos, Focus, Vector3.Up);
            }
        }

        public Vector3 CameraPos
        {
            get
            {
                return zoom * Vector3.Transform(Vector3.Forward, CameraRotation) + Focus;

            }
        }

        public Matrix ProjectionMatrix
        {
            get
            {
                return Matrix.CreatePerspectiveFieldOfView(1.5f, (float)Rect.Width /
                    (float)Rect.Height, 0.01f, 3.0f);
            }
        }

        public void OnMouseMove(InputEventArgs args)
        {
            if (Microsoft.Xna.Framework.Input.Mouse.GetState().RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed)
            {
                var delta = new Vector2(args.X, args.Y) - new Vector2(PreviousMousePosition.X,
                     PreviousMousePosition.Y);

                var keyboard = Microsoft.Xna.Framework.Input.Keyboard.GetState();
                if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) ||
                    keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift))
                {
                    zoom = global::System.Math.Min((float)global::System.Math.Max(zoom + delta.Y * 0.001f, 0.1f), 1.5f);
                }
                else
                {
                    phi += delta.Y * 0.01f;
                    theta -= delta.X * 0.01f;
                    phi = global::System.Math.Max(phi, 0.5f);
                    phi = global::System.Math.Min(phi, 1.5f);
                }
            }
        }

        public void OnScroll(InputEventArgs args)
        {
            zoom = global::System.Math.Min((float)global::System.Math.Max(args.ScrollValue > 0 ? zoom - 0.1f : zoom + 0.1f, 0.1f), 1.5f);
        }

        public Point ScreenToWorld(Vector2 screenCoord)
        {
            // Todo: This can be simplified.
            Viewport port = new Viewport(Rect);
            port.MinDepth = 0.0f;
            port.MaxDepth = 1.0f;
            Vector3 rayStart = port.Unproject(new Vector3(screenCoord.X, screenCoord.Y, 0.0f), ProjectionMatrix, ViewMatrix, Matrix.Identity);
            Vector3 rayEnd = port.Unproject(new Vector3(screenCoord.X, screenCoord.Y, 1.0f), ProjectionMatrix, ViewMatrix, Matrix.Identity);
            Vector3 bearing = (rayEnd - rayStart);
            bearing.Normalize();
            Ray ray = new Ray(rayStart, bearing);
            Plane worldPlane = new Plane(Vector3.Zero, Vector3.Forward, Vector3.Right);
            float? dist = ray.Intersects(worldPlane);

            if (dist.HasValue)
            {
                Vector3 pos = rayStart + bearing * dist.Value;
                return new Point((int)(pos.X * Overworld.Width), (int)(pos.Z * Overworld.Height));
            }
            else
            {
                return new Point(0, 0);
            }
        }

        public Vector3 GetWorldSpace(Vector2 worldCoord)
        {
            var height = 0.0f;
            if ((int)worldCoord.X > 0 && (int)worldCoord.Y > 0 &&
                (int)worldCoord.X < Overworld.Width && (int)worldCoord.Y < Overworld.Height)
                height = Overworld.Overworld.Map[(int)worldCoord.X, (int)worldCoord.Y].Height * 0.05f;
            return new Vector3(worldCoord.X / Overworld.Width, height, worldCoord.Y / Overworld.Height);
        }

        public Vector3 WorldToScreen(Vector2 worldCoord)
        {
            Viewport port = new Viewport(Rect);
            Vector3 worldSpace = GetWorldSpace(worldCoord);
            return port.Project(worldSpace, ProjectionMatrix, ViewMatrix, Matrix.Identity);
        }

        public void Update(Point MousePosition, DwarfTime Time)
        {
            //Because Gum doesn't send deltas on mouse move.
            PreviousMousePosition = MousePosition;
            var delta = 0.5f * (float)Time.ElapsedGameTime.TotalSeconds;
            Focus = GoalFocus * delta + Focus * (1.0f - delta);
        }
    }
}