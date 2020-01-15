using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using DwarfCorp.Gui.Widgets;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    public class PlaceObjectTool : PlayerTool
    {
        [ToolFactory("PlaceObject")]
        private static PlayerTool _factory(WorldManager World)
        {
            return new PlaceObjectTool(World);
        }

        public PlaceObjectTool(WorldManager World)
        {
            this.World = World;
        }

        public ResourceType CraftType { get; set; }
        public GameComponent PreviewBody { get; set; }
        private float Orientation = 0.0f;
        private bool OverrideOrientation = false;
        private bool RightPressed = false;
        private bool LeftPressed = false;

        private GameComponent CreatePreviewBody()
        {
            Blackboard blackboard = new Blackboard();

            blackboard.SetData<Resource>("Resource", new Resource(CraftType));

            var previewBody = EntityFactory.CreateEntity<GameComponent>(
                CraftType.Placement_EntityToCreate,
                World.UserInterface.VoxSelector.VoxelUnderMouse.WorldPosition,
                blackboard).GetRoot() as GameComponent;
            previewBody.SetFlagRecursive(GameComponent.Flag.Active, false);
            previewBody.SetVertexColorRecursive(Color.White);
            previewBody.SetFlag(GameComponent.Flag.ShouldSerialize, false);
            return previewBody;
        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            switch (button)
            {
                case (InputManager.MouseButton.Left):
                    {
                            if (ObjectHelper.IsValidPlacement(World.UserInterface.VoxSelector.VoxelUnderMouse, CraftType, World, PreviewBody, "build", "built"))
                            {
                                PreviewBody.SetFlag(GameComponent.Flag.ShouldSerialize, true);

                                Vector3 pos = World.UserInterface.VoxSelector.VoxelUnderMouse.WorldPosition + new Vector3(0.5f, 0.0f, 0.5f) + CraftType.Placement_SpawnOffset;
                                Vector3 startPos = pos + new Vector3(0.0f, -0.1f, 0.0f);

                                var newDesignation = new PlacementDesignation()
                                {
                                    ItemType = CraftType,
                                    Location = World.UserInterface.VoxSelector.VoxelUnderMouse,
                                    Orientation = Orientation,
                                    OverrideOrientation = OverrideOrientation,
                                    Entity = PreviewBody,
                                    WorkPile = new WorkPile(World.ComponentManager, startPos)
                                };

                                World.ComponentManager.RootComponent.AddChild(newDesignation.WorkPile);
                                newDesignation.WorkPile.AnimationQueue.Add(new EaseMotion(1.1f, Matrix.CreateTranslation(startPos), pos));
                                World.ParticleManager.Trigger("puff", pos, Color.White, 10);
                                                               
                                World.TaskManager.AddTask(new PlaceObjectTask(newDesignation));

                                if (!HandlePlaceExistingUpdate())
                                {
                                    PreviewBody = null;
                                    World.UserInterface.ShowToolPopup("Unable to place any more.");
                                    World.UserInterface.ChangeTool("SelectUnits");
                                }
                                else
                                    PreviewBody = CreatePreviewBody();
                            }

                        break;
                    }
                case (InputManager.MouseButton.Right):
                    {
                        var designation = World.PersistentData.Designations.EnumerateEntityDesignations(DesignationType.PlaceObject).Select(d => d.Tag as PlacementDesignation).FirstOrDefault(d => d.Location == World.UserInterface.VoxSelector.VoxelUnderMouse);
                        if (designation != null && World.PersistentData.Designations.GetEntityDesignation(designation.Entity, DesignationType.PlaceObject).HasValue(out var realDesignation))
                            World.TaskManager.CancelTask(realDesignation.Task);
                        break;
                    }
            }
        }

        private bool HandlePlaceExistingUpdate()
        {
            //var resources = World.ListResources().Where(r => Library.GetResourceType(r.Value.Type).HasValue(out var res) && res.TypeName == CraftType.TypeName).ToList();

            //var toPlace = World.PersistentData.Designations.EnumerateEntityDesignations().Where(designation => designation.Type == DesignationType.PlaceObject &&
            //    ((PlacementDesignation)designation.Tag).ItemType.TypeName == CraftType.TypeName).ToList();

            //if ((resources.Count - toPlace.Count) <= 0)
            //    return false;

            return true;
        }

        public override void OnBegin(Object Arguments)
        {
            World.UserInterface.VoxSelector.DrawBox = false;
            World.UserInterface.VoxSelector.DrawVoxel = false;

            CraftType = Arguments as ResourceType;
            if (CraftType == null)
                throw new InvalidOperationException();

            if (!HandlePlaceExistingUpdate())
                World.UserInterface.ShowToolPopup("Unable to place any more.");

            PreviewBody = CreatePreviewBody();
            Orientation = 0.0f;
            OverrideOrientation = false;

            World.UserInterface.ShowToolPopup(Library.GetString("place", CraftType.DisplayName));

        }

        public override void OnEnd()
        {
            World.UserInterface.VoxSelector.DrawBox = true;
            World.UserInterface.VoxSelector.DrawVoxel = true;

            if (PreviewBody != null)
            {
                PreviewBody.GetRoot().Delete();
                PreviewBody = null;
            }

            CraftType = null;
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {

        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (World.UserInterface.IsCameraRotationModeActive())
            {
                World.UserInterface.VoxSelector.Enabled = false;
                World.UserInterface.SetMouse(null);
                World.UserInterface.BodySelector.Enabled = false;
                return;
            }

            World.UserInterface.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;
            World.UserInterface.VoxSelector.Enabled = true;
            World.UserInterface.BodySelector.Enabled = false;

            if (World.UserInterface.IsMouseOverGui)
                World.UserInterface.SetMouse(World.UserInterface.MousePointer);
            else
                World.UserInterface.SetMouse(new Gui.MousePointer("mouse", 1, 4));

            if (PreviewBody == null || !World.UserInterface.VoxSelector.VoxelUnderMouse.IsValid)
                return;

            HandleOrientation();

            PreviewBody.LocalPosition = World.UserInterface.VoxSelector.VoxelUnderMouse.WorldPosition + new Vector3(0.5f, 0.0f, 0.5f) + CraftType.Placement_SpawnOffset;
            PreviewBody.UpdateTransform();
            PreviewBody.PropogateTransforms();

            foreach (var tinter in PreviewBody.EnumerateAll().OfType<Tinter>())
                tinter.Stipple = true;

            if (OverrideOrientation)
                PreviewBody.Orient(Orientation);
            else
                PreviewBody.OrientToWalls();

            var valid = ObjectHelper.IsValidPlacement(World.UserInterface.VoxSelector.VoxelUnderMouse, CraftType, World, PreviewBody, "build", "built");
            PreviewBody.SetVertexColorRecursive(valid ? GameSettings.Current.Colors.GetColor("Positive", Color.Green) : GameSettings.Current.Colors.GetColor("Negative", Color.Red));

            if (valid && CraftType.Placement_AllowRotation)
                World.UserInterface.ShowTooltip("Click to build. Press R/T to rotate.");
        }

        public override void Render2D(DwarfGame game, DwarfTime time)
        {
            if (PreviewBody != null)
            {
                Drawer2D.DrawPolygon(World.Renderer.Camera, new List<Vector3>() { PreviewBody.Position, PreviewBody.Position + PreviewBody.GlobalTransform.Right * 0.5f },
                    Color.White, 1, false, GameState.Game.GraphicsDevice.Viewport);
            }
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {

        }


        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {

        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        private void HandleOrientation()
        {
            // Don't attempt any control if the user is trying to type into a focus item.
            if (World.UserInterface.Gui.FocusItem == null || World.UserInterface.Gui.FocusItem.IsAnyParentTransparent() || World.UserInterface.Gui.FocusItem.IsAnyParentHidden())
            {
                KeyboardState state = Keyboard.GetState();
                bool leftKey = state.IsKeyDown(ControlSettings.Mappings.RotateObjectLeft);
                bool rightKey = state.IsKeyDown(ControlSettings.Mappings.RotateObjectRight);
                if (LeftPressed && !leftKey)
                {
                    OverrideOrientation = true;
                    LeftPressed = false;
                    Orientation += (float)(Math.PI / 2);
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_confirm_selection, PreviewBody.Position,
                        0.5f);
                }

                if (RightPressed && !rightKey)
                {
                    OverrideOrientation = true;
                    RightPressed = false;
                    Orientation -= (float)(Math.PI / 2);
                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_confirm_selection, PreviewBody.Position, 0.5f);
                }

                LeftPressed = leftKey;
                RightPressed = rightKey;
            }
        }
    }
}
