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
using System.Threading;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     This class is responsible for handling components. "Components" are one of the most important parts of the
    ///     DwarfCorp engine. Everything in the game is a collection of components. A collection of components is called an
    ///     "entity".
    ///     Components live in a tree-like structure, they have parents and children. Most components (called Locatable
    ///     components)
    ///     also have a position and orientation.
    ///     By adding and removing components to an entity, functionality can be changed.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class ComponentManager
    {
        /// <summary> Whether the component is rendered in reflections on water </summary>
        public enum WaterRenderType
        {
            Reflective,
            None
        }

        /// <summary> List of components to draw in the next frame </summary>
        private readonly List<GameComponent> componentsToDraw = new List<GameComponent>();
        /// <summary> Comopnents which are visible to the camera this frame </summary>
        private readonly HashSet<Body> visibleComponents = new HashSet<Body>();

        public ComponentManager()
        {
        }

        /// <summary> create a new component manager. <summary>
        public ComponentManager(PlayState state, string companyName, string companyMotto, NamedImageFrame companyLogo,
            Color companyColor, List<Faction> natives)
        {
            Components = new Dictionary<uint, GameComponent>();
            Removals = new List<GameComponent>();
            Additions = new List<GameComponent>();
            Camera = null;
            AdditionMutex = new Mutex();
            RemovalMutex = new Mutex();
            Factions = new FactionLibrary();
            if (natives != null && natives.Count > 0)
            {
                Factions.AddFactions(natives);
            }
            Factions.Initialize(state, companyName, companyMotto, companyLogo, companyColor);
            var playerOrigin = new Point((int) (PlayState.WorldOrigin.X), (int) (PlayState.WorldOrigin.Y));

            Factions.Factions["Player"].Center = playerOrigin;
            Factions.Factions["Motherland"].Center = new Point(playerOrigin.X + 50, playerOrigin.Y + 50);
        }

        #region picking

        /// <summary>
        /// Returns a list of components of type T without the given tag.
        /// </summary>
        public static List<T> FilterComponentsWithoutTag<T>(string tag, List<T> toFilter) where T : GameComponent
        {
            return toFilter.Where(component => !component.Tags.Contains(tag)).ToList();
        }

        /// <summary>
        /// Returns a list of components of type T with the given tag.
        /// </summary>
        public static List<T> FilterComponentsWithTag<T>(string tag, List<T> toFilter) where T : GameComponent
        {
            return toFilter.Where(component => component.Tags.Contains(tag)).ToList();
        }

        /// <summary>
        /// Determine whether the given component is under the mouse given a camera and viewport.
        /// </summary>
        public bool IsUnderMouse(Body component, MouseState mouse, Camera camera, Viewport viewPort)
        {
            var viewable = new List<Body>();
            Vector3 pos1 = viewPort.Unproject(new Vector3(mouse.X, mouse.Y, 0), camera.ProjectionMatrix,
                camera.ViewMatrix, Matrix.Identity);
            Vector3 pos2 = viewPort.Unproject(new Vector3(mouse.X, mouse.Y, 1), camera.ProjectionMatrix,
                camera.ViewMatrix, Matrix.Identity);
            Vector3 dir = Vector3.Normalize(pos2 - pos1);

            var toCast = new Ray(pos1, dir);

            return component.Intersects(toCast);
        }

        /// <summary>
        /// Finds all of the bodies under the mouse given a camera and viewport.
        /// </summary>
        public void GetBodiesUnderMouse(MouseState mouse, Camera camera, Viewport viewPort, List<Body> components)
        {
            Vector3 pos1 = viewPort.Unproject(new Vector3(mouse.X, mouse.Y, 0), camera.ProjectionMatrix,
                camera.ViewMatrix, Matrix.Identity);
            Vector3 pos2 = viewPort.Unproject(new Vector3(mouse.X, mouse.Y, 1), camera.ProjectionMatrix,
                camera.ViewMatrix, Matrix.Identity);
            Vector3 dir = Vector3.Normalize(pos2 - pos1);

            var toCast = new Ray(pos1, dir);
            var set = new HashSet<Body>();
            CollisionManager.GetObjectsIntersecting(toCast, set,
                CollisionManager.CollisionType.Dynamic | CollisionManager.CollisionType.Static);

            components.AddRange(set);
        }
        
        /// <summary>
        /// Determines if the given Body is visible to the given Camera
        /// </summary>
        public bool IsVisibleToCamera(Body component, Camera camera)
        {
            var frustrum = new BoundingFrustum(camera.ViewMatrix*camera.ProjectionMatrix);
            return (component.Intersects(frustrum));
        }

        /// <summary>
        /// Given a Camera, gets all the bodies visible to it.
        /// </summary>
        public void GetBodiesVisibleToCamera(Camera camera, List<Body> components)
        {
            var frustrum = new BoundingFrustum(camera.ViewMatrix*camera.ProjectionMatrix);
            GetBodiesIntersecting(frustrum, components,
                CollisionManager.CollisionType.Dynamic | CollisionManager.CollisionType.Static);
        }

        /// <summary>
        /// Given a camera, finds all the bodies NOT visible to that camera.
        /// </summary>
        public void GetBodiesInvisibleToCamera(Camera camera, List<Body> components)
        {
            var frustrum = new BoundingFrustum(camera.ViewMatrix*camera.ProjectionMatrix);

            foreach (GameComponent c in Components.Values)
            {
                if (c is Body && !((Body) c).Intersects(frustrum))
                {
                    components.Add((Body) c);
                }
            }
        }

        /// <summary>
        /// Finds all the bodies intersecting the given bounding sphere.
        /// </summary>
        public void GetBodiesIntersecting(BoundingSphere sphere, List<Body> components,
            CollisionManager.CollisionType type)
        {
            var set = new HashSet<Body>();
            CollisionManager.GetObjectsIntersecting(sphere, set, type);

            components.AddRange(set);
        }

        /// <summary>
        /// Finds all the bodies intersecting the given bounding frustum.
        /// </summary>
        public void GetBodiesIntersecting(BoundingFrustum frustrum, List<Body> components,
            CollisionManager.CollisionType type)
        {
            var set = new HashSet<Body>();
            CollisionManager.GetObjectsIntersecting(frustrum, set, type);

            components.AddRange(set);
        }

        /// <summary>
        /// Finds all the bodies intersecting the given bounding box.
        /// </summary>
        public void GetBodiesIntersecting(BoundingBox box, List<Body> components, CollisionManager.CollisionType type)
        {
            var set = new HashSet<Body>();
            CollisionManager.GetObjectsIntersecting(box, set, type);

            components.AddRange(set);
        }

        /// <summary>
        /// Finds all the bodies intersecting the given ray.
        /// </summary>
        public void GetBodiesIntersecting(Ray ray, List<Body> components, CollisionManager.CollisionType type)
        {
            var set = new HashSet<Body>();
            CollisionManager.GetObjectsIntersecting(ray, set, type);

            components.AddRange(set);
        }

        /// <summary>
        /// Finds all the bodies which are immediate children of the Root body and which
        /// are inside the given selection rectangle on the screen.
        /// </summary>
        public List<Body> SelectRootBodiesOnScreen(Rectangle selectionRectangle, Camera camera)
        {
            return (from component in RootComponent.Children.OfType<Body>()
                let screenPos = camera.Project(component.GlobalTransform.Translation)
                where screenPos.Z > 0
                      &&
                      (selectionRectangle.Contains((int) screenPos.X, (int) screenPos.Y) ||
                       selectionRectangle.Intersects(component.GetScreenRect(camera)))
                      &&
                      camera.GetFrustrum().Contains(component.GlobalTransform.Translation) != ContainmentType.Disjoint
                      && !PlayState.ChunkManager.ChunkData.CheckOcclusionRay(camera.Position, component.Position)
                select component).ToList();
        }

        /// <summary>
        /// Findds all the bodies on screen inside the given selection rectangle.
        /// </summary>
        public List<Body> SelectAllBodiesOnScreen(Rectangle selectionRectangle, Camera camera)
        {
            return (from component in Components.Values.OfType<Body>()
                let screenPos = camera.Project(component.GlobalTransform.Translation)
                where
                    selectionRectangle.Contains((int) screenPos.X, (int) screenPos.Y) ||
                    selectionRectangle.Intersects(component.GetScreenRect(camera)) && screenPos.Z > 0
                select component).ToList();
        }

        #endregion

        /// <summary>
        /// A dictionary from global ID to component containing all the GameComponents.
        /// </summary>
        public Dictionary<uint, GameComponent> Components { get; set; }

        /// <summary> A list of GameComponents to remove this frame </summary>
        private List<GameComponent> Removals { get; set; }

        /// <summary> A list of GameComponents to add thsi frame. </summary>
        private List<GameComponent> Additions { get; set; }

        /// <summary> The root component. All other components are children of this one. </summary>
        public Body RootComponent { get; set; }

        /// <summary> The main Camera </summary>
        private static Camera Camera { get; set; }

        /// <summary> Lock thsi mutex when adding bodies </summary>
        [JsonIgnore]
        public Mutex AdditionMutex { get; set; }

        /// <summary> lock this mutex when removing bodies </summary>
        [JsonIgnore]
        public Mutex RemovalMutex { get; set; }

        /// <summary> manages particle effects. </summary>
        public ParticleManager ParticleManager { get; set; }

        /// <summary> Spatial hash of all bodies used to test collisions </summary>
        [JsonIgnore]
        public CollisionManager CollisionManager { get; set; }

        /// <summary> static library of all the factions in the game </summary>
        public FactionLibrary Factions { get; set; }
        /// <summary> static library of the diplomatic relationships in the game </summary>
        public Diplomacy Diplomacy { get; set; }

        /// <summary> Called when the component manager is deserialized from JSON </summary>
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            AdditionMutex = new Mutex();
            RemovalMutex = new Mutex();
            Removals = new List<GameComponent>();
            Additions = new List<GameComponent>();
            CollisionManager = new CollisionManager(new BoundingBox());
        }

        /// <summary> Tells the ComponentManager to add a component on the next frame </summary>
        public void AddComponent(GameComponent component)
        {
            AdditionMutex.WaitOne();
            Additions.Add(component);
            AdditionMutex.ReleaseMutex();
        }

        /// <summary> Tells the ComponentManager to remove a component on the next frame </summary>
        public void RemoveComponent(GameComponent component)
        {
            RemovalMutex.WaitOne();
            Removals.Add(component);
            RemovalMutex.ReleaseMutex();
        }

        /// <summary> Immediately removes a GameComponent from the ComponentManager </summary>
        private void RemoveComponentImmediate(GameComponent component)
        {
            if (!Components.ContainsKey(component.GlobalID))
            {
                return;
            }

            Components.Remove(component.GlobalID);

            List<GameComponent> children = component.GetAllChildrenRecursive();

            foreach (GameComponent child in children)
            {
                Components.Remove(child.GlobalID);
            }
        }

        /// <summary> Immediately adds a Gamecomponent to the ComponentManager </summary>
        private void AddComponentImmediate(GameComponent component)
        {
            if (Components.ContainsKey(component.GlobalID) && Components[component.GlobalID] != component)
            {
                throw new IndexOutOfRangeException("Component was added that already exists.");
            }
            if (!Components.ContainsKey(component.GlobalID))
            {
                Components[component.GlobalID] = component;
            }
        }

        /// <summary> Updates all the GameComponents </summary>
        public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            // Update the scene graph containing all bodies.
            if (RootComponent != null)
            {
                RootComponent.UpdateTransformsRecursive();
            }

            // Update all factions.
            Factions.Update(gameTime);

            // Update every componnt.
            foreach (GameComponent component in Components.Values)
            {
                if (component.IsActive)
                {
                    component.Update(gameTime, chunks, camera);
                }

                if (component.IsDead)
                {
                    Removals.Add(component);
                    component.IsActive = false;
                    component.IsDead = true;
                    component.IsVisible = false;
                }
            }

            // Add and remove all components.
            HandleAddRemoves();
        }

        /// <summary> Add and remove any components this frame </summary>
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

        /// <summary> Get a list of bodies that intersect the camera's view frustum </summary>
        public List<Body> FrustrumCullLocatableComponents(Camera camera)
        {
            List<Body> visible = CollisionManager.GetVisibleObjects<Body>(camera.GetFrustrum(),
                CollisionManager.CollisionType.Dynamic | CollisionManager.CollisionType.Static);

            return visible;
        }


        /// <summary> 
        /// Render all of the components for reflection in the water.
        /// </summary>
        public bool RenderReflective(GameComponent component, float waterLevel)
        {
            var body = component as Body;
            if (body != null)
            {
                return body.GetBoundingBox().Min.Y > waterLevel - 2;
            }
            return true;
        }

        /// <summary>
        /// Render all of the components.
        /// </summary>
        public void Render(DwarfTime gameTime,
            ChunkManager chunks,
            Camera camera,
            SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice,
            Effect effect,
            WaterRenderType waterRenderMode, float waterLevel)
        {
            bool renderForWater = (waterRenderMode != WaterRenderType.None);

            // If not rendering for water, update the list of components
            // that is to be drawn.
            if (!renderForWater)
            {
                visibleComponents.Clear();
                componentsToDraw.Clear();


                List<Body> list = FrustrumCullLocatableComponents(camera);
                foreach (Body component in list)
                {
                    visibleComponents.Add(component);
                }


                Camera = camera;
                foreach (GameComponent component in Components.Values)
                {
                    bool isLocatable = component is Body;

                    if (isLocatable)
                    {
                        var loc = (Body) component;


                        if (((loc.GlobalTransform.Translation - camera.Position).LengthSquared() <
                             chunks.DrawDistanceSquared &&
                             visibleComponents.Contains(loc) || !(loc.FrustrumCull) ||
                             !(loc.WasAddedToOctree) && !loc.IsAboveCullPlane)
                            )
                        {
                            componentsToDraw.Add(component);
                        }
                    }
                    else
                    {
                        componentsToDraw.Add(component);
                    }
                }
            }


            effect.Parameters["xEnableLighting"].SetValue(GameSettings.Default.CursorLightEnabled ? 1 : 0);
            graphicsDevice.RasterizerState = RasterizerState.CullNone;
            
            // render all the components.
            foreach (GameComponent component in componentsToDraw)
            {
                if (waterRenderMode == WaterRenderType.Reflective && !RenderReflective(component, waterLevel))
                {
                    continue;
                }
                component.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderForWater);
            }

            effect.Parameters["xEnableLighting"].SetValue(0);
        }

        /// <summary> Comparator that tells us which body is closer to the camera </summary>
        public static int CompareZDepth(Body A, Body B)
        {
            if (A == B)
            {
                return 0;
            }

            if (A.Parent == B.Parent && A.DrawInFrontOfSiblings)
            {
                return 1;
            }
            if (B.Parent == A.Parent && B.DrawInFrontOfSiblings)
            {
                return -1;
            }
            if ((Camera.Position - A.GlobalTransform.Translation).LengthSquared() <
                (Camera.Position - B.GlobalTransform.Translation).LengthSquared())
            {
                return 1;
            }
            return -1;
        }

        /// <summary> Returns the maximum global ID needed for a component.
        /// This is only used during deserialization for book-keeping </summary>
        public uint GetMaxComponentID()
        {
            return Components.Aggregate<KeyValuePair<uint, GameComponent>, uint>(0,
                (current, component) => Math.Max(current, component.Value.GlobalID));
        }
    }
}
