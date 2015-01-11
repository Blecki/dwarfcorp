﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using DwarfCorpCore;
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
        public CollisionManager CollisionManager { get; set; }

        public FactionLibrary Factions { get; set; }


        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            AdditionMutex = new Mutex();
            RemovalMutex = new Mutex();
            Removals = new List<GameComponent>();
            Additions = new List<GameComponent>();
        }
       

        public ComponentManager()
        {
            
        }

        public ComponentManager(PlayState state, string companyName, string companyMotto, NamedImageFrame companyLogo, Color companyColor)
        {
            Components = new Dictionary<uint, GameComponent>();
            Removals = new List<GameComponent>();
            Additions = new List<GameComponent>();
            Camera = null;
            AdditionMutex = new Mutex();
            RemovalMutex = new Mutex();
            Factions = new FactionLibrary();
            Factions.Initialize(state, companyName, companyMotto, companyLogo, companyColor);
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
            return (from component in RootComponent.Children.OfType<Body>()
                    let screenPos = camera.Project(component.GlobalTransform.Translation)
                    where   screenPos.Z > 0 
                    && (selectionRectangle.Contains((int)screenPos.X, (int)screenPos.Y) || selectionRectangle.Intersects(component.GetScreenRect(camera))) 
                    && camera.GetFrustrum().Contains(component.GlobalTransform.Translation) != ContainmentType.Disjoint
                    select component).ToList();
        }

        public List<Body> SelectAllBodiesOnScreen(Rectangle selectionRectangle, Camera camera)
        {
            return (from component in Components.Values.OfType<Body>()
                    let screenPos = camera.Project(component.GlobalTransform.Translation)
                    where selectionRectangle.Contains((int)screenPos.X, (int)screenPos.Y) || selectionRectangle.Intersects(component.GetScreenRect(camera)) && screenPos.Z > 0
                    select component).ToList();
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

        public void Update(DwarfTime DwarfTime, ChunkManager chunks, Camera camera)
        {
            if(RootComponent != null)
            {
                RootComponent.UpdateTransformsRecursive();
            }

            Factions.Update(DwarfTime);


            foreach(GameComponent component in Components.Values)
            {
                if(component.IsActive)
                {
                    component.Update(DwarfTime, chunks, camera);
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


        public List<Body> FrustrumCullLocatableComponents(Camera camera)
        {
            List<Body> visible = CollisionManager.GetVisibleObjects<Body>(camera.GetFrustrum(), CollisionManager.CollisionType.Dynamic | CollisionManager.CollisionType.Static);
              
            return visible;
        }


        private HashSet<Body> visibleComponents = new HashSet<Body>();
        private List<GameComponent> componentsToDraw = new List<GameComponent>();

        public bool RenderRefractive(GameComponent component, float waterLevel)
        {
            if(component is Body)
            {
                return ((Body) component).GetBoundingBox().Min.Y < waterLevel + 2;
            }
            else
            {
                return true;
            }
        }

        public bool RenderReflective(GameComponent component, float waterLevel)
        {
            if(component is Body)
            {
                return ((Body) component).GetBoundingBox().Min.Y > waterLevel - 2;
            }
            else
            {
                return true;
            }
        }

        public enum WaterRenderType
        {
            Reflective,
            Refractive,
            None
        }

        public void Render(DwarfTime DwarfTime,
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
                visibleComponents.Clear();
                componentsToDraw.Clear();
                
                
                List<Body> list = FrustrumCullLocatableComponents(camera);
                foreach(Body component in list)
                {
                    visibleComponents.Add(component);
                }
                 

                ComponentManager.Camera = camera;
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
                else if(waterRenderMode == WaterRenderType.Refractive && !RenderRefractive(component, waterLevel))
                {
                    continue;
                }


                component.Render(DwarfTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderForWater);
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