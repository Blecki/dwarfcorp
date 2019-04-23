using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;
using Newtonsoft.Json;
using System.Globalization;
using System.Collections.Concurrent;

namespace DwarfCorp
{
    /// <summary>
    /// Handles components. All game objects (dwarves, trees, lamps, ravenous wolverines) are just a 
    /// collection of components. Together, the collection is called an 'entity'. Components form a 
    /// tree. Each component has a parent and 0 to N children.
    /// </summary>
    public class ComponentManager
    {
        public class ComponentSaveData //: Saving.ISaveableObject
        {
            public List<GameComponent> SaveableComponents;
            public uint RootComponent;
        }

        private Dictionary<uint, GameComponent> Components;
        private uint MaxGlobalID = 0;
        public const int InvalidID = 0;
        private List<MinimapIcon> MinimapIcons = new List<MinimapIcon>();
        private List<GameComponent> Removals = new List<GameComponent>();
        private List<GameComponent> Additions = new List<GameComponent>();

        public GameComponent RootComponent { get; private set; }

        public void SetRootComponent(GameComponent Component)
        {
            RootComponent = Component;
        }

        public int NumComponents()
        {
            return Components.Count;
        }

        private Mutex AdditionMutex = new Mutex();
        private Mutex RemovalMutex = new Mutex();

        public IEnumerable<MinimapIcon> GetMinimapIcons() { return MinimapIcons; }

        public WorldManager World { get; set; }

        public ComponentSaveData GetSaveData()
        {
            // Just in case the root was tagged unserializable for whatever reason.
            RootComponent.SetFlag(GameComponent.Flag.ShouldSerialize, true);

            foreach (var component in Components)
                component.Value.PrepareForSerialization();

            var serializableComponents = Components.Where(c => c.Value.IsFlagSet(GameComponent.Flag.ShouldSerialize)).Select(c => c.Value).ToList();

            return new ComponentSaveData
            {
                SaveableComponents = serializableComponents,
                RootComponent = RootComponent.GlobalID
            };
        }

        /// <summary>
        /// Must be called after serialization to avoid leaking references to dead components.
        /// </summary>
        public void CleanupSaveData()
        {
            foreach (var component in Components)
                component.Value.SerializableChildren = null;
        }

        private void StartThreads()
        {
            for (var i = 0; i < 4; ++i)
            {
                //var updateThread = new System.Threading.Thread(EntityTransformUpdateThread);
                //updateThread.Start();
            }
        }

        public ComponentManager(ComponentSaveData SaveData, WorldManager World)
        {
            this.World = World;
            World.ComponentManager = this;
            Components = new Dictionary<uint, GameComponent>();
            SaveData.SaveableComponents.RemoveAll(c => c == null);

            foreach (var component in SaveData.SaveableComponents)
            {
                Components.Add(component.GlobalID, component);
                component.World = World;
            }

            RootComponent = Components[SaveData.RootComponent] as GameComponent;

            foreach (var component in Components)
            {
                if (component.Value is MinimapIcon)
                    MinimapIcons.Add(component.Value as MinimapIcon);

                foreach (var system in World.UpdateSystems)
                    system.ComponentCreated(component.Value);
            }

            MaxGlobalID = Components.Aggregate<KeyValuePair<uint, GameComponent>, uint>(0, (current, component) => Math.Max(current, component.Value.GlobalID));

            foreach (var component in SaveData.SaveableComponents)
                component.PostSerialization(this);

            foreach (var component in SaveData.SaveableComponents)
            {
                component.ProcessTransformChange();
                component.CreateCosmeticChildren(this);
            }

            var removals = SaveData.SaveableComponents.Where(p => p.Parent == null && p != RootComponent).ToList();

            foreach(var component in removals)
            {
                Console.Error.WriteLine("Component {0} has no parent. removing.", component.Name);
                RemoveComponentImmediate(component);
                SaveData.SaveableComponents.Remove(component);
            }

            StartThreads();

            /*
            foreach (var component in Components)
            {
                if (component.Value.Parent != null && (!HasComponent(component.Value.Parent.GlobalID) || !component.Value.Parent.Children.Contains(component.Value)))
                {
                    Console.Error.WriteLine("Component {0} parent: {1} is not in the list of components", component.Value.Name, component.Value.Parent.Name);
                }
            */
        }

        public ComponentManager(WorldManager state)
        {
            World = state;
            Components = new Dictionary<uint, GameComponent>();
            StartThreads();
        }

        public List<GameComponent> SelectRootBodiesOnScreen(Rectangle selectionRectangle, Camera camera)
        {
            if (World.SelectionBuffer == null)
                return new List<GameComponent>();

            HashSet<GameComponent> toReturn = new HashSet<GameComponent>(); // Hashset ensures all bodies are unique.
            foreach (uint id in World.SelectionBuffer.GetIDsSelected(selectionRectangle))
            {
                GameComponent component;
                if (!Components.TryGetValue(id, out component))
                    continue;

                if (!component.IsVisible) continue; // Then why was it drawn in the selection buffer??
                var toAdd = component.GetRoot().GetComponent<GameComponent>();
                if (!toReturn.Contains(toAdd))
                    toReturn.Add(toAdd);
            }
            return toReturn.ToList();
        }

        private object _msgLock = new object();
        private List<KeyValuePair<GameComponent, Message> > _msgList = new List<KeyValuePair<GameComponent, Message> >();

        // Allows components to receive messages recursively while in a thread.
        public void ReceiveMessageLater(GameComponent component, Message msg)
        {
            lock (_msgLock)
            {
                _msgList.Add(new KeyValuePair<GameComponent, Message>(component, msg));
            }
        }

