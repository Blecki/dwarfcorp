using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    /// <summary>
    /// Tooltips are the little bits of text which appear on the screen when a mouse is hovering
    /// over a particular GUI element. Every GUI element can have a tooltip. They are used to help the player
    /// understand what certain buttons do.
    /// </summary>
    public class ToolTipManager
    {
        public DwarfGUI GUI { get; set; }

        public Timer HoverTimer { get; set; }

        public string ToolTip { get; set; }

        private MouseState LastMouse { get; set; }

        public int MovementThreshold { get; set; }

        public ToolTipManager(DwarfGUI gui)
        {
            GUI = gui;
            HoverTimer = new Timer(0.8f, true);
            ToolTip = "";
            LastMouse = Mouse.GetState();
            MovementThreshold = 2;
        }

        public void Update(GameTime time)
        {
            MouseState currentMouse = Mouse.GetState();

            int movement = Math.Abs(LastMouse.X - currentMouse.X) + Math.Abs(LastMouse.Y - currentMouse.Y);

            if(ToolTip != "" && movement > MovementThreshold)
            {
                ToolTip = "";
                HoverTimer.Reset(HoverTimer.TargetTimeSeconds);
            }
            else if(ToolTip == "" && movement < MovementThreshold)
            {
                HoverTimer.Update(time);

                if(HoverTimer.HasTriggered)
                {
                    List<string> tips = new List<string>();
                    GetToolTipsUnderMouseRecursive(GUI.RootComponent, tips);

                    ToolTip = tips.Count > 0 ? tips.Last() : "";
                }
            }

            LastMouse = currentMouse;

        }

        public void GetToolTipsUnderMouseRecursive(GUIComponent root, List<string> tips)
        {
            if(root.IsMouseOver && !string.IsNullOrEmpty(root.ToolTip))
            {
                tips.Add(root.ToolTip);
            }

            foreach(GUIComponent component in root.Children)
            {
                GetToolTipsUnderMouseRecursive(component, tips);   
            }
        }

        public void Render(GraphicsDevice device, SpriteBatch batch, GameTime time)
        {
            if (string.IsNullOrEmpty(ToolTip))
            {
                return;
            }

            Rectangle viewBounds = device.Viewport.Bounds;

            MouseState mouse = Mouse.GetState();

            Vector2 stringMeasure = Datastructures.SafeMeasure(GUI.SmallFont, ToolTip);

            Rectangle bounds = new Rectangle(mouse.X + 16, mouse.Y + 16, (int)(stringMeasure.X + 15), (int)(stringMeasure.Y + 15));

            if(bounds.Left < viewBounds.Left)
            {
                bounds.X = viewBounds.X;
            }

            if(bounds.Right > viewBounds.Right)
            {
                bounds.X = viewBounds.Right - bounds.Width;
            }

            if(bounds.Top < viewBounds.Top)
            {
                bounds.Y = viewBounds.Y;
            }

            if(bounds.Bottom > viewBounds.Bottom)
            {
                bounds.Y = viewBounds.Bottom - bounds.Height;
            }

            GUI.Skin.RenderToolTip(bounds, batch);
            Drawer2D.DrawAlignedText(batch, ToolTip, GUI.SmallFont, Color.White, Drawer2D.Alignment.Center, bounds);
        }


    }
}
