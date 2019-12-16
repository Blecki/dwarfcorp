using System;
using System.Collections.Generic;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;

namespace DwarfCorp
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class EntityFactoryAttribute : Attribute
    {
        public String Name;

        public EntityFactoryAttribute(String Name)
        {
            this.Name = Name;
        }
    }

    internal class EntityFactory
    {
        // Todo: Dear god. Kill these statics!
        public static WorldManager World = null;
        private static ComponentManager Components { get { return World.ComponentManager; } }

        private static Dictionary<string, Func<Vector3, Blackboard, GameComponent>> EntityFuncs { get; set; }

        public static void Cleanup()
        {
            World = null;
            //EntityFuncs = null;
        }

        public static IEnumerable<String> EnumerateEntityTypes()
        {
            return EntityFuncs.Keys;
        }

        public static void Initialize(WorldManager world)
        {
            World = world;
            if (EntityFuncs == null)
                EntityFuncs = new Dictionary<string, Func<Vector3, Blackboard, GameComponent>>();

            foreach (var method in AssetManager.EnumerateModHooks(typeof(EntityFactoryAttribute), typeof(GameComponent), new Type[]
            {
                typeof(ComponentManager),
                typeof(Vector3),
                typeof(Blackboard)
            }))
            {
                var attribute = method.GetCustomAttributes(false).FirstOrDefault(a => a is EntityFactoryAttribute) as EntityFactoryAttribute;
                if (attribute == null) continue;
                EntityFuncs[attribute.Name] = (position, data) => method.Invoke(null, new Object[] { world.ComponentManager, position, data }) as GameComponent;
            }
        }

        public static void RegisterEntity<T>(string id, Func<Vector3, Blackboard, T> function) where T : GameComponent
        {
            if (EntityFuncs == null)
                EntityFuncs = new Dictionary<string, Func<Vector3, Blackboard, GameComponent>>();
            EntityFuncs[id] = function;
        }

        public static bool HasEntity(string id)
        {
            return EntityFuncs.ContainsKey(id);
        }

        public static T CreateEntity<T>(string id, Vector3 location, Blackboard data = null) where T : GameComponent
        {
            if (EntityFuncs == null)
            {
                throw new NullReferenceException(String.Format("Can't create entity {0}. Entity library was not initialized.", id));
            }
            if (data == null) data = new Blackboard();
            if (EntityFuncs.ContainsKey(id))
            {
                var r = EntityFuncs[id].Invoke(location, data);
                // Todo: This is a hack. Creatures create a physics component and add themselves to it. 
                // Instead heirarchy should be creature -> physics -> everything else.
                // Todo: Make creature factories return their physics component, handily solving this issue.
                // Todo: Why is the component manager a static member, rather than being passed in?
                Components.RootComponent.AddChild(r.Parent == null ? r : r.Parent);
                return r as T;
            }
            else
            {
                string err = id ?? "null";
                throw new KeyNotFoundException("Unable to create entity of type " + err);
            }
        }

        public static IEnumerable<GameComponent> CreateResourcePiles(IEnumerable<Resource> resources, BoundingBox box)
        {
            //const int maxPileSize = 64;
            foreach (var resource in resources)
            {
                //for (int numRemaining = resource.Count; numRemaining > 0; numRemaining -= maxPileSize)
                //{
                    const int maxIters = 10;

                    for (int i = 0; i < maxIters; i++)
                    {
                        Vector3 pos = MathFunctions.RandVector3Box(box);
                        var voxel = new VoxelHandle(World.ChunkManager, GlobalVoxelCoordinate.FromVector3(pos));
                        if ((!voxel.IsValid) || !voxel.IsEmpty)
                            continue;

                    var body = new ResourceEntity(World.ComponentManager, resource, pos) as Physics;
                    
                        if (body != null)
                        {
                            body.Velocity = MathFunctions.RandVector3Cube();
                            body.Velocity.Normalize();
                            body.Velocity *= 5.0f;
                            body.IsSleeping = false;
                            yield return body;
                        }
                        break;
                    }
                //}
            }
        }
    }
}