        public void AddComponent(GameComponent component)
        {
            AdditionMutex.WaitOne();

            MaxGlobalID += 1;
            component.GlobalID = MaxGlobalID;
            Additions.Add(component);

            AdditionMutex.ReleaseMutex();
        }

        public void RemoveComponent(GameComponent component)
        {
            RemovalMutex.WaitOne();
            Removals.Add(component);
            RemovalMutex.ReleaseMutex();
        }

        public bool HasComponent(uint id)
        {
            return Components.ContainsKey(id) || Additions.Any(a => a.GlobalID == id);
        }

        private void RemoveComponentImmediate(GameComponent component)
        {
            if (!Components.ContainsKey(component.GlobalID))
                return;

            if (_componentList == null)
                _componentList = Components.Values.ToList();

            _componentList.Remove(component);
            Components.Remove(component.GlobalID);

            if (component is MinimapIcon)
                MinimapIcons.Remove(component as MinimapIcon);

            foreach (var system in World.UpdateSystems)
                system.ComponentDestroyed(component);

            foreach (var child in new List<GameComponent>(component.EnumerateChildren()))
                RemoveComponentImmediate(child);
        }

        private void AddComponentImmediate(GameComponent component)
        {
            if (Components.ContainsKey(component.GlobalID))
            {
                if (Object.ReferenceEquals(Components[component.GlobalID], component)) return;
                throw new InvalidOperationException("Attempted to add component with same ID as existing component.");
            }

            Components[component.GlobalID] = component;

            if (_componentList == null)
                _componentList = Components.Values.ToList();

            _componentList.Add(component);

            if (component is MinimapIcon)
                MinimapIcons.Add(component as MinimapIcon);

            foreach (var system in World.UpdateSystems)
                system.ComponentCreated(component);

            component.ProcessTransformChange();
        }

        public uint k = 0;
        private List<GameComponent> _componentList = null; // Why are we keeping this list twice????
        public ulong iter = 0;
        
        public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            iter++;
            PerformanceMonitor.PushFrame("Component Update");
            if (_componentList == null)
                _componentList = Components.Values.ToList();

            var playerPoint = World.Camera.Position;
            // Todo: Make this a sphere?
            var distanceVec = new Vector3(GameSettings.Default.EntityUpdateDistance, GameSettings.Default.EntityUpdateDistance, GameSettings.Default.EntityUpdateDistance);
            var updateBox = new BoundingBox(playerPoint - distanceVec, playerPoint + distanceVec);
            var componentsToUpdate = World.EnumerateIntersectingObjectsLoose(updateBox);
            PerformanceMonitor.SetMetric("ENTITIES UPDATED", componentsToUpdate.Count);

            foreach (var body in componentsToUpdate)
            {
                body.Update(gameTime, chunks, camera);
                body.ProcessTransformChange();
            }

            if (Debugger.Switches.ShowUpdateBox)
                foreach (var chunk in World.EnumerateChunksInBounds(updateBox))
                    Drawer3D.DrawBox(chunk.GetBoundingBox(), Color.Red, 0.4f, false);

            //for (uint j = 0; j < Math.Min(GameSettings.Default.EntityUpdateRate, _componentList.Count); j++)
            //{
            //    var c = (k + j) % (uint)_componentList.Count;
            //    if (iter % (ulong)_componentList[(int)c].UpdateRate == 0)
            //    {
            //        _componentList[(int)c].Update(gameTime, chunks, camera);
            //        if (_componentList[(int)c] is GameComponent body)
            //            //WorkOrders.Enqueue(body);
            //            body.ProcessTransformChange();
            //    }
            //}

            k += (uint)Math.Min(GameSettings.Default.EntityUpdateRate, _componentList.Count);

            PerformanceMonitor.PopFrame();

            AddRemove();
            ReceiveMessage();
        }

        private bool ExitThreads = false;

        public void Destroy()
        {
            ExitThreads = true;
        }

        private ConcurrentQueue<GameComponent> WorkOrders = new ConcurrentQueue<GameComponent>();

        private void EntityTransformUpdateThread()
        {
            Console.Out.WriteLine("Starting chunk regeneration thread.");
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

#if !DEBUG
            try
#endif
            {
                while (!DwarfGame.ExitGame && !ExitThreads)
                {
                    if (WorkOrders.TryDequeue(out GameComponent body))
                        body.ProcessTransformChange();
                    else
                        Thread.Sleep(100); // Nothing in the queue - lets take a break.
                }
            }
#if !DEBUG
            catch (Exception exception)
            {
                Console.Out.WriteLine("Component transform update thread exited due to an exception.");
                ProgramData.WriteExceptionLog(exception);
                throw;
            }
#endif
        }

        private void ReceiveMessage()
        {
            lock (_msgLock)
            {
                foreach (var msg in _msgList)
                {
                    msg.Key.ReceiveMessageRecursive(msg.Value);
                }
                _msgList.Clear();
            }
        }

        private void AddRemove()
        {
            AdditionMutex.WaitOne();
            foreach (GameComponent component in Additions)
                AddComponentImmediate(component);

            Additions.Clear();
            AdditionMutex.ReleaseMutex();

            RemovalMutex.WaitOne();
            var localRemovals = new List<GameComponent>(Removals);
            Removals.Clear();
            RemovalMutex.ReleaseMutex();

            foreach (var component in localRemovals)
                RemoveComponentImmediate(component);
        }

        public void UpdatePaused(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            PerformanceMonitor.PushFrame("Component Update");

            foreach (var component in Components.Values)
                component.UpdatePaused(gameTime, chunks, camera);

            PerformanceMonitor.PopFrame();
            
            AddRemove();
        }

        public GameComponent FindComponent(uint ID)
        {
            if (Components.TryGetValue(ID, out GameComponent result))
                return result;
            else
                return null;
        }
    }
}
