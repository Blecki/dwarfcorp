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
    public class BuildZoneTool : PlayerTool
    {
        [ToolFactory("BuildZone")]
        private static PlayerTool _factory(WorldManager World)
        {
            return new BuildZoneTool(World);
        }

        public ZoneType CurrentZoneType = null;

        public BuildZoneTool(WorldManager World)
        {
            this.World = World;
        }

        private DestroyZoneTool DestroyZoneTool; // I should probably be fired for this.

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            if (button == InputManager.MouseButton.Left)
            {
                foreach (BuildZoneOrder order in World.PersistentData.BuildDesignations)
                    order.SetTint(Color.White);

                foreach (var room in World.EnumerateZones())
                    room.SetTint(Color.White);

                if (CurrentZoneType == null)
                    return;

                if (button == InputManager.MouseButton.Left)
                    if (CurrentZoneType.CanBuildHere(voxels, World))
                    {
                        if (Library.CreateZone(CurrentZoneType.Name, World).HasValue(out var toBuild))
                        {
                            var order = new BuildZoneOrder(toBuild, World);
                            World.PersistentData.BuildDesignations.Add(order);
                            World.PersistentData.Zones.Add(toBuild);

                            foreach (var v in voxels.Where(v => v.IsValid && !v.IsEmpty))
                                order.VoxelOrders.Add(new BuildVoxelOrder(order, order.ToBuild, v));

                            order.WorkObjects.AddRange(Fence.CreateFences(World.ComponentManager,
                                ContentPaths.Entities.DwarfObjects.constructiontape,
                                order.VoxelOrders.Select(o => o.Voxel),
                                true));
                            foreach (var obj in order.WorkObjects)
                                obj.Manager.RootComponent.AddChild(obj);

                            World.TaskManager.AddTask(new BuildZoneTask(order));
                        }
                    }
            }
            else
                DestroyZoneTool.OnVoxelsSelected(voxels, button);
        }

        public override void OnBegin()
        {
            if (DestroyZoneTool == null)
                DestroyZoneTool = new DestroyZoneTool(World);
        }

        public override void OnEnd()
        {
            World.UserInterface.VoxSelector.Clear();
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {
            
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            MouseState mouse = Mouse.GetState();
            if (mouse.RightButton == ButtonState.Pressed)
                DestroyZoneTool.Update(game, time);
            else
            {
                if (World.UserInterface.IsCameraRotationModeActive())
                {
                    World.UserInterface.VoxSelector.Enabled = false;
                    World.UserInterface.SetMouse(null);
                    World.UserInterface.BodySelector.Enabled = false;
                    return;
                }

                World.UserInterface.VoxSelector.Enabled = true;
                World.UserInterface.BodySelector.Enabled = false;
                World.UserInterface.VoxSelector.DrawBox = true;
                World.UserInterface.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;

                if (World.UserInterface.IsMouseOverGui)
                    World.UserInterface.SetMouse(World.UserInterface.MousePointer);
                else
                    World.UserInterface.SetMouse(new Gui.MousePointer("mouse", 1, 4));
            }
        }


        public override void Render2D(DwarfGame game, DwarfTime time)
        {

        }

        // Todo: Why is the graphics device passed in when we have a perfectly good global we're using instead?
        public override void Render3D(DwarfGame game, DwarfTime time)
        {
            MouseState mouse = Mouse.GetState();
            if (mouse.RightButton == ButtonState.Pressed)
                DestroyZoneTool.Render3D(game, time);
        }

        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {
            
        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {
            MouseState mouse = Mouse.GetState();
            if (mouse.RightButton == ButtonState.Pressed)
                DestroyZoneTool.OnVoxelsDragged(voxels, button);
            else
            {
                World.UserInterface.VoxSelector.SelectionColor = Color.White;

                foreach (var order in World.PersistentData.BuildDesignations)
                    order.SetTint(Color.White);

                foreach (var room in World.EnumerateZones())
                    room.SetTint(Color.White);

                if (CurrentZoneType == null)
                    return;

                if (button == InputManager.MouseButton.Left)
                {
                    World.Tutorial("build " + CurrentZoneType.Name);

                    if (CurrentZoneType.CanBuildHere(voxels, World))
                        World.UserInterface.ShowTooltip("Release to build here.");
                    else
                        World.UserInterface.VoxSelector.SelectionColor = GameSettings.Default.Colors.GetColor("Negative", Color.Red);
                }
            }
        }
    }
}
