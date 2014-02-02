using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        public LocatableComponent RootComponent { get; set; }

        private static Camera Camera { get; set; }

        [JsonIgnore]
        public Mutex AdditionMutex { get; set; }
        [JsonIgnore]
        public Mutex RemovalMutex { get; set; }

        public  ParticleManager ParticleManager { get; set; }
        public CollisionManager CollisionManager { get; set; }

        public FactionLibrary Factions { get; set; }

        public ComponentManager()
        {
            Components = new Dictionary<uint, GameComponent>();
            Removals = new List<GameComponent>();
            Additions = new List<GameComponent>();
            Camera = null;
            AdditionMutex = new Mutex();
            RemovalMutex = new Mutex();
            Factions = new FactionLibrary();
            Factions.Initialize();
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

        public bool IsUnderMouse(LocatableComponent component, MouseState mouse, Camera camera, Viewport viewPort)
        {
            List<LocatableComponent> viewable = new List<LocatableComponent>();
            Vector3 pos1 = viewPort.Unproject(new Vector3(mouse.X, mouse.Y, 0), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 pos2 = viewPort.Unproject(new Vector3(mouse.X, mouse.Y, 1), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 dir = Vector3.Normalize(pos2 - pos1);

            Ray toCast = new Ray(pos1, dir);

            return component.Intersects(toCast);
        }


        public void GetComponentsUnderMouse(MouseState mouse, Camera camera, Viewport viewPort, List<LocatableComponent> components)
        {
            Vector3 pos1 = viewPort.Unproject(new Vector3(mouse.X, mouse.Y, 0), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 pos2 = viewPort.Unproject(new Vector3(mouse.X, mouse.Y, 1), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 dir = Vector3.Normalize(pos2 - pos1);

            Ray toCast = new Ray(pos1, dir);
            HashSet<LocatableComponent> set = new HashSet<LocatableComponent>();
            CollisionManager.GetObjectsIntersecting(toCast, set, CollisionManager.CollisionType.Dynamic | CollisionManager.CollisionType.Static);

            components.AddRange(set);
        }

        public bool IsVisibleToCamera(LocatableComponent component, Camera camera)
        {
            BoundingFrustum frustrum = new BoundingFrustum(camera.ViewMatrix * camera.ProjectionMatrix);
            return (component.Intersects(frustrum));
        }

        public void GetComponentsVisibleToCamera(Camera camera, List<LocatableComponent> components)
        {
            BoundingFrustum frustrum = new BoundingFrustum(camera.ViewMatrix * camera.ProjectionMatrix);
            GetComponentsIntersecting(frustrum, components, CollisionManager.CollisionType.Dynamic | CollisionManager.CollisionType.Static);
        }

        public void GetComponentsInVisibleToCamera(Camera camera, List<LocatableComponent> components)
        {
            BoundingFrustum frustrum = new BoundingFrustum(camera.ViewMatrix * camera.ProjectionMatrix);

            foreach(GameComponent c in Components.Values)
            {
                if(c is LocatableComponent && !((LocatableComponent) c).Intersects(frustrum))
                {
                    components.Add((LocatableComponent) c);
                }
            }
        }

        public void GetComponentsIntersecting(BoundingSphere sphere, List<LocatableComponent> components, CollisionManager.CollisionType type)
        {
            HashSet<LocatableComponent> set = new HashSet<LocatableComponent>();
            CollisionManager.GetObjectsIntersecting(sphere, set, type);

            components.AddRange(set);
        }

        public void GetComponentsIntersecting(BoundingFrustum frustrum, List<LocatableComponent> components, CollisionManager.CollisionType type)
        {
            HashSet<LocatableComponent> set = new HashSet<LocatableComponent>();
            CollisionManager.GetObjectsIntersecting(frustrum, set, type);

            components.AddRange(set);
        }

        public void GetComponentsIntersecting(BoundingBox box, List<LocatableComponent> components, CollisionManager.CollisionType type)
        {
            HashSet<LocatableComponent> set = new HashSet<LocatableComponent>();
            CollisionManager.GetObjectsIntersecting(box, set, type);

            components.AddRange(set);
        }

        public void GetComponentsIntersecting(Ray ray, List<LocatableComponent> components, CollisionManager.CollisionType type)
        {
            HashSet<LocatableComponent> set = new HashSet<LocatableComponent>();
            CollisionManager.GetObjectsIntersecting(ray, set, type);

            components.AddRange(set);
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
            Components.Remove(component.GlobalID);

            List<GameComponent> children = component.GetAllChildrenRecursive();

            foreach(GameComponent child in children)
            {
                Components.Remove(child.GlobalID);
            }
        }

        private void AddComponentImmediate(GameComponent component)
        {
            Components[component.GlobalID] = component;
        }

        public void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            if(RootComponent != null)
            {
                RootComponent.UpdateTransformsRecursive();
            }

            Factions.Update(gameTime);

            List<GameComponent> Removals = new List<GameComponent>();

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


        public List<LocatableComponent> FrustrumCullLocatableComponents(Camera camera)
        {
            List<LocatableComponent> visible = CollisionManager.GetVisibleObjects<LocatableComponent>(camera.GetFrustrum(), CollisionManager.CollisionType.Dynamic | CollisionManager.CollisionType.Static);
            //CollisionManager.GetObjectsIntersecting(camera.GetFrustrum(), visible, CollisionManager.CollisionType.Dynamic | CollisionManager.CollisionType.Static);

            return visible;
        }


        private HashSet<LocatableComponent> visibleComponents = new HashSet<LocatableComponent>();
        private List<GameComponent> componentsToDraw = new List<GameComponent>();

        public bool RenderRefractive(GameComponent component, float waterLevel)
        {
            if(component is LocatableComponent)
            {
                return ((LocatableComponent) component).GetBoundingBox().Min.Y < waterLevel + 2;
            }
            else
            {
                return true;
            }
        }

        public bool RenderReflective(GameComponent component, float waterLevel)
        {
            if(component is LocatableComponent)
            {
                return ((LocatableComponent) component).GetBoundingBox().Min.Y > waterLevel - 2;
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

        public void Render(GameTime gameTime,
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
                
                
                List<LocatableComponent> list = FrustrumCullLocatableComponents(camera);
                foreach(LocatableComponent component in list)
                {
                    visibleComponents.Add(component);
                }
                 

                ComponentManager.Camera = camera;
                foreach(GameComponent component in Components.Values)
                {
                    bool isLocatable = component is LocatableComponent;

                    if(isLocatable)
                    {
                        LocatableComponent loc = (LocatableComponent) component;


                        if(((loc.GlobalTransform.Translation - camera.Position).LengthSquared() < chunks.DrawDistanceSquared &&
                            visibleComponents.Contains(loc) || !(loc.FrustrumCull) || !(loc.WasAddedToOctree))
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


            effect.Parameters["xEnableLighting"].SetValue(GameSettings.Default.CursorLightEnabled);
            effect.Parameters["xLightColor"].SetValue(new Vector4(0, 0, 1, 0));
            effect.Parameters["xLightPos"].SetValue(PlayState.CursorLightPos);


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


                component.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderForWater);
            }

            effect.Parameters["xEnableLighting"].SetValue(false);
        }

        public static int CompareZDepth(LocatableComponent A, LocatableComponent B)
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
    }

}