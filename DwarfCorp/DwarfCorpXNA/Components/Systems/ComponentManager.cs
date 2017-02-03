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

        private List<GameComponent> Removals { get; set; }
       
        private List<GameComponent> Additions { get; set; }

        public Body RootComponent { get; set; }

        private static Camera Camera { get; set; }

        [JsonIgnore]
        public Mutex AdditionMutex { get; set; }
        [JsonIgnore]
        public Mutex RemovalMutex { get; set; }

        public  ParticleManager ParticleManager { get; set; }
        [JsonIgnore]
        public CollisionManager CollisionManager { get; set; }

        public FactionLibrary Factions { get; set; }
        public Diplomacy Diplomacy { get; set; }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            AdditionMutex = new Mutex();
            RemovalMutex = new Mutex();
            Removals = new List<GameComponent>();
            Additions = new List<GameComponent>();
            CollisionManager = new CollisionManager(new BoundingBox());
        }
       

        public ComponentManager()
        {
            
        }

        public ComponentManager(WorldManager state, CompanyInformation CompanyInformation, List<Faction> natives )
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
            Factions.Initialize(state, CompanyInformation);
            Point playerOrigin = new Point((int)(WorldManager.WorldOrigin.X), (int)(WorldManager.WorldOrigin.Y));

            Factions.Factions["Player"].Center = playerOrigin;
            Factions.Factions["Motherland"].Center = new Point(playerOrigin.X + 50, playerOrigin.Y + 50);
        }

        #region picking

        public static List<T> FilterComponentsWithoutTag<T>(string tag, List<T> toFilter) where T : GameComponent
        {
            return toFilter.Where(component => !component.Tags.Contains(tag)).ToList();
        }

        public static List<T> FilterComponentsWithTag<T>(string tag, List<T> toFilter) where T : GameComponent
        {
            return toFilter.Where(component => component.Tags.Contains(tag)).ToList();
        }

        public bool IsUnderMouse(Body component, MouseState mouse, Camera camera, Viewport viewPort)
        {
            List<Body> viewable = new List<Body>();
            Vector3 pos1 = viewPort.Unproject(new Vector3(mouse.X, mouse.Y, 0), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 pos2 = viewPort.Unproject(new Vector3(mouse.X, mouse.Y, 1), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 dir = Vector3.Normalize(pos2 - pos1);

            Ray toCast = new Ray(pos1, dir);

            return component.Intersects(toCast);
        }


        public void GetBodiesUnderMouse(MouseState mouse, Camera camera, Viewport viewPort, List<Body> components)
        {
            Vector3 pos1 = viewPort.Unproject(new Vector3(mouse.X, mouse.Y, 0), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 pos2 = viewPort.Unproject(new Vector3(mouse.X, mouse.Y, 1), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 dir = Vector3.Normalize(pos2 - pos1);

            Ray toCast = new Ray(pos1, dir);
            HashSet<Body> set = new HashSet<Body>();
            CollisionManager.GetObjectsIntersecting(toCast, set, CollisionManager.CollisionType.Dynamic | CollisionManager.CollisionType.Static);

            components.AddRange(set);
        }

        public bool IsVisibleToCamera(Body component, Camera camera)
        {
            BoundingFrustum frustrum = new BoundingFrustum(camera.ViewMatrix * camera.ProjectionMatrix);
            return (component.Intersects(frustrum));
        }

        public void GetBodiesVisibleToCamera(Camera camera, List<Body> components)
        {
            BoundingFrustum frustrum = new BoundingFrustum(camera.ViewMatrix * camera.ProjectionMatrix);
            GetBodiesIntersecting(frustrum, components, CollisionManager.CollisionType.Dynamic | CollisionManager.CollisionType.Static);
        }

        public void GetBodiesInvisibleToCamera(Camera camera, List<Body> components)
        {
            BoundingFrustum frustrum = new BoundingFrustum(camera.ViewMatrix * camera.ProjectionMatrix);

            foreach(GameComponent c in Components.Values)
            {
                if(c is Body && !((Body) c).Intersects(frustrum))
                {
                    components.Add((Body) c);
                }
            }
        }

        public void GetBodiesIntersecting(BoundingSphere sphere, List<Body> components, CollisionManager.CollisionType type)
        {
            HashSet<Body> set = new HashSet<Body>();
            CollisionManager.GetObjectsIntersecting(sphere, set, type);

            components.AddRange(set);
        }

        public void GetBodiesIntersecting(BoundingFrustum frustrum, List<Body> components, CollisionManager.CollisionType type)
        {
            HashSet<Body> set = new HashSet<Body>();
            CollisionManager.GetObjectsIntersecting(frustrum, set, type);

            components.AddRange(set);
        }

        public void GetBodiesIntersecting(BoundingBox box, List<Body> components, CollisionManager.CollisionType type)
        {
            HashSet<Body> set = new HashSet<Body>();
            CollisionManager.GetObjectsIntersecting(box, set, type);

            components.AddRange(set);
        }

        public void GetBodiesIntersecting(Ray ray, List<Body> components, CollisionManager.CollisionType type)
        {
            HashSet<Body> set = new HashSet<Body>();
            CollisionManager.GetObjectsIntersecting(ray, set, type);

            components.AddRange(set);
        }

        public List<Body> SelectRootBodiesOnScreen(Rectangle selectionRectangle, Camera camera)
        {
            /*
            return (from component in RootComponent.Children.OfType<Body>()
                    let screenPos = camera.Project(component.GlobalTransform.Translation)
                    where   screenPos.Z > 0 
                    && (selectionRectangle.Contains((int)screenPos.X, (int)screenPos.Y) || selectionRectangle.Intersects(component.GetScreenRect(camera))) 
                    && camera.GetFrustrum().Contains(component.GlobalTransform.Translation) != ContainmentType.Disjoint
                    && !WorldManager.ChunkManager.ChunkData.CheckOcclusionRay(camera.Position, component.Position)
                    select component).ToList();
             */
            if (WorldManager.SelectionBuffer == null)
            {
                return new List<Body>();
            }
            List<Body> toReturn = new List<Body>();
            foreach (uint id in WorldManager.SelectionBuffer.GetIDsSelected(selectionRectangle))
            {
                GameComponent component;
                if (!Components.TryGetValue(id, out component))
                {
                    continue;
                }
                toReturn.Add(component.GetRootComponent().GetComponent<Body>());
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
            if(!Components.ContainsKey(component.GlobalID))
            {
                return;
            }

            Components.Remove(component.GlobalID);

            List<GameComponent> children = component.GetAllChildrenRecursive();

            foreach(GameComponent child in children)
            {
                Components.Remove(child.GlobalID);
            }
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
            }
        }

        public void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if(RootComponent != null)
            {
                RootComponent.UpdateTransformsRecursive();
            }

            Factions.Update(gameTime);


            foreach(GameComponent component in Components.Values)
            {
                if(component.IsActive)
                {
                    component.Update(gameTime, chunks, camera);
                }

                if(component.IsDead)
                {
                    Removals.Add(component);
                    component.IsActive = false;
                    component.IsDead = true;
                    component.IsVisible = false;
                }
            }


            HandleAddRemoves();
        }

        public void HandleAddRemoves()
        {
            AdditionMutex.WaitOne();
            foreach(GameComponent component in Additions)
            {
                AddComponentImmediate(component);
            }

            Additions.Clear();
            AdditionMutex.ReleaseMutex();

            RemovalMutex.WaitOne();
            foreach(GameComponent component in Removals)
            {
                RemoveComponentImmediate(component);
            }

            Removals.Clear();
            RemovalMutex.ReleaseMutex();
        }
                
        private HashSet<Body> visibleComponents = new HashSet<Body>();
        private List<GameComponent> componentsToDraw = new List<GameComponent>();

        public bool RenderReflective(GameComponent component, float waterLevel)
        {
            var body = component as Body;
            if(body != null)
            {
                return body.GetBoundingBox().Min.Y > waterLevel - 2;
            }
            else
            {
                return true;
            }
        }

        public enum WaterRenderType
        {
            Reflective,
            None
        }

        public void RenderSelectionBuffer(DwarfTime time, ChunkManager chunks, Camera camera,
            SpriteBatch spriteBatch, GraphicsDevice graphics, Effect effect)
        {
            effect.CurrentTechnique = effect.Techniques["Selection"];
            foreach (GameComponent component in componentsToDraw)
            {
                if (component.IsVisible)
                    component.RenderSelectionBuffer(time, chunks, camera, spriteBatch, graphics, effect);
            }
        }

        public void Render(DwarfTime gameTime,
            ChunkManager chunks,
            Camera camera,
            SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice,
            Effect effect,
            WaterRenderType waterRenderMode, float waterLevel)
        {
            bool renderForWater = (waterRenderMode != WaterRenderType.None);

            if(!renderForWater)
            {
                visibleComponents = CollisionManager.GetVisibleObjects<Body>(camera.GetFrustrum(),
                    CollisionManager.CollisionType.Dynamic | CollisionManager.CollisionType.Static);

                componentsToDraw.Clear();
               
                Camera = camera;
                foreach(GameComponent component in Components.Values)
                {
                    bool isLocatable = component is Body;

                    if(isLocatable)
                    {
                        Body loc = (Body) component;


                        if(((loc.GlobalTransform.Translation - camera.Position).LengthSquared() < chunks.DrawDistanceSquared &&
                            visibleComponents.Contains(loc) || !(loc.FrustrumCull) || !(loc.WasAddedToOctree) && !loc.IsAboveCullPlane)
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

            foreach(GameComponent component in componentsToDraw)
            {
                if(waterRenderMode == WaterRenderType.Reflective && !RenderReflective(component, waterLevel))
                {
                    continue;
                }
                component.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderForWater);
            }

            effect.Parameters["xEnableLighting"].SetValue(0);
        }

        public static int CompareZDepth(Body A, Body B)
        {
            if(A == B)
            {
                return 0;
            }

            else if(A.Parent == B.Parent && A.DrawInFrontOfSiblings)
            {
                return 1;
            }
            else if(B.Parent == A.Parent && B.DrawInFrontOfSiblings)
            {
                return -1;
            }
            else if((Camera.Position - A.GlobalTransform.Translation).LengthSquared() < (Camera.Position - B.GlobalTransform.Translation).LengthSquared())
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        public uint GetMaxComponentID()
        {
            return Components.Aggregate<KeyValuePair<uint, GameComponent>, uint>(0, (current, component) => Math.Max(current, component.Value.GlobalID));
        }
    }

}