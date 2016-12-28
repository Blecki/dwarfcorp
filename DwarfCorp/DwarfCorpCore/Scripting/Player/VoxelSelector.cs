using System.Collections.Generic;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    public enum VoxelSelectionType
    {
        SelectFilled,
        SelectEmpty
    }

    /// <summary>
    ///     This class handles selecting regions of voxels with the mouse.
    /// </summary>
    public class VoxelSelector
    {
        public delegate void OnDragged(List<Voxel> voxels, InputManager.MouseButton button);

        public delegate Voxel OnLeftPressed();

        public delegate List<Voxel> OnLeftReleased();

        public delegate Voxel OnRightPressed();

        public delegate List<Voxel> OnRightReleased();


        public delegate void OnSelected(List<Voxel> voxels, InputManager.MouseButton button);

        public Voxel FirstVoxel = default(Voxel);
        public Voxel VoxelUnderMouse = default(Voxel);
        private bool isLeftPressed;
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

        public Color SelectionColor { get; set; }
        public Color DeleteColor { get; set; }
        public Color CurrentColor { get; set; }
        public float CurrentWidth { get; set; }
        public float SelectionWidth { get; set; }
        public VoxelSelectionType SelectionType { get; set; }

        public OnSelected Selected { get; set; }
        public Camera CameraController { get; set; }
        public GraphicsDevice Graphics { get; set; }
        public ChunkManager Chunks { get; set; }
        public List<Voxel> SelectionBuffer { get; set; }
        public bool Enabled { get; set; }
        public float BoxYOffset { get; set; }
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
            bool newVoxel = underMouse != null && !underMouse.Equals(VoxelUnderMouse);

            if (underMouse != null)
            {
                VoxelUnderMouse = underMouse;
                PlayState.CursorLightPos = underMouse.Position + new Vector3(0.5f, 0.5f, 0.5f);

                if (Enabled && underMouse.TypeName != "empty" && underMouse.IsExplored)
                {
                    string info = underMouse.TypeName;


                    if (PlayState.PlayerFaction.RoomBuilder.IsInRoom(underMouse))
                    {
                        Room room = PlayState.PlayerFaction.RoomBuilder.GetMostLikelyRoom(underMouse);

                        if (room != null)
                            info += " (" + room.ID + ")";
                    }
                    PlayState.GUI.ToolTipManager.PopupInfo(info);
                }
            }

            if (!Enabled)
            {
                return;
            }

            if (keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt))
            {
                BoxYOffset += (mouse.ScrollWheelValue - LastMouseWheel)*0.01f;
                LastMouseWheel = mouse.ScrollWheelValue;
                newVoxel = true;
            }
            else
            {
                LastMouseWheel = mouse.ScrollWheelValue;
            }


            if (underMouse != null)
            {
                BoundingBox box = underMouse.GetBoundingBox().Expand(0.05f);
                Drawer3D.DrawBox(box, CurrentColor, CurrentWidth, true);
            }

            if (isLeftPressed)
            {
                if (mouse.LeftButton == ButtonState.Released)
                {
                    isLeftPressed = false;
                    LeftReleasedCallback();
                    BoxYOffset = 0;
                }
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


                        if (BoxYOffset > 0)
                        {
                            buffer.Max.Y += BoxYOffset;
                        }
                        else if (BoxYOffset < 0)
                        {
                            buffer.Min.Y += BoxYOffset;
                        }

                        SelectionBuffer = Chunks.GetVoxelsIntersecting(buffer);

                        if (newVoxel)
                            Dragged.Invoke(SelectionBuffer, InputManager.MouseButton.Left);
                    }
                }
            }
            else if (mouse.LeftButton == ButtonState.Pressed)
            {
                LeftPressedCallback();
                isLeftPressed = true;
                BoxYOffset = 0;
            }


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

            var screenRect = new Rectangle(0, 0, 10, 10);
            Vector3 half = Vector3.One*0.5f;
            Color dotColor = Mouse.GetState().LeftButton == ButtonState.Pressed ? SelectionColor : DeleteColor;
            dotColor.A = 90;
            foreach (Voxel v in SelectionBuffer)
            {
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