// EntityFactory.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
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
        public static WorldManager World = null;
        private static ComponentManager Components { get { return World.ComponentManager; } }

        private static Dictionary<string, Func<Vector3, Blackboard, GameComponent>> EntityFuncs { get; set; }

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

        public static T CreateEntity<T>(string id, Vector3 location, Blackboard data = null) where T : GameComponent
        {
            if (data == null) data = new Blackboard();
            if (EntityFuncs.ContainsKey(id))
            {
                var r = EntityFuncs[id].Invoke(location, data);
                // Todo: This is a hack. Creatures create a physics component and add themselves to it. 
                // Instead heirarchy should be creature -> physics -> everything else.
                // Todo: Make creature factories return their physics component, handily solving this issue.
                Components.RootComponent.AddChild(r.Parent == null ? r : r.Parent);
                return r as T;
            }
            else
            {
                string err = id ?? "null";
                throw new KeyNotFoundException("Unable to create entity of type " + err);
            }
        }

        public static IEnumerable<Body> CreateResourcePiles(IEnumerable<ResourceAmount> resources, BoundingBox box)
        {
            const int maxPileSize = 64;
            foreach (ResourceAmount resource in resources)
            {
                for (int numRemaining = resource.NumResources; numRemaining > 0; numRemaining -= maxPileSize)
                {
                    const int maxIters = 10;

                    for (int i = 0; i < maxIters; i++)
                    {
                        Vector3 pos = MathFunctions.RandVector3Box(box);
                        var voxel = new VoxelHandle(World.ChunkManager.ChunkData,
                        GlobalVoxelCoordinate.FromVector3(pos));
                        if ((!voxel.IsValid) || !voxel.IsEmpty)
                        {
                            continue;
                        }

                        Physics body = EntityFactory.CreateEntity<Physics>(resource.ResourceType + " Resource",
                        pos, Blackboard.Create<int>("num", Math.Min(numRemaining, maxPileSize))) as Physics;


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
                }
            }
        }
    }
}
