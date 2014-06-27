﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DwarfCorp
{
    public class Minimap : ImagePanel
    {
        public Vector3 HomePosition { get; set; }
        protected bool HomeSet = false;
        public RenderTarget2D RenderTarget = null;
        public Texture2D ColorMap { get; set; }
        public Texture2D Frame { get; set; }
        protected int RenderWidth = 196;
        protected int RenderHeight = 196;

        public OrbitCamera Camera { get; set; }

        public PlayState PlayState { get; set; }

        public bool IsMinimized { get; set; }

        protected Button ZoomInButton { get; set; }
        protected Button ZoomOutButton { get; set; }
        protected Button ZoomHomeButton { get; set; }
        protected Button MinimizeButton { get; set; }
        bool SuppressClick { get; set; }

        public Minimap(DwarfGUI gui, GUIComponent parent, int width, int height, PlayState playState, Texture2D colormap, Texture2D frame):
            base(gui, parent, new ImageFrame())
        {
            Frame = frame;
            SuppressClick = false;
            ColorMap = colormap;
            
            RenderWidth = width;
            RenderHeight = height;
            RenderTarget = new RenderTarget2D(GameState.Game.GraphicsDevice, RenderWidth, RenderHeight, false, SurfaceFormat.Color, DepthFormat.Depth24);
            Image.Image = RenderTarget;
            Image.SourceRect = new Rectangle(0, 0, RenderWidth, RenderHeight);
            PlayState = playState;

            ConstrainSize = true;

            Camera = new OrbitCamera(0, 0, 0.01f, new Vector3(0, 0, 0), new Vector3(0, 0, 0),  2.5f, 1.0f, 0.1f, 1000.0f)
            {
                Projection = DwarfCorp.Camera.ProjectionMode.Orthographic
            };

            ZoomInButton = new Button(GUI, this, "", GUI.SmallFont, Button.ButtonMode.ImageButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.ZoomIn))
            {
                LocalBounds = new Rectangle(1, 1, 32, 32),
                ToolTip =  "Zoom in"
            };

            ZoomInButton.OnClicked += zoomInButton_OnClicked;

            ZoomOutButton = new Button(GUI, this, "", GUI.SmallFont, Button.ButtonMode.ImageButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.ZoomOut))
            {
                LocalBounds = new Rectangle(33, 1, 32, 32),
                ToolTip =  "Zoom out"
            };

            ZoomOutButton.OnClicked += zoomOutButton_OnClicked;


            ZoomHomeButton = new Button(GUI, this, "", GUI.SmallFont, Button.ButtonMode.ImageButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.ZoomHome))
            {
                LocalBounds = new Rectangle(65, 1, 32, 32),
                ToolTip =  "Home camera"
            };

            ZoomHomeButton.OnClicked +=ZoomHomeButton_OnClicked;

            MinimizeButton = new Button(GUI, this, "", GUI.SmallFont, Button.ButtonMode.ImageButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.SmallArrowDown))
            {
                LocalBounds = new Rectangle(width - 32, 0, 32, 32),
                ToolTip = "Show/Hide Map"
            };

            MinimizeButton.OnClicked += MinimizeButton_OnClicked;

            OnClicked += Minimap_OnClicked;

            
        }

        public void SetMinimized(bool minimized)
        {
            IsMinimized = minimized;
            if (IsMinimized)
            {
                MinimizeButton.Image = GUI.Skin.GetSpecialFrame(GUISkin.Tile.SmallArrowUp);
                MinimizeButton.LocalBounds = new Rectangle(RenderWidth - 32, RenderHeight - 32, 32, 32);
            }
            else
            {
                MinimizeButton.Image = GUI.Skin.GetSpecialFrame(GUISkin.Tile.SmallArrowDown);
                MinimizeButton.LocalBounds = new Rectangle(RenderWidth - 32, 0, 32, 32);
            }
        }

        void MinimizeButton_OnClicked()
        {
            SuppressClick = true;
            SetMinimized(!IsMinimized);
        }

        void ZoomHomeButton_OnClicked()
        {
            PlayState.Camera.Target = HomePosition;
            PlayState.Camera.UpdateViewMatrix();
        }

        bool IsOverButtons(int x, int y)
        {
            return (MinimizeButton.GlobalBounds.Contains(x, y) || ZoomInButton.GlobalBounds.Contains(x, y) || ZoomOutButton.GlobalBounds.Contains(x, y) || ZoomHomeButton.GlobalBounds.Contains(x, y));
        }

        void Minimap_OnClicked()
        {
            MouseState mouseState = Mouse.GetState();

            Viewport viewPort = new Viewport(RenderTarget.Bounds);
            Rectangle imageBounds = GetImageBounds();
            if (IsOverButtons(mouseState.X, mouseState.Y) || IsMinimized || SuppressClick || !imageBounds.Contains(mouseState.X, mouseState.Y))
            {
                SuppressClick = false;
                return;
            }
            Vector3 forward = (PlayState.Camera.Target - PlayState.Camera.Position);
            forward.Normalize();

            Vector3 pos = viewPort.Unproject(new Vector3(mouseState.X - imageBounds.X, mouseState.Y - imageBounds.Y, 0), Camera.ProjectionMatrix, Camera.ViewMatrix, Matrix.Identity) - forward * 10;
            PlayState.Camera.Target = new Vector3(pos.X, PlayState.Camera.Target.Y, pos.Z);
        }

        void zoomOutButton_OnClicked()
        {
            Camera.FOV = Math.Min(Camera.FOV * 1.1f, (float)Math.PI);
            Camera.UpdateProjectionMatrix();
        }

        void zoomInButton_OnClicked()
        {
            Camera.FOV = Math.Max(Camera.FOV * 0.9f, 0.1f);
            Camera.UpdateProjectionMatrix();
        }


        public override void Render(GameTime time, SpriteBatch batch)
        {
            if(!IsMinimized)
            {
                base.Render(time, batch);
                Rectangle imageBounds = GetImageBounds();
                Rectangle frameBounds = new Rectangle(imageBounds.X, imageBounds.Y + imageBounds.Height - Frame.Height, Frame.Width, Frame.Height);
                batch.Draw(Frame, frameBounds, Color.White);
            }
            else
            {
                MinimizeButton.Render(time, batch);
            }
        }

        public override void PreRender(GameTime time, SpriteBatch sprites)
        {
            if(!HomeSet)
            {
                HomePosition = PlayState.Camera.Target;
                HomeSet = true;
            }

            if(IsVisible && !IsMinimized && GUI.RootComponent.IsVisible)
            {
                Viewport originalViewport = PlayState.GraphicsDevice.Viewport;
                Camera.Update(time, PlayState.ChunkManager);
                Camera.Target = PlayState.Camera.Target + Vector3.Up * 50;
                Camera.Phi = -(float) Math.PI * 0.5f;
                Camera.Theta = PlayState.Camera.Theta;
                Camera.UpdateProjectionMatrix();

                PlayState.GraphicsDevice.SetRenderTarget(RenderTarget);
                PlayState.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 0, 0);
                PlayState.DefaultShader.Parameters["xView"].SetValue(Camera.ViewMatrix);
                PlayState.DefaultShader.Parameters["xEnableFog"].SetValue(false);

                PlayState.DefaultShader.Parameters["xProjection"].SetValue(Camera.ProjectionMatrix);
                PlayState.DefaultShader.CurrentTechnique = PlayState.DefaultShader.Techniques["Textured_colorscale"];

                PlayState.GraphicsDevice.BlendState = BlendState.AlphaBlend;

                PlayState.ChunkManager.RenderAll(Camera, time, PlayState.GraphicsDevice, PlayState.DefaultShader, Matrix.Identity, ColorMap);
                PlayState.WaterRenderer.DrawWaterFlat(PlayState.GraphicsDevice, Camera.ViewMatrix, Camera.ProjectionMatrix, PlayState.DefaultShader, PlayState.ChunkManager);


                PlayState.GraphicsDevice.Textures[0] = null;
                PlayState.GraphicsDevice.Indices = null;
                PlayState.GraphicsDevice.SetVertexBuffer(null);
                PlayState.DefaultShader.Parameters["xEnableFog"].SetValue(true);

                DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, RasterizerState.CullNone);
                Viewport viewPort = new Viewport(RenderTarget.Bounds);
                foreach(MinimapIcon icon in PlayState.ComponentManager.RootComponent.GetChildrenOfTypeRecursive<MinimapIcon>())
                {
                    
                   Vector3 screenPos = viewPort.Project(icon.GlobalTransform.Translation, Camera.ProjectionMatrix, Camera.ViewMatrix, Matrix.Identity);

                    if(RenderTarget.Bounds.Contains((int) screenPos.X, (int) screenPos.Y))
                    {
                        sprites.Draw(icon.Icon.Image, new Vector2(screenPos.X, screenPos.Y), icon.Icon.SourceRect, Color.White, 0.0f, new Vector2(icon.Icon.SourceRect.Width / 2.0f, icon.Icon.SourceRect.Height / 2.0f), icon.IconScale, SpriteEffects.None, 0);
                    }
                }

         
                sprites.End();

                PlayState.GraphicsDevice.SetRenderTarget(null);
                
            }

            base.PreRender(time, sprites);
        }


    }
}
