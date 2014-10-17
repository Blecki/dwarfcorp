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

    public enum VoxelSelectionType
    {
        SelectFilled,
        SelectEmpty
    }

    /// <summary>
    /// This class handles selecting regions of voxels with the mouse.
    /// </summary>
    public class VoxelSelector
    {
        public delegate Voxel OnLeftPressed();

        public delegate List<Voxel> OnLeftReleased();

        public delegate Voxel OnRightPressed();

        public delegate List<Voxel> OnRightReleased();

        public delegate void OnSelected(List<Voxel> voxels, InputManager.MouseButton button);

        public Color SelectionColor { get; set; }
        public Color DeleteColor { get; set; }
        public Color CurrentColor { get; set; }
        public float CurrentWidth { get; set; }
        public float SelectionWidth { get; set; }
        public VoxelSelectionType SelectionType { get; set; }
        public event OnLeftPressed LeftPressed;
        public event OnRightPressed RightPressed;
        public event OnLeftReleased LeftReleased;
        public event OnRightReleased RightReleased;
        public OnSelected Selected { get; set; }
        public Camera CameraController { get; set; }
        public GraphicsDevice Graphics { get; set; }
        public ChunkManager Chunks { get; set; }
        public List<Voxel> SelectionBuffer { get; set; }
        private bool isLeftPressed = false;
        private bool isRightPressed = false;
        public Voxel FirstVoxel = default(Voxel);
        public bool Enabled { get; set; }
        public float BoxYOffset { get; set; }
        public int LastMouseWheel { get; set; }

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
            Selected = SelectedCallback;
            Enabled = true;
            DeleteColor = Color.Red;
            BoxYOffset = 0;
            LastMouseWheel = 0;
        }

        public void Update()
        {
            MouseState mouse = Mouse.GetState();
            KeyboardState keyboard = Keyboard.GetState();

            Voxel underMouse = GetVoxelUnderMouse();

            if(underMouse != null)
            {
                PlayState.CursorLightPos = underMouse.Position + new Vector3(0.5f, 0.5f, 0.5f);
            }

            if(!Enabled)
            {
                return;
            }

            if(keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt))
            {
                BoxYOffset += (float) (mouse.ScrollWheelValue - LastMouseWheel) * 0.01f;
                LastMouseWheel = mouse.ScrollWheelValue;
            }
            else
            {
                LastMouseWheel = mouse.ScrollWheelValue;
            }


            if(underMouse != null)
            {
                BoundingBox box = underMouse.GetBoundingBox().Expand(0.05f);
                Drawer3D.DrawBox(box, CurrentColor, CurrentWidth, true);
            }

            if(isLeftPressed)
            {
                if(mouse.LeftButton == ButtonState.Released)
                {
                    isLeftPressed = false;
                    LeftReleasedCallback();
                    BoxYOffset = 0;
                }
                else
                {
                    if(SelectionBuffer.Count == 0)
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


                        if(BoxYOffset > 0)
                        {
                            buffer.Max.Y += BoxYOffset;
                        }
                        else if(BoxYOffset < 0)
                        {
                            buffer.Min.Y += BoxYOffset;
                        }

                        SelectionBuffer = Chunks.ChunkData.GetVoxelsIntersecting(buffer);
                    }
                }
            }
            else if(mouse.LeftButton == ButtonState.Pressed)
            {
                LeftPressedCallback();
                isLeftPressed = true;
                BoxYOffset = 0;
            }


            if(isRightPressed)
            {
                if(mouse.RightButton == ButtonState.Released)
                {
                    isRightPressed = false;
                    RightReleasedCallback();
                    BoxYOffset = 0;
                }
                else
                {
                    if(SelectionBuffer.Count == 0)
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
                        if(BoxYOffset > 0)
                        {
                            buffer.Max.Y += BoxYOffset;
                        }
                        else if(BoxYOffset < 0)
                        {
                            buffer.Min.Y += BoxYOffset;
                        }


                        SelectionBuffer = Chunks.ChunkData.GetVoxelsIntersecting(buffer);
                    }
                }
            }
            else if(mouse.RightButton == ButtonState.Pressed)
            {
                RightPressedCallback();
                BoxYOffset = 0;
                isRightPressed = true;
            }
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
            if(SelectionBuffer.Count <= 0)
            {
                return;
            }

            BoundingBox superset = GetSelectionBox(0.1f);

            Drawer3D.DrawBox(superset, Mouse.GetState().LeftButton == ButtonState.Pressed ? SelectionColor : DeleteColor, SelectionWidth, false);
        }

        public Voxel GetVoxelUnderMouse()
        {
            MouseState mouse = Mouse.GetState();

            Voxel v = Chunks.ChunkData.GetFirstVisibleBlockHitByMouse(mouse, CameraController, Graphics.Viewport);

            if (v == null)
            {
                return null;
            }

            if(!v.IsEmpty)
            {
                if(Keyboard.GetState().IsKeyDown(Keys.Space))
                {
                    //PlayState.ParticleManager.Trigger("flame", v.Position, new Color(v.SunColors[(int)VoxelVertex.FrontTopRight],  v.AmbientColors[(int)VoxelVertex.FrontTopRight],  v.DynamicColors[(int)VoxelVertex.FrontTopRight]), 5);
                }
                else if(Keyboard.GetState().IsKeyDown(ControlSettings.Default.SliceSelected))
                {
                    Chunks.ChunkData.SetMaxViewingLevel(v.Position.Y, ChunkManager.SliceMode.Y);
                }
                else if(Keyboard.GetState().IsKeyDown(ControlSettings.Default.SliceSelectedUp))
                {
                    Chunks.ChunkData.SetMaxViewingLevel(Chunks.ChunkData.ChunkSizeY, ChunkManager.SliceMode.Y);
                }
            }

            switch(SelectionType)
            {
                case VoxelSelectionType.SelectFilled:
                    if(!v.IsEmpty)
                    {
                        return v;
                    }
                    else
                    {
                        return default(Voxel);
                    }


                case VoxelSelectionType.SelectEmpty:
                    if(!v.IsEmpty)
                    {
                        Ray mouseRay = Chunks.ChunkData.GetMouseRay(mouse, CameraController, Graphics.Viewport);
                        float? dist = mouseRay.Intersects(v.GetBoundingBox());

                        if(dist.HasValue)
                        {
                            float length = dist.Value;

                            Vector3 hit = mouseRay.Position + mouseRay.Direction * length;

                            Vector3 antiDelta = new Vector3(0, 0, 0);

                            Vector3 delta = hit - (v.Position + new Vector3(0.5f, 0.5f, 0.5f));
                            Vector3 absDelta = new Vector3((float) Math.Abs(delta.X), (float) Math.Abs(delta.Y), (float) Math.Abs(delta.Z));

                            if(absDelta.X > absDelta.Y && absDelta.X > absDelta.Z)
                            {
                                antiDelta = new Vector3((float) Math.Sign(delta.X), 0, 0);
                            }
                            else if(absDelta.Y > absDelta.X && absDelta.Y > absDelta.Z)
                            {
                                antiDelta = new Vector3(0, (float) Math.Sign(delta.Y), 0);
                            }
                            else if(absDelta.Z > absDelta.Y && absDelta.Z > absDelta.X)
                            {
                                antiDelta = new Vector3(0, 0, (float) Math.Sign(delta.Z));
                            }
                            else
                            {
                                break;
                            }

                            Voxel atRef = Chunks.ChunkData.GetVoxelerenceAtWorldLocation(v.Position + new Vector3(0.5f, 0.5f, 0.5f) + antiDelta);

                            if (atRef != null)
                            {
                                return atRef;
                            }
                        }
                    }
                    else
                    {
                        return default(Voxel);
                    }
                    break;
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
            List<Voxel> toReturn = new List<Voxel>();
            if(SelectionBuffer.Count > 0)
            {
                toReturn.AddRange(SelectionBuffer);
                SelectionBuffer.Clear();
                Selected.Invoke(toReturn, InputManager.MouseButton.Left);
            }
            return toReturn;
        }

        public List<Voxel> RightReleasedCallback()
        {
            List<Voxel> toReturn = new List<Voxel>();
            toReturn.AddRange(SelectionBuffer);
            SelectionBuffer.Clear();
            Selected.Invoke(toReturn, InputManager.MouseButton.Right);
            return toReturn;
        }
    }

}