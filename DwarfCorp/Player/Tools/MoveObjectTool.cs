using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using DwarfCorp.Gui.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    public class MoveObjectTool : PlayerTool
    {
        [ToolFactory("MoveObjects")] // Todo: Normalize name
        private static PlayerTool _factory(WorldManager World)
        {
            return new MoveObjectTool(World);
        }

        public MoveObjectTool(WorldManager World)
        {
            this.World = World;
        }

        private enum ToolState
        {
            Selecting,
            Dragging
        }

        private ToolState State = ToolState.Selecting;

        private GameComponent SelectedBody { get; set; }
        private bool OverrideOrientation = false;
        private float CurrentOrientation = 0.0f;

        private bool leftPressed = false;
        private bool rightPressed = false;

        private Matrix OrigTransform { get; set; }

        public MoveObjectTool()
        {
        }

        public override void OnBegin(Object Arguments)
        {
            State = ToolState.Selecting;
        }

        public override void OnEnd()
        {
            if (SelectedBody != null)
            {
                foreach (var tinter in SelectedBody.GetRoot().EnumerateAll().OfType<Tinter>())
                {
                    tinter.VertexColorTint = Color.White;
                    tinter.Stipple = false;
                }

                if (State == ToolState.Dragging)
                {
                    SelectedBody.LocalTransform = OrigTransform;
                    SelectedBody.PropogateTransforms();
                }
            }
        }

        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {
            
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {
           
        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public bool CanMove(GameComponent entity)
        {
            return entity.Tags.Contains("Moveable") && !entity.IsReserved;
        }

        public void StartDragging(GameComponent entity)
        {
            SelectedBody = entity;
            OrigTransform = SelectedBody.LocalTransform;
            State = ToolState.Dragging;
            SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_confirm_selection, SelectedBody.Position, 0.1f);
            OverrideOrientation = false;
            CurrentOrientation = 0.0f;
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (World.UserInterface.IsCameraRotationModeActive())
                return;

            World.UserInterface.VoxSelector.Enabled = false;
            World.UserInterface.BodySelector.Enabled = false;

            if (World.UserInterface.IsMouseOverGui)
                World.UserInterface.SetMouse(World.UserInterface.MousePointer);
            else
                World.UserInterface.SetMouse(new Gui.MousePointer("mouse", 1, 9));

            MouseState mouse = Mouse.GetState();


            if (State == ToolState.Selecting)
            {
                if (SelectedBody != null)
                    foreach (var tinter in SelectedBody.GetRoot().EnumerateAll().OfType<Tinter>())
                    {
                        tinter.VertexColorTint = Color.White;
                        tinter.Stipple = false;
                    }
                
                SelectedBody = World.ComponentManager.SelectRootBodiesOnScreen(new Rectangle(mouse.X, mouse.Y, 1, 1), World.Renderer.Camera)
                    .Where(body => body.Tags.Contains("Moveable"))
                    .FirstOrDefault();

                if (SelectedBody != null)
                {
                    if (SelectedBody.IsReserved)
                        World.UserInterface.ShowTooltip("Can't move this " + SelectedBody.Name + "\nIt is being used.");
                    else
                    {
                        World.UserInterface.ShowTooltip("Left click and drag to move this " + SelectedBody.Name);
                        foreach (var tinter in SelectedBody.GetRoot().EnumerateAll().OfType<Tinter>())
                        {
                            tinter.VertexColorTint = Color.Blue;
                            tinter.Stipple = false;
                        }
                    }

                    if (mouse.LeftButton == ButtonState.Pressed)
                    {
                        StartDragging(SelectedBody);
                    }
                }
            }
            else if (State == ToolState.Dragging)
            {
                if (SelectedBody == null) throw new InvalidProgramException();

                if (SelectedBody.GetRoot().GetComponent<CraftDetails>().HasValue(out var craftDetails))
                {
                    if (Library.GetCraftable(craftDetails.CraftType).HasValue(out var craftable) && craftable.AllowRotation)
                    {
                        HandleOrientation();
                        World.UserInterface.ShowTooltip(String.Format("Press {0}/{1} to rotate.", ControlSettings.Mappings.RotateObjectLeft, ControlSettings.Mappings.RotateObjectRight));
                    }

                    var voxelUnderMouse = World.UserInterface.VoxSelector.VoxelUnderMouse;
                    if (voxelUnderMouse.IsValid && voxelUnderMouse.IsEmpty)
                    {
                        var spawnOffset = Vector3.Zero;

                        if (Library.GetCraftable(craftDetails.CraftType).HasValue(out var craftItem))
                        {
                            spawnOffset = craftItem.SpawnOffset;

                            SelectedBody.LocalPosition = voxelUnderMouse.WorldPosition + new Vector3(0.5f, 0.0f, 0.5f) + spawnOffset;
                            SelectedBody.UpdateTransform();

                            if (OverrideOrientation)
                                SelectedBody.Orient(CurrentOrientation);
                            else
                                SelectedBody.OrientToWalls();

                            SelectedBody.PropogateTransforms();

                            var validPlacement = ObjectHelper.IsValidPlacement(voxelUnderMouse, craftItem, World, SelectedBody, "move", "moved");

                            foreach (var tinter in SelectedBody.GetRoot().EnumerateAll().OfType<Tinter>())
                            {
                                tinter.VertexColorTint = validPlacement ? Color.Green : Color.Red;
                                tinter.Stipple = true;
                            }

                            if (mouse.LeftButton == ButtonState.Released)
                            {
                                if (validPlacement)
                                {

                                }
                                else
                                {
                                    SelectedBody.LocalTransform = OrigTransform;
                                    SelectedBody.PropogateTransforms();
                                }

                                foreach (var tinter in SelectedBody.GetRoot().EnumerateAll().OfType<Tinter>())
                                {
                                    tinter.VertexColorTint = Color.White;
                                    tinter.Stipple = false;
                                }

                                State = ToolState.Selecting;
                            }
                        }
                        else
                            Console.Error.WriteLine("{0} had no craft item.", craftDetails.CraftType);

                    }
                }
                else
                    Console.Error.WriteLine("Entity had no craft details.");
            }
        }

        private void HandleOrientation()
        {
            // Don't attempt any camera control if the user is trying to type intoa focus item.
            if (World.UserInterface.Gui.FocusItem != null && !World.UserInterface.Gui.FocusItem.IsAnyParentTransparent() && !World.UserInterface.Gui.FocusItem.IsAnyParentHidden())
                return;

            KeyboardState state = Keyboard.GetState();
            bool leftKey = state.IsKeyDown(ControlSettings.Mappings.RotateObjectLeft);
            bool rightKey = state.IsKeyDown(ControlSettings.Mappings.RotateObjectRight);
            if (leftPressed && !leftKey)
            {
                OverrideOrientation = true;
                leftPressed = false;
                CurrentOrientation += (float) (Math.PI/2);
                if (SelectedBody != null)
                {
                    SelectedBody.Orient(CurrentOrientation);
                    SelectedBody.UpdateBoundingBox();
                    SelectedBody.UpdateTransform();
                    SelectedBody.PropogateTransforms();
                    SelectedBody.UpdateBoundingBox();
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_confirm_selection, SelectedBody.Position, 0.5f);
                }
            }

            if (rightPressed && !rightKey)
            {
                OverrideOrientation = true;
                rightPressed = false;
                CurrentOrientation -= (float) (Math.PI/2);
                if (SelectedBody != null)
                {
                    SelectedBody.Orient(CurrentOrientation);
                    SelectedBody.UpdateBoundingBox();
                    SelectedBody.UpdateTransform();
                    SelectedBody.PropogateTransforms();
                    SelectedBody.UpdateBoundingBox();
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_confirm_selection, SelectedBody.Position, 0.5f);
                }
            }

            leftPressed = leftKey;
            rightPressed = rightKey;
        }
        public override void Render2D(DwarfGame game, DwarfTime time)
        {
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
        }

    }
}
