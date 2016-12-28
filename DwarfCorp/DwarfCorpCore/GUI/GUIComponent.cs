// GUIComponent.cs
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
        #region properties
        /// <summary>
        /// Occurs when the GUI component updated
        /// </summary>
        public event UpdateDelegate OnUpdate;
        /// <summary>
        /// Occurs when the GUI component is rendered.
        /// </summary>
        public event RenderDelegate OnRender;
        /// <summary>
        /// Occurs when the GUI component is clicked (pressed then released).
        /// </summary>
        public event ClickedDelegate OnClicked;
        /// <summary>
        /// Occurs when the GUI component is left clicked.
        /// </summary>
        public event ClickedDelegate OnLeftClicked;
        /// <summary>
        /// Occurs when the GUI component is right clicked.
        /// </summary>
        public event ClickedDelegate OnRightClicked;
        /// <summary>
        /// Occurs when the GUI component is pressed by the mouse.
        /// </summary>
        public event ClickedDelegate OnPressed;
        /// <summary>
        /// Occurs when the GUI component is pressed by the left mouse.
        /// </summary>
        public event ClickedDelegate OnLeftPressed;
        /// <summary>
        /// Occurs when the GUI component is pressed by the right mouse.
        /// </summary>
        public event ClickedDelegate OnRightPressed;
        /// <summary>
        /// Occurs when the player hovers the mouse over the GUI component.
        /// </summary>
        public event MouseHoveredDelegate OnHover;
        /// <summary>
        /// Occurs when the player has released a mouse button while over the component.
        /// </summary>
        public event ReleasedDelegate OnRelease;
        /// <summary>
        /// Occurs when the player has stopped hovering over the component.
        /// </summary>
        public event MouseUnHoveredDelegate OnUnHover;
        /// <summary>
        /// Occurs when the mouse wheel is scrolled while over the component.
        /// </summary>
        public event MouseScrolledDelegate OnScrolled;
        /// <summary>
        /// Occurs when the player drags the mouse over the component while a button is pressed.
        /// </summary>
        public event MouseDraggedDelegate OnDragged;

        /// <summary>
        /// Gets or sets the parent of the component.
        /// </summary>
        /// <value>
        /// The parent.
        /// </value>
        public GUIComponent Parent { get; set; }
        /// <summary>
        /// Gets or sets the children of the component.
        /// </summary>
        /// <value>
        /// The children.
        /// </value>
        public List<GUIComponent> Children { get; set; }

        /// <summary>
        /// The tool tip displayed when the player mouses over the component.
        /// </summary>
        public string ToolTip = "";

        /// <summary>
        /// Gets or sets the local bounds with respect to the parent.
        /// </summary>
        /// <value>
        /// The local bounds.
        /// </value>
        public Rectangle LocalBounds
        {
            get { return localBounds; }
            set
            {
                localBounds = value;
                GlobalBounds = Parent != null ? new Rectangle(Parent.GlobalBounds.X + LocalBounds.X, Parent.GlobalBounds.Y + LocalBounds.Y, LocalBounds.Width, LocalBounds.Height) : LocalBounds;
            }
        }

        /// <summary>
        /// Gets or sets the global bounds of the component with respect to the screen.
        /// </summary>
        /// <value>
        /// The global bounds.
        /// </value>
        public Rectangle GlobalBounds { get; protected set; }
        /// <summary>
        /// Gets or sets the GUI.
        /// </summary>
        /// <value>
        /// The GUI.
        /// </value>
        public DwarfGUI GUI { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is mouse over.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is mouse over; otherwise, <c>false</c>.
        /// </value>
        public bool IsMouseOver { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is left pressed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is left pressed; otherwise, <c>false</c>.
        /// </value>
        public bool IsLeftPressed { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this instance is right pressed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is right pressed; otherwise, <c>false</c>.
        /// </value>
        public bool IsRightPressed { get; set; }
        /// <summary>
        /// The local bounds
        /// </summary>
        private Rectangle localBounds;
        /// <summary>
        /// Gets or sets a value indicating whether this instance is visible.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is visible; otherwise, <c>false</c>.
        /// </value>
        public bool IsVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to ignore click events when the GUI component is clicked.
        /// </summary>
        /// <value>
        /// <c>true</c> if [override click behavior]; otherwise, <c>false</c>.
        /// </value>
        public bool OverrideClickBehavior { get; set; }

        /// <summary>
        /// Gets or sets the children to remove this update frame.
        /// </summary>
        /// <value>
        /// The children to remove.
        /// </value>
        protected List<GUIComponent> ChildrenToRemove { get; set; }
        /// <summary>
        /// Gets or sets the children to add this update frame.
        /// </summary>
        /// <value>
        /// The children to add.
        /// </value>
        protected List<GUIComponent> ChildrenToAdd { get; set; }
        /// <summary>
        /// Gets or sets the draw order. The draw order is an arbitrary floating point
        /// value. GUI components are sorted by this value, with the smallest ones getting drawn first.
        /// </summary>
        /// <value>
        /// The draw order.
        /// </value>
        public float DrawOrder { get; set; }

        /// <summary>
        /// This defines the behavior of the component whenever it is inside a layout. 
        /// </summary>
        public enum SizeMode
        {
            /// <summary>
            /// Fixed components do not change their size while in layouts.
            /// </summary>
            Fixed,
            /// <summary>
            /// Fitted components maximize their size to fit inside layouts.
            /// </summary>
            Fit
        };


        /// <summary>
        /// Gets or sets the width size mode.
        /// </summary>
        /// <value>
        /// The width size mode.
        /// </value>
        public SizeMode WidthSizeMode { get; set; }
        /// <summary>
        /// Gets or sets the height size mode.
        /// </summary>
        /// <value>
        /// The height size mode.
        /// </value>
        public SizeMode HeightSizeMode { get; set; }
        /// <summary>
        /// Gets or sets the minimum width.
        /// </summary>
        /// <value>
        /// The minimum width.
        /// </value>
        public int MinWidth { get; set; }
        /// <summary>
        /// Gets or sets the minimum height.
        /// </summary>
        /// <value>
        /// The minimum height.
        /// </value>
        public int MinHeight { get; set; }
        /// <summary>
        /// Gets or sets the maximum width.
        /// </summary>
        /// <value>
        /// The maximum width.
        /// </value>
        public int MaxWidth { get; set; }
        /// <summary>
        /// Gets or sets the maximum height.
        /// </summary>
        /// <value>
        /// The maximum height.
        /// </value>
        public int MaxHeight { get; set; }

        /// <summary>
        /// A clipping bounds can be passed into the GUI. This value indicates whether or not
        /// the GUI component is inside the clipping bounds. If it is outside, it is not updated
        /// or rendered.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is clipped; otherwise, <c>false</c>.
        /// </value>
        public bool IsClipped { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to trigger mouse over related events.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [trigger mouse over]; otherwise, <c>false</c>.
        /// </value>
        public bool TriggerMouseOver { get; set; }


        #endregion

        /// <summary>
        /// A GUITween is an animation that causes a GUI component to move smoothly relative
        /// to its parent.
        /// </summary>
        public class GUITween
        {
            /// <summary>
            /// Gets or sets the tween function. <see cref="Easing"/>
            /// </summary>
            /// <value>
            /// The tween function.
            /// </value>
            public Func<float, float, float, float, float> TweenFn { get; set; }
            /// <summary>
            /// Gets or sets the tween timer. The tween timer is the amount of time 
            /// that has passed during the tween animation.
            /// </summary>
            /// <value>
            /// The tween timer.
            /// </value>
            public Timer TweenTimer { get; set; }

            /// <summary>
            /// A tween type defines whether the GUI component is getting created
            /// by this animation, destroyed, or merely moved.
            /// </summary>
            public enum TweenType
            {
                /// <summary>
                /// When tweening in, the GUI component will be inactive until the tween ends.
                /// </summary>
                TweenIn,
                /// <summary>
                /// When tweening out, the GUI component will be destroyed after the tween ends.
                /// </summary>
                TweenOut,
                /// <summary>
                /// When animating, the GUI component merely moves during the tween.
                /// </summary>
                TweenAnimate
            }

            /// <summary>
            /// Gets or sets the tween type.
            /// </summary>
            /// <value>
            /// The tween type.
            /// </value>
            public TweenType Tween { get; set; }

            /// <summary>
            /// The tween starts with these local bounds.
            /// </summary>
            /// <value>
            /// The start in local bounds.
            /// </value>
            public Rectangle Start { get; set; }
            /// <summary>
            /// The tween ends with these local bounds.
            /// </summary>
            /// <value>
            /// The ending local bounds.
            /// </value>
            public Rectangle End { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="GUITween"/> class.
            /// </summary>
            public GUITween()
            {
                
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="GUITween"/> class.
            /// </summary>
            /// <param name="time">The time in seconds to tween for.</param>
            public GUITween(float time)
            {
                TweenTimer = new Timer(time, true, Timer.TimerMode.Real);
            }

            /// <summary>
            /// Gets the current rectangle during animation.
            /// </summary>
            /// <returns>The current rectangle</returns>
            public Rectangle GetCurrentRect()
            {
                if (TweenTimer.HasTriggered)
                {
                    return End;
                }
                else
                {
                    float t = TweenFn(TweenTimer.CurrentTimeSeconds, 0.0f, 1.0f, TweenTimer.TargetTimeSeconds);
                    return MathFunctions.Lerp(Start, End, t);
                }
            }

            /// <summary>
            /// Updates the tween animation.
            /// </summary>
            /// <param name="time">The current time.</param>
            public void Update(DwarfTime time)
            {
                TweenTimer.Update(time);
            }
            
        }

        /// <summary>
        /// Gets or sets the tweens. Tweens are animations that the GUI component is
        /// undergoing (it is a FIFO queue)
        /// </summary>
        /// <value>
        /// The tweens.
        /// </value>
        public List<GUITween> Tweens { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GUIComponent"/> class.
        /// </summary>
        /// <param name="gui">The GUI.</param>
        /// <param name="parent">The parent.</param>
        public GUIComponent(DwarfGUI gui, GUIComponent parent)
        {
            TriggerMouseOver = true;
            DrawOrder = -1;
            WidthSizeMode = SizeMode.Fixed;
            HeightSizeMode = SizeMode.Fixed;
            MinWidth = -1;
            MinHeight = -1;
            MaxWidth = -1;
            MaxHeight = -1;
            Children = new List<GUIComponent>();
            LocalBounds = new Rectangle();
            GlobalBounds = new Rectangle();
            GUI = gui;
            IsMouseOver = false;
            IsLeftPressed = false;
            IsRightPressed = false;
            IsClipped = false;
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
            OnUpdate += dummy;
            OnRender += dummy;
            OnDragged += GUIComponent_OnDragged;
            OnScrolled += GUIComponent_OnScrolled;

            ChildrenToRemove = new List<GUIComponent>();
            ChildrenToAdd = new List<GUIComponent>();
            Tweens = new List<GUITween>();
        }

        /// <summary>
        /// Called whenever the component is dragged.
        /// </summary>
        /// <param name="button">The button.</param>
        /// <param name="delta">The motion made by the mouse when dragged</param>
        void GUIComponent_OnDragged(InputManager.MouseButton button, Vector2 delta)
        {
    
        }


        /// <summary>
        /// Initializes a new instance of the <see cref="GUIComponent"/> class.
        /// </summary>
        protected GUIComponent()
        {
           
        }

        /// <summary>
        /// Invokes the click delegate.
        /// </summary>
        public void InvokeClick()
        {
            OnClicked();
        }

        /// <summary>
        /// Called whenever the mouse wheel scrolls while the mouse is over the GUI component
        /// </summary>
        /// <param name="amount">The amount (in ticks)</param>
        private void GUIComponent_OnScrolled(int amount)
        {
        }


        /// <summary>
        /// Determines whether the specified component is an anscestor of this component.
        /// The component is an ancestor if this component is in the recursive list of
        /// children of the anscestor.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <returns>
        ///   <c>true</c> if the specified component is an anscestor; otherwise, <c>false</c>.
        /// </returns>
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

        /// <summary>
        /// Just ensures that all callbacks are registered with something.
        /// </summary>
        public void dummy()
        {
        }

        /// <summary>
        /// Adds the child on the next update cycle.
        /// </summary>
        /// <param name="component">The component to add</param>
        public void AddChild(GUIComponent component)
        {
            if(!ChildrenToAdd.Contains(component))
            {
                ChildrenToAdd.Add(component);
            }
        }

        /// <summary>
        /// Removes the child on the next update cycle.
        /// </summary>
        /// <param name="component">The component to remove</param>
        public void RemoveChild(GUIComponent component)
        {
            if(!ChildrenToRemove.Contains(component))
            {
                ChildrenToRemove.Add(component);
            }
        }

        /// <summary>
        /// Determines whether the mouse is over this component or any of its downstream children..
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is mouse over recursive]; otherwise, <c>false</c>.
        /// </returns>
        public virtual bool IsMouseOverRecursive()
        {
            if(!IsVisible || !TriggerMouseOver)
            {
                return false;
            }

            bool mouseOver =  (IsMouseOver && this != GUI.RootComponent) || Children.Any(child => child.IsMouseOverRecursive());
            return mouseOver;
        }

        /// <summary>
        /// Updates the GlobalBounds of this GUIComponent and all its children.
        /// </summary>
        public void UpdateTransformsRecursive()
        {
            GlobalBounds =
                Parent != null ?
                new Rectangle(LocalBounds.Left + Parent.GlobalBounds.Left, 
                              LocalBounds.Top + Parent.GlobalBounds.Top, 
                              LocalBounds.Width, LocalBounds.Height) : LocalBounds;


            foreach(GUIComponent child in Children)
            {
                child.UpdateTransformsRecursive();
            }
        }

        /// <summary>
        /// Determines whether this GUIComponent's parent or any of its ancestors are visible.
        /// </summary>
        /// <returns>True if the parent or any of its ancestors are visible.</returns>
        public bool ParentVisibleRecursive()
        {
            if (Parent != null)
                return Parent.IsVisible && Parent.ParentVisibleRecursive();

            return true;
        }

        /// <summary>
        /// Handles clicks, drags and mouse scroll wheel motion.
        /// </summary>
        /// <param name="state">The mouse state.</param>
        private void HandleClicks(MouseState state)
        {
            if(IsMouseOver)
            {
                if(state.ScrollWheelValue != GUI.LastScrollWheel)
                {
                    OnScrolled(GUI.LastScrollWheel - state.ScrollWheelValue);
                }

                if (state.LeftButton == ButtonState.Pressed)
                {
                    OnDragged(InputManager.MouseButton.Left, new Vector2(state.X - GUI.LastMouseX, state.Y - GUI.LastMouseY));
                }


                if (state.RightButton == ButtonState.Pressed)
                {
                    OnDragged(InputManager.MouseButton.Right, new Vector2(state.X - GUI.LastMouseX, state.Y - GUI.LastMouseY));
                }

                if (state.MiddleButton == ButtonState.Pressed)
                {
                    OnDragged(InputManager.MouseButton.Middle, new Vector2(state.X - GUI.LastMouseX, state.Y - GUI.LastMouseY));
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

        /// <summary>
        /// Clears all children and resets this component as an empty GUIComponent.
        /// </summary>
        public void Reset()
        {
            foreach (GUIComponent child in Children)
            {
                child.ClearChildren();
            }

            foreach (GUIComponent child in ChildrenToAdd)
            {
                child.ClearChildren();
            }

            Children.Clear();
            ChildrenToAdd.Clear();
            ChildrenToRemove.Clear();
        }

        /// <summary>
        /// Clears the children of this instance.
        /// </summary>
        public void ClearChildren()
        {
            foreach(GUIComponent child in Children)
            {
                RemoveChild(child);
            }
        }

        /// <summary>
        /// Animate the GUI component in from a side of the screen.
        /// </summary>
        /// <param name="alignment">The side of the screen to tween from.</param>
        /// <param name="time">The time in seconds to tween</param>
        /// <param name="tweenFn">The tween function <see cref="Easing"/></param>
        public void TweenIn(Drawer2D.Alignment alignment, float time = 0.5f, Func<float, float, float, float, float> tweenFn = null)
        {
            Point start = new Point(0, 0);
            switch (alignment)
            {
                   case Drawer2D.Alignment.Bottom:
                    start = new Point(LocalBounds.X, Parent.LocalBounds.Height);
                    break;
                   case Drawer2D.Alignment.Top:
                    start = new Point(LocalBounds.X, -LocalBounds.Height);
                    break;
                   case Drawer2D.Alignment.Left:
                    start = new Point(-LocalBounds.Width, LocalBounds.Y);
                    break;
                   case Drawer2D.Alignment.Right:
                    start = new Point(Parent.LocalBounds.Width, LocalBounds.Y);
                    break;
            }
            TweenIn(start, time, tweenFn);
        }

        /// <summary>
        /// Animates the GUI component off of the screen and then destroys it.
        /// </summary>
        /// <param name="alignment">The side of the screen to animate off of.</param>
        /// <param name="time">The time in seconds to animate.</param>
        /// <param name="tweenFn">The tween function <see cref="Easing"/>.</param>
        public void TweenOut(Drawer2D.Alignment alignment, float time = 0.5f, Func<float, float, float, float, float> tweenFn = null)
        {
            Point end = new Point(0, 0);
            switch (alignment)
            {
                case Drawer2D.Alignment.Bottom:
                    end = new Point(LocalBounds.X, Parent.LocalBounds.Height);
                    break;
                case Drawer2D.Alignment.Top:
                    end = new Point(LocalBounds.X, -LocalBounds.Height);
                    break;
                case Drawer2D.Alignment.Left:
                    end = new Point(-LocalBounds.Width, LocalBounds.Y);
                    break;
                case Drawer2D.Alignment.Right:
                    end = new Point(Parent.LocalBounds.Width, LocalBounds.Y);
                    break;
            }
            TweenOut(end, time, tweenFn);
        }

        /// <summary>
        /// Animate the GUI component onto the screen from some starting point.
        /// </summary>
        /// <param name="start">The starting point of the animation.</param>
        /// <param name="time">The time in seconds to animate.</param>
        /// <param name="tweenFn">The tween function <see cref="Easing"/></param>
        public void TweenIn(Point start, float time = 0.5f,  Func<float, float, float, float, float> tweenFn = null)
        {
            if (tweenFn == null)
            {
                tweenFn = Easing.CubicEaseInOut;
            }
            GUITween guiTween = new GUITween(time + MathFunctions.Rand()*0.1f)
            {
                End = LocalBounds,
                Start = new Rectangle(start.X, start.Y, LocalBounds.Width, LocalBounds.Height),
                Tween = GUITween.TweenType.TweenIn,
                TweenFn = tweenFn
            };
            
            Tweens.Add(guiTween);
            LocalBounds = guiTween.Start;
            IsVisible = true;
        }

        /// <summary>
        /// Animate the GUI component off of the screen and then destroy it.
        /// </summary>
        /// <param name="end">The end point of the animation.</param>
        /// <param name="time">The time in seconds of the animation.</param>
        /// <param name="tweenFn">The tween function <see cref="Easing"/></param>
        public void TweenOut(Point end, float time = 0.5f, Func<float, float, float, float, float> tweenFn = null)
        {
            if (tweenFn == null)
            {
                tweenFn = Easing.CubicEaseInOut;
            }

            Tweens.Add(new GUITween(time + MathFunctions.Rand() * 0.1f)
            {
                Start = LocalBounds,
                End = new Rectangle(end.X, end.Y, LocalBounds.Width, LocalBounds.Height),
                Tween = GUITween.TweenType.TweenOut,
                TweenFn = tweenFn
            });
        }

        /// <summary>
        /// Given a clip bounds, clips this GUI component and all its children
        /// recursively.
        /// </summary>
        /// <param name="clip">The clip rectangle on the screen.</param>
        public void ClipRecursive(Rectangle clip)
        {
            IsClipped = !GlobalBounds.Intersects(clip);

            foreach (GUIComponent component in Children)
            {
                component.ClipRecursive(clip);
            }
        }

        /// <summary>
        /// Updates this GUI component.
        /// </summary>
        /// <param name="time">The current time.</param>
        public virtual void Update(DwarfTime time)
        {
            if(!IsVisible)
            {
                return;
            }

            UpdateSize();

            if (Tweens.Count > 0)
            {
                GUITween currTween = Tweens.First();
                currTween.Update(time);
                LocalBounds = currTween.GetCurrentRect();
                if (currTween.TweenTimer.HasTriggered)
                {
                    switch (currTween.Tween)
                    {
                        case GUITween.TweenType.TweenAnimate:
                            break;
                        case GUITween.TweenType.TweenIn:
                            break;
                        case GUITween.TweenType.TweenOut:
                            IsVisible = false;
                            LocalBounds = currTween.Start;
                            break;
                    }

                    Tweens.RemoveAt(0);
                }
            }

            OnUpdate.Invoke();

            foreach(GUIComponent child in Children)
            {
                child.Update(time);
            }

            MouseState state = Mouse.GetState();

            if (GUI.EnableMouseEvents)
            {
                if (OverrideClickBehavior)
                {
                    HandleClicks(state);
                }
                else if (!IsClipped  && GlobalBounds.Contains(state.X, state.Y))
                {
                    if (IsMouseOver)
                    {
                        HandleClicks(state);
                    }

                    if (!IsMouseOver)
                    {
                        IsMouseOver = true;
                        OnHover();
                    }
                }
                else if (IsMouseOver)
                {
                    IsMouseOver = false;
                    OnUnHover();
                    IsLeftPressed = false;
                    IsRightPressed = false;
                }
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

        /// <summary>
        /// Update the size of this GUI component and all its children, taking
        /// into account the SizeMode.
        /// </summary>
        public virtual void UpdateSizeRecursive()
        {
            UpdateSize();

            foreach(GUIComponent child in Children)
            {
                child.UpdateSizeRecursive();
            }

            UpdateTransformsRecursive();
        }

        /// <summary>
        /// Updates the size of this component, taking into account the
        /// SizeMode.
        /// </summary>
        public virtual void UpdateSize()
        {
            int w = LocalBounds.Width;
            int h = LocalBounds.Height;
            switch (WidthSizeMode)
            {
                case SizeMode.Fixed:
                    break;
                case SizeMode.Fit:
                    w = Parent.LocalBounds.Width;
                    break;
            }

            switch (HeightSizeMode)
            {
                case SizeMode.Fixed:
                    break;
                case SizeMode.Fit:
                    h = Parent.LocalBounds.Height;
                    break;   
            }

            LocalBounds = new Rectangle(LocalBounds.X, LocalBounds.Y, w, h);
            ClipSizes();

        }

        /// <summary>
        /// Clips the size of the component so that it fits the maximum
        /// and minimum sizes.
        /// </summary>
        public virtual void ClipSizes()
        {
            int w = LocalBounds.Width;
            int h = LocalBounds.Height;
            if (MinWidth > 0)
            {
                w = Math.Max(MinWidth, w);
            }

            if (MaxWidth > 0)
            {
                w = Math.Min(MaxWidth, w);
            }

            if (MinHeight > 0)
            {
                h = Math.Max(MinHeight, h);
            }

            if (MaxHeight > 0)
            {
                h = Math.Min(MaxHeight, h);
            }

            LocalBounds = new Rectangle(LocalBounds.X, LocalBounds.Y, w, h);

        }

        /// <summary>
        /// Clips the size and position of the rectangle so that it 
        /// falls inside the screen.
        /// </summary>
        /// <param name="rect">The rectangle to clip</param>
        /// <param name="device">The graphics device on whose viewport we are going to clip</param>
        /// <returns>A rectangle that fits onto the screen.</returns>
        protected static Rectangle ClipToScreen(Rectangle rect, GraphicsDevice device)
        {
            const int minScreenX = 0;
            const int minScreenY = 0;
            int maxScreenX = device.Viewport.Bounds.Right;
            int maxScreenY = device.Viewport.Bounds.Bottom;
            int x = Math.Min(Math.Max(minScreenX, rect.X), maxScreenX);
            int y = Math.Min(Math.Max(minScreenY, rect.Y), maxScreenY);
            int maxX = Math.Max(Math.Min(rect.Right, maxScreenX), minScreenX);
            int maxY = Math.Max(Math.Min(rect.Bottom, maxScreenY), minScreenY);

            return new Rectangle(x, y, Math.Max(maxX - x, 0), Math.Max(maxY - y, 0));
        }

        /// <summary>
        /// Called just before the component renders. Also calls this on all the component's children.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <param name="sprites">The sprites.</param>
        public virtual void PreRender(DwarfTime time, SpriteBatch sprites)
        {
            foreach(GUIComponent child in Children)
            {
                child.PreRender(time, sprites);
            }
        }

        /// <summary>
        /// Called just after the component renders. Also calls this on all the component's children.
        /// </summary>
        /// <param name="time">The time.</param>
        public virtual void PostRender(DwarfTime time)
        {
            foreach (GUIComponent child in Children)
            {
                child.PostRender(time);
            }
        }

        /// <summary>
        /// Renders the component and all its children.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <param name="batch">The sprite batch.</param>
        public virtual void Render(DwarfTime time, SpriteBatch batch)
        {
            UpdateSize();
            Children.Sort(
                (child1, child2) => child1.DrawOrder.CompareTo(child2.DrawOrder));
            if(!IsVisible)
            {
                return;
            }

            OnRender.Invoke();

            foreach(GUIComponent child in Children)
            {
                child.Render(time, batch);
            }
        }

        /// <summary>
        /// Renders a debug rectangle around the component.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <param name="batch">The sprite batch.</param>
        public virtual void DebugRender(DwarfTime time, SpriteBatch batch)
        {
            Drawer2D.DrawRect(batch, GlobalBounds, IsMouseOver ? Color.Red : Color.White, 1);


            foreach (GUIComponent child in Children)
            {
                child.DebugRender(time, batch);
            }
        }

        /// <summary>
        /// Destroys this instance and removes it from its parent.
        /// </summary>
        public void Destroy()
        {
            if(Parent == null)
            {
                GUI.RootComponent = null;
            }
            else
            {
                Parent.RemoveChild(this);
            }
        }
    }

}