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
        private static PlayerTool _factory(GameMaster Master)
        {
            return new AttackTool(Master);
        }

        public AttackTool(GameMaster Master)
        {
            Player = Master;
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

            if (Player.World.Diplomacy.GetPolitics(creature.Faction, Player.Faction).GetCurrentRelationship() ==
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

                if (Player.World.Diplomacy.GetPolitics(creature.Faction, Player.Faction).GetCurrentRelationship() ==
                    Relationship.Loving)
                {
                    Player.Faction.World.ShowTooltip("We refuse to attack allies.");
                    shown = true;
                    continue;
                }
                Player.Faction.World.ShowTooltip("Click to attack this " + creature.Stats.CurrentClass.Name);
                shown = true;
            }
            if (!shown)
                DefaultOnMouseOver(bodies);
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (Player.IsCameraRotationModeActive())
            {
                Player.VoxSelector.Enabled = false;
                Player.BodySelector.Enabled = false;
                Player.World.SetMouse(null);
                return;
            }

            Player.VoxSelector.Enabled = false;
            Player.BodySelector.Enabled = true;
            Player.BodySelector.AllowRightClickSelection = true;


            if (Player.World.IsMouseOverGui)
                Player.World.SetMouse(Player.World.MousePointer);
            else
                Player.World.SetMouse(new Gui.MousePointer("mouse", 1, 2));
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

                if (Player.World.Diplomacy.GetPolitics(creature.Faction, Player.Faction).GetCurrentRelationship() == Relationship.Loving)
                {
                    Player.Faction.World.ShowToolPopup("We refuse to attack allies.");
                    continue;
                }

                Drawer3D.DrawBox(other.BoundingBox, GameSettings.Default.Colors.GetColor("Hunt", Color.Red), 0.1f, false);

                if (button == InputManager.MouseButton.Left)
                {
                    var task = new KillEntityTask(other, KillEntityTask.KillType.Attack);
                    Player.TaskManager.AddTask(task);
                    Player.Faction.World.ShowToolPopup("Will attack this " + creature.Stats.CurrentClass.Name);
                    OnConfirm(Player.Faction.SelectedMinions);
                }
                else if (button == InputManager.MouseButton.Right)
                {
                    var designation = Player.Faction.Designations.GetEntityDesignation(other, DesignationType.Attack);
                    if (designation != null)
                    {
                        Player.TaskManager.CancelTask(designation.Task);
                        Player.Faction.World.ShowToolPopup("Attack cancelled for " + creature.Stats.CurrentClass.Name);
                    }
                }
            }
        }
    }
}
