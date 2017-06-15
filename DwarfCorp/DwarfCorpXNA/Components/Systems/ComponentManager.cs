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
    /// This class is responsible for handling components. "Components" are one of the most important parts of the 
    /// DwarfCorp engine. Everything in the game is a collection of components. A collection of components is called an "entity".
    /// Components live in a tree-like structure, they have parents and children. Most components (called Locatable components)
    /// also have a position and orientation.
    /// By adding and removing components to an entity, functionality can be changed.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class ComponentManager
    {
        public Dictionary<uint, GameComponent> Components { get; set; }

        private Dictionary<System.Type, List<IUpdateableComponent>> UpdateableComponents { get; set; }

        [JsonIgnore]
        public List<IRenderableComponent> Renderables { get; private set; }

        private List<GameComponent> Removals { get; set; }
        private List<GameComponent> Additions { get; set; }

        public Body RootComponent { get; set; }

        private static Camera Camera { get; set; }

        [JsonIgnore]
        public Mutex AdditionMutex { get; set; }
        [JsonIgnore]
        public Mutex RemovalMutex { get; set; }

        public ParticleManager ParticleManager { get; set; }
        [JsonIgnore]
        public CollisionManager CollisionManager { get; set; }

        public FactionLibrary Factions { get; set; }
        public Diplomacy Diplomacy { get; set; }

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
            Vector3 origin = new Vector3(World.WorldOrigin.X, 0, World.WorldOrigin.Y);
            Vector3 extents = new Vector3(1500, 1500, 1500);
            CollisionManager = new CollisionManager(new BoundingBox(origin - extents, origin + extents)); GameObjectCaching.Reset();
            RootComponent.RefreshCacheTypesRecursive();
            World.Natives.Clear();
            foreach (Faction faction in Factions.Factions.Values)
            {
                if (faction.Race.IsNative && faction.Race.IsIntelligent && !faction.IsRaceFaction)
                {
                    World.Natives.Add(faction);
                }
            }

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
            Factions = new FactionLibrary();
            if (natives != null && natives.Count > 0)
            {
                Factions.AddFactions(state, natives);
            }
            Factions.Initialize(state, CompanyInformation);
            Point playerOrigin = new Point((int)(World.WorldOrigin.X), (int)(World.WorldOrigin.Y));

            Factions.Factions["Player"].Center = playerOrigin;
            Factions.Factions["Motherland"].Center = new Point(playerOrigin.X + 50, playerOrigin.Y + 50);
        }

        #region picking

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

        #endregion


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
            GamePerformance.Instance.StartTrackPerformance("Update Transforms");
            if (RootComponent != null)
            {
                RootComponent.UpdateTransformsRecursive(null);
            }
            GamePerformance.Instance.StopTrackPerformance("Update Transforms");

            GamePerformance.Instance.StartTrackPerformance("Factions");
            Factions.Update(gameTime);
            GamePerformance.Instance.StopTrackPerformance("Factions");

            GamePerformance.Instance.TrackValueType("Component Count", Components.Count);
            GamePerformance.Instance.TrackValueType("Updateable Count", UpdateableComponents.Count);
            GamePerformance.Instance.TrackValueType("Renderable Count", Renderables.Count);

            GamePerformance.Instance.StartTrackPerformance("Update Components");
            foreach (var componentType in UpdateableComponents)
                foreach (var component in componentType.Value)
                {
                    //component.Manager = this;

                    if (component.IsActive)
                    {
                        //GamePerformance.Instance.StartTrackPerformance("Component Update " + component.GetType().Name);
                        component.Update(gameTime, chunks, camera);
                        //GamePerformance.Instance.StopTrackPerformance("Component Update " + component.GetType().Name);
                    }
                }

            GamePerformance.Instance.StopTrackPerformance("Update Components");

            HandleAddRemoves();
        }

        public void HandleAddRemoves()
        {
            AdditionMutex.WaitOne();
            foreach (GameComponent component in Additions)
            {
                AddComponentImmediate(component);
            }

            Additions.Clear();
            AdditionMutex.ReleaseMutex();

            RemovalMutex.WaitOne();
            foreach (GameComponent component in Removals)
            {
                RemoveComponentImmediate(component);
            }

            Removals.Clear();
            RemovalMutex.ReleaseMutex();
        }

        public uint GetMaxComponentID()
        {
            return Components.Aggregate<KeyValuePair<uint, GameComponent>, uint>(0, (current, component) => Math.Max(current, component.Value.GlobalID));
        }
    }

}
