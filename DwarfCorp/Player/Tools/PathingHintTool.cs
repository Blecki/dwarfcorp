using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class PathingHintTool : PlayerTool
    {
        [ToolFactory("PathingHint")]
        private static PlayerTool _factory(WorldManager World)
        {
            return new PathingHintTool(World);
        }

        public PathingHintTool(WorldManager World)
        {
            this.World = World;
        }

        public override void OnBegin(Object Arguments)
        {
            World.UserInterface.VoxSelector.SelectionColor = Color.White;
            World.UserInterface.VoxSelector.DrawBox = true;
            World.UserInterface.VoxSelector.DrawVoxel = true;
            //World.Tutorial("mine");
        }

        public override void OnEnd()
        {
            World.UserInterface.VoxSelector.Clear();
        }

        public override void OnVoxelsSelected(List<VoxelHandle> refs, InputManager.MouseButton button)
        {

            if (button == InputManager.MouseButton.Left)
            {
                foreach (var v in refs)
                {
                    var x = v;
                    x.PathingHintSet = (byte)1;
                }
            }
            else
            {
                foreach (var v in refs)
                {
                    var x = v;
                    x.PathingHintSet = (byte)0;
                }
            }
        }

        public override void OnMouseOver(IEnumerable<GameComponent> bodies)
        {
            //throw new NotImplementedException();
        }

        public override void Update(DwarfGame game, DwarfTime time)
        {
            if (World.UserInterface.IsCameraRotationModeActive())
            {
                World.UserInterface.VoxSelector.Enabled = false;
                World.UserInterface.BodySelector.Enabled = false;
                World.UserInterface.SetMouse(null);
                return;
            }

            World.UserInterface.VoxSelector.Enabled = true;

            if (World.UserInterface.VoxSelector.VoxelUnderMouse.IsValid && !World.UserInterface.IsMouseOverGui)
            {
                World.UserInterface.ShowTooltip(World.UserInterface.VoxSelector.VoxelUnderMouse.IsExplored ? World.UserInterface.VoxSelector.VoxelUnderMouse.Type.Name : "???");
            }

            if (World.UserInterface.IsMouseOverGui)
                World.UserInterface.SetMouse(World.UserInterface.MousePointer);
            else
                World.UserInterface.SetMouse(new Gui.MousePointer("mouse", 1, 1));

            World.UserInterface.BodySelector.Enabled = false;
            World.UserInterface.VoxSelector.SelectionType = VoxelSelectionType.SelectFilled;
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
