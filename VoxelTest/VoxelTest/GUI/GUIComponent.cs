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
    /// <summary>
    /// Every element in the GUI is a GUI component. Components have children and parents. 
    /// They have rectangles where they exist on the screen. They may be clicked or accept other inputs.
    /// </summary>
    public class GUIComponent
    {
        public event ClickedDelegate OnClicked;
        public event ClickedDelegate OnLeftClicked;
        public event ClickedDelegate OnRightClicked;
        public event ClickedDelegate OnPressed;
        public event ClickedDelegate OnLeftPressed;
        public event ClickedDelegate OnRightPressed;
        public event MouseHoveredDelegate OnHover;
        public event ReleasedDelegate OnRelease;
        public event MouseUnHoveredDelegate OnUnHover;
        public event MouseScrolledDelegate OnScrolled;

        public int LastScrollWheel { get; set; }
        public GUIComponent Parent { get; set; }
        public List<GUIComponent> Children { get; set; }

        public string ToolTip = "";

        public Rectangle LocalBounds
        {
            get { return localBounds; }
            set
            {
                localBounds = value;
                GlobalBounds = Parent != null ? new Rectangle(Parent.GlobalBounds.X + LocalBounds.X, Parent.GlobalBounds.Y + LocalBounds.Y, LocalBounds.Width, LocalBounds.Height) : LocalBounds;
            }
        }

        public Rectangle GlobalBounds { get; set; }

        public DwarfGUI GUI { get; set; }
        public bool IsMouseOver { get; set; }
        public bool IsLeftPressed { get; set; }
        public bool IsRightPressed { get; set; }
        private Rectangle localBounds;
        public bool IsVisible { get; set; }

        public bool OverrideClickBehavior { get; set; }

        protected List<GUIComponent> ChildrenToRemove { get; set; }
        protected List<GUIComponent> ChildrenToAdd { get; set; }

        public GUIComponent(DwarfGUI gui, GUIComponent parent)
        {
            Children = new List<GUIComponent>();
            LocalBounds = new Rectangle();
            GlobalBounds = new Rectangle();
            GUI = gui;
            IsMouseOver = false;
            IsLeftPressed = false;
            IsRightPressed = false;
            Parent = parent;
            IsVisible = true;
            OverrideClickBehavior = false;
            if(parent != null)
            {
                Parent.AddChild(this);
            }
            OnClicked += dummy;
            OnLeftClicked += dummy;
            OnRightClicked += dummy;
            OnPressed += dummy;
            OnLeftPressed += dummy;
            OnRightPressed += dummy;
            OnHover += dummy;
            OnRelease += dummy;
            OnUnHover += dummy;
            OnScrolled += SillyGUIComponent_OnScrolled;

            ChildrenToRemove = new List<GUIComponent>();
            ChildrenToAdd = new List<GUIComponent>();
            LastScrollWheel = 0;
        }

        private void SillyGUIComponent_OnScrolled(int amount)
        {
        }


        public bool HasAnscestor(GUIComponent component)
        {
            if(Parent == component)
            {
                return true;
            }
            else if(Parent == null)
            {
                return false;
            }
            else
            {
                return Parent.HasAnscestor(component);
            }
        }

        public void dummy()
        {
        }

        public void AddChild(GUIComponent component)
        {
            if(!ChildrenToAdd.Contains(component))
            {
                ChildrenToAdd.Add(component);
            }
        }

        public void RemoveChild(GUIComponent component)
        {
            if(!ChildrenToRemove.Contains(component))
            {
                ChildrenToRemove.Add(component);
            }
        }

        public virtual bool IsMouseOverRecursive()
        {
            if(!IsVisible)
            {
                return false;
            }

            bool mouseOver =  (IsMouseOver && this != GUI.RootComponent) || Children.Any(child => child.IsMouseOverRecursive());
            
            /*
            List<GUIComponent> childrenMouseOver = Children.FindAll(child => child.IsMouseOverRecursive());

            if(childrenMouseOver.Count > 0)
            {
                return true;
            }
            */

            return mouseOver;
        }

        public void UpdateTransformsRecursive()
        {
            GlobalBounds = Parent != null ? new Rectangle(LocalBounds.Left + Parent.GlobalBounds.Left, LocalBounds.Top + Parent.GlobalBounds.Top, LocalBounds.Width, LocalBounds.Height) : LocalBounds;


            foreach(GUIComponent child in Children)
            {
                child.UpdateTransformsRecursive();
            }
        }

        private void HandleClicks(MouseState state)
        {
            if(IsMouseOver)
            {
                if(state.ScrollWheelValue != LastScrollWheel)
                {
                    OnScrolled(LastScrollWheel - state.ScrollWheelValue);
                    LastScrollWheel = state.ScrollWheelValue;
                }
            }

            if(state.LeftButton == ButtonState.Pressed)
            {
                if(!IsLeftPressed)
                {
                    IsLeftPressed = true;
                    OnLeftPressed();
                    OnPressed();
                }
            }
            else
            {
                if(IsLeftPressed)
                {
                    OnLeftClicked();
                    OnClicked();
                    IsLeftPressed = false;
                    OnRelease();
                }
            }

            if(state.RightButton == ButtonState.Pressed)
            {
                if(IsRightPressed)
                {
                    return;
                }

                IsRightPressed = true;
                OnRightPressed();
                OnPressed();
            }
            else
            {
                if(!IsRightPressed)
                {
                    return;
                }

                OnRightClicked();
                OnClicked();
                IsRightPressed = false;
                OnRelease();
            }
        }

        public void ClearChildren()
        {
            foreach(GUIComponent child in Children)
            {
                RemoveChild(child);
            }
        }

        public virtual void Update(GameTime time)
        {
            if(!IsVisible)
            {
                return;
            }

            foreach(GUIComponent child in Children)
            {
                child.Update(time);
            }

            MouseState state = Mouse.GetState();


            if(OverrideClickBehavior)
            {
                HandleClicks(state);
            }
            else if(GlobalBounds.Contains(state.X, state.Y))
            {
                if(IsMouseOver)
                {
                    HandleClicks(state);
                }

                if(!IsMouseOver)
                {
                    IsMouseOver = true;
                    OnHover();
                }
            }
            else if(IsMouseOver)
            {
                IsMouseOver = false;
                OnUnHover();
                IsLeftPressed = false;
                IsRightPressed = false;
            }


            foreach(GUIComponent child in ChildrenToAdd)
            {
                Children.Add(child);
            }
            ChildrenToAdd.Clear();

            foreach(GUIComponent child in ChildrenToRemove)
            {
                if(!Children.Remove(child))
                {
                    Console.Out.WriteLine("Something's wrong with removing child...");
                }
            }
            ChildrenToRemove.Clear();
        }

        protected Rectangle ClipToScreen(Rectangle rect, GraphicsDevice device)
        {
            const int minScreenX = 0;
            const int minScreenY = 0;
            int maxScreenX = device.Viewport.Bounds.Right;
            int maxScreenY = device.Viewport.Bounds.Bottom;
            int x = Math.Min(Math.Max(minScreenX, rect.X), maxScreenX);
            int y = Math.Min(Math.Max(minScreenY, rect.Y), maxScreenY);
            int maxX = Math.Max(Math.Min(rect.Right, maxScreenX), minScreenX);
            int maxY = Math.Max(Math.Min(rect.Bottom, maxScreenY), minScreenY);

            return new Rectangle(x, y, Math.Abs(maxX - x), Math.Abs(maxY - y));
        }

        public virtual void PreRender(GameTime time, SpriteBatch sprites)
        {
            foreach(GUIComponent child in Children)
            {
                child.PreRender(time, sprites);
            }
        }

        public virtual void PostRender(GameTime time)
        {
            foreach (GUIComponent child in Children)
            {
                child.PostRender(time);
            }
        }

        public virtual void Render(GameTime time, SpriteBatch batch)
        {
            if(!IsVisible)
            {
                return;
            }

            foreach(GUIComponent child in Children)
            {
                child.Render(time, batch);
            }
        }

        public virtual void DebugRender(GameTime time, SpriteBatch batch)
        {
            Drawer2D.DrawRect(batch, GlobalBounds, IsMouseOver ? Color.Red : Color.White, 1);


            foreach (GUIComponent child in Children)
            {
                child.DebugRender(time, batch);
            }
        }
    }

}