using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    ///     The instance manager maintains a set of named instance arrays, and initializes
    ///     some starting models.
    /// </summary>
    public class InstanceManager
    {
        public InstanceManager()
        {
            Instances = new Dictionary<string, FixedInstanceArray>();
        }

        public Dictionary<string, FixedInstanceArray> Instances { get; set; }

        public void Clear()
        {
            foreach (var fixedInstanceArray in Instances)
            {
                fixedInstanceArray.Value.Clear();
            }
        }

        private void CreateBillboard(string name, ContentManager content, int count)
        {
            var arr = new FixedInstanceArray(name, PrimitiveLibrary.BatchBillboardPrimitives[name],
                PrimitiveLibrary.BatchBillboardPrimitives[name].Texture, count, BlendState.NonPremultiplied)
            {
                ShouldRebuild = true
            };
            AddInstances(name, arr);
        }

        public void CreateStatics(ContentManager content)
        {
            var pinetree = new FixedInstanceArray("pine", PrimitiveLibrary.BatchBillboardPrimitives["pine"],
                TextureManager.GetTexture(ContentPaths.Entities.Plants.pine), 50*GameSettings.Default.NumMotes,
                BlendState.NonPremultiplied)
            {
                ShouldRebuild = true
            };
            AddInstances("pine", pinetree);

            var palmTree = new FixedInstanceArray("palm", PrimitiveLibrary.BatchBillboardPrimitives["palm"],
                TextureManager.GetTexture(ContentPaths.Entities.Plants.palm), 50*GameSettings.Default.NumMotes,
                BlendState.NonPremultiplied)
            {
                ShouldRebuild = true
            };
            AddInstances("palm", palmTree);

            var snowPine = new FixedInstanceArray("snowpine", PrimitiveLibrary.BatchBillboardPrimitives["snowpine"],
                TextureManager.GetTexture(ContentPaths.Entities.Plants.snowpine), 50*GameSettings.Default.NumMotes,
                BlendState.NonPremultiplied)
            {
                ShouldRebuild = true
            };
            AddInstances("snowpine", snowPine);


            CreateBillboard("berrybush", content, GameSettings.Default.NumMotes);
            CreateBillboard("cactus", content, GameSettings.Default.NumMotes);
            CreateBillboard("grass", content, GameSettings.Default.NumMotes);
            CreateBillboard("frostgrass", content, GameSettings.Default.NumMotes);
            CreateBillboard("flower", content, GameSettings.Default.NumMotes);
            CreateBillboard("deadbush", content, GameSettings.Default.NumMotes);
            CreateBillboard("vine", content, GameSettings.Default.NumMotes);
            CreateBillboard("gnarled", content, GameSettings.Default.NumMotes);
            CreateBillboard("mushroom", content, GameSettings.Default.NumMotes);
        }

        public FixedInstanceArray GetInstances(string name)
        {
            if (Instances.ContainsKey(name))
            {
                return Instances[name];
            }
            return null;
        }

        public void AddInstances(string name, FixedInstanceArray instances)
        {
            Instances[name] = instances;
        }

        public void RemoveInstance(string name, InstanceData instance)
        {
            if (!Instances.ContainsKey(name))
            {
            }
            Instances[name].Remove(instance);
        }

        public void RemoveInstances(string name, List<InstanceData> instances)
        {
            if (!Instances.ContainsKey(name) || instances == null)
            {
            }
            FixedInstanceArray data = Instances[name];

            for (int i = 0; i < instances.Count; i++)
            {
                data.Remove(instances[i]);
            }
        }

        public void AddInstance(string name, InstanceData instance)
        {
            if (!Instances.ContainsKey(name))
            {
            }
            if (instance != null)
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
            if (!Instances.ContainsKey(name))
            {
                return null;
            }
            var toReturn = new InstanceData(transform, color, true);
            Instances[name].Add(toReturn);
            return toReturn;
        }

        public void Update(DwarfTime time, Camera cam, GraphicsDevice graphics)
        {
            foreach (FixedInstanceArray list in Instances.Values)
            {
                list.Update(time, cam, graphics);
            }
        }

        public void Render(GraphicsDevice device, Effect effect, Camera camera, bool resetVertices)
        {
            foreach (FixedInstanceArray list in Instances.Values)
            {
                list.Render(device, effect, camera, resetVertices);
            }
            effect.CurrentTechnique = effect.Techniques["Textured"];
            effect.Parameters["xWorld"].SetValue(Matrix.Identity);
        }
    }
}