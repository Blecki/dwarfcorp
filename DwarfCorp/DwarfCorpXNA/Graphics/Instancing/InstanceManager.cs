using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            FixedInstanceArray arr = new FixedInstanceArray(name, PrimitiveLibrary.BatchBillboardPrimitives[name], PrimitiveLibrary.BatchBillboardPrimitives[name].Texture, count, BlendState.NonPremultiplied)
            {
                ShouldRebuild = true,
                EnableWind = true
            };
            AddInstances(name, arr);
        }

        public void CreateStatics(ContentManager content)
        {
            FixedInstanceArray pinetree = new FixedInstanceArray("pine", PrimitiveLibrary.BatchBillboardPrimitives["pine"], TextureManager.GetTexture(ContentPaths.Entities.Plants.pine), (int)(50 * GameSettings.Default.NumMotes), BlendState.NonPremultiplied)
            {
                ShouldRebuild = true,
                EnableWind = true
            };
            AddInstances("pine", pinetree);

            FixedInstanceArray palmTree = new FixedInstanceArray("palm", PrimitiveLibrary.BatchBillboardPrimitives["palm"], TextureManager.GetTexture(ContentPaths.Entities.Plants.palm), (int)(50 * GameSettings.Default.NumMotes), BlendState.NonPremultiplied)
            {
                ShouldRebuild = true,
                EnableWind = true
            };
            AddInstances("palm", palmTree);

            FixedInstanceArray snowPine = new FixedInstanceArray("snowpine", PrimitiveLibrary.BatchBillboardPrimitives["snowpine"], TextureManager.GetTexture(ContentPaths.Entities.Plants.snowpine), (int)(50 * GameSettings.Default.NumMotes), BlendState.NonPremultiplied)
            {
                ShouldRebuild = true,
                EnableWind = true
            };
            AddInstances("snowpine", snowPine);


            CreateBillboard("berrybush", content, (int)( GameSettings.Default.NumMotes));
            CreateBillboard("cactus", content, (int)(GameSettings.Default.NumMotes));
            CreateBillboard("grass", content, (int) (GameSettings.Default.NumMotes));
            CreateBillboard("frostgrass", content, (int) (GameSettings.Default.NumMotes));
            CreateBillboard("flower", content, (int) (GameSettings.Default.NumMotes));
            CreateBillboard("deadbush", content, (int) (GameSettings.Default.NumMotes));
            CreateBillboard("vine", content, (int) (GameSettings.Default.NumMotes));
            CreateBillboard("gnarled", content, (int) (GameSettings.Default.NumMotes));
            CreateBillboard("mushroom", content, (int)(GameSettings.Default.NumMotes));
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
            if(!Instances.ContainsKey(name) || instances == null)
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

        public void Update(DwarfTime time, Camera cam, GraphicsDevice graphics)
        {
            foreach(FixedInstanceArray list in Instances.Values)
            {
                list.Update(time, cam, graphics);
            }
        }

        public void RenderSelectionBuffer(GraphicsDevice device, Shader effect, Camera camera, bool resetVertices)
        {
            foreach (FixedInstanceArray list in Instances.Values)
            {
                list.RenderSelectionBuffer(device, effect, camera, resetVertices);
            }
        }

        public void Render(GraphicsDevice device, Shader effect, Camera camera, bool resetVertices)
        {
            foreach(FixedInstanceArray list in Instances.Values)
            {
                list.Render(device, effect, camera, resetVertices);
            }
            effect.CurrentTechnique = effect.Techniques[Shader.Technique.Textured];
            effect.World = Matrix.Identity;
        }
    }

}
