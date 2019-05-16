using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class ToolFactoryAttribute : Attribute
    {
        public String Name;

        public ToolFactoryAttribute(String Name)
        {
            this.Name = Name;
        }
    }

    /// <summary>
    /// The player's tools are a state machine. A build tool is a particular player
    /// state. Contains callbacks to when voxels are selected.
    /// </summary>
    public abstract class PlayerTool
    {
        protected WorldManager World;

        public abstract void OnVoxelsDragged(List<VoxelHandle> voxels, InputManager.MouseButton button);
        public abstract void OnVoxelsSelected(List<VoxelHandle> voxels, InputManager.MouseButton button);
        public abstract void OnBodiesSelected(List<GameComponent> bodies, InputManager.MouseButton button);
        public abstract void OnMouseOver(IEnumerable<GameComponent> bodies);
        public abstract void Update(DwarfGame game, DwarfTime time);
        public abstract void Render2D(DwarfGame game, DwarfTime time);
        public abstract void Render3D(DwarfGame game, DwarfTime time);
        public abstract void OnBegin();
        public abstract void OnEnd();

        public virtual void OnConfirm(List<CreatureAI> minions)
        {
            if (minions.Count > 0)
            {
                Vector3 avgPostiion = Vector3.Zero;
                foreach (CreatureAI creature in minions)
                {
                    avgPostiion += creature.Position;
                }
                avgPostiion /= minions.Count;
                minions.First().Creature.NoiseMaker.MakeNoise("Ok", avgPostiion);
            }
        }

        public virtual void DefaultOnMouseOver(IEnumerable<GameComponent> bodies)
        {
            StringBuilder sb = new StringBuilder();

            List<GameComponent> bodyList = bodies.ToList();
            for (int i = 0; i < bodyList.Count; i++)
            {
                sb.Append(bodyList[i].GetMouseOverText());
                if (i < bodyList.Count - 1)
                {
                    sb.Append("\n");
                }
            }
            World.ShowTooltip(sb.ToString());
        }

        public virtual void Destroy()
        {

        }
    }
}
