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

    public class SillyGUI
    {
        public SillyGUIComponent RootComponent { get; set; }
        DwarfGame m_game = null;
        public SpriteFont DefaultFont { get; set; }
        public SpriteFont SmallFont { get; set; }
        public SpriteFont TitleFont { get; set; }
        public GUISkin Skin { get; set; }
        public Vector2 GlobalOffset { get; set; }
        public SillyGUIComponent FocusComponent { get; set; }
        public InputManager Input { get; set; }
        public List<SillyGUIComponent> DrawAfter { get; set; }
        public Color DefaultTextColor { get; set; }
        public Color DefaultStrokeColor { get; set; }
        public GraphicsDevice Graphics { get; set; }

        public SillyGUI(DwarfGame game, SpriteFont defaultFont, SpriteFont titleFont, SpriteFont smallFont, InputManager input)
        {
            SmallFont = smallFont;
            Graphics = game.GraphicsDevice;
            m_game = game;
            RootComponent = new SillyGUIComponent(this, null);
            RootComponent.LocalBounds = new Rectangle(0, 0, 0, 0);
            DefaultFont = defaultFont;
            Skin = new GUISkin(TextureManager.GetTexture("GUISheet"), 32, 32);
            Skin.SetDefaults();
            TitleFont = titleFont;
            GlobalOffset = Vector2.Zero;
            FocusComponent = null;
            Input = input;
            DrawAfter = new List<SillyGUIComponent>();
            DefaultTextColor = new Color(48, 27, 0);
            DefaultStrokeColor = new Color(100, 100, 100, 100);
        }

        public void Update(GameTime time)
        {
            if (!m_game.IsMouseVisible)
            {
                return;
            }

            
            if (FocusComponent == null)
            {
                RootComponent.Update(time);
            }
            else
            {
                FocusComponent.Update(time);
            }
            
        
        }

        public void Render(GameTime time, SpriteBatch batch, Vector2 globalOffset)
        {
            GlobalOffset = globalOffset;

            RootComponent.LocalBounds = new Rectangle((int)globalOffset.X, (int)globalOffset.Y, 0, 0);
            RootComponent.UpdateTransformsRecursive();
            RootComponent.Render(time, batch);

            if (FocusComponent != null)
            {
                FocusComponent.Render(time, batch);
            }

            
            foreach (SillyGUIComponent component in DrawAfter)
            {
                component.Render(time, batch);
            }
             

            DrawAfter.Clear();
        }

        public bool IsMouseOver()
        {
            return RootComponent.IsMouseOverRecursive();
        }
    }
}
