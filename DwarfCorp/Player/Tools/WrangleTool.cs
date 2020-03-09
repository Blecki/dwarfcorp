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

        public static bool CanCatch(WorldManager World, GameComponent Animal, bool Print)
        {
            if (Animal.GetRoot().GetComponent<Creature>().HasValue(out var creature))
            {
                if (!Animal.GetRoot().Tags.Contains("DomesticAnimal"))
                    return false;

                var pens = World.EnumerateZones().Where(room => room is AnimalPen).Cast<AnimalPen>().Where(pen => pen.IsBuilt && pen.CanHold(creature.Stats.SpeciesName));

                if (pens.Any())
                {
                    if (Print)
                        World.UserInterface.ShowTooltip("Will wrangle this " + creature.Stats.SpeciesName);
                    return true;
                }
                else
                {
                    if (Print)
                        World.UserInterface.ShowTooltip("Can't wrangle this " + creature.Stats.SpeciesName + " : need more animal pens.");
                }
            }

            return false;
        }

        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {
            var tasks = new List<Task>();

            foreach (GameComponent animal in bodies.Where(c => c.Tags.Contains("DomesticAnimal")))
            {
                Drawer3D.DrawBox(animal.BoundingBox, Color.Tomato, 0.1f, false);

                switch (button)
                {
                    case InputManager.MouseButton.Left:
                        if (CanCatch(World, animal, true) && animal.GetRoot().GetComponent<Creature>().HasValue(out var creature))
                            tasks.Add(new WrangleAnimalTask(creature) { Priority = TaskPriority.Medium });
                        break;
                    case InputManager.MouseButton.Right:
                        if (World.PersistentData.Designations.GetEntityDesignation(animal, DesignationType.Wrangle).HasValue(out var existingOrder))
                            World.TaskManager.CancelTask(existingOrder.Task);
                        break;
                }
            }

            if (tasks.Count > 0)
            {
                World.TaskManager.AddTasks(tasks);
                OnConfirm(World.PersistentData.SelectedMinions);
            }
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {

        }


        public override void OnBegin(Object Arguments)
        {
            World.Tutorial("wrangle");
            World.UserInterface.ShowToolPopup("Left click to tell dwarves to catch animals.\nRight click to cancel catching.\nRequires animal pen.");
        }

        public override void OnEnd()
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

            World.UserInterface.BodySelector.AllowRightClickSelection = true;
            World.UserInterface.VoxSelector.Enabled = false;
            World.UserInterface.BodySelector.Enabled = true;

            if (World.UserInterface.IsMouseOverGui)
                World.UserInterface.SetMouse(World.UserInterface.MousePointer);
            else
                World.UserInterface.SetMouse(new Gui.MousePointer("mouse", 1, 7));
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
            foreach (var animal in World.UserInterface.BodySelector.CurrentBodies)
                if (animal.Tags.Contains("DomesticAnimal"))
                    Drawer3D.DrawBox(animal.BoundingBox, Color.LightGreen, 0.1f, false);
        }

        public override void Render2D(DwarfGame game, DwarfTime time)
        {
        }


    }
}
