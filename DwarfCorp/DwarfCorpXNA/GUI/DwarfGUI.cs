// DwarfGUI.cs
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
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{

    public delegate void UpdateDelegate();
    public delegate void RenderDelegate();

    public delegate void ClickedDelegate();

    public delegate void PressedDelegate();

    public delegate void MouseHoveredDelegate();

    public delegate void MouseUnHoveredDelegate();

    public delegate void ReleasedDelegate();

    public delegate void MouseScrolledDelegate(int amount);
    public delegate void MouseDraggedDelegate(InputManager.MouseButton button, Vector2 delta);

    /// <summary>
    /// A proprietary GUI system written from scratch. Based loosely on the Qt framework.
    /// GUI elements are laid out in a tree. Children are normally drawn after parents.
    /// Handles input from the user on its own. Elements are drawn with native XNA drawing functions. 
    /// </summary>
    public class DwarfGUI
    {
        public GUIComponent RootComponent { get; set; }
        private readonly DwarfGame game = null;
        public SpriteFont DefaultFont { get; set; }
        public SpriteFont SmallFont { get; set; }
        public SpriteFont TitleFont { get; set; }
        public GUISkin Skin { get; set; }
        public Vector2 GlobalOffset { get; set; }
        public GUIComponent FocusComponent { get; set; }
        public InputManager Input { get; set; }
        public List<GUIComponent> DrawAfter { get; set; }
        public Color DefaultTextColor { get; set; }
        public Color DefaultStrokeColor { get; set; }
        public GraphicsDevice Graphics { get; set; }
        public bool IsMouseVisible { get; set; }
        public GUISkin.MousePointer MouseMode { get; set; }
        public int MouseScale = 2;
        public Color MouseTint = Color.White;
        public ToolTipManager ToolTipManager { get; set; }
       
        public bool DebugDraw { get; set; }
        public int LastScrollWheel { get; set; }
        public bool EnableMouseEvents { get; set; }
        public float LastMouseX { get; set; }
        public float LastMouseY { get; set; }

        public DwarfGUI(DwarfGame game, SpriteFont defaultFont, SpriteFont titleFont, SpriteFont smallFont, InputManager input)
        {
            EnableMouseEvents = true;
            IsMouseVisible = true;
            MouseMode = GUISkin.MousePointer.Pointer;
            SmallFont = smallFont;
            Graphics = game.GraphicsDevice;
            this.game = game;
            RootComponent = new GUIComponent(this, null)
            {
                LocalBounds = new Rectangle(0, 0, 0, 0)
            };

            DefaultFont = defaultFont;
            Skin = new GUISkin(TextureManager.GetTexture(ContentPaths.GUI.gui_widgets), 32, 32, TextureManager.GetTexture(ContentPaths.GUI.pointers), 16, 16);
            Skin.SetDefaults();
            TitleFont = titleFont;
            GlobalOffset = Vector2.Zero;
            FocusComponent = null;
            Input = input;
            DrawAfter = new List<GUIComponent>();
            DefaultTextColor = new Color(48, 27, 0);
            DefaultStrokeColor = Color.Transparent;
            DebugDraw = false;
            ToolTipManager = new ToolTipManager(this);
            LastScrollWheel = 0;
            LastMouseX = 0;
            LastMouseY = 0;
        }


        public static Rectangle AspectRatioFit(Rectangle sourceArea, Rectangle fitArea)
        {
            float[] ratios = { (float)fitArea.Width / (float)sourceArea.Width, (float)fitArea.Height / (float)sourceArea.Height };
                float minRatio = Math.Min(ratios[0], ratios[1]);
                float ratio = minRatio;

                return new Rectangle(fitArea.X, fitArea.Y, (int)(sourceArea.Width * ratio), (int) (sourceArea.Height * ratio));
        }

        public static string WrapLines(string text, Rectangle bounds, SpriteFont textFont)
        {
            Vector2 measurement = Datastructures.SafeMeasure(textFont, text);

            if (measurement.X < bounds.Width)
            {
                return text;
            }

            string[] originalWords = text.Split(' ');

            List<string> wrappedLines = new List<string>();

            StringBuilder actualLine = new StringBuilder();
            double actualWidth = 0;

            foreach (var item in originalWords)
            {
                Vector2 itemMeasure = Datastructures.SafeMeasure(textFont, item + " ");
                actualLine.Append(item + " ");
                actualWidth += (int)itemMeasure.X;

                if (actualWidth >= bounds.Width - itemMeasure.X)
                {
                    wrappedLines.Add(actualLine.ToString());
                    actualLine.Clear();
                    actualWidth = 0;
                }
            }

            if (actualLine.Length > 0)
                wrappedLines.Add(actualLine.ToString());

            string toReturn = "";

            foreach (var line in wrappedLines)
            {
                toReturn += line + "\n";
            }

            return toReturn;
        }

        public void Update(DwarfTime time)
        {
            RootComponent.LocalBounds = new Rectangle(0, 0, GameState.Game.GraphicsDevice.Viewport.Width, GameState.Game.GraphicsDevice.Viewport.Height);
            ToolTipManager.Update(time);

            if(!IsMouseVisible)
            {
                return;
            }

            if(FocusComponent == null)
            {
                RootComponent.Update(time);
            }
            else
            {
                FocusComponent.Update(time);
            }

            LastScrollWheel = Mouse.GetState().ScrollWheelValue;
            LastMouseX = Mouse.GetState().X;
            LastMouseY = Mouse.GetState().Y;
        }

        public void PreRender(DwarfTime time, SpriteBatch sprites)
        {
            RootComponent.PreRender(time, sprites);
        }

        public void PostRender(DwarfTime time)
        {
            if (IsMouseVisible)
            {
                MouseState mouse = Mouse.GetState();
                Skin.RenderMouse(mouse.X, mouse.Y, MouseScale, MouseMode, DwarfGame.SpriteBatch, MouseTint);
            }
            ToolTipManager.Render(Graphics, DwarfGame.SpriteBatch, time);
        }

        public void Render(DwarfTime time, SpriteBatch batch, Vector2 globalOffset)
        {
            GlobalOffset = globalOffset;


            RootComponent.LocalBounds = new Rectangle((int) globalOffset.X, (int) globalOffset.Y, 0, 0);
            RootComponent.UpdateTransformsRecursive();
            RootComponent.Render(time, batch);

            if(FocusComponent != null)
            {
                FocusComponent.Render(time, batch);
            }


            foreach(GUIComponent component in DrawAfter)
            {
                component.Render(time, batch);
            }


            DrawAfter.Clear();

            if(DebugDraw)
            {
                RootComponent.DebugRender(time, batch);
            }

        }

        public bool IsMouseOver()
        {
            return RootComponent.IsMouseOverRecursive();
        }
    }

}