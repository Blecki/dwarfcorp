using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    public class BodySelector
    {
        private bool isLeftPressed;
        private bool isRightPressed;

        public ComponentManager Components { get; set; }
        public Color SelectionColor = Color.White;
        public Color DeleteColor { get; set; }
        public Color CurrentColor = Color.White;
        public Camera CameraController { get; set; }
        public GraphicsDevice Graphics { get { return GameState.Game.GraphicsDevice; } }
        public List<GameComponent> SelectionBuffer = new List<GameComponent>();
        public bool Enabled = true;
        public Vector3 ClickPoint3D { get; set; }
        public Point ClickPoint { get; set; }
        public Rectangle SelectionRectangle = new Rectangle(0, 0, 0, 0);
        public bool AllowRightClickSelection = true;
        public Timer MouseOverTimer = new Timer(1.0f / 30.0f, false, Timer.TimerMode.Real);
        public List<GameComponent> CurrentBodies = new List<GameComponent>();
        public Func<List<GameComponent>> LeftReleased;
        public Func<List<GameComponent>> RightReleased;
        public Action<List<GameComponent>, InputManager.MouseButton> Selected;
        public Action<IEnumerable<GameComponent>> MouseOver;
        public WorldManager World;
        private List<GameComponent> SelectedEntities = new List<GameComponent>();

        public BodySelector(Camera camera, GraphicsDevice graphics, ComponentManager components)
        {
            World = components.World;
            CameraController = camera;
            Components = components;
            LeftReleased = LeftReleasedCallback;
            RightReleased = RightReleasedCallback;
            MouseOver = bodies => { };
            Selected += SelectedCallback;
            DeleteColor = GameSettings.Default.Colors.GetColor("Negative", Color.Red);
        }

        /// <summary>
        ///     Called when some number of bodies are underneath the mouse for a certain amount of time.
        /// </summary>
        /// <param name="entities">A list of bodies that were underneath the mouse.</param>
        private void OnMouseOver(IEnumerable<GameComponent> entities)
        {
            MouseOver.Invoke(entities);
            foreach (GameComponent body in entities)
                World.UserInterface.ShowInfo(body.GlobalID, body.GetDescription());
        }

        public void Update()
        {
            if (!Enabled)
                return;

            var mouse = Mouse.GetState();
            var keyboard = Keyboard.GetState();

            // Select bodies under the mouse if it is hovering.
            MouseOverTimer.Update(DwarfTime.LastTime);
            if (MouseOverTimer.HasTriggered)
            {
                SelectedEntities = Components.FindRootBodiesInsideScreenRectangle(new Rectangle(mouse.X - 2, mouse.Y - 2, 4, 4), CameraController);

                if (SelectedEntities.Count > 0)
                    OnMouseOver(SelectedEntities);
                else
                    OnMouseOver(new List<GameComponent>());
            }

            // If the left mouse button is pressed, update the selection rectangle.
            if (isLeftPressed)
            {
                if (mouse.LeftButton == ButtonState.Released)
                {
                    isLeftPressed = false;
                    SelectionBuffer = Components.FindRootBodiesInsideScreenRectangle(SelectionRectangle, CameraController);
                    LeftReleased.Invoke();
                }
                else
                    UpdateSelectionRectangle(mouse.X, mouse.Y);
            }
            // Otherwise, if the mouse has first been pressed, initialize selection
            else if (mouse.LeftButton == ButtonState.Pressed)
            {
                isLeftPressed = true;
                ClickPoint = new Point(mouse.X, mouse.Y);
                ClickPoint3D = World.Renderer.CursorLightPos;
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
                        SelectionBuffer = Components.FindRootBodiesInsideScreenRectangle(SelectionRectangle, CameraController);
                        RightReleased.Invoke();
                    }
                    else
                        UpdateSelectionRectangle(mouse.X, mouse.Y);
                }
                // Otherwise if the mouse has been first pressed, initialize selection
                else if (mouse.RightButton == ButtonState.Pressed)
                {
                    isRightPressed = true;
                    ClickPoint = new Point(mouse.X, mouse.Y);
                    ClickPoint3D = World.Renderer.CursorLightPos;
                    SelectionRectangle = new Rectangle(mouse.X, mouse.Y, 0, 0);
                }
            }

            // If no mouse button has been pressed, there are no bodies currently being selected.
            if (!isLeftPressed && !isRightPressed)
                CurrentBodies.Clear();
        }

        /// <summary>
        ///     Given the current moue position, update the selection rectangle.
        /// </summary>
        /// <param name="mouseX">The current position of the mouse (X, pixels)</param>
        /// <param name="mouseY">The current position of the mouse (Y, pixels)</param>
        private void UpdateSelectionRectangle(int mouseX, int mouseY)
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
                CurrentBodies = Components.FindRootBodiesInsideScreenRectangle(SelectionRectangle, CameraController);
            }
        }

        /// <summary>
        ///     Called whenever bodies are selected.
        /// </summary>
        /// <param name="bodies">The bodies that were selected.</param>
        /// <param name="button">The mouse button (left, right, middle) that was pressed to select the bodies.</param>
        public void SelectedCallback(List<GameComponent> bodies, InputManager.MouseButton button)
        {
        }


        /// <summary>
        ///     Render the selection rectangle.
        /// </summary>
        /// <param name="batch">Sprite batch to render with.</param>
        public void Render(SpriteBatch batch)
        {
            if (!isLeftPressed && !isRightPressed)
                return;

            var rectColor = SelectionColor;
            if (isRightPressed && AllowRightClickSelection)
                rectColor = DeleteColor;

            Drawer2D.DrawRect(batch, SelectionRectangle, rectColor, 4);
        }


        /// <summary>
        ///     Called whenever the left mouse button was released.
        /// </summary>
        /// <returns>A list of selected bodies.</returns>
        private List<GameComponent> LeftReleasedCallback()
        {
            var toReturn = new List<GameComponent>();
            toReturn.AddRange(SelectionBuffer);
            SelectionBuffer.Clear();
            Selected.Invoke(toReturn, InputManager.MouseButton.Left);
            return toReturn;
        }

        /// <summary>
        ///     Called whenever the right mouse button was released.
        /// </summary>
        /// <returns>A list of selected bodies.</returns>
        private List<GameComponent> RightReleasedCallback()
        {
            var toReturn = new List<GameComponent>();
            toReturn.AddRange(SelectionBuffer);
            SelectionBuffer.Clear();
            Selected.Invoke(toReturn, InputManager.MouseButton.Right);
            return toReturn;
        }
    }
}