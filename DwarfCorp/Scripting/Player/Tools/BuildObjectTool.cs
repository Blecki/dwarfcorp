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
    public class BuildObjectTool : PlayerTool
    {
        [ToolFactory("BuildObject")]
        private static PlayerTool _factory(GameMaster Master)
        {
            return new BuildObjectTool(Master);
        }

        public BuildObjectTool(GameMaster Master)
        {
            Player = Master;
        }

        public CraftItem CraftType { get; set; }
        public GameComponent PreviewBody { get; set; }
        public List<ResourceAmount> SelectedResources;
        private float Orientation = 0.0f;
        private bool OverrideOrientation = false;
        private bool RightPressed = false;
        private bool LeftPressed = false;

        public enum PlacementMode
        {
            BuildNew,
            PlaceExisting
        }

        public PlacementMode Mode;
        public String ExistingPlacement;

        [JsonIgnore]
        public WorldManager World { get; set; }

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
                Player.VoxSelector.VoxelUnderMouse.WorldPosition,
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
                        if (ObjectHelper.IsValidPlacement(Player.VoxSelector.VoxelUnderMouse, CraftType, Player.World, PreviewBody, "build", "built"))
                        {
                            PreviewBody.SetFlag(GameComponent.Flag.ShouldSerialize, true);

                            Vector3 pos = Player.VoxSelector.VoxelUnderMouse.WorldPosition + new Vector3(0.5f, 0.0f, 0.5f) + CraftType.SpawnOffset;
                            Vector3 startPos = pos + new Vector3(0.0f, -0.1f, 0.0f);

                            CraftDesignation newDesignation = new CraftDesignation()
                            {
                                ItemType = CraftType,
                                Location = Player.VoxSelector.VoxelUnderMouse,
                                Orientation = Orientation,
                                OverrideOrientation = OverrideOrientation,
                                Valid = true,
                                Entity = PreviewBody,
                                SelectedResources = SelectedResources,
                                WorkPile = new WorkPile(World.ComponentManager, startPos)
                            };

                            if (Mode == PlacementMode.PlaceExisting)
                            {
                                newDesignation.ExistingResource = ExistingPlacement;
                            }

                            World.ComponentManager.RootComponent.AddChild(newDesignation.WorkPile);
                            newDesignation.WorkPile.AnimationQueue.Add(new EaseMotion(1.1f, Matrix.CreateTranslation(startPos), pos));
                            World.ParticleManager.Trigger("puff", pos, Color.White, 10);

                            World.Master.TaskManager.AddTask(new CraftItemTask(newDesignation));


                            if (Mode == PlacementMode.PlaceExisting && !HandlePlaceExistingUpdate())
                            {
                                World.ShowToolPopup("Unable to place any more.");
                                Mode = PlacementMode.BuildNew;
                            }

                            PreviewBody = CreatePreviewBody();
                        }

                        break;
                    }
                case (InputManager.MouseButton.Right):
                    {
                        var designation = World.PlayerFaction.Designations.EnumerateEntityDesignations(DesignationType.Craft).Select(d => d.Tag as CraftDesignation).FirstOrDefault(d => d.Location == Player.VoxSelector.VoxelUnderMouse);
                        if (designation != null)
                        {
                            var realDesignation = World.PlayerFaction.Designations.GetEntityDesignation(designation.Entity, DesignationType.Craft);
                            if (realDesignation != null)
                                World.Master.TaskManager.CancelTask(realDesignation.Task);
                        }
                        break;
                    }
            }
        }

        private bool HandlePlaceExistingUpdate()
        {
            var resources = World.PlayerFaction.ListResources().Where(r => ResourceLibrary.GetResourceByName(r.Value.Type).CraftInfo.CraftItemType == CraftType.Name).ToList();

            var toPlace = World.PlayerFaction.Designations.EnumerateEntityDesignations().Where(designation => designation.Type == DesignationType.Craft &&
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
            SelectedResources.AddRange(ResourceLibrary.GetResourceByName(ExistingPlacement).CraftInfo.Resources);
            return true;

            /*
            var resource = World.PlayerFaction.ListResources().First(r => ResourceLibrary.GetResourceByName(r.Value.String).CraftItnfo.CraftItemType == CraftType.Name);
            if (resource.Value != null)
            {
                ExistingPlacement = resource.Key;
                SelectedResources = ResourceLibrary.GetResourceByName(ExistingPlacement).CraftItnfo.Resources;
                return true;
            }
            ExistingPlacement = null;
            SelectedResources = null;
            return false;
            */
        }

        public override void OnBegin()
        {
            Player.VoxSelector.DrawBox = false;
            Player.VoxSelector.DrawVoxel = false;

            if (CraftType == null)
                throw new InvalidOperationException();

            if (Mode == PlacementMode.PlaceExisting)
            {
                if (!HandlePlaceExistingUpdate())
                {
                    Mode = PlacementMode.BuildNew;
                    World.ShowToolPopup("Unable to place any more.");
                }
            }

            PreviewBody = CreatePreviewBody();
            Orientation = 0.0f;
            OverrideOrientation = false;
        }

        public override void OnEnd()
        {
            Player.VoxSelector.DrawBox = true;
            Player.VoxSelector.DrawVoxel = true;

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
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                Player.World.SetMouse(null);
                Player.BodySelector.Enabled = false;
                return;
            }

            Player.VoxSelector.Enabled = true;
            Player.BodySelector.Enabled = false;

            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 4));

            if (PreviewBody == null || !Player.VoxSelector.VoxelUnderMouse.IsValid)
                return;

            HandleOrientation();

            PreviewBody.LocalPosition = Player.VoxSelector.VoxelUnderMouse.WorldPosition + new Vector3(0.5f, 0.0f, 0.5f) + CraftType.SpawnOffset;
            PreviewBody.UpdateTransform();
            PreviewBody.PropogateTransforms();

            foreach (var tinter in PreviewBody.EnumerateAll().OfType<Tinter>())
                tinter.Stipple = true;

            if (OverrideOrientation)
                PreviewBody.Orient(Orientation);
            else
                PreviewBody.OrientToWalls();

            var valid = ObjectHelper.IsValidPlacement(Player.VoxSelector.VoxelUnderMouse, CraftType, Player.World, PreviewBody, "build", "built");
            PreviewBody.SetVertexColorRecursive(valid ? GameSettings.Default.Colors.GetColor("Positive", Color.Green) : GameSettings.Default.Colors.GetColor("Negative", Color.Red));

            if (valid && CraftType.AllowRotation)
                World.ShowTooltip("Click to build. Press R/T to rotate.");
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
            if (World.Gui.FocusItem == null || World.Gui.FocusItem.IsAnyParentTransparent() || World.Gui.FocusItem.IsAnyParentHidden())
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
