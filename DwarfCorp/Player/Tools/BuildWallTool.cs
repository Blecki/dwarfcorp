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

        public class BuildWallToolArguments
        {
            public byte VoxelType;
            public bool Floor;
        }

        public BuildWallTool(WorldManager World)
        {
            this.World = World;
        }

        public Shader Effect;
        private List<VoxelHandle> Selected { get; set; }
        private BuildWallToolArguments Arguments;

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            if (Arguments == null)
                return;

            if (Selected == null)
                Selected = new List<VoxelHandle>();

            Selected.Clear();

            switch (button)
            {
                case (InputManager.MouseButton.Left):
                    {

                        List<Task> assignments = new List<Task>();
                        var validRefs = voxels.Where(r => !World.PersistentData.Designations.IsVoxelDesignation(r, DesignationType.Put)
                            && World.UserInterface.VoxSelector.SelectionType == VoxelSelectionType.SelectEmpty ? r.IsEmpty : !r.IsEmpty).ToList();

                        foreach (var r in voxels)
                        {
                            if (!Arguments.Floor && !r.IsEmpty) continue;
                            if (Arguments.Floor && r.IsEmpty) continue;

                            if (World.PersistentData.Designations.GetVoxelDesignation(r, DesignationType.Put).HasValue(out var existingDesignation))
                                World.TaskManager.CancelTask(existingDesignation.Task);

                            var above = VoxelHelpers.GetVoxelAbove(r);

                            if (above.IsValid && above.LiquidType != LiquidType.None)
                                continue;

                            if (Library.GetVoxelType(Arguments.VoxelType).HasValue(out VoxelType vType) && r.Type != vType)                            
                                assignments.Add(new BuildVoxelTask(r, vType.Name));
                        }

                        World.TaskManager.AddTasks(assignments);
                        break;
                    }
                case (InputManager.MouseButton.Right):
                    {
                        foreach (var r in voxels)
                            if (World.PersistentData.Designations.GetVoxelDesignation(r, DesignationType.Put).HasValue(out var designation))
                                World.TaskManager.CancelTask(designation.Task);

                        break;
                    }
            }
        }

        public override void OnBegin(Object Arguments)
        {
            if (Arguments is BuildWallToolArguments arguments)
                this.Arguments = arguments;
            else
                throw new InvalidOperationException();

            if (this.Arguments.Floor)
                World.UserInterface.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
            else
                World.UserInterface.VoxSelector.SelectionType = VoxelSelectionType.SelectEmpty;

            if (Library.GetVoxelType(this.Arguments.VoxelType).HasValue(out var vType))
                World.UserInterface.ShowToolPopup("Click and drag to build " + vType.Name + " wall.");
        }

        public override void OnEnd()
        {
            if (Selected != null)
                Selected.Clear();
            World.UserInterface.VoxSelector.Clear();
            Arguments = null;
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
            }
            else
            {
                World.UserInterface.VoxSelector.Enabled = true;
                World.UserInterface.BodySelector.Enabled = false;

                if (World.UserInterface.IsMouseOverGui)
                    World.UserInterface.SetMouse(World.UserInterface.MousePointer);
                else
                    World.UserInterface.SetMouse(new Gui.MousePointer("mouse", 1, 4));

                MouseState mouse = Mouse.GetState();
                if (mouse.RightButton == ButtonState.Pressed)
                    World.UserInterface.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
                else
                    World.UserInterface.VoxSelector.SelectionType = Arguments.Floor ? VoxelSelectionType.SelectFilled : VoxelSelectionType.SelectEmpty;

                if (Arguments == null || !Library.GetVoxelType(Arguments.VoxelType).HasValue(out var vType) || !World.CanBuildVoxel(vType))
                {
                    World.UserInterface.ShowToolPopup("Not enough resources.");
                    World.UserInterface.ChangeTool("SelectUnits");
                }
            }
        }

        private void DrawVoxels(float alpha, IEnumerable<VoxelHandle> selected)
        {
            Effect.VertexColorTint = new Color(0.5f, 1.0f, 0.5f, alpha);
            Vector3 offset = World.UserInterface.VoxSelector.SelectionType == VoxelSelectionType.SelectEmpty ? Vector3.Zero : Vector3.Up * 0.15f;

            if (Library.GetVoxelType(Arguments.VoxelType).HasValue(out VoxelType vType))
                if (Library.GetVoxelPrimitive(vType).HasValue(out BoxPrimitive primitive))
                {
                    foreach (var voxel in selected)
                    {
                        Effect.World = Matrix.CreateTranslation(voxel.WorldPosition + offset);
                        foreach (EffectPass pass in Effect.CurrentTechnique.Passes)
                        {
                            pass.Apply();
                            primitive.Render(GameState.Game.GraphicsDevice);
                        }
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
                Selected = new List<VoxelHandle>();

            if (Arguments == null)
                Selected.Clear();

            if (mouse.LeftButton == ButtonState.Pressed)
                DrawVoxels(time, Selected);
            else if (mouse.RightButton != ButtonState.Pressed)
            {
                var underMouse = World.UserInterface.VoxSelector.VoxelUnderMouse;
                if (underMouse.IsValid)
                    DrawVoxels(time, new List<VoxelHandle>() { World.UserInterface.VoxSelector.VoxelUnderMouse });
            }
        }

        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {
            
        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            if (Arguments == null)
                return;

            if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                World.UserInterface.ShowTooltip("Release to build.");
            else
                World.UserInterface.ShowTooltip("Release to cancel.");

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
