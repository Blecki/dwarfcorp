using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DwarfCorp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp.Gui
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

        public Widget RootItem { get; private set; }
        public Widget TooltipItem { get; private set; }
        private List<Widget> UpdateItems = new List<Widget>();
        public Widget MouseDownItem { get; private set; }

        public bool MouseVisible = true;
        public MousePointer MousePointer = null;
        public TileReference MouseOverlaySheet = null;
        public Point MousePosition = new Point(0, 0);
        private DateTime MouseMotionTime = DateTime.Now;
        public float SecondsBeforeTooltip = 0.5f;
        public String TooltipFont = "font8";
        public int TooltipTextSize = 1;
        public float CursorBlinkTime = 0.3f;
        internal double RunTime = 0.0f;
        private List<Widget> PopupStack = new List<Widget>();

        public MousePointer SpecialIndicator = null;
        public Point SpecialIndicatorPosition = new Point(0, 0);
        public Rectangle? SpecialHiliteRegion = null;
        public Widget SpecialHighligtedWidget = null;
        public String SpecialHiliteSheet = null;

        public void ClearSpecials()
        {
            SpecialIndicator = null;
            SpecialHiliteRegion = null;
            SpecialHiliteSheet = null;
        }

        public Root(RenderData RenderData)
        {
            this.RenderData = RenderData;
                    
            ResetGui();

            // Grab initial mouse position.
            var mouse = Mouse.GetState();
            MousePosition = ScreenPointToGuiPoint(new Point(mouse.X, mouse.Y));
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
                    Rect = RenderData.VirtualScreen,
                    Transparent = true
                });

        }


        public string Sanitize(String Name)
        {
            return Name == null ? null : Regex.Replace(Name, "/", "\\");
        }

        /// <summary>
        /// Get a named tile sheet.
        /// </summary>
        /// <param name="Name"></param>
        /// <returns></returns>
        public ITileSheet GetTileSheet(String Name)
        {
            if (Name == null || !RenderData.TileSheets.ContainsKey(Name))
            {
                // Lol this is a hack to make the game behave gracefully whenever there is a bad specification for
                // a tile sheet.
                return RenderData.TileSheets["error"];
            }
            return RenderData.TileSheets[Sanitize(Name)];
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

            // If the widget is in the popup stack, remove it and any later popups from stack.
            if (PopupStack.Contains(Widget)) PopupStack = PopupStack.Take(PopupStack.IndexOf(Widget)).ToList();

            SafeCall(Widget.OnClose, Widget);
        }

        public void DestroyWidget(Widget Widget)
        {
            CleanupWidget(Widget);
            if (Widget.Parent != null) Widget.Parent.RemoveChild(Widget);            
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

        /// <summary>
        /// Show a widget as a popup. Replaces any existing popup widget already displayed.
        /// </summary>
        /// <param name="Popup"></param>
        public void ShowModalPopup(Widget Popup)
        {
            var screenDarkener = new Widget()
            {
                Background = new TileReference("basic", 0),
                BackgroundColor = new Vector4(0, 0, 0, 0.5f),
                Rect = RootItem.Rect
            };

            RootItem.AddChild(screenDarkener);
            PopupStack.Add(screenDarkener);

            screenDarkener.AddChild(Popup);

            // Need to hook the popup's OnClose to also remove the darkener.
            var lambdaOnClose = Popup.OnClose;
            Popup.OnClose = (sender) =>
            {
                this.SafeCall(lambdaOnClose, sender);
                screenDarkener.Children.Clear(); // Avoid recursing back into popup's OnClose.
                DestroyWidget(screenDarkener);
            };
        }


        /// <summary>
        /// Show a widget as a popup. Replaces any existing popup widget already displayed.
        /// </summary>
        /// <param name="Popup"></param>
        public void ShowMinorPopup(Widget Popup)
        {
            PopupStack.Add(Popup);
            RootItem.AddChild(Popup);
        }

        public void ShowTooltip(Point Where, String Tip)
        {
            if (String.IsNullOrEmpty(Tip))
            {
                if (TooltipItem != null)
                    DestroyWidget(TooltipItem);
                return;
            }

            var item = ConstructWidget(new Widget
            {
                Text = Tip,
                Border = "border-dark",
                Font = TooltipFont,
                TextSize = TooltipTextSize,
                TextColor = new Vector4(1, 1, 1, 1),
                IsFloater = true,
            });

            var bestSize = item.GetBestSize();
            Rectangle rect = new Rectangle(
                // ?? Why are we assuming the tooltip is being opened at the mouse position?
                Where.X + (MousePointer == null ? 0 : GetTileSheet(MousePointer.Sheet).TileWidth) + 2,
                Where.Y + (MousePointer == null ? 0 : GetTileSheet(MousePointer.Sheet).TileWidth) + 2, bestSize.X, bestSize.Y);

            rect = MathFunctions.SnapRect(rect, RenderData.VirtualScreen);
            item.Rect = rect;
            ShowTooltip(Where, item);
        }

        public void ShowTooltip(Point Where, Widget Tip)
        {
            if (TooltipItem != null && TooltipItem.Text == Tip.Text)
            {
                //TODO (Mklingen): handle the case where the tooltip is the same,
                // but the widget has moved.

                var myBestSize = TooltipItem.GetBestSize();
                Rectangle myRect = new Rectangle(
                    // ?? Why are we assuming the tooltip is being opened at the mouse position?
                    Where.X + (MousePointer == null ? 0 : GetTileSheet(MousePointer.Sheet).TileWidth) + 2,
                    Where.Y + (MousePointer == null ? 0 : GetTileSheet(MousePointer.Sheet).TileWidth) + 2, myBestSize.X, myBestSize.Y);

                if (myRect == TooltipItem.Rect)
                {
                    return;
                }
            }

            RootItem.AddChild(Tip);

            var bestSize = Tip.GetBestSize();
            Rectangle rect = new Rectangle(
                // ?? Why are we assuming the tooltip is being opened at the mouse position?
                Where.X + (MousePointer == null ? 0 : GetTileSheet(MousePointer.Sheet).TileWidth) + 2,
                Where.Y + (MousePointer == null ? 0 : GetTileSheet(MousePointer.Sheet).TileWidth) + 2, bestSize.X, bestSize.Y);

            rect = MathFunctions.SnapRect(rect, RenderData.VirtualScreen);
            Tip.Rect = rect;

            if (TooltipItem != null)
                DestroyWidget(TooltipItem);

            TooltipItem = Tip;
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
            float mouseX = P.X - RenderData.RealScreen.X;
            float mouseY = P.Y - RenderData.RealScreen.Y;
            mouseX /= RenderData.ScaleRatio;
            mouseY /= RenderData.ScaleRatio;
            return new Point((int)mouseX, (int)mouseY);
        }

        private bool IsHoverPartOfPopup()
        {
            if (PopupStack.Count == 0 || HoverItem == null) return false;

            var item = PopupStack[PopupStack.Count - 1];
                if (Object.ReferenceEquals(item, HoverItem)) return true;
                if (HoverItem.IsChildOf(item)) return true;
            
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

                        if (HoverItem != null && !Object.ReferenceEquals(HoverItem, MouseDownItem))
                            SafeCall(HoverItem.OnMouseMove, HoverItem,
                                new InputEventArgs { X = MousePosition.X, Y = MousePosition.Y });
                                                
                    }
                    break;
                case InputEvents.MouseDown:
                    {
                        MousePosition = ScreenPointToGuiPoint(new Point(Args.X, Args.Y));
                        var newArgs = new InputEventArgs
                        {
                            Alt = Args.Alt,
                            Control = Args.Control,
                            Shift = Args.Shift,
                            X = MousePosition.X,
                            Y = MousePosition.Y,
                            MouseButton = Args.MouseButton
                        };

                        MouseDownItem = null;
                        if (PopupStack.Count != 0)
                        {
                            if (IsHoverPartOfPopup())
                                MouseDownItem = HoverItem;
                        }
                        else
                            MouseDownItem = HoverItem;

                        if (MouseDownItem !=  null)
                        {
                            CallOnMouseDown(MouseDownItem, newArgs);
                        }
                    }
                    break;
                case InputEvents.MouseUp:
                    {
                        var newArgs = new InputEventArgs
                        {
                            Alt = Args.Alt,
                            Control = Args.Control,
                            Shift = Args.Shift,
                            X = MousePosition.X,
                            Y = MousePosition.Y,
                            MouseButton = Args.MouseButton
                        };

                        if (MouseDownItem != null)
                        {
                            CallOnMouseUp(MouseDownItem, newArgs);
                        }
                        //MouseDownItem = null;
                    }
                    break;
                case InputEvents.MouseClick:
                    {
                        MousePosition = ScreenPointToGuiPoint(new Point(Args.X, Args.Y));

                        var newArgs = new InputEventArgs
                        {
                            Alt = Args.Alt,
                            Control = Args.Control,
                            Shift = Args.Shift,
                            X = MousePosition.X,
                            Y = MousePosition.Y,
                            MouseButton = Args.MouseButton
                        };

                        if (PopupStack.Count != 0)
                        {
                            if (HoverItem == null || !IsHoverPartOfPopup())
                            {
                                if (PopupStack[PopupStack.Count - 1].PopupDestructionType == PopupDestructionType.DestroyOnOffClick)
                                    DestroyWidget(PopupStack[PopupStack.Count - 1]);

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
                case InputEvents.MouseWheel:
                   {
                        var newArgs = new InputEventArgs
                        {
                            Alt = Args.Alt,
                            Control = Args.Control,
                            Shift = Args.Shift,
                            ScrollValue = Args.ScrollValue
                        };

                        if (HoverItem != null)
                        {
                            Args.Handled = true;
                            CallOnScroll(HoverItem, newArgs);
                        }
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

        private void CallOnMouseDown(Widget Widget, InputEventArgs Args)
        {
            SafeCall(Widget.OnMouseDown, Widget, Args);
            var parent = Widget.Parent;
            while (parent != null)
            {
                if (parent.TriggerOnChildClick)
                    SafeCall(parent.OnMouseDown, parent, Args);
                parent = parent.Parent;
            }
        }

        private void CallOnMouseUp(Widget Widget, InputEventArgs Args)
        {
            SafeCall(Widget.OnMouseUp, Widget, Args);
            var parent = Widget.Parent;
            while (parent != null)
            {
                if (parent.TriggerOnChildClick)
                    SafeCall(parent.OnMouseUp, parent, Args);
                parent = parent.Parent;
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

        private void CallOnScroll(Widget Widget, InputEventArgs Args)
        {
            SafeCall(Widget.OnScroll, Widget, Args);
            var parent = Widget.Parent;
            while (parent != null)
            {
                if (parent.TriggerOnChildClick)
                    SafeCall(parent.OnScroll, parent, Args);
                parent = parent.Parent;
            }
        }

        public void Update(GameTime Time)
        {
            // Update mouse pointer animation.
            if (MousePointer != null)
                MousePointer.Update((float)Time.ElapsedGameTime.TotalSeconds);

            if (SpecialIndicator != null)
                SpecialIndicator.Update((float)Time.ElapsedGameTime.TotalSeconds);

            // Check to see if tooltip should be displayed.
            if (TooltipItem == null && HoverItem != null && !String.IsNullOrEmpty(HoverItem.Tooltip))
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

            var scale = RenderData.RealScreen.Width / RenderData.VirtualScreen.Width;

            // Need to offset by the subpixel portion to avoid screen artifacts.
            // Remove this offset is porting to Monogame, monogame does it correctly.
            RenderData.Effect.Parameters["World"].SetValue(
                Matrix.CreateTranslation(RenderData.RealScreen.X, RenderData.RealScreen.Y, 1.0f)
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

            var scale = RenderData.RealScreen.Width / RenderData.VirtualScreen.Width;

            // Need to offset by the subpixel portion to avoid screen artifacts.
            // Remove this offset is porting to Monogame, monogame does it correctly.
            RenderData.Effect.Parameters["World"].SetValue(
                Matrix.CreateTranslation(RenderData.RealScreen.X, RenderData.RealScreen.Y, 1.0f)
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

            DrawMouse();
        }

        public void RedrawPopups()
        {
            RenderData.Device.DepthStencilState = DepthStencilState.None;

            RenderData.Effect.CurrentTechnique = RenderData.Effect.Techniques[0];

            RenderData.Effect.Parameters["View"].SetValue(Matrix.Identity);

            RenderData.Effect.Parameters["Projection"].SetValue(
                Matrix.CreateOrthographicOffCenter(0, RenderData.Device.Viewport.Width,
                RenderData.Device.Viewport.Height, 0, -32, 32));

            var scale = RenderData.RealScreen.Width / RenderData.VirtualScreen.Width;

            // Need to offset by the subpixel portion to avoid screen artifacts.
            // Remove this offset is porting to Monogame, monogame does it correctly.
            RenderData.Effect.Parameters["World"].SetValue(
                Matrix.CreateTranslation(RenderData.RealScreen.X, RenderData.RealScreen.Y, 1.0f)
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

            foreach (var item in PopupStack)
                item.GetRenderMesh().Render(RenderData.Device);
        }

        public void DrawMouse()
        {
            if (RootItem.Hidden)
            {
                return;
            }

            RenderData.Effect.Parameters["Texture"].SetValue(RenderData.Texture);
            RenderData.Effect.CurrentTechnique.Passes[0].Apply();

            if (SpecialHiliteRegion.HasValue && !String.IsNullOrEmpty(SpecialHiliteSheet))
            {
                if (SpecialHighligtedWidget == null || !SpecialHighligtedWidget.Hidden)
                {
                    var sheet = GetTileSheet(SpecialHiliteSheet);
                    var area = SpecialHiliteRegion.Value.Interior(-sheet.TileWidth, -sheet.TileHeight,
                        -sheet.TileWidth, -sheet.TileHeight);
                    var mesh = Mesh.CreateScale9Background(area, sheet);
                    mesh.Render(RenderData.Device);
                }
            }

            if (SpecialIndicator != null)
            {
                if (SpecialHighligtedWidget == null || !SpecialHighligtedWidget.Hidden)
                {
                    var tileSheet = GetTileSheet(SpecialIndicator.Sheet);
                    var mouseMesh = Mesh.Quad()
                        .Scale(tileSheet.TileWidth, tileSheet.TileHeight)
                        .Translate(
                            SpecialIndicatorPosition.X +
                            (float) Math.Sin(DwarfTime.LastTime.TotalRealTime.TotalSeconds*4.0)*8.0f,
                            SpecialIndicatorPosition.Y)
                        .Texture(tileSheet.TileMatrix(SpecialIndicator.AnimationFrame));
                    mouseMesh.Render(RenderData.Device);
                }
            }

            if (MouseVisible && MousePointer != null)
            {
                var tileSheet = GetTileSheet(MousePointer.Sheet);
                var mouseMesh = Mesh.Quad()
                    .Scale(tileSheet.TileWidth, tileSheet.TileHeight)
                    .Translate(MousePosition.X, MousePosition.Y)
                    .Texture(tileSheet.TileMatrix(MousePointer.AnimationFrame));
                mouseMesh.Render(RenderData.Device);

                if (MouseOverlaySheet != null)
                {
                    var overlaySheet = GetTileSheet(MouseOverlaySheet.Sheet);
                    var overlayMesh = Mesh.Quad()
                        .Scale(overlaySheet.TileWidth, overlaySheet.TileHeight)
                        .Translate(MousePosition.X + overlaySheet.TileWidth / 2, MousePosition.Y + overlaySheet.TileHeight / 2)
                        .Texture(overlaySheet.TileMatrix(MouseOverlaySheet.Tile));
                    overlayMesh.Render(RenderData.Device);
                }
            }
        }
    }
}
