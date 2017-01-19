﻿// Minimap.cs
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
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DwarfCorp
{
    public class Minimap : ImagePanel, IDisposable
    {
        public Vector3 HomePosition { get; set; }
        protected bool HomeSet = false;
        public RenderTarget2D RenderTarget = null;
        public Texture2D ColorMap { get; set; }
        public Texture2D Frame { get; set; }
        protected int RenderWidth = 196;
        protected int RenderHeight = 196;

        public OrbitCamera Camera { get; set; }

        public WorldManager PlayState { get; set; }

        public bool IsMinimized { get; set; }

        protected Button ZoomInButton { get; set; }
        protected Button ZoomOutButton { get; set; }
        protected Button ZoomHomeButton { get; set; }
        protected Button MinimizeButton { get; set; }
        bool SuppressClick { get; set; }

        public Minimap(DwarfGUI gui, GUIComponent parent, int width, int height, WorldManager playState, Texture2D colormap, Texture2D frame):
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
                Projection = global::DwarfCorp.Camera.ProjectionMode.Orthographic
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
                TweenOut(Drawer2D.Alignment.Bottom);
            }
            else
            {
                MinimizeButton.Image = GUI.Skin.GetSpecialFrame(GUISkin.Tile.SmallArrowDown);
                MinimizeButton.LocalBounds = new Rectangle(RenderWidth - 32, 0, 32, 32);
                TweenIn(Drawer2D.Alignment.Bottom);
            }
        }

        void MinimizeButton_OnClicked()
        {
            SuppressClick = true;
            SetMinimized(!IsMinimized);
        }

        void ZoomHomeButton_OnClicked()
        {
            WorldManager.Camera.UpdateViewMatrix();
            WorldManager.Camera.ZoomTargets.Clear();
            WorldManager.Camera.ZoomTargets.Add(HomePosition);
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
            Vector3 forward = (WorldManager.Camera.Target - WorldManager.Camera.Position);
            forward.Normalize();

            Vector3 pos = viewPort.Unproject(new Vector3(mouseState.X - imageBounds.X, mouseState.Y - imageBounds.Y, 0), Camera.ProjectionMatrix, Camera.ViewMatrix, Matrix.Identity) - forward * 10;
            Vector3 target = new Vector3(pos.X, WorldManager.Camera.Target.Y, pos.Z);
            float height = WorldManager.ChunkManager.ChunkData.GetFilledVoxelGridHeightAt(target.X, target.Y, target.Z);
            target.Y = Math.Max(height + 15, target.Y);
            target = MathFunctions.Clamp(target, WorldManager.ChunkManager.Bounds);
            WorldManager.Camera.ZoomTargets.Clear();
            WorldManager.Camera.ZoomTargets.Add(target);
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

        public override void Update(DwarfTime time)
        {
            if (IsMinimized)
            {
                MinimizeButton.Update(time);
            }
            base.Update(time);
        }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            base.Render(time, batch);

            if (IsVisible)
            {
                Rectangle imageBounds = GetImageBounds();
                Rectangle frameBounds = new Rectangle(imageBounds.X, imageBounds.Y + imageBounds.Height - Frame.Height,
                    Frame.Width, Frame.Height);
                batch.Draw(Frame, frameBounds, Color.White);
            }
            else if (IsMinimized)
            {
                MinimizeButton.Render(time, batch);
                MinimizeButton.IsVisible = true;
            }
        }

        public override void PreRender(DwarfTime time, SpriteBatch sprites)
        {
            if(!HomeSet)
            {
                HomePosition = WorldManager.Camera.Target;
                HomeSet = true;
            }

            if(IsVisible && !IsMinimized && GUI.RootComponent.IsVisible)
            {
                
                Camera.Update(time, WorldManager.ChunkManager);
                Camera.Target = WorldManager.Camera.Target + Vector3.Up * 50;
                Camera.Phi = -(float) Math.PI * 0.5f;
                Camera.Theta = WorldManager.Camera.Theta;
                Camera.UpdateProjectionMatrix();
                
                PlayState.GraphicsDevice.SetRenderTarget(RenderTarget);
                
                PlayState.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 0, 0);
                WorldManager.DefaultShader.Parameters["xView"].SetValue(Camera.ViewMatrix);
                WorldManager.DefaultShader.Parameters["xEnableFog"].SetValue(0);

                WorldManager.DefaultShader.Parameters["xProjection"].SetValue(Camera.ProjectionMatrix);
                WorldManager.DefaultShader.CurrentTechnique = WorldManager.DefaultShader.Techniques["Textured_colorscale"];

                PlayState.GraphicsDevice.BlendState = BlendState.NonPremultiplied;

                WorldManager.ChunkManager.RenderAll(Camera, time, PlayState.GraphicsDevice, WorldManager.DefaultShader, Matrix.Identity, ColorMap);
                WorldManager.WaterRenderer.DrawWaterFlat(PlayState.GraphicsDevice, Camera.ViewMatrix, Camera.ProjectionMatrix, WorldManager.DefaultShader, WorldManager.ChunkManager);
                PlayState.GraphicsDevice.Textures[0] = null;
                PlayState.GraphicsDevice.Indices = null;
                PlayState.GraphicsDevice.SetVertexBuffer(null);

                WorldManager.DefaultShader.Parameters["xEnableFog"].SetValue(1);

                DwarfGame.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.PointClamp, null, RasterizerState.CullNone);
                Viewport viewPort = new Viewport(RenderTarget.Bounds);

                foreach (MinimapIcon icon in GameObjectCaching.MinimapIcons)
                {
                    if (!icon.Parent.IsVisible)
                    {
                        continue;
                    }

                   Vector3 screenPos = viewPort.Project(icon.GlobalTransform.Translation, Camera.ProjectionMatrix, Camera.ViewMatrix, Matrix.Identity);

                    if(RenderTarget.Bounds.Contains((int) screenPos.X, (int) screenPos.Y))
                    {
                        DwarfGame.SpriteBatch.Draw(icon.Icon.Image, new Vector2(screenPos.X, screenPos.Y), icon.Icon.SourceRect, Color.White, 0.0f, new Vector2(icon.Icon.SourceRect.Width / 2.0f, icon.Icon.SourceRect.Height / 2.0f), icon.IconScale, SpriteEffects.None, 0);
                    }
                }

         
                DwarfGame.SpriteBatch.End();

                PlayState.GraphicsDevice.BlendState = BlendState.NonPremultiplied;
                PlayState.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                PlayState.GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
                PlayState.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
                PlayState.GraphicsDevice.SetRenderTarget(null);

                
            }

            base.PreRender(time, sprites);
        }


        public void Dispose()
        {
            if(RenderTarget != null && !RenderTarget.IsDisposed)
                RenderTarget.Dispose();
        }
    }
}
