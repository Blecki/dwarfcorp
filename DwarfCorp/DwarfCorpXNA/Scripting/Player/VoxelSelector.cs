using System.Collections.Generic;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    /// <summary>
    /// The behavior of the voxel selector depends on its type.
    /// </summary>
    public enum VoxelSelectionType
    {
        /// <summary>
        /// Selects only filled voxels
        /// </summary>
        SelectFilled,
        /// <summary>
        /// Selects only empty voxels
        /// </summary>
        SelectEmpty
    }

    /// <summary>
    /// This class handles selecting and deselecting regions of voxels with the mouse. It is used
    /// in multiple tools.
    /// </summary>
    public class VoxelSelector
    {
        /// <summary>
        /// Called whenever the mouse cursor is dragged.
        /// </summary>
        /// <param name="voxels">The voxels selected.</param>
        /// <param name="button">The button depressed.</param>
        public delegate void OnDragged(List<Voxel> voxels, InputManager.MouseButton button);

        /// <summary>
        /// Called whenever the left mouse button is pressed
        /// </summary>
        /// <returns>The voxel under the mouse</returns>
        public delegate Voxel OnLeftPressed();

        /// <summary>
        /// Called whenever the left mouse button is released.
        /// </summary>
        /// <returns>A list of voxels that were selected</returns>
        public delegate List<Voxel> OnLeftReleased();

        /// <summary>
        /// Called whenever the right mouse button is pressed.
        /// </summary>
        /// <returns>The voxel under the mouse</returns>
        public delegate Voxel OnRightPressed();

        /// <summary>
        /// Called whenever the right mouse button is released
        /// </summary>
        /// <returns>List of voxels selected.</returns>
        public delegate List<Voxel> OnRightReleased();

        /// <summary>
        /// Called whenever a list of voxels have been selected.
        /// </summary>
        /// <param name="voxels">The voxels.</param>
        /// <param name="button">The button depressed to select the voxels.</param>
        public delegate void OnSelected(List<Voxel> voxels, InputManager.MouseButton button);

        /// <summary>
        /// The first voxel selected before the player begins dragging the mouse.
        /// </summary>
        public Voxel FirstVoxel = default(Voxel);
        /// <summary>
        /// The voxel currently under the mouse.
        /// </summary>
        public Voxel VoxelUnderMouse = default(Voxel);
        /// <summary>
        /// True if the left mouse button is depressed.
        /// </summary>
        private bool isLeftPressed;
        /// <summary>
        /// True if the right mouse button is depressed.
        /// </summary>
        private bool isRightPressed;


        public VoxelSelector(Camera camera, GraphicsDevice graphics, ChunkManager chunks)
        {
            SelectionType = VoxelSelectionType.SelectEmpty;
            SelectionColor = Color.White;
            SelectionWidth = 0.1f;
            CurrentWidth = 0.08f;
            CurrentColor = Color.White;
            CameraController = camera;
            Graphics = graphics;
            Chunks = chunks;
            SelectionBuffer = new List<Voxel>();
            LeftPressed = LeftPressedCallback;
            RightPressed = RightPressedCallback;
            LeftReleased = LeftReleasedCallback;
            RightReleased = RightReleasedCallback;
            Dragged = DraggedCallback;
            Selected = SelectedCallback;
            Enabled = true;
            DeleteColor = Color.Red;
            BoxYOffset = 0;
            LastMouseWheel = 0;
        }

        /// <summary>
        /// The color to draw while left mouse button is clicked.
        /// </summary>
        /// <value>
        /// The color of the selection.
        /// </value>
        public Color SelectionColor { get; set; }
        /// <summary>
        /// The color to draw while right mouse button is clicked
        /// </summary>
        /// <value>
        /// The color of the delete.
        /// </value>
        public Color DeleteColor { get; set; }
        /// <summary>
        /// The current color to draw the selection box.
        /// </summary>
        /// <value>
        /// The color of the current.
        /// </value>
        public Color CurrentColor { get; set; }
        /// <summary>
        /// The width of lines to draw while selecting.
        /// </summary>
        /// <value>
        /// The width of the current.
        /// </value>
        public float CurrentWidth { get; set; }
        /// <summary>
        /// The width of the lines to draw while selecting.
        /// </summary>
        /// <value>
        /// The width of the selection.
        /// </value>
        public float SelectionWidth { get; set; }
        /// <summary>
        /// Gets or sets the type of the selection.
        /// </summary>
        /// <value>
        /// The type of the selection.
        /// </value>
        public VoxelSelectionType SelectionType { get; set; }
        /// <summary>
        /// Called when voxels are selected.
        /// </summary>
        public OnSelected Selected { get; set; }
        public Camera CameraController { get; set; }
        public GraphicsDevice Graphics { get; set; }
        public ChunkManager Chunks { get; set; }
        /// <summary>
        /// This is the list of voxels currently selected.
        /// </summary>
        /// <value>
        /// The selection buffer.
        /// </value>
        public List<Voxel> SelectionBuffer { get; set; }

        /// <summary>
        /// If this selector is enabled, when the player clicks they 
        /// will be able to select voxels.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled { get; set; }
        /// <summary>
        /// This value indicates how many voxels above or below the mouse
        /// the player is currently selecting (for example to dig pits using ALT)
        /// </summary>
        /// <value>
        /// The box y offset.
        /// </value>
        public float BoxYOffset { get; set; }
        /// <summary>
        /// Gets or sets the last value of the mouse wheel.
        /// </summary>
        /// <value>
        /// The last mouse wheel.
        /// </value>
        public int LastMouseWheel { get; set; }
        public event OnLeftPressed LeftPressed;
        public event OnRightPressed RightPressed;
        public event OnLeftReleased LeftReleased;
        public event OnRightReleased RightReleased;
        public event OnDragged Dragged;

        public void Update()
        {
            MouseState mouse = Mouse.GetState();
            KeyboardState keyboard = Keyboard.GetState();

            Voxel underMouse = GetVoxelUnderMouse();
            // Keep track of whether a new voxel has been selected.
            bool newVoxel = underMouse != null && !underMouse.Equals(VoxelUnderMouse);

            // If there is a voxel under the mouse...
            if (underMouse != null)
            {
                VoxelUnderMouse = underMouse;
                // Update the cursor light.
                WorldManager.CursorLightPos = underMouse.Position + new Vector3(0.5f, 0.5f, 0.5f);

                // Get the type of the voxel and display it to the player.
                if (Enabled && !underMouse.IsEmpty && underMouse.IsExplored)
                {
                    string info = underMouse.TypeName;

                    // If it belongs to a room, display that information.
                    if (WorldManager.PlayerFaction.RoomBuilder.IsInRoom(underMouse))
                    {
                        Room room = WorldManager.PlayerFaction.RoomBuilder.GetMostLikelyRoom(underMouse);

                        if (room != null)
                            info += " (" + room.ID + ")";
                    }
                    WorldManager.GUI.ToolTipManager.PopupInfo(info);
                }
            }

            // Do nothing if not enabled.
            if (!Enabled)
            {
                return;
            }
            bool altPressed = false;
            // If the left or right ALT keys are pressed, we can adjust the height of the selection
            // for building pits and tall walls using the mouse wheel.
            if (keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt))
            {
                BoxYOffset += (mouse.ScrollWheelValue - LastMouseWheel) * 0.01f;
                LastMouseWheel = mouse.ScrollWheelValue;
                newVoxel = true;
                altPressed = true;
            }
            else
            {
                LastMouseWheel = mouse.ScrollWheelValue;
            }

            // Draw a box around the current voxel under the mouse.
            if (underMouse != null)
            {
                BoundingBox box = underMouse.GetBoundingBox().Expand(0.05f);
                Drawer3D.DrawBox(box, CurrentColor, CurrentWidth, true);
            }

            // If the left mouse button is pressed, update the slection buffer.
            if (isLeftPressed)
            {
                // On release, select voxels.
                if (mouse.LeftButton == ButtonState.Released)
                {
                    isLeftPressed = false;
                    LeftReleasedCallback();
                    BoxYOffset = 0;
                }
                // Otherwise, update the selection buffer
                else
                {
                    if (SelectionBuffer.Count == 0)
                    {
                        FirstVoxel = underMouse;
                        SelectionBuffer.Add(underMouse);
                    }
                    else
                    {
                        SelectionBuffer.Clear();
                        SelectionBuffer.Add(FirstVoxel);
                        SelectionBuffer.Add(underMouse);
                        BoundingBox buffer = GetSelectionBox();

                        // Update the selection box to account for offsets from mouse wheel.
                        if (BoxYOffset > 0)
                        {
                            buffer.Max.Y += BoxYOffset;
                        }
                        else if (BoxYOffset < 0)
                        {
                            buffer.Min.Y += BoxYOffset;
                        }

                        SelectionBuffer = Chunks.GetVoxelsIntersecting(buffer);

                        if (!altPressed)
                        {
                            if (SelectionType == VoxelSelectionType.SelectFilled)
                            { 
                                SelectionBuffer.RemoveAll(
                                voxel =>
                                    (!voxel.Equals(underMouse) && !Chunks.ChunkData.IsVoxelVisibleSurface(voxel)));
                            }
                        }

                        if (newVoxel)
                            Dragged.Invoke(SelectionBuffer, InputManager.MouseButton.Left);
                    }
                }
            }
            // If the mouse was not previously pressed, but is now pressed, then notify us of that.
            else if (mouse.LeftButton == ButtonState.Pressed)
            {
                LeftPressedCallback();
                isLeftPressed = true;
                BoxYOffset = 0;
            }

            // Case where the right mouse button is pressed (mirrors left mouse button)
            // TODO(Break this into a function)
            if (isRightPressed)
            {
                if (mouse.RightButton == ButtonState.Released)
                {
                    isRightPressed = false;
                    RightReleasedCallback();
                    BoxYOffset = 0;
                }
                else
                {
                    if (SelectionBuffer.Count == 0)
                    {
                        SelectionBuffer.Add(underMouse);
                        FirstVoxel = underMouse;
                    }
                    else
                    {
                        SelectionBuffer.Clear();
                        SelectionBuffer.Add(FirstVoxel);
                        SelectionBuffer.Add(underMouse);
                        BoundingBox buffer = GetSelectionBox();
                        if (BoxYOffset > 0)
                        {
                            buffer.Max.Y += BoxYOffset;
                        }
                        else if (BoxYOffset < 0)
                        {
                            buffer.Min.Y += BoxYOffset;
                        }


                        SelectionBuffer = Chunks.GetVoxelsIntersecting(buffer);
                        if (!altPressed)
                        {
                            SelectionBuffer.RemoveAll(
                                voxel =>
                                    (!voxel.Equals(underMouse) && Chunks.ChunkData.IsVoxelOccluded(voxel)));
                        }
                        if (newVoxel)
                            Dragged.Invoke(SelectionBuffer, InputManager.MouseButton.Right);
                    }
                }
            }
            else if (mouse.RightButton == ButtonState.Pressed)
            {
                RightPressedCallback();
                BoxYOffset = 0;
                isRightPressed = true;
            }
        }

        public void DraggedCallback(List<Voxel> voxels, InputManager.MouseButton button)
        {
        }


        public void SelectedCallback(List<Voxel> voxels, InputManager.MouseButton button)
        {
        }

        public BoundingBox GetSelectionBox(float expansion)
        {
            List<BoundingBox> aabbs = (from voxel in SelectionBuffer
                                       where voxel != null
                                       select voxel.GetBoundingBox()).ToList();

            BoundingBox superset = MathFunctions.GetBoundingBox(aabbs).Expand(expansion);

            return superset;
        }

        public BoundingBox GetSelectionBox()
        {
            List<BoundingBox> aabbs = (from voxel in SelectionBuffer
                                       where voxel != null
                                       select voxel.GetBoundingBox()).ToList();

            BoundingBox superset = MathFunctions.GetBoundingBox(aabbs);

            return superset;
        }

        public void Render()
        {
            if (SelectionBuffer.Count <= 0)
            {
                return;
            }

            BoundingBox superset = GetSelectionBox(0.1f);

            Drawer3D.DrawBox(superset, Mouse.GetState().LeftButton == ButtonState.Pressed ? SelectionColor : DeleteColor,
                SelectionWidth, false);

            var screenRect = new Rectangle(0, 0, 5, 5);
            Vector3 half = Vector3.One * 0.5f;
            Color dotColor = Mouse.GetState().LeftButton == ButtonState.Pressed ? SelectionColor : DeleteColor;
            dotColor.A = 90;
            foreach (Voxel v in SelectionBuffer)
            {
                if (v == null) continue;
                
                if ((SelectionType == VoxelSelectionType.SelectFilled && !v.IsEmpty)
                    || (SelectionType == VoxelSelectionType.SelectEmpty && v.IsEmpty))
                {
                    Drawer2D.DrawRect(v.Position + half, screenRect, dotColor, Color.Transparent, 0.0f);
                }
            }
        }

        public Voxel GetVoxelUnderMouse()
        {
            MouseState mouse = Mouse.GetState();

            Voxel v = Chunks.ChunkData.GetFirstVisibleBlockHitByMouse(mouse, CameraController, Graphics.Viewport,
                SelectionType == VoxelSelectionType.SelectEmpty);

            if (v == null || v.Chunk == null)
            {
                return null;
            }

            if (!v.IsEmpty)
            {
                if (Keyboard.GetState().IsKeyDown(ControlSettings.Mappings.SliceSelected))
                {
                    Chunks.ChunkData.SetMaxViewingLevel(v.Position.Y, ChunkManager.SliceMode.Y);
                }
                else if (Keyboard.GetState().IsKeyDown(ControlSettings.Mappings.Unslice))
                {
                    Chunks.ChunkData.SetMaxViewingLevel(Chunks.ChunkData.ChunkSizeY, ChunkManager.SliceMode.Y);
                }
            }

            switch (SelectionType)
            {
                case VoxelSelectionType.SelectFilled:
                    if (!v.IsEmpty)
                    {
                        return v;
                    }
                    return default(Voxel);


                case VoxelSelectionType.SelectEmpty:
                    return v;
            }

            return default(Voxel);
        }

        public Voxel LeftPressedCallback()
        {
            SelectionBuffer.Clear();
            return GetVoxelUnderMouse();
        }

        public Voxel RightPressedCallback()
        {
            SelectionBuffer.Clear();
            return GetVoxelUnderMouse();
        }

        public List<Voxel> LeftReleasedCallback()
        {
            var toReturn = new List<Voxel>();
            if (SelectionBuffer.Count > 0)
            {
                toReturn.AddRange(SelectionBuffer);
                SelectionBuffer.Clear();
                Selected.Invoke(toReturn, InputManager.MouseButton.Left);
            }
            return toReturn;
        }

        public List<Voxel> RightReleasedCallback()
        {
            var toReturn = new List<Voxel>();
            toReturn.AddRange(SelectionBuffer);
            SelectionBuffer.Clear();
            Selected.Invoke(toReturn, InputManager.MouseButton.Right);
            return toReturn;
        }
    }
}