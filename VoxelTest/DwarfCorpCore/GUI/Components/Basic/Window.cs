using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    public class Window : Panel
    {
        public Rectangle DragArea { get; set; }
        public Rectangle ResizeArea { get; set; }
        public bool IsDragging { get; set; }
        public bool IsResizing { get; set; }
        public Point DragStart { get; set; }
        public Point ResizeStartSize { get; set; }
        public Point ResizeStartPosition { get; set; }
        public Button CloseButton { get; set; }

        public enum WindowButtons
        {
            NoButtons,
            CloseButton
        }

        public Window(DwarfGUI gui, GUIComponent parent, WindowButtons buttons = WindowButtons.NoButtons) 
            : base(gui, parent)
        {
            IsDragging = false;
            IsResizing = false;
            Mode = PanelMode.Window;

            if (buttons == WindowButtons.CloseButton)
            {
                CloseButton = new Button(GUI, this, "", GUI.DefaultFont, Button.ButtonMode.ImageButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.SmallEx));
                CloseButton.OnClicked += CloseButton_OnClicked;
            }
        }

        void CloseButton_OnClicked()
        {
            IsVisible = false;
        }


        void Window_OnPressed()
        {
            if (IsDragging || IsResizing)
            {
                return;
            }

            MouseState mouseState = Mouse.GetState();

            if (mouseState.LeftButton != ButtonState.Pressed)
            {
                return;
            }

            if (DragArea.Contains(mouseState.X, mouseState.Y))
            {
                IsDragging = true;
                DragStart = new Point(-LocalBounds.X + mouseState.X, -LocalBounds.Y + mouseState.Y);
            }
            else if (ResizeArea.Contains(mouseState.X, mouseState.Y))
            {
                IsResizing = true;
                ResizeStartSize = new Point(LocalBounds.Width, LocalBounds.Height);
                ResizeStartPosition = new Point(mouseState.X, mouseState.Y);
            }
        }

        public override void Update(GameTime time)
        {
            if (IsVisible)
            {
                UpdateAreas();
                Window_OnPressed();

                if (IsDragging)
                {
                    Drag();
                }

                if (IsResizing)
                {
                    Resize();
                }
            }
            base.Update(time);
        }

        public void Resize()
        {
            MouseState mouseState = Mouse.GetState();
            if (mouseState.LeftButton != ButtonState.Pressed)
            {
                IsResizing = false;
                return;
            }
            int mx = mouseState.X;
            int my = mouseState.Y;
            int dx = mx - ResizeStartPosition.X;
            int dy = my - ResizeStartPosition.Y;
            LocalBounds = new Rectangle(LocalBounds.X, LocalBounds.Y, Math.Max(ResizeStartSize.X + dx, 64), Math.Max(ResizeStartSize.Y + dy, 64));

        }

        public void Drag()
        {
            MouseState mouseState = Mouse.GetState();
            if (mouseState.LeftButton != ButtonState.Pressed)
            {
                IsDragging = false;
                return;
            }
            int mx = mouseState.X;
            int my = mouseState.Y;
            int x = mx - DragStart.X;
            int y = my - DragStart.Y;
            LocalBounds = new Rectangle(x, y, Math.Max(LocalBounds.Width, 64), Math.Max(LocalBounds.Height, 64));

        }


        public override bool IsMouseOverRecursive()
        {
            MouseState mouseState = Mouse.GetState();
            Rectangle expanded = GlobalBounds;
            expanded.Inflate(32, 32);
            return IsVisible && (expanded.Contains(mouseState.X, mouseState.Y) || DragArea.Contains(mouseState.X, mouseState.Y) || ResizeArea.Contains(mouseState.X, mouseState.Y) || base.IsMouseOverRecursive());
        }

        public virtual void UpdateAreas()
        {
            DragArea = new Rectangle(GlobalBounds.X - 32, GlobalBounds.Y - 32, GlobalBounds.Width + 64, 48);
            ResizeArea = new Rectangle(GlobalBounds.Right - 32, GlobalBounds.Bottom - 32, 64, 64);

            if(CloseButton != null)
                CloseButton.LocalBounds = new Rectangle(GlobalBounds.Width - 5, -31,  32, 32);
        }
    }
}
