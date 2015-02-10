using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{


    /// <summary>
    /// This class handles selecting regions of bodies with the mouse.
    /// </summary>
    public class BodySelector
    {

        public delegate List<Body> OnLeftReleased();

        public delegate List<Body> OnRightReleased();

        public delegate void OnSelected(List<Body> voxels, InputManager.MouseButton button);
        public ComponentManager Components { get; set; }
        public Color SelectionColor { get; set; }
        public Color DeleteColor { get; set; }
        public Color CurrentColor { get; set; }
        public float CurrentWidth { get; set; }
        public float SelectionWidth { get; set; }
        public VoxelSelectionType SelectionType { get; set; }
        public event OnLeftReleased LeftReleased;
        public event OnRightReleased RightReleased;
        public OnSelected Selected { get; set; }
        public Camera CameraController { get; set; }
        public GraphicsDevice Graphics { get; set; }
        public ChunkManager Chunks { get; set; }
        public List<Body> SelectionBuffer { get; set; }
        private bool isLeftPressed = false;
        private bool isRightPressed = false;
        public bool Enabled { get; set; }
        public float BoxYOffset { get; set; }
        public int LastMouseWheel { get; set; }

        public Vector3 ClickPoint3D { get; set; }
        public Point ClickPoint { get; set; }
        public Rectangle SelectionRectangle { get; set; }

        public bool AllowRightClickSelection { get; set; }

        public BodySelector(Camera camera, GraphicsDevice graphics, ComponentManager components)
        {
            AllowRightClickSelection = true;
            SelectionColor = Color.White;
            SelectionWidth = 0.1f;
            CurrentWidth = 0.08f;
            CurrentColor = Color.White;
            CameraController = camera;
            Graphics = graphics;
            Components = components;
            SelectionBuffer = new List<Body>();
            LeftReleased = LeftReleasedCallback;
            RightReleased = RightReleasedCallback;
            Selected += SelectedCallback;
            Enabled = true;
            DeleteColor = Color.Red;
            BoxYOffset = 0;
            LastMouseWheel = 0;
            SelectionRectangle = new Rectangle(0, 0, 0, 0);
        }

        public List<Body> SelectBodies(Rectangle screenRectangle)
        {
            return Components.SelectRootBodiesOnScreen(screenRectangle, CameraController);
        }

        public void Update()
        {
            MouseState mouse = Mouse.GetState();
            KeyboardState keyboard = Keyboard.GetState();
         
            if (!Enabled)
            {
                return;
            }

            if (keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt))
            {
                BoxYOffset += (float)(mouse.ScrollWheelValue - LastMouseWheel) * 0.01f;
                LastMouseWheel = mouse.ScrollWheelValue;
            }
            else
            {
                LastMouseWheel = mouse.ScrollWheelValue;
            }

            if (isLeftPressed || (isRightPressed && AllowRightClickSelection))
            {
                if (mouse.LeftButton == ButtonState.Released || (mouse.RightButton == ButtonState.Released && AllowRightClickSelection))
                {
                    isLeftPressed = false;
                    SelectionBuffer = Components.SelectRootBodiesOnScreen(SelectionRectangle, CameraController);
                    LeftReleased.Invoke();
                }
                else
                {
                    Vector3 screenPoint = CameraController.Project(ClickPoint3D);
                    ClickPoint = new Point((int)screenPoint.X, (int)screenPoint.Y);
                    UpdateSelectionRectangle(mouse.X, mouse.Y);
                }
            }
            else if (mouse.LeftButton == ButtonState.Pressed || (mouse.RightButton == ButtonState.Pressed && AllowRightClickSelection))
            {
                isLeftPressed = true;
                ClickPoint = new Point(mouse.X, mouse.Y);
                ClickPoint3D = PlayState.CursorLightPos;
                SelectionRectangle = new Rectangle(mouse.X, mouse.Y, 0, 0);
            }


            if (isRightPressed && AllowRightClickSelection)
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
            else if (mouse.RightButton == ButtonState.Pressed && AllowRightClickSelection)
            {
                SelectionRectangle = new Rectangle(mouse.X, mouse.Y, 0, 0);
                isRightPressed = true;
            }
        }

        public void UpdateSelectionRectangle(int mouseX, int mouseY)
        {
            int top = Math.Min(ClickPoint.Y, mouseY);
            int left = Math.Min(ClickPoint.X, mouseX);
            int right = Math.Max(ClickPoint.X, mouseX);
            int bottom = Math.Max(ClickPoint.Y, mouseY);
            SelectionRectangle = new Rectangle(left, top, Math.Max(right - left, 0), Math.Max(bottom - top, 0));
        }


        public void SelectedCallback(List<Body> bodies, InputManager.MouseButton button)
        {
        }



        public void Render(SpriteBatch batch)
        {
            if(!isLeftPressed && !isRightPressed)
            {
                return;
            }
            else
            {
                Color rectColor = SelectionColor;

                if(isRightPressed && AllowRightClickSelection)
                {
                    rectColor = DeleteColor;
                }
                Drawer2D.DrawRect(batch, SelectionRectangle, rectColor, 4);
            }
        }

       
        public List<Body> LeftReleasedCallback()
        {
            List<Body> toReturn = new List<Body>();
            toReturn.AddRange(SelectionBuffer);
            SelectionBuffer.Clear();
            Selected.Invoke(toReturn, InputManager.MouseButton.Left);
            return toReturn;
        }

        public List<Body> RightReleasedCallback()
        {
            List<Body> toReturn = new List<Body>();
            toReturn.AddRange(SelectionBuffer);
            SelectionBuffer.Clear();
            Selected.Invoke(toReturn, InputManager.MouseButton.Right);
            return toReturn;
        }
    }

}