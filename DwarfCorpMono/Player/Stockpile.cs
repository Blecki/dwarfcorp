using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp
{



    public class Stockpile : Zone
    {
        public List<Resource> AllowedResources { get; set; }
        public Dictionary<Voxel, LocatableComponent> ComponentVoxels { get; set; }
        public Dictionary<Voxel, bool> IsReserved{ get; set; }
        public Dictionary<Voxel, BoxPrimitive> OriginalPrimitives { get; set; }
        private static uint maxID = 0;

        public bool IsFull()
        {
            foreach (KeyValuePair<Voxel, LocatableComponent> pair in ComponentVoxels)
            {
                if (pair.Value == null && !IsReserved[pair.Key])
                {
                    return false;
                }
                
            }

            return true;
        }

        public static uint NextID()
        {
            maxID++;
            return maxID;
        }


        public Stockpile(string id) :
             this(id, new List<Resource>(), new Dictionary<Voxel, LocatableComponent>())
        {

        }

        public Stockpile(string id, List<Resource> allowedResources, Dictionary<Voxel, LocatableComponent> componentVoxels) :
            base(id)
        {
            AllowedResources = allowedResources;
            ComponentVoxels = componentVoxels;
            OriginalPrimitives = new Dictionary<Voxel, BoxPrimitive>();
            IsReserved = new Dictionary<Voxel, bool>();

            foreach (var pair in componentVoxels)
            {
                IsReserved[pair.Key] = false;
            }
        }

        public void RemoveComponentVoxel(Voxel voxel)
        {
            if (ComponentVoxels.ContainsKey(voxel))
            {
                ComponentVoxels.Remove(voxel);
                OriginalPrimitives.Remove(voxel);
                IsReserved.Remove(voxel);
            }
        }

        public void AddComponentVoxel(Voxel voxel, GraphicsDevice graphics)
        {
            if (!ComponentVoxels.ContainsKey(voxel))
            {
                ComponentVoxels[voxel] = null;
                OriginalPrimitives[voxel] = voxel.Primitive;
                voxel.Chunk.ShouldRebuild = true;
                voxel.Primitive = GenerateStockpileModel(voxel.Primitive, graphics);
                IsReserved[voxel] = false;
            }

        }

        public LocatableComponent GetNextResourceFromList(List<string> resources)
        {
            foreach (string r in resources)
            {
                LocatableComponent component = GetNextResourceWithTag(r);

                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        public LocatableComponent GetNextResourceWithTagIgnore(string tag, List<LocatableComponent> ignores)
        {
            foreach (LocatableComponent loc in ComponentVoxels.Values)
            {
                if (loc != null && loc.Tags.Contains(tag) && !ignores.Contains(loc))
                {
                    return loc;
                }
            }

            return null;
        }

        public LocatableComponent GetNextResourceWithTag(string tag)
        {
            foreach (LocatableComponent loc in ComponentVoxels.Values)
            {
                if (loc != null && loc.Tags.Contains(tag))
                {
                    return loc;
                }
            }

            return null;
        }

        public bool RemoveResource(LocatableComponent resource)
        {
            Voxel foundResource = null;
            foreach (Voxel v in ComponentVoxels.Keys)
            {
                if (ComponentVoxels[v] == resource)
                {
                    foundResource = v;
                    break;
                }
            }

            if (foundResource != null)
            {
                //ComponentVoxels.Remove(foundResource);
                //RemoveFirstItem(resource.Tags[0]);
                ComponentVoxels[foundResource] = null;
                IsReserved[foundResource] = false;
                resource.IsStocked = false;
                return true;
            }



            return false;
        }

        public LocatableComponent GetNextResource()
        {
            foreach (LocatableComponent loc in ComponentVoxels.Values)
            {
                if (loc != null)
                {
                    return loc;
                }
            }

            return null;
        }

        public Voxel GetNextFreeVoxel(Vector3 pos)
        {
            Voxel closest = null;
            double closestDist = double.MaxValue;

            foreach (Voxel v in ComponentVoxels.Keys)
            {
                if (v != null && !IsReserved[v])
                {
                    double d = (v.Position - pos).LengthSquared();

                    if (d < closestDist)
                    {
                        closestDist = d;
                        closest = v;
                    }
                }
            }

            return closest;
        }

        public void ResetVoxelTextures()
        {
            foreach (Voxel v in OriginalPrimitives.Keys)
            {
                v.Primitive = OriginalPrimitives[v];
                v.Chunk.ShouldRebuild = true;
            }
        }

        public  static BoxPrimitive GenerateStockpileModel(BoxPrimitive other, GraphicsDevice graphics)
        {
            return BoxPrimitive.RetextureTop(other, graphics, new Point(4, 0));
        }

        public bool CanPutResource(LocatableComponent resource)
        {
            if (resource == null)
            {
                return false;
            }

            foreach (Resource r in AllowedResources)
            {
                if (resource.Tags.Contains(r.ResourceName))
                {
                    return true;
                }
            }

            return true;
        }

        public bool PutResource(LocatableComponent resource, Voxel voxel)
        {
            Item item = new Item(resource.Tags[0], this, resource);
            
            if (!CanPutResource(resource))
            {
                return false;
            }
            else
            {
                if (!ComponentVoxels.ContainsKey(voxel))
                {
                    return false;
                }
                else if (ComponentVoxels[voxel] != null)
                {
                    return false;
                }
                else
                {
                    Items.Add(item);
                    ComponentVoxels[voxel] = resource;
                    return true;
                }
            }
        }

        public bool Intersects(Voxel v)
        {
            BoundingBox larger = new BoundingBox(v.GetBoundingBox().Min - new Vector3(0.1f, 0.1f, 0.1f), v.GetBoundingBox().Max + new Vector3(0.1f, 0.1f, 0.1f));

            foreach (Voxel voxel in ComponentVoxels.Keys)
            {
                if (voxel.GetBoundingBox().Intersects(larger))
                {
                    return true;
                }
            }

            return false;
        }

        public BoundingBox GetBoundingBox()
        {
            List<BoundingBox> boxes = new List<BoundingBox>();
            foreach (Voxel v in ComponentVoxels.Keys)
            {
                boxes.Add(v.GetBoundingBox());
            }


            return LinearMathHelpers.GetBoundingBox(boxes);
        }
    }
}
