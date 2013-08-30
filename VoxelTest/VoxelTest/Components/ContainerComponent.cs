using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class ContainerComponent : GameComponent
    {
        public Container Container { get; set; }
        public static List<ContainerComponent> Containers = null;


        public ContainerComponent(ComponentManager manager, string name, LocatableComponent parent, Container container) :
            base(manager, name, parent)
        {
            if (Containers == null)
            {
                Containers = new List<ContainerComponent>();
            }


            Container = container;
            Containers.Add(this);
            Container.UserData = parent;
        }

        public string GetContents()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();

            foreach (LocatableComponent r in Container.Resources)
            {
                if (!counts.ContainsKey(r.Tags[0]))
                {
                    counts[r.Tags[0]] = 0;
                }

                counts[r.Tags[0]]++;
            }

            string toReturn = "";
            foreach (KeyValuePair<string, int> count in counts)
            {
                toReturn += count.Key + " : " + count.Value + "\n";
            }

            return toReturn;
        }

        public override void Render(GameTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Effect effect, bool renderingForWater)
        {
            Drawer2D.DrawText(GetContents(), Container.UserData.GlobalTransform.Translation, Color.White, Color.Black);
            base.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderingForWater);
        }

        public override void Die()
        {
            Containers.Remove(this);
            base.Die();
        }

    }
}
