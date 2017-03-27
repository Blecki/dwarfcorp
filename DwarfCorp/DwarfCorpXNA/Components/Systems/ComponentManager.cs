// ComponentManager.cs
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
        public Dictionary<System.Type, List<IUpdateableComponent>> UpdateableComponents { get; set; }
        public List<IRenderableComponent> RenderableComponents { get; set; }

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
        }

        public ComponentManager()
        {

        }

        public ComponentManager(WorldManager state, CompanyInformation CompanyInformation, List<Faction> natives)
        {
            World = state;
            Components = new Dictionary<uint, GameComponent>();
            UpdateableComponents = new Dictionary<Type, List<IUpdateableComponent>>();
            RenderableComponents = new List<IRenderableComponent>();
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

        public static List<T> FilterComponentsWithTag<T>(string tag, List<T> toFilter) where T : GameComponent
        {
            return toFilter.Where(component => component.Tags.Contains(tag)).ToList();
        }

        public void GetBodiesIntersecting(BoundingBox box, List<Body> components, CollisionManager.CollisionType type)
        {
            HashSet<IBoundedObject> set = new HashSet<IBoundedObject>();
            CollisionManager.GetObjectsIntersecting(box, set, type);

            components.AddRange(set.Where(o => o is Body).Select(o => o as Body));
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
            List<Body> toReturn = new List<Body>();
            foreach (uint id in World.SelectionBuffer.GetIDsSelected(selectionRectangle))
            {
                GameComponent component;
                if (!Components.TryGetValue(id, out component))
                {
                    continue;
                }
                if (!component.IsVisible) continue;
                toReturn.Add(component.GetEntityRootComponent().GetComponent<Body>());
            }
            return toReturn;
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
                RenderableComponents.Remove(component as IRenderableComponent);
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
                    RenderableComponents.Add(component as IRenderableComponent);
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
            GamePerformance.Instance.TrackValueType("Renderable Count", RenderableComponents.Count);

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


        private List<IRenderableComponent> visibleComponents = new List<IRenderableComponent>();

        public enum WaterRenderType
        {
            Reflective,
            None
        }

        public void RenderSelectionBuffer(DwarfTime time, ChunkManager chunks, Camera camera,
            SpriteBatch spriteBatch, GraphicsDevice graphics, Shader effect)
        {
            effect.CurrentTechnique = effect.Techniques["Selection"];
            foreach (Body bodyToDraw in visibleComponents)
            {
                if (bodyToDraw.IsVisible)
                    bodyToDraw.RenderSelectionBuffer(time, chunks, camera, spriteBatch, graphics, effect);
            }
        }

        public void Render(DwarfTime gameTime,
            ChunkManager chunks,
            Camera camera,
            SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice,
            Shader effect,
            WaterRenderType waterRenderMode, float waterLevel)
        {
            bool renderForWater = (waterRenderMode != WaterRenderType.None);

            if (!renderForWater)
            {
                visibleComponents.Clear();

                BoundingFrustum frustrum = camera.GetFrustrum();

                foreach (IRenderableComponent b in RenderableComponents)
                {
                    if (!b.IsVisible) continue;
                    if (b.IsAboveCullPlane) continue;

                    if (b.FrustrumCull)
                    {
                        if ((b.GlobalTransform.Translation - camera.Position).LengthSquared() >= chunks.DrawDistanceSquared) continue;
                        if (!(b.GetBoundingBox().Intersects(frustrum))) continue;
                    }

                    System.Diagnostics.Debug.Assert(!visibleComponents.Contains(b));
                    visibleComponents.Add(b);
                }

                Camera = camera;
            }

            effect.EnableLighting = GameSettings.Default.CursorLightEnabled;
            graphicsDevice.RasterizerState = RasterizerState.CullNone;

            visibleComponents.Sort(CompareZDepth);
            foreach (IRenderableComponent bodyToDraw in visibleComponents)
            {
                if (waterRenderMode == WaterRenderType.Reflective &&
                   !(bodyToDraw.GetBoundingBox().Min.Y > waterLevel - 2))
                {
                    continue;
                }
                bodyToDraw.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderForWater);
            }
            effect.EnableLighting = false;
        }

        public static int CompareZDepth(IRenderableComponent A, IRenderableComponent B)
        {
            if (A == B)
            {
                return 0;
            }
            return
                -(Camera.Position - A.GlobalTransform.Translation).LengthSquared()
                    .CompareTo((Camera.Position - B.GlobalTransform.Translation).LengthSquared());
        }

        public uint GetMaxComponentID()
        {
            return Components.Aggregate<KeyValuePair<uint, GameComponent>, uint>(0, (current, component) => Math.Max(current, component.Value.GlobalID));
        }
    }

}
