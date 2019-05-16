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
    public class BuildWallTool : PlayerTool
    {
        [ToolFactory("BuildWall")]
        private static PlayerTool _factory(WorldManager World)
        {
            return new BuildWallTool(World);
        }

        public BuildWallTool(WorldManager World)
        {
            this.World = World;
        }

        public Shader Effect;
        public byte CurrentVoxelType { get; set; }
        private List<VoxelHandle> Selected { get; set; }
        public bool BuildFloor = false;

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            if (CurrentVoxelType == 0)
                return;

            if (Selected == null)
                Selected = new List<VoxelHandle>();

            Selected.Clear();

            switch (button)
            {
                case (InputManager.MouseButton.Left):
                    {

                        List<Task> assignments = new List<Task>();
                        var validRefs = voxels.Where(r => !World.PlayerFaction.Designations.IsVoxelDesignation(r, DesignationType.Put)
                            && World.Master.VoxSelector.SelectionType == VoxelSelectionType.SelectEmpty ? r.IsEmpty : !r.IsEmpty).ToList();

                        foreach (var r in voxels)
                        {
                            // Todo: Mode should be a property of the tool, not grabbed out of the vox selector.
                            if (World.Master.VoxSelector.SelectionType == VoxelSelectionType.SelectEmpty && !r.IsEmpty) continue;
                            if (World.Master.VoxSelector.SelectionType == VoxelSelectionType.SelectFilled && r.IsEmpty) continue;

                            var existingDesignation = World.PlayerFaction.Designations.GetVoxelDesignation(r, DesignationType.Put);
                            if (existingDesignation != null)
                                World.Master.TaskManager.CancelTask(existingDesignation.Task);

                            var above = VoxelHelpers.GetVoxelAbove(r);

                            if (above.IsValid && above.LiquidType != LiquidType.None)
                            {
                                continue;
                            }

                            assignments.Add(new BuildVoxelTask(r, Library.GetVoxelType(CurrentVoxelType).Name));
                        }

                        //TaskManager.AssignTasks(assignments, Faction.FilterMinionsWithCapability(Player.World.Master.SelectedMinions, GameMaster.ToolMode.BuildZone));
                        World.Master.TaskManager.AddTasks(assignments);
                        break;
                    }
                case (InputManager.MouseButton.Right):
                    {
                        foreach (var r in voxels)
                        {
                            var designation = World.PlayerFaction.Designations.GetVoxelDesignation(r, DesignationType.Put);
                            if (designation != null)
                                World.Master.TaskManager.CancelTask(designation.Task);
                        }
                        break;
                    }
            }
        }

        public override void OnBegin()
        {

        }

        public override void OnEnd()
        {
            if (Selected != null)
                Selected.Clear();
            CurrentVoxelType = 0;
            World.Master.VoxSelector.Clear();
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {
            
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (World.Master.IsCameraRotationModeActive())
            {
                World.Master.VoxSelector.Enabled = false;
                World.SetMouse(null);
                return;
            }

            World.Master.VoxSelector.Enabled = true;
            World.Master.BodySelector.Enabled = false;

            if (World.IsMouseOverGui)
                World.SetMouse(World.MousePointer);
            else
                World.SetMouse(new Gui.MousePointer("mouse", 1, 4));

            MouseState mouse = Mouse.GetState();
            if (mouse.RightButton == ButtonState.Pressed)
                World.Master.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
            else
                World.Master.VoxSelector.SelectionType = BuildFloor ? VoxelSelectionType.SelectFilled : VoxelSelectionType.SelectEmpty;

        }

        private void DrawVoxels(float alpha, IEnumerable<VoxelHandle> selected)
        {
            Effect.VertexColorTint = new Color(0.5f, 1.0f, 0.5f, alpha);
            Vector3 offset = World.Master.VoxSelector.SelectionType == VoxelSelectionType.SelectEmpty ? Vector3.Zero : Vector3.Up * 0.15f;

            foreach (var voxel in selected)
            {
                Effect.World = Matrix.CreateTranslation(voxel.WorldPosition + offset);
                foreach (EffectPass pass in Effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Library.GetVoxelPrimitive(CurrentVoxelType).Render(GameState.Game.GraphicsDevice);
                }
            }
        }

        private void DrawVoxels(DwarfTime time, IEnumerable<VoxelHandle> selected)
        {
            GameState.Game.GraphicsDevice.DepthStencilState = DepthStencilState.None;
            Effect = World.Renderer.DefaultShader;

            float t = (float)time.TotalGameTime.TotalSeconds;
            float st = (float)Math.Sin(t * 4) * 0.5f + 0.5f;
            float alpha = 0.25f * st + 0.6f;

            Effect.MainTexture = AssetManager.GetContentTexture(ContentPaths.Terrain.terrain_tiles);
            Effect.LightRamp = Color.White;
            Effect.SetTexturedTechnique();
            DrawVoxels(MathFunctions.Clamp(alpha * 0.5f, 0.25f, 1.0f), selected);
            GameState.Game.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            DrawVoxels(MathFunctions.Clamp(alpha, 0.25f, 1.0f), selected);
            Effect.LightRamp = Color.White;
            Effect.VertexColorTint = Color.White;
            Effect.World = Matrix.Identity;
        }

        public override void Render2D(DwarfGame game, DwarfTime time)
        {

        }


        public override void Render3D(DwarfGame game, DwarfTime time)
        {
            var mouse = Mouse.GetState();

            if (Selected == null)
            {
                Selected = new List<VoxelHandle>();
            }

            if (CurrentVoxelType == 0)
            {
                Selected.Clear();
            }

            if (mouse.LeftButton == ButtonState.Pressed)
            {
                DrawVoxels(time, Selected);
            }
            else if (mouse.RightButton != ButtonState.Pressed)
            {
                var underMouse = World.Master.VoxSelector.VoxelUnderMouse;
                if (underMouse.IsValid)
                {
                    DrawVoxels(time, new List<VoxelHandle>() { World.Master.VoxSelector.VoxelUnderMouse });
                }
            }
        }

        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {
            
        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            if (CurrentVoxelType == 0)
                return;

            if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                World.ShowTooltip("Release to build.");
            else
                World.ShowTooltip("Release to cancel.");

            if (Selected == null)
            {
                Selected = new List<VoxelHandle>();
            }
            Selected.Clear();

            foreach (var voxel in voxels)
            {
                Selected.Add(voxel);
            }
        }
    }
}
