using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    public class VoxelSelector
    {
        public  delegate VoxelRef OnLeftPressed();
        public delegate List<VoxelRef> OnLeftReleased();
        public delegate VoxelRef OnRightPressed();
        public delegate List<VoxelRef> OnRightReleased();
        public delegate void OnSelected(List<VoxelRef> voxels, InputManager.MouseButton button);

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
        public List<VoxelRef> SelectionBuffer { get; set; }
        private bool isLeftPressed = false;
        private bool isRightPressed = false;
        public VoxelRef FirstVoxel = default(VoxelRef);
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
            SelectionBuffer = new List<VoxelRef>();
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

            VoxelRef underMouse = GetVoxelUnderMouse();

            if (underMouse != null)
            {
                PlayState.CursorLightPos = underMouse.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f);
            }

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
            


            if (underMouse != null)
            {
                BoundingBox box = underMouse.GetBoundingBox();
                box.Min -= new Vector3(0.05f, 0.05f, 0.05f);
                box.Max += new Vector3(0.05f, 0.05f, 0.05f);
                SimpleDrawing.DrawBox(box, CurrentColor, CurrentWidth, true);
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


                        if (BoxYOffset > 0)
                        {
                            buffer.Max.Y += BoxYOffset;
                        }
                        else if(BoxYOffset < 0)
                        {
                            buffer.Min.Y += BoxYOffset;
                        }

                        SelectionBuffer = Chunks.GetVoxelsIntersecting(buffer);
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

        public void SelectedCallback(List<VoxelRef> voxels, InputManager.MouseButton button)
        {

        }

        public BoundingBox GetSelectionBox(float expansion)
        {
            List<BoundingBox> aabbs = new List<BoundingBox>();
            foreach (VoxelRef voxel in SelectionBuffer)
            {
                if (voxel != null)
                {
                    aabbs.Add(voxel.GetBoundingBox());
                }
            }

            BoundingBox superset = LinearMathHelpers.GetBoundingBox(aabbs);

            Vector3 ext = new Vector3(1, 1, 1);
            superset.Min -= ext * expansion;
            superset.Max += ext * expansion;

            return superset;
        }

        public BoundingBox GetSelectionBox()
        {
            List<BoundingBox> aabbs = new List<BoundingBox>();
            foreach (VoxelRef voxel in SelectionBuffer)
            {
                if (voxel != null)
                {
                    aabbs.Add(voxel.GetBoundingBox());
                }
            }

            BoundingBox superset = LinearMathHelpers.GetBoundingBox(aabbs);

            return superset;
        }

        public void Render()
        {
            if (SelectionBuffer.Count > 0)
            {
                BoundingBox superset = GetSelectionBox(0.1f);

                if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                {
                    SimpleDrawing.DrawBox(superset, SelectionColor, SelectionWidth, false);
                }
                else
                {
                    SimpleDrawing.DrawBox(superset, DeleteColor, SelectionWidth, false);
                }
            }
        }

        public VoxelRef GetVoxelUnderMouse()
        {
            MouseState mouse = Mouse.GetState();

            Voxel v = Chunks.GetFirstVisibleBlockHitByMouse(mouse, CameraController, Graphics.Viewport);
  

            if (v != null)
            {
   
                if (Keyboard.GetState().IsKeyDown(Keys.Space))
                {
                    //PlayState.ParticleManager.Trigger("flame", v.Position, new Color(v.SunColors[(int)VoxelVertex.FrontTopRight],  v.AmbientColors[(int)VoxelVertex.FrontTopRight],  v.DynamicColors[(int)VoxelVertex.FrontTopRight]), 5);
                }
                else if (Keyboard.GetState().IsKeyDown(ControlSettings.Default.SliceSelected))
                {
                    Chunks.SetMaxViewingLevel(v.Position.Y, ChunkManager.SliceMode.Y);
                }
                else if (Keyboard.GetState().IsKeyDown(ControlSettings.Default.SliceSelectedUp))
                {
                    Chunks.SetMaxViewingLevel(v.Position.Y + 1, ChunkManager.SliceMode.Y);
                }
            }

            switch (SelectionType)
            {
                case VoxelSelectionType.SelectFilled:
                    if (v != null)
                    {
                        return v.GetReference();
                    }
                    else
                    {
                        return default(VoxelRef);
                    }


                case VoxelSelectionType.SelectEmpty:
                    if (v != null)
                    {
                        Ray mouseRay = Chunks.GetMouseRay(mouse, CameraController, Graphics.Viewport);
                        float? dist = mouseRay.Intersects(v.GetBoundingBox());

                        if (dist.HasValue)
                        {
                            float length = dist.Value;

                            Vector3 hit = mouseRay.Position + mouseRay.Direction * length;

                            Vector3 antiDelta = new Vector3(0, 0, 0);

                            Vector3 delta = hit - (v.Position + new Vector3(0.5f, 0.5f, 0.5f));
                            Vector3 absDelta = new Vector3((float)Math.Abs(delta.X), (float)Math.Abs(delta.Y), (float)Math.Abs(delta.Z));

                            if (absDelta.X > absDelta.Y && absDelta.X > absDelta.Z)
                            {
                                antiDelta = new Vector3((float)Math.Sign(delta.X), 0, 0);
                            }
                            else if (absDelta.Y > absDelta.X && absDelta.Y > absDelta.Z)
                            {
                                antiDelta = new Vector3(0, (float)Math.Sign(delta.Y), 0);
                            }
                            else if (absDelta.Z > absDelta.Y && absDelta.Z > absDelta.X)
                            {
                                antiDelta = new Vector3(0, 0, (float)Math.Sign(delta.Z));
                            }
                            else
                            {
                                break;
                            }
                            
                            List<VoxelRef> refs = Chunks.GetVoxelReferencesAtWorldLocation(v.Position + new Vector3(0.5f, 0.5f, 0.5f) + antiDelta);

                            

                            if (refs.Count > 0)
                            {
                                return refs[0];
                            }
                        }
                    }
                    else
                    {
                        return default(VoxelRef);
                    }
                    break;
            }

            return default(VoxelRef);
        }

        public VoxelRef LeftPressedCallback()
        {
            SelectionBuffer.Clear();
            return GetVoxelUnderMouse();
        }

        public VoxelRef RightPressedCallback()
        {
            SelectionBuffer.Clear();
            return GetVoxelUnderMouse();
        }

        public List<VoxelRef> LeftReleasedCallback()
        {
            List<VoxelRef> toReturn = new List<VoxelRef>();
            if (SelectionBuffer.Count > 0)
            {
                toReturn.AddRange(SelectionBuffer);
                SelectionBuffer.Clear();
                Selected.Invoke(toReturn, InputManager.MouseButton.Left);
            }
            return toReturn;
        }

        public List<VoxelRef> RightReleasedCallback()
        {
            List<VoxelRef> toReturn = new List<VoxelRef>();
            toReturn.AddRange(SelectionBuffer);
            SelectionBuffer.Clear();
            Selected.Invoke(toReturn, InputManager.MouseButton.Right);
            return toReturn;
        }


    }
}
