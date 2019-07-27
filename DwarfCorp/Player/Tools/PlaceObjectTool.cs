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

        public CraftItem CraftType { get; set; }
        public GameComponent PreviewBody { get; set; }
        public List<ResourceAmount> SelectedResources;
        private float Orientation = 0.0f;
        private bool OverrideOrientation = false;
        private bool RightPressed = false;
        private bool LeftPressed = false;

        public String ExistingPlacement;

        private GameComponent CreatePreviewBody()
        {
            Blackboard blackboard = new Blackboard();
            if (SelectedResources != null && SelectedResources.Count > 0)
            {
                blackboard.SetData<List<ResourceAmount>>("Resources", SelectedResources);
            }
            blackboard.SetData<string>("CraftType", CraftType.Name);

            var previewBody = EntityFactory.CreateEntity<GameComponent>(
                CraftType.EntityName,
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

                            Vector3 pos = World.UserInterface.VoxSelector.VoxelUnderMouse.WorldPosition + new Vector3(0.5f, 0.0f, 0.5f) + CraftType.SpawnOffset;
                            Vector3 startPos = pos + new Vector3(0.0f, -0.1f, 0.0f);

                            var newDesignation = new CraftDesignation()
                            {
                                ItemType = CraftType,
                                Location = World.UserInterface.VoxSelector.VoxelUnderMouse,
                                Orientation = Orientation,
                                OverrideOrientation = OverrideOrientation,
                                Valid = true,
                                Entity = PreviewBody,
                                SelectedResources = SelectedResources,
                                WorkPile = new WorkPile(World.ComponentManager, startPos)
                            };

                                newDesignation.ExistingResource = ExistingPlacement;

                            World.ComponentManager.RootComponent.AddChild(newDesignation.WorkPile);
                            newDesignation.WorkPile.AnimationQueue.Add(new EaseMotion(1.1f, Matrix.CreateTranslation(startPos), pos));
                            World.ParticleManager.Trigger("puff", pos, Color.White, 10);

                            World.TaskManager.AddTask(new CraftItemTask(newDesignation));


                            if (!HandlePlaceExistingUpdate())
                                World.UserInterface.ShowToolPopup("Unable to place any more.");

                            PreviewBody = CreatePreviewBody();
                        }

                        break;
                    }
                case (InputManager.MouseButton.Right):
                    {
                        var designation = World.PersistentData.Designations.EnumerateEntityDesignations(DesignationType.Craft).Select(d => d.Tag as CraftDesignation).FirstOrDefault(d => d.Location == World.UserInterface.VoxSelector.VoxelUnderMouse);
                        if (designation != null && World.PersistentData.Designations.GetEntityDesignation(designation.Entity, DesignationType.Craft).HasValue(out var realDesignation))
                            World.TaskManager.CancelTask(realDesignation.Task);
                        break;
                    }
            }
        }

        private bool HandlePlaceExistingUpdate()
        {
            var resources = World.ListResources().Where(r => Library.GetResourceType(r.Value.Type).CraftInfo.CraftItemType == CraftType.Name).ToList();

            var toPlace = World.PersistentData.Designations.EnumerateEntityDesignations().Where(designation => designation.Type == DesignationType.Craft &&
                ((CraftDesignation)designation.Tag).ItemType.Name == CraftType.Name).ToList();

            if (resources.Sum(r => r.Value.Count) <= toPlace.Count)
            {
                ExistingPlacement = null;
                SelectedResources = new List<ResourceAmount>();
                return false;
            }

            String resourceType = null;
            int i = 0;
            int j = 0;
            while (i <= toPlace.Count && j < resources.Count)
            {
                i += resources[j].Value.Count;
                resourceType = resources[j].Key;
                j++;
            }
            ExistingPlacement = resourceType;
            SelectedResources = new List<ResourceAmount>();
            SelectedResources.AddRange(Library.GetResourceType(ExistingPlacement).CraftInfo.Resources);
            return true;
        }

        public override void OnBegin()
        {
            World.UserInterface.VoxSelector.DrawBox = false;
            World.UserInterface.VoxSelector.DrawVoxel = false;

            if (CraftType == null)
                throw new InvalidOperationException();

                if (!HandlePlaceExistingUpdate())
                    World.UserInterface.ShowToolPopup("Unable to place any more.");

            PreviewBody = CreatePreviewBody();
            Orientation = 0.0f;
            OverrideOrientation = false;
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

            PreviewBody.LocalPosition = World.UserInterface.VoxSelector.VoxelUnderMouse.WorldPosition + new Vector3(0.5f, 0.0f, 0.5f) + CraftType.SpawnOffset;
            PreviewBody.UpdateTransform();
            PreviewBody.PropogateTransforms();

            foreach (var tinter in PreviewBody.EnumerateAll().OfType<Tinter>())
                tinter.Stipple = true;

            if (OverrideOrientation)
                PreviewBody.Orient(Orientation);
            else
                PreviewBody.OrientToWalls();

            var valid = ObjectHelper.IsValidPlacement(World.UserInterface.VoxSelector.VoxelUnderMouse, CraftType, World, PreviewBody, "build", "built");
            PreviewBody.SetVertexColorRecursive(valid ? GameSettings.Default.Colors.GetColor("Positive", Color.Green) : GameSettings.Default.Colors.GetColor("Negative", Color.Red));

            if (valid && CraftType.AllowRotation)
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
