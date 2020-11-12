using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
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
        public delegate void OnDragged(List<VoxelHandle> voxels, InputManager.MouseButton button);

        public void Clear()
        {
            SelectionBuffer.Clear();
        }

        /// <summary>
        /// Called whenever a list of voxels have been selected.
        /// </summary>
        /// <param name="voxels">The voxels.</param>
        /// <param name="button">The button depressed to select the voxels.</param>
        public delegate void OnSelected(List<VoxelHandle> voxels, InputManager.MouseButton button);

        /// <summary>
        /// The first voxel selected before the player begins dragging the mouse.
        /// </summary>
        public VoxelHandle FirstVoxel = VoxelHandle.InvalidHandle;
        /// <summary>
        /// The voxel currently under the mouse.
        /// </summary>
        public VoxelHandle VoxelUnderMouse = VoxelHandle.InvalidHandle;
        /// <summary>
        /// True if the left mouse button is depressed.
        /// </summary>
        private bool isLeftPressed;
        /// <summary>
        /// True if the right mouse button is depressed.
        /// </summary>
        private bool isRightPressed;

        public IVoxelBrush Brush = VoxelBrushes.BoxBrush;

        public SoundSource ClickSound;
        public SoundSource DragSound;
        public SoundSource ReleaseSound;

        public WorldManager World;

        public bool DrawBox = true;
        public bool DrawVoxel = true;

        // Todo: Remove unused arguments
        public VoxelSelector(WorldManager World)
        {
            this.World = World;

            SelectionType = VoxelSelectionType.SelectEmpty;
            SelectionColor = Color.White;
            SelectionWidth = 0.1f;
            CurrentWidth = 0.08f;
            CurrentColor = Color.White;
            SelectionBuffer = new List<VoxelHandle>();
            Dragged = DraggedCallback;
            Selected = SelectedCallback;
            Enabled = true;
            DeleteColor = Color.Red;
            BoxYOffset = 0;
            LastMouseWheel = 0;
            ClickSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_gui_change_selection);
            ClickSound.RandomPitch = false;
            DragSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_gui_click_voxel);
            DragSound.RandomPitch = false;
            ReleaseSound = SoundSource.Create(ContentPaths.Audio.Oscar.sfx_gui_confirm_selection);
            ReleaseSound.RandomPitch = false;
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
        public Camera CameraController { get { return World.Renderer.Camera; } }
        public GraphicsDevice Graphics { get { return GameState.Game.GraphicsDevice; } }
        public ChunkManager Chunks { get { return World.ChunkManager; } }
        /// <summary>
        /// This is the list of voxels currently selected.
        /// </summary>
        /// <value>
        /// The selection buffer.
        /// </value>
        public List<VoxelHandle> SelectionBuffer { get; set; }

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

        private int PrevBoxYOffsetInt = 0;
        /// <summary>
        /// Gets or sets the last value of the mouse wheel.
        /// </summary>
        /// <value>
        /// The last mouse wheel.
        /// </value>
        public int LastMouseWheel { get; set; }
        public event OnDragged Dragged;

        public void Update()
        {
            MouseState mouse = Mouse.GetState();
            KeyboardState keyboard = Keyboard.GetState();

            var currentHoverVoxel = GetVoxelUnderMouse();

            if (!currentHoverVoxel.IsValid)
                return;

            bool isNewVoxelUnderMouse = currentHoverVoxel.IsValid && currentHoverVoxel != VoxelUnderMouse;
            
            // Prevent selection of top layer because building here causes graphical glitches.
            if (SelectionType == VoxelSelectionType.SelectEmpty && currentHoverVoxel.Coordinate.Y == World.WorldSizeInVoxels.Y - 1)
                return;

            VoxelUnderMouse = currentHoverVoxel;
            World.Renderer.CursorLightPos = currentHoverVoxel.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f);

            if (!Enabled)
                return;

            // Get the type of the voxel and display it to the player.
            if (Enabled && !currentHoverVoxel.IsEmpty && currentHoverVoxel.IsExplored)
            {
                string info = currentHoverVoxel.Type.Name;

                // If it belongs to a room, display that information.
                if (World.IsInZone(currentHoverVoxel))
                {
                    var room = World.GetMostLikelyZone(currentHoverVoxel);

                    if (room != null)
                        info += " (" + room.ID + ")";
                }

                World.UserInterface.ShowInfo(Gui.Widgets.InfoTray.TopEntry, info);
            }


            bool altPressed = HandleAltPressed(mouse, keyboard, ref isNewVoxelUnderMouse);

            // Draw a box around the current voxel under the mouse.
            if (currentHoverVoxel.IsValid && DrawVoxel)
            {
                BoundingBox box = currentHoverVoxel.GetBoundingBox().Expand(0.05f);
                Drawer3D.DrawBox(box, CurrentColor, CurrentWidth, true);
            }

            HandleMouseButton(mouse.LeftButton, currentHoverVoxel, isNewVoxelUnderMouse, altPressed, ref isLeftPressed, InputManager.MouseButton.Left);
            HandleMouseButton(mouse.RightButton, currentHoverVoxel, isNewVoxelUnderMouse, altPressed, ref isRightPressed, InputManager.MouseButton.Right);
        }

        private void HandleMouseButton(ButtonState ButtonState, VoxelHandle underMouse, bool newVoxel, bool altPressed, ref bool ButtonPressed, InputManager.MouseButton Button)
        {
            // If the left mouse button is pressed, update the slection buffer.
            if (ButtonPressed)
            {
                // On release, select voxels.
                if (ButtonState == ButtonState.Released)
                {
                    ReleaseSound.Play(World.Renderer.CursorLightPos);
                    ButtonPressed = false;

                    if (SelectionBuffer.Count > 0)
                    {
                        var t = new List<VoxelHandle>(SelectionBuffer);
                        SelectionBuffer.Clear();
                        Selected.Invoke(t, Button);
                    }

                    BoxYOffset = 0;
                    PrevBoxYOffsetInt = 0;
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
                            buffer.Max.Y = MathFunctions.Clamp(buffer.Max.Y + (int)BoxYOffset, 0, World.WorldSizeInVoxels.Y - 1);
                        else if (BoxYOffset < 0)
                            buffer.Min.Y = MathFunctions.Clamp(buffer.Min.Y - (int)BoxYOffset, 0, World.WorldSizeInVoxels.Y - 1);

                        SelectionBuffer = Select(buffer, FirstVoxel.WorldPosition, underMouse.WorldPosition).ToList();

                        if (!altPressed && Brush.CullUnseenVoxels && SelectionType == VoxelSelectionType.SelectFilled)
                        {
                            SelectionBuffer.RemoveAll(v =>
                            {
                                if (v.Equals(underMouse)) return false;
                                if (World.PersistentData.Designations.IsVoxelDesignation(v, DesignationType.Put)) return false; // Treat put designations as solid.
                                return !VoxelHelpers.DoesVoxelHaveVisibleSurface(World, v);
                            });
                        }

                        if (newVoxel)
                        {
                            DragSound.Play(World.Renderer.CursorLightPos, SelectionBuffer.Count / 20.0f);
                            Dragged.Invoke(SelectionBuffer, Button);
                        }
                    }
                }
            }
            // If the mouse was not previously pressed, but is now pressed, then notify us of that.
            else if (ButtonState == ButtonState.Pressed)
            {
                ClickSound.Play(World.Renderer.CursorLightPos); ;
                ButtonPressed = true;
                BoxYOffset = 0;
                PrevBoxYOffsetInt = 0;
            }
        }

        private bool HandleAltPressed(MouseState mouse, KeyboardState keyboard, ref bool newVoxel)
        {
            bool altPressed = false;
            
            // If the left or right ALT keys are pressed, we can adjust the height of the selection for building pits and tall walls using the mouse wheel.
            if (keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt))
            {
                var change = mouse.ScrollWheelValue - LastMouseWheel;
                BoxYOffset += (change) * 0.01f;
                int offset = (int)BoxYOffset;
                if (offset != PrevBoxYOffsetInt)
                {
                    DragSound.Play(World.Renderer.CursorLightPos);
                    newVoxel = true;
                }
                PrevBoxYOffsetInt = offset;
                altPressed = true;
            }
            else
            {
                PrevBoxYOffsetInt = 0;
            }

            LastMouseWheel = mouse.ScrollWheelValue;

            return altPressed;
        }

        public IEnumerable<VoxelHandle> Select(BoundingBox buffer, Vector3 start, Vector3 end)
        {
            return Brush.Select(buffer, start, end, SelectionType != VoxelSelectionType.SelectEmpty)
                .Select(c => new VoxelHandle(Chunks, c))
                .Where(v => VoxelPassesSelectionCriteria(v));
        }

        public void DraggedCallback(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
        }


        public void SelectedCallback(List<VoxelHandle> voxels, InputManager.MouseButton button)
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
                return;
            
            BoundingBox superset = GetSelectionBox(0.1f);

            if (DrawBox)
            {
                Drawer3D.DrawBox(superset, Mouse.GetState().LeftButton == ButtonState.Pressed ? SelectionColor : DeleteColor,  SelectionWidth, false);

                var screenRect = new Rectangle(0, 0, 5, 5);
                Vector3 half = Vector3.One * 0.5f;
                Color dotColor = Mouse.GetState().LeftButton == ButtonState.Pressed ? SelectionColor : DeleteColor;
                dotColor.A = 90;

                foreach (var v in SelectionBuffer)
                {
                    if (!v.IsValid) continue;

                    if (!v.IsExplored || VoxelPassesSelectionCriteria(v))
                        Drawer2D.DrawRect(World.Renderer.Camera, v.WorldPosition + half, screenRect, dotColor, Color.Transparent, 0.0f);
                }
            }
        }

        public VoxelHandle GetVoxelUnderMouse()
        {
            var mouse = KeyManager.TrueMousePos;

            var v = VoxelHelpers.FindFirstVisibleVoxelOnScreenRay(
                Chunks,
                mouse.X,
                mouse.Y,
                CameraController,
                Graphics.Viewport,
                150.0f,
                SelectionType == VoxelSelectionType.SelectEmpty,
                VoxelPassesRayFilter);

            if (!v.IsValid)
                return VoxelHandle.InvalidHandle;

            if (VoxelPassesSelectionCriteria(v))
                return v;

            return VoxelHandle.InvalidHandle;
        }

        private bool VoxelPassesSelectionCriteria(VoxelHandle V)
        {
            if (!V.IsValid) return false;

            switch (SelectionType)
            {
                case VoxelSelectionType.SelectPrism:
                    return true;
                case VoxelSelectionType.SelectFilled:
                    return !V.IsEmpty || !V.IsExplored || World.PersistentData.Designations.IsVoxelDesignation(V, DesignationType.Put);
                case VoxelSelectionType.SelectEmpty:
                    return V.IsEmpty && V.IsExplored;
                default:
                    return false;
            }
        }

        private bool VoxelPassesRayFilter(VoxelHandle V)
        {
            if (!V.IsValid) return false;
            return !V.IsEmpty || !V.IsExplored || World.PersistentData.Designations.IsVoxelDesignation(V, DesignationType.Put);
        }
    }
}
