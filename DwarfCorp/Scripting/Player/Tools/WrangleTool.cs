using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class WrangleTool : PlayerTool
    {
        [ToolFactory("Wrangle")]
        private static PlayerTool _factory(WorldManager World)
        {
            return new WrangleTool(World);
        }

        public WrangleTool(WorldManager World)
        {
            this.World = World;
        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public bool CanCatch(GameComponent animal, bool print=false)
        {
            var creature = animal.GetRoot().GetComponent<Creature>();
            if (creature == null)
                return false;

            if (!animal.GetRoot().Tags.Contains("DomesticAnimal"))
                return false;

            var pens = World.PlayerFaction.GetRooms().Where(room => room is AnimalPen).Cast<AnimalPen>().Where(pen => pen.IsBuilt &&
                            (pen.Species == "" || pen.Species == creature.Stats.CurrentClass.Name));

            if (pens.Any())
            {
                if (print)
                    World.UserInterface.ShowTooltip("Will wrangle this " + animal.GetRoot().GetComponent<Creature>().Stats.CurrentClass.Name);
                return true;
            }
            else
            {
                if (print)
                    World.UserInterface.ShowTooltip("Can't wrangle this " + animal.GetRoot().GetComponent<Creature>().Stats.CurrentClass.Name + " : need more animal pens.");
            }

            return false;
        }

        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {
            List<Task> tasks = new List<Task>();
            foreach (GameComponent animal in bodies.Where(c => c.Tags.Contains("DomesticAnimal")))
            {
                Drawer3D.DrawBox(animal.BoundingBox, Color.Tomato, 0.1f, false);
                switch (button)
                {
                    case InputManager.MouseButton.Left:
                        {
                            if (CanCatch(animal, true))
                            {
                                var task = new WrangleAnimalTask(animal.GetRoot().GetComponent<Creature>()) { Priority = Task.PriorityType.Medium };
                                tasks.Add(task);
                            }
                        }
                        break;
                    case InputManager.MouseButton.Right:
                        {
                            var existingOrder = World.PlayerFaction.Designations.GetEntityDesignation(animal, DesignationType.Wrangle);
                            if (existingOrder != null)
                                World.Master.TaskManager.CancelTask(existingOrder.Task);
                        }
                        break;
                }
            }

            if (tasks.Count > 0)
            {
                World.Master.TaskManager.AddTasks(tasks);
                OnConfirm(World.PlayerFaction.SelectedMinions);
            }
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {

        }


        public override void OnBegin()
        {

        }

        public override void OnEnd()
        {

        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (World.Master.IsCameraRotationModeActive())
            {
                World.Master.VoxSelector.Enabled = false;
                World.UserInterface.SetMouse(null);
                World.Master.BodySelector.Enabled = false;
                return;
            }

            World.Master.BodySelector.AllowRightClickSelection = true;
            World.Master.VoxSelector.Enabled = false;
            World.Master.BodySelector.Enabled = true;

            if (World.UserInterface.IsMouseOverGui)
                World.UserInterface.SetMouse(World.UserInterface.MousePointer);
            else
                World.UserInterface.SetMouse(new Gui.MousePointer("mouse", 1, 7));
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
            foreach (var animal in World.Master.BodySelector.CurrentBodies)
                if (animal.Tags.Contains("DomesticAnimal"))
                    Drawer3D.DrawBox(animal.BoundingBox, Color.LightGreen, 0.1f, false);
        }

        public override void Render2D(DwarfGame game, DwarfTime time)
        {
        }


    }
}
