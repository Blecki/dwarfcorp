using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorpCore;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{
    /// <summary>
    /// The instance manager maintains a set of named instance arrays, and initializes
    /// some starting models.
    /// </summary>
    public class InstanceManager
    {
        public Dictionary<string, FixedInstanceArray> Instances { get; set; }

        public InstanceManager()
        {
            Instances = new Dictionary<string, FixedInstanceArray>();
        }

        public void Clear()
        {
            foreach(var fixedInstanceArray in Instances)
            {
                fixedInstanceArray.Value.Clear();
            }
        }

        private void CreateBillboard(string name, ContentManager content, int count)
        {
            FixedInstanceArray arr = new FixedInstanceArray(name, PrimitiveLibrary.BatchBillboardPrimitives[name].VertexBuffer, PrimitiveLibrary.BatchBillboardPrimitives[name].Texture, count, BlendState.AlphaBlend)
            {
                ShouldRebuild = true
            };
            AddInstances(name, arr);
        }

        public void CreateStatics(ContentManager content)
        {
            FixedInstanceArray pinetree = new FixedInstanceArray("pine", PrimitiveLibrary.BatchBillboardPrimitives["tree"].VertexBuffer, TextureManager.GetTexture(ContentPaths.Entities.Plants.pine), (int)(50 * GameSettings.Default.NumMotes), BlendState.AlphaBlend)
            {
                ShouldRebuild = true
            };
            AddInstances("pine", pinetree);

            FixedInstanceArray palmTree = new FixedInstanceArray("palm", PrimitiveLibrary.BatchBillboardPrimitives["tree"].VertexBuffer, TextureManager.GetTexture(ContentPaths.Entities.Plants.palm), (int)(50 * GameSettings.Default.NumMotes), BlendState.AlphaBlend)
            {
                ShouldRebuild = true
            };
            AddInstances("palm", palmTree);

            FixedInstanceArray snowPine = new FixedInstanceArray("snowpine", PrimitiveLibrary.BatchBillboardPrimitives["tree"].VertexBuffer, TextureManager.GetTexture(ContentPaths.Entities.Plants.snowpine), (int)(50 * GameSettings.Default.NumMotes), BlendState.AlphaBlend)
            {
                ShouldRebuild = true
            };
            AddInstances("snowpine", snowPine);


            CreateBillboard("berrybush", content, (int)(300 * GameSettings.Default.NumMotes));
            CreateBillboard("grass", content, (int) (300 * GameSettings.Default.NumMotes));
            CreateBillboard("frostgrass", content, (int) (300 * GameSettings.Default.NumMotes));
            CreateBillboard("flower", content, (int) (300 * GameSettings.Default.NumMotes));
            CreateBillboard("deadbush", content, (int) (300 * GameSettings.Default.NumMotes));
            CreateBillboard("vine", content, (int) (300 * GameSettings.Default.NumMotes));
            CreateBillboard("gnarled", content, (int) (300 * GameSettings.Default.NumMotes));
            CreateBillboard("mushroom", content, (int)(300 * GameSettings.Default.NumMotes));
        }

        public FixedInstanceArray GetInstances(string name)
        {
            if(Instances.ContainsKey(name))
            {
                return Instances[name];
            }
            else
            {
                return null;
            }
        }

        public void AddInstances(string name, FixedInstanceArray instances)
        {
            Instances[name] = instances;
        }

        public void RemoveInstance(string name, InstanceData instance)
        {
            if(!Instances.ContainsKey(name))
            {
                return;
            }
            else
            {
                Instances[name].Remove(instance);
            }
        }

        public void RemoveInstances(string name, List<InstanceData> instances)
        {
            if(!Instances.ContainsKey(name))
            {
                return;
            }
            else
            {
                FixedInstanceArray data = Instances[name];

                for(int i = 0; i < instances.Count; i++)
                {
                    data.Remove(instances[i]);
                }
            }
        }

        public void AddInstance(string name, InstanceData instance)
        {
            if(!Instances.ContainsKey(name))
            {
                return;
            }
            else if(instance != null)
            {
                Instances[name].Add(instance);
            }
            else
            {
                throw new NullReferenceException();
            }
        }

        public InstanceData AddInstance(string name, Matrix transform, Color color)
        {
            if(!Instances.ContainsKey(name))
            {
                return null;
            }
            else
            {
                InstanceData toReturn = new InstanceData(transform, color, true);
                Instances[name].Add(toReturn);
                return toReturn;
            }
        }

        public void Update(GameTime time, Camera cam, GraphicsDevice graphics)
        {
            foreach(FixedInstanceArray list in Instances.Values)
            {
                list.Update(time, cam, graphics);
            }
        }

        public void Render(GraphicsDevice device, Effect effect, Camera camera, bool resetVertices)
        {
            foreach(FixedInstanceArray list in Instances.Values)
            {
                list.Render(device, effect, camera, resetVertices);
            }
        }
    }

}