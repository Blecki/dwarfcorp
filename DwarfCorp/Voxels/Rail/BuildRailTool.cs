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

namespace DwarfCorp.Rail
{
    public class BuildRailTool : PlayerTool
    {
        [ToolFactory("BuildRail")]
        private static PlayerTool _factory(WorldManager World)
        {
            return new BuildRailTool(World);
        }

        public Rail.JunctionPattern Pattern;
        private List<RailEntity> PreviewBodies = new List<RailEntity>();
        private bool RightPressed = false;
        private bool LeftPressed = false;
        public bool GodModeSwitch = false;
        public bool CanPlace = false;

        private static CraftItem RailCraftItem = new CraftItem
        {
            Description = StringLibrary.GetString("rail-description"),
            RequiredResources = new List<Quantitiy<Resource.ResourceTags>>
                        {
                            new Quantitiy<Resource.ResourceTags>(Resource.ResourceTags.Rail, 1)
                        },
            Icon = new Gui.TileReference("resources", 38),
            BaseCraftTime = 10,
            Prerequisites = new List<CraftItem.CraftPrereq>() { CraftItem.CraftPrereq.OnGround },
            CraftLocation = "",
            Name = "Rail",
            DisplayName = StringLibrary.GetString("rail"),
            ShortDisplayName = StringLibrary.GetString("rail"),
            Type = CraftItem.CraftType.Object,
            AddToOwnedPool = true,
            Moveable = false            
        };

        public BuildRailTool(WorldManager World)
        {
            this.World = World;
        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            if (button == InputManager.MouseButton.Left)
                if (CanPlace)
                {
                    RailHelper.Place(World, PreviewBodies, GodModeSwitch);
                    PreviewBodies.Clear();
                    CreatePreviewBodies(World.ComponentManager, World.Master.VoxSelector.VoxelUnderMouse);
                }
        }

        public override void OnBegin()
        {
            World.Tutorial("place rail");
            global::System.Diagnostics.Debug.Assert(Pattern != null);
            GodModeSwitch = false;
            CreatePreviewBodies(World.ComponentManager, new VoxelHandle(World.ChunkManager, new GlobalVoxelCoordinate(0, 0, 0)));
        }

        public override void OnEnd()
        {
            foreach (var body in PreviewBodies)
                body.GetRoot().Delete();
            PreviewBodies.Clear();
            Pattern = null;
            World.Master.VoxSelector.DrawVoxel = true;
            World.Master.VoxSelector.DrawBox = true;
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {

        }

        private void CreatePreviewBodies(ComponentManager ComponentManager, VoxelHandle Location)
        {
            foreach (var body in PreviewBodies)
                body.GetRoot().Delete();

            PreviewBodies.Clear();
            foreach (var piece in Pattern.Pieces)
                PreviewBodies.Add(RailHelper.CreatePreviewBody(ComponentManager, Location, piece));
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            CanPlace = false;

            if (World.Master.IsCameraRotationModeActive())
            {
                World.Master.VoxSelector.Enabled = false;
                World.UserInterface.SetMouse(null);
                World.Master.BodySelector.Enabled = false;
                return;
            }

            World.Master.VoxSelector.Enabled = true;
            World.Master.BodySelector.Enabled = false;
            World.Master.VoxSelector.DrawBox = false;
            World.Master.VoxSelector.DrawVoxel = false;
            World.Master.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;

            if (World.UserInterface.IsMouseOverGui)
                World.UserInterface.SetMouse(World.UserInterface.MousePointer);
            else
                World.UserInterface.SetMouse(new Gui.MousePointer("mouse", 1, 4));

            // Don't attempt any control if the user is trying to type into a focus item.
            if (World.UserInterface.Gui.FocusItem != null && !World.UserInterface.Gui.FocusItem.IsAnyParentTransparent() && !World.UserInterface.Gui.FocusItem.IsAnyParentHidden())
            {
                return;
            }

            KeyboardState state = Keyboard.GetState();
            bool leftKey = state.IsKeyDown(ControlSettings.Mappings.RotateObjectLeft);
            bool rightKey = state.IsKeyDown(ControlSettings.Mappings.RotateObjectRight);
            if (LeftPressed && !leftKey)
                Pattern = Pattern.Rotate(Rail.PieceOrientation.East);
            if (RightPressed && !rightKey)
                Pattern = Pattern.Rotate(Rail.PieceOrientation.West);
            LeftPressed = leftKey;
            RightPressed = rightKey;

            var tint = Color.White;

            for (var i = 0; i < PreviewBodies.Count && i < Pattern.Pieces.Count; ++i)
                PreviewBodies[i].UpdatePiece(Pattern.Pieces[i], World.Master.VoxSelector.VoxelUnderMouse);

            if (RailHelper.CanPlace(World, PreviewBodies))
            {
                CanPlace = true;
                tint = GameSettings.Default.Colors.GetColor("Positive", Color.Green);
            }
            else
                tint = GameSettings.Default.Colors.GetColor("Negative", Color.Red);
        
            foreach (var body in PreviewBodies)
                body.SetVertexColorRecursive(tint);
        }

        public override void Render2D(DwarfGame game, DwarfTime time)
        {
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
    }
}
