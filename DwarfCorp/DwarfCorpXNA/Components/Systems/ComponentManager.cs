using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// Handles components. All game objects (dwarves, trees, lamps, ravenous wolverines) are just a 
    /// collection of components. Together, the collection is called an 'entity'. Components form a 
    /// tree. Each component has a parent and 0 to N children.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class ComponentManager
    {
        [JsonProperty]
        private Dictionary<uint, GameComponent> Components;

        private Dictionary<System.Type, List<IUpdateableComponent>> UpdateableComponents;
        private List<IRenderableComponent> Renderables;

        public IEnumerable<IRenderableComponent> GetRenderables()
        {
            return Renderables;
        }

        private List<GameComponent> Removals { get; set; }
        private List<GameComponent> Additions { get; set; }

        public Body RootComponent { get; set; }

        private static Camera Camera { get; set; }

        [JsonIgnore]
        private Mutex AdditionMutex { get; set; }
        [JsonIgnore]
        private Mutex RemovalMutex { get; set; }

        public ParticleManager ParticleManager { get; set; }


        [JsonIgnore]
        public WorldManager World { get; set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            AdditionMutex = new Mutex();
            RemovalMutex = new Mutex();
            Removals = new List<GameComponent>();
            Additions = new List<GameComponent>();
            World = (WorldManager)context.Context;
            GameObjectCaching.Reset();
            RootComponent.RefreshCacheTypesRecursive();
            World.Natives.Clear();

            foreach (var component in Components)
            {
                if (component.Value is IUpdateableComponent)
                {
                    var type = component.Value.GetType();
                    if (!UpdateableComponents.ContainsKey(type))
                        UpdateableComponents.Add(type, new List<IUpdateableComponent>());
                    UpdateableComponents[type].Add(component.Value as IUpdateableComponent);
                }

                if (component.Value is IRenderableComponent)
                    Renderables.Add(component.Value as IRenderableComponent);
            }
        }

        public ComponentManager()
        {

        }

        public ComponentManager(WorldManager state, CompanyInformation CompanyInformation, List<Faction> natives)
        {
            World = state;
            Components = new Dictionary<uint, GameComponent>();
            UpdateableComponents = new Dictionary<Type, List<IUpdateableComponent>>();
            Renderables = new List<IRenderableComponent>();
            Removals = new List<GameComponent>();
            Additions = new List<GameComponent>();
            Camera = null;
            AdditionMutex = new Mutex();
            RemovalMutex = new Mutex();
        }

        public List<Body> SelectRootBodiesOnScreen(Rectangle selectionRectangle, Camera camera)
        {
            /*
            return (from component in RootComponent.Children.OfType<Body>()
                    let screenPos = camera.Project(component.GlobalTransform.Translation)
                    where   screenPos.Z > 0 
                    && (selectionRectangle.Contains((int)screenPos.X, (int)screenPos.Y) || selectionRectangle.Intersects(component.GetScreenRect(camera))) 
                    && camera.GetFrustrum().Contains(component.GlobalTransform.Translation) != ContainmentType.Disjoint
                    && !World.ChunkManager.ChunkData.CheckOcclusionRay(camera.Position, component.Position)
                    select component).ToList();
             */
            if (World.SelectionBuffer == null)
            {
                return new List<Body>();
            }
            HashSet<Body> toReturn = new HashSet<Body>();
            foreach (uint id in World.SelectionBuffer.GetIDsSelected(selectionRectangle))
            {
                GameComponent component;
                if (!Components.TryGetValue(id, out component))
                {
                    continue;
                }
                if (!component.IsVisible) continue;
                var toAdd = component.GetEntityRootComponent().GetComponent<Body>();
                if (!toReturn.Contains(toAdd))
                    toReturn.Add(component.GetEntityRootComponent().GetComponent<Body>());
            }
            return toReturn.ToList();
        }

        public void AddComponent(GameComponent component)
        {
            AdditionMutex.WaitOne();
            Additions.Add(component);
            AdditionMutex.ReleaseMutex();
        }

        public void RemoveComponent(GameComponent component)
        {
            RemovalMutex.WaitOne();
            Removals.Add(component);
            RemovalMutex.ReleaseMutex();
        }

        private void RemoveComponentImmediate(GameComponent component)
        {
            if (!Components.ContainsKey(component.GlobalID))
            {
                return;
            }

            Components.Remove(component.GlobalID);
            if (component is IUpdateableComponent)
            {
                var type = component.GetType();
                if (UpdateableComponents.ContainsKey(type))
                    UpdateableComponents[type].Remove(component as IUpdateableComponent);
            }
            if (component is IRenderableComponent)
            {
                Renderables.Remove(component as IRenderableComponent);
            }

            foreach (var child in component.GetAllChildrenRecursive())
                RemoveComponentImmediate(child);
        }

        private void AddComponentImmediate(GameComponent component)
        {
            if (Components.ContainsKey(component.GlobalID) && Components[component.GlobalID] != component)
            {
                throw new IndexOutOfRangeException("Component was added that already exists.");
            }
            else if (!Components.ContainsKey(component.GlobalID))
            {
                Components[component.GlobalID] = component;
                if (component is IUpdateableComponent)
                {
                    var type = component.GetType();
                    if (!UpdateableComponents.ContainsKey(type))
                        UpdateableComponents.Add(type, new List<IUpdateableComponent>());
                    UpdateableComponents[type].Add(component as IUpdateableComponent);
                }
                if (component is IRenderableComponent)
                {
                    Renderables.Add(component as IRenderableComponent);
                }
            }
        }

        public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (RootComponent != null)
                RootComponent.UpdateTransformsRecursive(null);

            foreach (var componentType in UpdateableComponents)
                foreach (var component in componentType.Value)
                    if (component.IsActive)
                        component.Update(gameTime, chunks, camera);
            
            AdditionMutex.WaitOne();
            foreach (GameComponent component in Additions)
                AddComponentImmediate(component);

            Additions.Clear();
            AdditionMutex.ReleaseMutex();

            RemovalMutex.WaitOne();
            foreach (GameComponent component in Removals)
                RemoveComponentImmediate(component);

            Removals.Clear();
            RemovalMutex.ReleaseMutex();
        }

        public uint GetMaxComponentID()
        {
            return Components.Aggregate<KeyValuePair<uint, GameComponent>, uint>(0, (current, component) => Math.Max(current, component.Value.GlobalID));
        }
    }

}
