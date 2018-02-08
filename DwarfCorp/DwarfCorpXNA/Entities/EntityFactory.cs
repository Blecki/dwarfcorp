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

    /// <summary>
    /// This class is used to create entities. It should probably be replaced with a more modular system (or a set of data files)
    /// Right now, its just an ugly class for initializing most of the entities in the game.
    /// </summary>
    internal class EntityFactory
    {
        public static WorldManager World = null;
        private static ComponentManager Components { get { return World.ComponentManager; } }
        private static List<Action> LazyActions = new List<Action>();

        public static Dictionary<string, Func<Vector3, Blackboard, GameComponent>> EntityFuncs { get; set; }

        // This exists in case we want to call the entity factory from  a thread, allowing us
        // to lazy-load entities later.
        public static void DoLazyActions()
        {
            foreach (var func in LazyActions)
            {
                if (func != null)
                    func.Invoke();
            }
            LazyActions.Clear();
        }

        public static void Initialize(WorldManager world)
        {
            World = world;

            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                {
                    if (!method.IsStatic) continue;
                    if (method.ReturnType != typeof(GameComponent)) continue;

                    var attribute = method.GetCustomAttributes(false).FirstOrDefault(a => a is EntityFactoryAttribute) as EntityFactoryAttribute;
                    if (attribute == null) continue;

                    var parameters = method.GetParameters();
                    if (parameters.Length != 3) continue;
                    if (parameters[0].ParameterType != typeof(ComponentManager)) continue;
                    if (parameters[1].ParameterType != typeof(Vector3)) continue;
                    if (parameters[2].ParameterType != typeof(Blackboard)) continue;

                    RegisterEntity(attribute.Name, (position, data) => method.Invoke(null, new Object[] { world.ComponentManager, position, data }) as GameComponent);
                }
            }

            RegisterEntity("Arrow", (position, data) => new ArrowProjectile(world.ComponentManager, position, data.GetData("Velocity", Vector3.Up * 10 + MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)), data.GetData<Body>("Target", null)));
            RegisterEntity("Bullet", (position, data) => new BulletProjectile(world.ComponentManager, position, data.GetData("Velocity", Vector3.Up * 10 + MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)), data.GetData<Body>("Target", null)));
            RegisterEntity("Web", (position, data) => new WebProjectile(world.ComponentManager, position, data.GetData("Velocity", Vector3.Up * 10 + MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)), data.GetData<Body>("Target", null)));
            RegisterEntity("Fireball", (position, data) => new FireballProjectile(world.ComponentManager, position, data.GetData("Velocity", Vector3.Up * 10 + MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)), data.GetData<Body>("Target", null)));
            RegisterEntity("Fairy", (position, data) => new Fairy(world.ComponentManager, "Player", position));
            
            
            RegisterEntity("Ladder", (position, data) => new Ladder(world.ComponentManager, position, data.GetData<List<ResourceAmount>>("Resources", new List<ResourceAmount>() { new ResourceAmount(ResourceType.Wood) })));
            RegisterEntity("RandTrinket", (position, data) => CreateRandomTrinket(world, position));
            RegisterEntity("RandFood", (position, data) => CreateRandomFood(world, position));
            RegisterEntity("Snow Cloud", (position, data) => new Cloud(world.ComponentManager, 0.1f, 50, 40, position) { TypeofStorm = StormType.SnowStorm });
            RegisterEntity("Rain Cloud", (position, data) => new Cloud(world.ComponentManager, 0.1f, 50, 40, position) { TypeofStorm = StormType.RainStorm });
            RegisterEntity("Storm", (position, data) =>
            {
                Weather.CreateForecast(world.Time.CurrentDate, world.ChunkManager.Bounds, world, 3);
                Weather.CreateStorm(MathFunctions.RandVector3Cube() * 10, MathFunctions.Rand(0.05f, 1.0f), world);
                return new Cloud(world.ComponentManager, 0.1f, 50, 40, position);
            });
            RegisterEntity("MudGolem", (position, data) => new MudGolem(new CreatureStats(new MudGolemClass(), 0), "dirt_particle", "Carnivore", world.PlanService, World.Factions.Factions["Carnivore"], world.ComponentManager, "Mud Golem", position));
            RegisterEntity("SnowGolem", (position, data) => new MudGolem(new CreatureStats(new SnowGolemClass(), 0), "snow_particle", "Carnivore", world.PlanService, World.Factions.Factions["Carnivore"], world.ComponentManager, "Snow Golem", position));
            RegisterEntity("Mud", (position, data) => new MudProjectile(world.ComponentManager, position, data.GetData("Velocity", Vector3.Up * 10 + MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)), data.GetData<Body>("Target", null)));
            RegisterEntity("Snowball", (position, data) => new SnowballProjectile(world.ComponentManager, position, data.GetData("Velocity", Vector3.Up * 10 + MathFunctions.RandVector3Box(-10, 10, 0, 0, -10, 10)), data.GetData<Body>("Target", null)));
        }

        private static GameComponent CreateRandomFood(WorldManager world, Vector3 position)
        {
            IEnumerable<Resource> foods = ResourceLibrary.GetResourcesByTag(Resource.ResourceTags.RawFood);

            Resource randresource = ResourceLibrary.CreateMeal(Datastructures.SelectRandom(foods).Name,
                Datastructures.SelectRandom(foods).Name);
            return new ResourceEntity(world.ComponentManager, new ResourceAmount(randresource.Name), position);
        }


        public static ResourceEntity CreateRandomTrinket(WorldManager world, Vector3 pos)
        {
            Resource randResource = ResourceLibrary.GenerateTrinket(Datastructures.SelectRandom(ResourceLibrary.Resources.Where(r => r.Value.Tags.Contains(Resource.ResourceTags.Material))).Key, MathFunctions.Rand(0.1f, 3.5f));

            if (MathFunctions.RandEvent(0.5f))
            {
                randResource = ResourceLibrary.EncrustTrinket(randResource.Name, Datastructures.SelectRandom(ResourceLibrary.Resources.Where(r => r.Value.Tags.Contains(Resource.ResourceTags.Gem))).Key);
            }

            return new ResourceEntity(world.ComponentManager, new ResourceAmount(randResource.Name), pos);
        }

        public static void RegisterEntity<T>(string id, Func<Vector3, Blackboard, T> function) where T : GameComponent
        {
            if (EntityFuncs == null)
            {
                EntityFuncs = new Dictionary<string, Func<Vector3, Blackboard, GameComponent>>();
            }
            EntityFuncs[id] = function;
        }

        public static void GhostEntity(Body Entity, Color Tint)
        {
            Entity.SetFlagRecursive(GameComponent.Flag.Active, false);
            Entity.SetTintRecursive(Tint);
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

        public static void DoLazy(Action action)
        {
            LazyActions.Add(action);
        }

    }
}
