using System;
using System.Collections.Generic;
using System.Linq;
using DwarfCorp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace Gum
{
    /// <summary>
    /// Root of a GUI.
    /// </summary>
    public class Root
    {
        public RenderData RenderData { get; private set; }

        // Todo: Hilite hover item, indicating it is interactive. Logic properly belongs in a widget.
        public Widget HoverItem { get; private set; }
        public Widget FocusItem { get; private set; }

        public static Point MinimumSize = new Point(1024, 768);
        public Rectangle VirtualScreen { get; private set; }
        public Rectangle RealScreen { get; private set; }
        public Point ResolutionAtCreation { get; private set; }
        public int ScaleRatio { get; private set; }
        public Widget RootItem { get; private set; }
        public Widget TooltipItem { get; private set; }
        private List<Widget> UpdateItems = new List<Widget>();
        public Widget MouseDownItem { get; private set; }

        public MousePointer MousePointer = null;
        public Point MousePosition = new Point(0, 0);
        private DateTime MouseMotionTime = DateTime.Now;
        public float SecondsBeforeTooltip = 0.5f;
        public String TooltipFont = "font";
        public int TooltipTextSize = 1;
        public float CursorBlinkTime = 0.3f;
        internal double RunTime = 0.0f;

        private List<Widget> PopupStack = new List<Widget>();

        public Root(Point IdealSize, RenderData RenderData)
        {
            this.RenderData = RenderData;
                    
            ResizeVirtualScreen(IdealSize);
            ResetGui();

            // Grab initial mouse position.
            var mouse = Mouse.GetState();
            MousePosition = ScreenPointToGuiPoint(new Point(mouse.X, mouse.Y));
        }

        public bool ResolutionChanged()
        {
            if (ResolutionAtCreation.X != RenderData.ActualScreenBounds.X) return true;
            if (ResolutionAtCreation.Y != RenderData.ActualScreenBounds.Y) return true;
            return false;
        }
               
        /// <summary>
        /// Reset the gui, clearing out all existing widgets.
        /// </summary>
        public void ResetGui()
        {
            HoverItem = null;
            FocusItem = null;
            PopupStack.Clear();
            TooltipItem = null;
            RootItem = ConstructWidget(new Widget
                {
                    Rect = VirtualScreen,
                    Transparent = true
                });

        }

        /// <summary>
        /// Resize the virtual screen and find the ideal size and positioning.
        /// </summary>
        /// <param name="VirtualSize"></param>
        public void ResizeVirtualScreen(Point VirtualSize)
        {
            this.VirtualScreen = new Rectangle(0, 0, VirtualSize.X, VirtualSize.Y);
            this.ResolutionAtCreation = RenderData.ActualScreenBounds;

            // Calculate ideal on screen size.
            // Size should never be smaller than the size of the virtual screen supplied.
            var screenSize = RenderData.ActualScreenBounds;
            ScaleRatio = 1;

            // How many times can we multiply the ideal size and still fit on the screen?
            //while (((VirtualSize.X * (ScaleRatio + 1)) <= screenSize.X) &&
            //    ((VirtualSize.Y * (ScaleRatio + 1)) <= screenSize.Y))
            //    ScaleRatio += 1;

            // How much space did we leave to the left and right? 
            var horizontalExpansion = ((screenSize.X - (VirtualSize.X * ScaleRatio)) / 2) / ScaleRatio;
            var verticalExpansion = ((screenSize.Y - (VirtualSize.Y * ScaleRatio)) / 2) / ScaleRatio;

            VirtualScreen = new Rectangle(0, 0, VirtualSize.X + horizontalExpansion + horizontalExpansion,
                VirtualSize.Y + verticalExpansion + verticalExpansion);

            RealScreen = new Rectangle(0, 0, VirtualScreen.Width * ScaleRatio, VirtualScreen.Height * ScaleRatio);
            RealScreen = new Rectangle((screenSize.X - RealScreen.Width) / 2,
                (screenSize.Y - RealScreen.Height) / 2,
                RealScreen.Width, RealScreen.Height);
        }

        /// <summary>
        /// Get a named tile sheet.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public ITileSheet GetTileSheet(String Name)
        {
            return RenderData.TileSheets[Name];
        }


        /// <summary>
        /// Widgets must be constructed or some operations will fail. Use this function to construct a widget 
        /// when the widget is not being immediately added to its parent.
        /// </summary>
        /// <param name="NewWidget"></param>
        /// <returns></returns>
        public Widget ConstructWidget(Widget NewWidget)
        {
            NewWidget._Construct(this);
            return NewWidget;
        }

        /// <summary>
        /// Prepare a widget for destruction - make sure any lingering references to it or its children
        /// are removed.
        /// </summary>
        /// <param name="Widget"></param>
        internal void CleanupWidget(Widget Widget)
        {
            foreach (var child in Widget.Children)
                CleanupWidget(child);
            Widget.Root = null;
            Widget.Constructed = false;
            if (Object.ReferenceEquals(FocusItem, Widget)) FocusItem = null;
            if (Object.ReferenceEquals(HoverItem, Widget)) HoverItem = null;
            if (Object.ReferenceEquals(TooltipItem, Widget)) TooltipItem = null;
            UpdateItems.RemoveAll(p => Object.ReferenceEquals(p, Widget));
            if (PopupStack.Contains(Widget)) PopupStack = new List<Widget>();
        }

        private void CleanupPopupStack()
        {
            foreach (var item in PopupStack)
            {
                SafeCall(item.OnPopupClose, item);
                DestroyWidget(item);
            }

            PopupStack.Clear();
        }

        public void DestroyWidget(Widget Widget)
        {
            CleanupWidget(Widget);
            if (Widget.Parent != null) Widget.Parent.RemoveChild(Widget);
            SafeCall(Widget.OnClose, Widget);
        }            

        public Widget RegisterForUpdate(Widget Widget)
        {
            if (!Object.ReferenceEquals(this, Widget.Root)) throw new InvalidOperationException();
            if (!UpdateItems.Contains(Widget))
                UpdateItems.Add(Widget);
            return Widget;
        }

        /// <summary>
        /// Shortcut function for showing a uiitem as a 'dialog'.
        /// </summary>
        /// <param name="Dialog"></param>
        public void ShowDialog(Widget Dialog)
        {
            RootItem.AddChild(Dialog);
        }

        public enum PopupExclusivity
        {
            DestroyExistingPopups,
            AddToStack
        }
        
        /// <summary>
        /// Show a widget as a popup. Replaces any existing popup widget already displayed.
        /// </summary>
        /// <param name="Popup"></param>
        public void ShowPopup(Widget Popup, PopupExclusivity Exclusivity = PopupExclusivity.AddToStack)
        {
            if (Exclusivity == PopupExclusivity.DestroyExistingPopups)
                CleanupPopupStack();

            PopupStack.Add(Popup);
            RootItem.AddChild(Popup);
        }

        public void ShowTooltip(Point Where, String Tip)
        {
            var item = ConstructWidget(new Widget
            {
                Text = Tip,
                Border = "border-dark",
                Font = TooltipFont,
                TextSize = TooltipTextSize,
                TextColor = new Vector4(1, 1, 1, 1)
            });

            var bestSize = item.GetBestSize();
            Rectangle rect = new Rectangle(
                Where.X + (MousePointer == null ? 0 : GetTileSheet(MousePointer.Sheet).TileWidth) + 2,
                Where.Y, bestSize.X, bestSize.Y);

            rect = MathFunctions.SnapRect(rect, RealScreen);
            item.Rect = rect;
            RootItem.AddChild(item);
            
            if (TooltipItem != null)
                DestroyWidget(TooltipItem);

            TooltipItem = item;
        }

        /// <summary>
        /// Set keyboard focus to the specified widget. Fires lose and gain focus events as appropriate.
        /// </summary>
        /// <param name="On"></param>
        public void SetFocus(Widget On)
        {
            if (!Object.ReferenceEquals(this, On.Root)) throw new InvalidOperationException();
            if (Object.ReferenceEquals(FocusItem, On)) return;

            if (FocusItem != null) SafeCall(FocusItem.OnLoseFocus, FocusItem);
            FocusItem = On; 
            if (FocusItem != null) SafeCall(FocusItem.OnGainFocus, FocusItem);
        }

        /// <summary>
        /// Shortcut to call an action without having to check for null.
        /// </summary>
        /// <param name="Action"></param>
        /// <param name="Widget"></param>
        /// <param name="Args"></param>
        public void SafeCall<T>(Action<Widget, T> Action, Widget Widget, T Args)
        {
            if (Action != null) Action(Widget, Args);
        }


        /// <summary>
        /// Shortcut to call an action without having to check for null.
        /// </summary>
        /// <param name="Action"></param>
        /// <param name="Widget"></param>
        public void SafeCall(Action<Widget> Action, Widget Widget)
        {
            if (Action != null) Action(Widget);
        }

        public Point ScreenPointToGuiPoint(Point P)
        {
            // Transform mouse from screen space to virtual gui space.
            float mouseX = P.X - RealScreen.X;
            float mouseY = P.Y - RealScreen.Y;
            mouseX /= ScaleRatio;
            mouseY /= ScaleRatio;
            return new Point((int)mouseX, (int)mouseY);
        }

        private bool IsHoverPartOfPopup()
        {
            if (PopupStack.Count == 0 || HoverItem == null) return false;

            foreach (var item in PopupStack)
            {
                if (Object.ReferenceEquals(item, HoverItem)) return true;
                if (HoverItem.IsChildOf(item)) return true;
            }
            return false;
        }

        /// <summary>
        /// Process mouse events.
        /// </summary>
        /// <param name="Event"></param>
        /// <param name="Args"></param>
        public void HandleInput(InputEvents Event, InputEventArgs Args)
        {
            switch (Event)
            {
                case InputEvents.MouseMove:
                    {
                        // Destroy tooltips when the mouse moves.
                        MouseMotionTime = DateTime.Now;
                        
                        MousePosition = ScreenPointToGuiPoint(new Point(Args.X, Args.Y));
                        var newArgs = new InputEventArgs { X = MousePosition.X, Y = MousePosition.Y };
                        // Detect hover item and fire mouse enter/leave events as appropriate.
                        var newHoverItem = RootItem.FindWidgetAt(MousePosition.X, MousePosition.Y);
                        if (!Object.ReferenceEquals(newHoverItem, HoverItem))
                        {
                            if (HoverItem != null) SafeCall(HoverItem.OnMouseLeave, HoverItem, newArgs);
                            if (newHoverItem != null) SafeCall(newHoverItem.OnMouseEnter, newHoverItem, newArgs);
                            if (TooltipItem != null) DestroyWidget(TooltipItem);
                            HoverItem = newHoverItem;
                        }

                        if (MouseDownItem != null)
                            SafeCall(MouseDownItem.OnMouseMove, MouseDownItem,
                                new InputEventArgs { X = MousePosition.X, Y = MousePosition.Y });
                        
                        if (HoverItem != null && 
                            !IsHoverPartOfPopup() && 
                            PopupStack.Count > 0 && 
                            PopupStack[PopupStack.Count - 1].PopupDestructionType == PopupDestructionType.DestroyOnMouseLeave)
                            CleanupPopupStack();
                    }
                    break;
                case InputEvents.MouseDown:
                    {
                        MousePosition = ScreenPointToGuiPoint(new Point(Args.X, Args.Y));
                        var newArgs = new InputEventArgs { X = MousePosition.X, Y = MousePosition.Y };

                        MouseDownItem = null;
                        if (PopupStack.Count != 0)
                        {
                            if (IsHoverPartOfPopup())
                                MouseDownItem = HoverItem;
                        }
                        else
                            MouseDownItem = HoverItem;
                    }
                    break;
                case InputEvents.MouseUp:
                    //MouseDownItem = null;
                    break;
                case InputEvents.MouseClick:
                    {
                        MousePosition = ScreenPointToGuiPoint(new Point(Args.X, Args.Y));
                        var newArgs = new InputEventArgs { X = MousePosition.X, Y = MousePosition.Y };

                        if (PopupStack.Count != 0)
                        {
                            if (HoverItem == null || !IsHoverPartOfPopup())
                            {
                                if (PopupStack[PopupStack.Count - 1].PopupDestructionType == PopupDestructionType.DestroyOnOffClick)
                                    CleanupPopupStack();

                                MouseDownItem = null;
                                return;
                            }

                            if (IsHoverPartOfPopup())
                            {
                                Args.Handled = true;
                                if (Object.ReferenceEquals(HoverItem, MouseDownItem))
                                    CallOnClick(HoverItem, newArgs);
                                MouseDownItem = null;
                                return;
                            }

                            MouseDownItem = null;
                            return;
                        }

                        if (HoverItem != null && Object.ReferenceEquals(HoverItem, MouseDownItem))
                        {
                            Args.Handled = true;
                            CallOnClick(HoverItem, newArgs);
                        }
                        MouseDownItem = null;
                    }
                    break;
                case InputEvents.KeyPress:
                    if (FocusItem != null) SafeCall(FocusItem.OnKeyPress, FocusItem, Args);
                    break;
                case InputEvents.KeyDown:
                    if (FocusItem != null) SafeCall(FocusItem.OnKeyDown, FocusItem, Args);
                    break;
                case InputEvents.KeyUp:
                    if (FocusItem != null) SafeCall(FocusItem.OnKeyUp, FocusItem, Args);
                    break;
            }
        }

        private void CallOnClick(Widget Widget, InputEventArgs Args)
        {
            SafeCall(Widget.OnClick, Widget, Args);
            var parent = Widget.Parent;
            while (parent != null)
            {
                if (parent.TriggerOnChildClick)
                    SafeCall(parent.OnClick, parent, Args);
                parent = parent.Parent;
            }
        }

        public void Update(GameTime Time)
        {
            // Update mouse pointer animation.
            if (MousePointer != null)
                MousePointer.Update((float)Time.ElapsedGameTime.TotalSeconds);

            // Check to see if tooltip should be displayed.
            if (HoverItem != null && !String.IsNullOrEmpty(HoverItem.Tooltip))
            {
                var hoverTime = DateTime.Now - MouseMotionTime;
                if (hoverTime.TotalSeconds > SecondsBeforeTooltip)
                    ShowTooltip(MousePosition, HoverItem.Tooltip);
            }

            RunTime = Time.TotalGameTime.TotalSeconds;

            if (FocusItem != null) SafeCall(FocusItem.OnUpdateWhileFocus, FocusItem);

            var localCopy = new List<Widget>(UpdateItems);
            foreach (var item in localCopy)
                SafeCall(item.OnUpdate, item, Time);

            if (HoverItem != null) SafeCall(HoverItem.OnHover, HoverItem);
        }

        public void Draw()
        {
            Draw(Point.Zero);
        }

        public void DrawMesh(Mesh Mesh, Texture2D Texture)
        {
            RenderData.Device.DepthStencilState = DepthStencilState.None;

            RenderData.Effect.CurrentTechnique = RenderData.Effect.Techniques[0];

            RenderData.Effect.Parameters["View"].SetValue(Matrix.Identity);

            RenderData.Effect.Parameters["Projection"].SetValue(
                Matrix.CreateOrthographicOffCenter(0, RenderData.Device.Viewport.Width,
                RenderData.Device.Viewport.Height, 0, -32, 32));

            var scale = RealScreen.Width / VirtualScreen.Width;

            // Need to offset by the subpixel portion to avoid screen artifacts.
            // Remove this offset is porting to Monogame, monogame does it correctly.
            RenderData.Effect.Parameters["World"].SetValue(
                Matrix.CreateTranslation(RealScreen.X, RealScreen.Y, 1.0f)
                * Matrix.CreateScale(scale)
#if GEMXNA
                * Matrix.CreateTranslation(-0.5f, -0.5f, 0.0f));
#elif GEMMONO
                );
#elif GEMFNA
);
#endif

            RenderData.Effect.Parameters["Texture"].SetValue(Texture);

            RenderData.Effect.CurrentTechnique.Passes[0].Apply();

            Mesh.Render(RenderData.Device);
        }

        /// <summary>
        /// Draw a quad using the device provided earlier. Depth testing should be off.
        /// </summary>
        public void DrawQuad(Rectangle Quad, Texture2D Texture)
        {            
            var mesh = Mesh.Quad()
                    .Scale(Quad.Width, Quad.Height)
                    .Translate(Quad.X, Quad.Y);
            DrawMesh(mesh, Texture);
        }

        /// <summary>
        /// Draw the GUI using the device provided earlier. Depth testing should be off.
        /// </summary>
        public void Draw(Point Offset)
        {
            RenderData.Device.DepthStencilState = DepthStencilState.None;

            RenderData.Effect.CurrentTechnique = RenderData.Effect.Techniques[0];

            RenderData.Effect.Parameters["View"].SetValue(Matrix.Identity);

            RenderData.Effect.Parameters["Projection"].SetValue(
                Matrix.CreateOrthographicOffCenter(Offset.X, Offset.X + RenderData.Device.Viewport.Width,
                Offset.Y + RenderData.Device.Viewport.Height, Offset.Y, -32, 32));

            var scale = RealScreen.Width / VirtualScreen.Width;

            // Need to offset by the subpixel portion to avoid screen artifacts.
            // Remove this offset is porting to Monogame, monogame does it correctly.
            RenderData.Effect.Parameters["World"].SetValue(
                Matrix.CreateTranslation(RealScreen.X, RealScreen.Y, 1.0f)
                * Matrix.CreateScale(scale)
#if GEMXNA
                * Matrix.CreateTranslation(-0.5f, -0.5f, 0.0f));
#elif GEMMONO
                );
#elif GEMFNA
                );
#endif

            RenderData.Effect.Parameters["Texture"].SetValue(RenderData.Texture);

            RenderData.Effect.CurrentTechnique.Passes[0].Apply();

            var mesh = RootItem.GetRenderMesh();
            mesh.Render(RenderData.Device);

            if (MousePointer != null)
            {
                var tileSheet = GetTileSheet(MousePointer.Sheet);
                var mouseMesh = Mesh.Quad()
                    .Scale(tileSheet.TileWidth, tileSheet.TileHeight)
                    .Translate(MousePosition.X, MousePosition.Y)
                    .Texture(tileSheet.TileMatrix(MousePointer.AnimationFrame));
                mouseMesh.Render(RenderData.Device);
            }
        }
    }
}
