using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{

    public delegate void ClickedDelegate();

    public delegate void PressedDelegate();

    public delegate void MouseHoveredDelegate();

    public delegate void MouseUnHoveredDelegate();

    public delegate void ReleasedDelegate();

    public delegate void MouseScrolledDelegate(int amount);

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

        public DwarfGUI(DwarfGame game, SpriteFont defaultFont, SpriteFont titleFont, SpriteFont smallFont, InputManager input)
        {
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
            Skin = new GUISkin(TextureManager.GetTexture("GUISheet"), 32, 32, TextureManager.GetTexture(ContentPaths.GUI.pointers), 16, 16);
            Skin.SetDefaults();
            TitleFont = titleFont;
            GlobalOffset = Vector2.Zero;
            FocusComponent = null;
            Input = input;
            DrawAfter = new List<GUIComponent>();
            DefaultTextColor = new Color(48, 27, 0);
            DefaultStrokeColor = new Color(100, 100, 100, 100);
            DebugDraw = false;
            ToolTipManager = new ToolTipManager(this);
        }


        public static Rectangle AspectRatioFit(Rectangle sourceArea, Rectangle fitArea)
        {
            float[] ratios = { (float)fitArea.Width / (float)sourceArea.Width, (float)fitArea.Height / (float)sourceArea.Height };
                float minRatio = Math.Min(ratios[0], ratios[1]);
                float ratio = minRatio;

                return new Rectangle(fitArea.X, fitArea.Y, (int)(sourceArea.Width * ratio), (int) (sourceArea.Height * ratio));
        }

        public void Update(GameTime time)
        {

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

        }

        public void PreRender(GameTime time, SpriteBatch sprites)
        {
            RootComponent.PreRender(time, sprites);
        }

        public void PostRender(GameTime time)
        {
            
        }

        public void Render(GameTime time, SpriteBatch batch, Vector2 globalOffset)
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
            ToolTipManager.Render(Graphics, batch, time);

            if(DebugDraw)
            {
                RootComponent.DebugRender(time, batch);
            }

            if(IsMouseVisible)
            {
                MouseState mouse = Mouse.GetState();
                Skin.RenderMouse(mouse.X, mouse.Y, MouseScale, MouseMode, batch, MouseTint);
            }
        }




        public bool IsMouseOver()
        {
            return RootComponent.IsMouseOverRecursive();
        }
    }

}