using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace DwarfCorp
{
    public class AttackTool : PlayerTool
    {
        [ToolFactory("Attack")]
        private static PlayerTool _factory(WorldManager World)
        {
            return new AttackTool(World);
        }

        public AttackTool(WorldManager World)
        {
            this.World = World;
        }

        public override void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button)
        {

        }

        public override void OnBegin()
        {
            
        }

        public override void OnEnd()
        {
            
        }

        public bool CanAttack(GameComponent other)
        {
            var creature = other.EnumerateAll().OfType<Creature>().FirstOrDefault();
            if (creature == null)
            {
                return false;
            }

            if (World.Diplomacy.GetPolitics(creature.Faction, World.PlayerFaction).GetCurrentRelationship() ==
                Relationship.Loving)
            {
                return false;
            }
            return true;
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {
            bool shown = false;
            foreach (GameComponent other in bodies)
            {
                var creature = other.EnumerateAll().OfType<Creature>().FirstOrDefault();
                if (creature == null)
                {
                    continue;
                }

                if (World.Diplomacy.GetPolitics(creature.Faction, World.PlayerFaction).GetCurrentRelationship() ==
                    Relationship.Loving)
                {
                    World.ShowTooltip("We refuse to attack allies.");
                    shown = true;
                    continue;
                }
                World.ShowTooltip("Click to attack this " + creature.Stats.CurrentClass.Name);
                shown = true;
            }
            if (!shown)
                DefaultOnMouseOver(bodies);
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (World.Master.IsCameraRotationModeActive())
            {
                World.Master.VoxSelector.Enabled = false;
                World.Master.BodySelector.Enabled = false;
                World.SetMouse(null);
                return;
            }

            World.Master.VoxSelector.Enabled = false;
            World.Master.BodySelector.Enabled = true;
            World.Master.BodySelector.AllowRightClickSelection = true;


            if (World.IsMouseOverGui)
                World.SetMouse(World.MousePointer);
            else
                World.SetMouse(new Gui.MousePointer("mouse", 1, 2));
        }

        public override void Render3D(DwarfGame game, DwarfTime time)
        {
            
        }

        public override void Render2D(DwarfGame game, DwarfTime time)
        {

        }

        public override void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button)
        {

            foreach (GameComponent other in bodies)
            {
                var creature = other.EnumerateAll().OfType<Creature>().FirstOrDefault();
                if (creature == null)
                {
                    continue;
                }

                if (World.Diplomacy.GetPolitics(creature.Faction, World.PlayerFaction).GetCurrentRelationship() == Relationship.Loving)
                {
                    World.PlayerFaction.World.ShowToolPopup("We refuse to attack allies.");
                    continue;
                }

                Drawer3D.DrawBox(other.BoundingBox, GameSettings.Default.Colors.GetColor("Hunt", Color.Red), 0.1f, false);

                if (button == InputManager.MouseButton.Left)
                {
                    var task = new KillEntityTask(other, KillEntityTask.KillType.Attack);
                    World.Master.TaskManager.AddTask(task);
                    World.PlayerFaction.World.ShowToolPopup("Will attack this " + creature.Stats.CurrentClass.Name);
                    OnConfirm(World.PlayerFaction.SelectedMinions);
                }
                else if (button == InputManager.MouseButton.Right)
                {
                    var designation = World.PlayerFaction.Designations.GetEntityDesignation(other, DesignationType.Attack);
                    if (designation != null)
                    {
                        World.Master.TaskManager.CancelTask(designation.Task);
                        World.PlayerFaction.World.ShowToolPopup("Attack cancelled for " + creature.Stats.CurrentClass.Name);
                    }
                }
            }
        }
    }
}
