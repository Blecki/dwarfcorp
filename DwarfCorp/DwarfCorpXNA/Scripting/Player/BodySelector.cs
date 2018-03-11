using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    /// <summary>
    ///     This class handles selecting and deselecting bodies with the mouse..
    /// </summary>
    public class BodySelector
    {
        /// <summary>
        ///     Called whenever the left mouse button is released.
        /// </summary>
        /// <returns>A list of bodies selected by the mouse.</returns>
        public delegate List<Body> OnLeftReleased();

        /// <summary>
        ///     Called whenever the right mouse button is released.
        /// </summary>
        /// <returns>A list of bodies deselected by the mouse.</returns>
        public delegate List<Body> OnRightReleased();

        /// <summary>
        ///     Called whenever a list of bodies were selected.
        /// </summary>
        /// <param name="bodies">List of bodies selected by the mouse</param>
        /// <param name="button">The mouse button pressed during selection</param>
        public delegate void OnSelected(List<Body> bodies, InputManager.MouseButton button);

        /// <summary>
        /// Called whenever the mouse hovers over some bodies.
        /// </summary>
        /// <param name="bodies">The bodies that the mouse was hovering over.</param>
        public delegate void OnMouseOverEvent(IEnumerable<Body> bodies);


        /// <summary>
        ///     True whenever the left mouse button is down.
        /// </summary>
        private bool isLeftPressed;

        /// <summary>
        ///     True whenever the right mouse button is down
        /// </summary>
        private bool isRightPressed;

        /// <summary>
        ///     Create a new body selector.
        /// </summary>
        /// <param name="camera">The camera associated with the selector.</param>
        /// <param name="graphics">The graphics device associated with the camera.</param>
        /// <param name="components">Selectable components in a manager.</param>
        public BodySelector(Camera camera, GraphicsDevice graphics, ComponentManager components)
        {
            World = components.World;
            AllowRightClickSelection = true;
            SelectionColor = Color.White;
            CurrentColor = Color.White;
            CameraController = camera;
            Components = components;
            SelectionBuffer = new List<Body>();
            CurrentBodies = new List<Body>();
            LeftReleased = LeftReleasedCallback;
            RightReleased = RightReleasedCallback;
            MouseOver = bodies => { };
            Selected += SelectedCallback;
            Enabled = true;
            DeleteColor = Color.Red;
            SelectionRectangle = new Rectangle(0, 0, 0, 0);
            MouseOverTimer = new Timer(1.0f / 30.0f, false, Timer.TimerMode.Real);
        }

        /// <summary>
        ///     Component manager to use for selecting bodies.
        /// </summary>
        public ComponentManager Components { get; set; }

        /// <summary>
        ///     Color of screen rectangle to draw while selecting bodies.
        /// </summary>
        public Color SelectionColor { get; set; }

        /// <summary>
        ///     Color of screen rectangle to draw while deselecting bodies.
        /// </summary>
        public Color DeleteColor { get; set; }

        /// <summary>
        ///     Color of the screen rectangle currently being drawn.
        /// </summary>
        public Color CurrentColor { get; set; }

        /// <summary>
        ///     Called whenever bodies are selected.
        /// </summary>
        public OnSelected Selected { get; set; }

        /// <summary>
        ///     Camera that is making selections.
        /// </summary>
        public Camera CameraController { get; set; }

        /// <summary>
        ///     Graphics device associated with the camera.
        /// </summary>
        public GraphicsDevice Graphics { get { return GameState.Game.GraphicsDevice; } }

        /// <summary>
        ///     List of bodies currently selected.
        /// </summary>
        public List<Body> SelectionBuffer { get; set; }

        /// <summary>
        ///     If true, the body selector will be active.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        ///     The point in the world that was first clicked by the selector
        ///     (calculated using raycasting)
        /// </summary>
        public Vector3 ClickPoint3D { get; set; }

        /// <summary>
        ///     The point on the screen that was first clicked by the selector
        ///     (in pixels).
        /// </summary>
        public Point ClickPoint { get; set; }

        /// <summary>
        ///     Screen-space rectangle (in pixels) selecting objects.
        /// </summary>
        public Rectangle SelectionRectangle { get; set; }

        /// <summary>
        ///     If true, objects can be selected by right clicking as well as left clicking.
        /// </summary>
        public bool AllowRightClickSelection { get; set; }

        /// <summary>
        ///     If the mouse has not moved during this timer's timeout, then an object can be a candidate
        ///     for "mouse-over". This allows us to display information about the object to the user.
        /// </summary>
        public Timer MouseOverTimer { get; set; }

        /// <summary>
        ///     These are all the bodies in the selection rectangle (note: this is not the same as the SelectionBuffer,
        ///     which is the list of bodies in the selection rectange *at the time the mouse was last released*)
        /// </summary>
        public List<Body> CurrentBodies { get; set; }

        /// <summary>
        ///     Called whenever the left mouse button is released.
        /// </summary>
        public event OnLeftReleased LeftReleased;

        /// <summary>
        ///     Called whenever the right mouse button is released.
        /// </summary>
        public event OnRightReleased RightReleased;

        /// <summary>
        /// Occurs when the mouse hovers over a body.
        /// </summary>
        public event OnMouseOverEvent MouseOver;

        public WorldManager World;

        /// <summary>
        ///     Returns a list of bodies that are inside the given screen rectangle.
        /// </summary>
        /// <param name="screenRectangle">A rectangle on the screen (in pixls)</param>
        /// <returns>The list of bodies whose screen-space bounding boxes intersect the rectangle.</returns>
        public List<Body> SelectBodies(Rectangle screenRectangle)
        {
            return Components.SelectRootBodiesOnScreen(screenRectangle, CameraController);
        }

        /// <summary>
        ///     Called when some number of bodies are underneath the mouse for a certain amount of time.
        /// </summary>
        /// <param name="entities">A list of bodies that were underneath the mouse.</param>
        public void OnMouseOver(IEnumerable<Body> entities)
        {
            MouseOver.Invoke(entities);
            string desc = "";
            bool first = true;
            foreach (Body body in entities)
            {
                if (!first) desc += "\n";
                desc += body.GetDescription();
                first = false;
            }
            // Create a description of the body and display it on the screen.
            World.ShowInfo(desc);
        }

        /// <summary>
        ///     Called every tick.
        /// </summary>
        public void Update()
        {
            MouseState mouse = Mouse.GetState();
            KeyboardState keyboard = Keyboard.GetState();

            if (!Enabled)
            {
                return;
            }

            // Select bodies under the mouse if it is hovering.
            MouseOverTimer.Update(DwarfTime.LastTime);
            if (MouseOverTimer.HasTriggered)
            {
                List<Body> selected = SelectBodies(new Rectangle(mouse.X - 2, mouse.Y - 2, 4, 4));

                if (selected.Count > 0)
                {
                    OnMouseOver(selected);
                }
                else
                {
                    OnMouseOver(new List<Body>());
                }
            }

            // If the left mouse button is pressed, update the selection rectangle.
            if (isLeftPressed)
            {
                if (mouse.LeftButton == ButtonState.Released)
                {
                    isLeftPressed = false;
                    SelectionBuffer = Components.SelectRootBodiesOnScreen(SelectionRectangle, CameraController);
                    LeftReleased.Invoke();
                }
                else
                {
                    UpdateSelectionRectangle(mouse.X, mouse.Y);
                }
            }
                // Otherwise, if the mouse has first been pressed, initialize selection
            else if (mouse.LeftButton == ButtonState.Pressed)
            {
                isLeftPressed = true;
                ClickPoint = new Point(mouse.X, mouse.Y);
                ClickPoint3D = World.CursorLightPos;
                SelectionRectangle = new Rectangle(mouse.X, mouse.Y, 0, 0);
            }

            if (AllowRightClickSelection)
            {
                // If the right mouse button has been sustained-pressed, update
                // the selection rectangle.
                if (isRightPressed)
                {
                    if (mouse.RightButton == ButtonState.Released)
                    {
                        isRightPressed = false;
                        SelectionBuffer = Components.SelectRootBodiesOnScreen(SelectionRectangle, CameraController);
                        RightReleased.Invoke();
                    }
                    else
                    {
                        UpdateSelectionRectangle(mouse.X, mouse.Y);
                    }
                }
                    // Otherwise if the mouse has been first pressed, initialize selection
                else if (mouse.RightButton == ButtonState.Pressed)
                {
                    isRightPressed = true;
                    ClickPoint = new Point(mouse.X, mouse.Y);
                    ClickPoint3D = World.CursorLightPos;
                    SelectionRectangle = new Rectangle(mouse.X, mouse.Y, 0, 0);
                }
            }

            // If no mouse button has been pressed, there are no bodies currently being selected.
            if (!(isLeftPressed || isRightPressed))
            {
                CurrentBodies.Clear();
            }
        }

        /// <summary>
        ///     Given the current moue position, update the selection rectangle.
        /// </summary>
        /// <param name="mouseX">The current position of the mouse (X, pixels)</param>
        /// <param name="mouseY">The current position of the mouse (Y, pixels)</param>
        public void UpdateSelectionRectangle(int mouseX, int mouseY)
        {
            int top = Math.Min(ClickPoint.Y, mouseY);
            int left = Math.Min(ClickPoint.X, mouseX);
            int right = Math.Max(ClickPoint.X, mouseX);
            int bottom = Math.Max(ClickPoint.Y, mouseY);

            var newRect = new Rectangle(left, top, Math.Max(right - left, 0), Math.Max(bottom - top, 0));

            if (SelectionRectangle.Left != left || SelectionRectangle.Top != top || SelectionRectangle.Right != right ||
                SelectionRectangle.Bottom != bottom)
            {
                SelectionRectangle = newRect;
                CurrentBodies = Components.SelectRootBodiesOnScreen(SelectionRectangle, CameraController);
            }
        }

        /// <summary>
        ///     Called whenever bodies are selected.
        /// </summary>
        /// <param name="bodies">The bodies that were selected.</param>
        /// <param name="button">The mouse button (left, right, middle) that was pressed to select the bodies.</param>
        public void SelectedCallback(List<Body> bodies, InputManager.MouseButton button)
        {
        }


        /// <summary>
        ///     Render the selection rectangle.
        /// </summary>
        /// <param name="batch">Sprite batch to render with.</param>
        public void Render(SpriteBatch batch)
        {
            if (!isLeftPressed && !isRightPressed)
            {
                return;
            }
            Color rectColor = SelectionColor;

            if (isRightPressed && AllowRightClickSelection)
            {
                rectColor = DeleteColor;
            }
            Drawer2D.DrawRect(batch, SelectionRectangle, rectColor, 4);
        }


        /// <summary>
        ///     Called whenever the left mouse button was released.
        /// </summary>
        /// <returns>A list of selected bodies.</returns>
        public List<Body> LeftReleasedCallback()
        {
            var toReturn = new List<Body>();
            toReturn.AddRange(SelectionBuffer);
            SelectionBuffer.Clear();
            Selected.Invoke(toReturn, InputManager.MouseButton.Left);
            return toReturn;
        }

        /// <summary>
        ///     Called whenever the right mouse button was released.
        /// </summary>
        /// <returns>A list of selected bodies.</returns>
        public List<Body> RightReleasedCallback()
        {
            var toReturn = new List<Body>();
            toReturn.AddRange(SelectionBuffer);
            SelectionBuffer.Clear();
            Selected.Invoke(toReturn, InputManager.MouseButton.Right);
            return toReturn;
        }
    }
}