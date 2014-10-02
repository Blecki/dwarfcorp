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
    /// <summary>
    /// When using this tool, the player specifies that certain
    /// entities should be put into stockpiles.
    /// </summary>
    public class GatherTool : PlayerTool
    {

        public Color GatherDesignationColor { get; set; }
        public float GatherDesignationGlowRate { get; set; }

        public GatherTool()
        {

        }


        public override void OnBodiesSelected(List<Body> bodies, InputManager.MouseButton button)
        {
            List<Body> resourcesPickedByMouse = ComponentManager.FilterComponentsWithTag("Resource", bodies);
            List<Task> assignments = new List<Task>();
            foreach(Body resource in resourcesPickedByMouse.Where(resource => resource.IsActive && resource.IsVisible && resource.Parent == PlayState.ComponentManager.RootComponent))
            {
                Drawer3D.DrawBox(resource.BoundingBox, Color.LightGoldenrodYellow, 0.05f, true);

                if(button == InputManager.MouseButton.Left)
                {
                    Player.Faction.AddGatherDesignation(resource);

                    assignments.Add(new GatherItemTask(resource));
                }
                else
                {
                    if(!Player.Faction.GatherDesignations.Contains(resource))
                    {
                        continue;
                    }

                    Player.Faction.GatherDesignations.Remove(resource);
                }
            }

            TaskManager.AssignTasks(assignments, PlayState.Master.FilterMinionsWithCapability(PlayState.Master.SelectedMinions, GameMaster.ToolMode.Gather));
        }

        public override void OnVoxelsSelected(List<VoxelRef> voxels, InputManager.MouseButton button)
        {

        }

        public override void Update(DwarfGame game, GameTime time)
        {
           
            if (Player.IsCameraRotationModeActive())
            {
                return;
            }
            Player.VoxSelector.Enabled = false;
            Player.BodySelector.Enabled = true;
            PlayState.GUI.IsMouseVisible = true;

            if (PlayState.GUI.IsMouseOver())
            {
                PlayState.GUI.MouseMode = GUISkin.MousePointer.Pointer;
            }
            else
            {
                PlayState.GUI.MouseMode = GUISkin.MousePointer.Gather;
            }


        }

        public override void Render(DwarfGame game, GraphicsDevice graphics, GameTime time)
        {
            /*
            Color drawColor = GatherDesignationColor;

            float alpha = (float)Math.Abs(Math.Sin(time.TotalGameTime.TotalSeconds * GatherDesignationGlowRate));
            drawColor.R = (byte)(Math.Min(drawColor.R * alpha + 50, 255));
            drawColor.G = (byte)(Math.Min(drawColor.G * alpha + 50, 255));
            drawColor.B = (byte)(Math.Min(drawColor.B * alpha + 50, 255));

            foreach (BoundingBox box in Player.Faction.GatherDesignations.Select(d => d.GetBoundingBox()))
            {
                Drawer3D.DrawBox(box, drawColor, 0.05f * alpha + 0.05f, true);
            }
             */

        }
    }
}
