using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System.Threading;

namespace DwarfCorp
{
    public class ComponentManager
    {
        public Dictionary<uint, GameComponent> Components { get; set; }
        private List<GameComponent> Removals { get; set; }
        private List<GameComponent> Additions { get; set; }
        public LocatableComponent RootComponent { get; set; }
        private static Camera m_camera { get; set; }
        public Mutex AdditionMutex { get; set; }
        public Mutex RemovalMutex { get; set; }

        public ComponentManager()
        {
            Components = new Dictionary<uint, GameComponent>();
            Removals = new List<GameComponent>();
            Additions = new List<GameComponent>();
            m_camera = null;
            AdditionMutex = new Mutex();
            RemovalMutex = new Mutex();
        }


        #region picking

        public static List<T> FilterComponentsWithoutTag<T>(string tag, List<T> toFilter) where T : GameComponent
        {
            List<T> toReturn = new List<T>();

            foreach (T component in toFilter)
            {
                if (!component.Tags.Contains(tag))
                {
                    toReturn.Add(component);
                }
            }

            return toReturn;
        }

        public static List<T> FilterComponentsWithTag<T>(string tag, List<T> toFilter) where T : GameComponent
        {
            List<T> toReturn = new List<T>();

            foreach(T component in toFilter)
            {
                if(component.Tags.Contains(tag))
                {
                    toReturn.Add(component);
                }
            }

            return toReturn;
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


        public void GetComponentsUnderMouse(MouseState mouse,  Camera camera, Viewport viewPort, List<LocatableComponent> components)
        {

            Vector3 pos1 = viewPort.Unproject(new Vector3(mouse.X, mouse.Y, 0), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 pos2 = viewPort.Unproject(new Vector3(mouse.X, mouse.Y, 1), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            Vector3 dir = Vector3.Normalize(pos2 - pos1);

            Ray toCast = new Ray(pos1, dir);
            HashSet<LocatableComponent> set = new HashSet<LocatableComponent>();
            LocatableComponent.m_octree.Root.GetComponentsIntersecting<LocatableComponent>(toCast, set);

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
            GetComponentsIntersecting(frustrum, components);
        }

        public void GetComponentsInVisibleToCamera(Camera camera, List<LocatableComponent> components)
        {
            BoundingFrustum frustrum = new BoundingFrustum(camera.ViewMatrix * camera.ProjectionMatrix);

            foreach (GameComponent c in Components.Values)
            {
                if (c is LocatableComponent && !((LocatableComponent)c).Intersects(frustrum))
                {
                    components.Add((LocatableComponent)c);
                }
            }
        }

        public void GetComponentsIntersecting(BoundingSphere sphere, List<LocatableComponent> components)
        {
            HashSet<LocatableComponent> set = new HashSet<LocatableComponent>();
            LocatableComponent.m_octree.Root.GetComponentsIntersecting<LocatableComponent>(sphere, set);

            components.AddRange(set);
        }

        public void GetComponentsIntersecting(BoundingFrustum frustrum, List<LocatableComponent> components)
        {
            HashSet<LocatableComponent> set = new HashSet<LocatableComponent>();
            LocatableComponent.m_octree.Root.GetComponentsIntersecting<LocatableComponent>(frustrum, set);

            components.AddRange(set);
        }

        public void GetComponentsIntersecting(BoundingBox box, List<LocatableComponent> components)
        {
            HashSet<LocatableComponent> set = new HashSet<LocatableComponent>();
            LocatableComponent.m_octree.Root.GetComponentsIntersecting(box, set);

            components.AddRange(set);
        }

        public void GetComponentsIntersecting(Ray ray, List<LocatableComponent> components)
        {
            HashSet<LocatableComponent> set = new HashSet<LocatableComponent>();
            LocatableComponent.m_octree.Root.GetComponentsIntersecting<LocatableComponent>(ray, set);

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

            foreach (GameComponent child in children)
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

            if (RootComponent != null)
            {
                RootComponent.UpdateTransformsRecursive();
            }

            List<GameComponent> Removals = new List<GameComponent>();

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
                }
            }


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

      
        public HashSet<LocatableComponent> FrustrumCullLocatableComponents(Camera camera)
        {
            HashSet<LocatableComponent> visible = new HashSet<LocatableComponent>();
            LocatableComponent.m_octree.Root.GetComponentsIntersecting<LocatableComponent>(camera.GetFrustrum(), visible);

            return visible;
        }


        HashSet<LocatableComponent> visibleComponents = new HashSet<LocatableComponent>();
        List<GameComponent> componentsToDraw = new List<GameComponent>();

        public bool RenderRefractive(GameComponent component, float waterLevel)
        {
            if (component is LocatableComponent)
            {
                return ((LocatableComponent)component).GetBoundingBox().Min.Y < waterLevel + 2;
            }
            else
            {
                return true;
            }
        }

        public bool RenderReflective(GameComponent component, float waterLevel)
        {
            if (component is LocatableComponent)
            {
                return ((LocatableComponent)component).GetBoundingBox().Min.Y > waterLevel - 2;
            }
            else
            {
                return true;
            }
        }

        public enum WaterRenderType
        {
            Reflective,
            Refractive, None
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

            if (!renderForWater)
            {
                visibleComponents.Clear();
                componentsToDraw.Clear();
                visibleComponents = FrustrumCullLocatableComponents(camera);

                m_camera = camera;
                foreach (GameComponent component in Components.Values)
                {
                    bool isLocatable = component is LocatableComponent;

                    if (isLocatable)
                    {
                        LocatableComponent loc = (LocatableComponent)component;


                        if (((loc.GlobalTransform.Translation - camera.Position).LengthSquared() < chunks.DrawDistanceSquared &&
                            (visibleComponents.Contains(component) || visibleComponents.Contains(component.Parent as LocatableComponent)) || !(loc.FrustrumCull) || !(loc.WasAddedToOctree))
                            && loc.BoundingBox.Min.Y < chunks.MaxViewingLevel + 3)
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
           


            foreach (GameComponent component in componentsToDraw)
            {
                if(waterRenderMode == WaterRenderType.Reflective && !RenderReflective(component, waterLevel))
                {
                    continue;
                }
                else if (waterRenderMode == WaterRenderType.Refractive && !RenderRefractive(component, waterLevel))
                {
                    continue;
                }


                component.Render(gameTime, chunks, camera, spriteBatch, graphicsDevice, effect, renderForWater);
            }

            effect.Parameters["xEnableLighting"].SetValue(false);


        }

        public static int CompareZDepth(LocatableComponent A, LocatableComponent B)
        {

            if (A == B)
            {
                return 0;
            }

            else if (A.Parent == B.Parent && A.DrawInFrontOfSiblings)
            {
                return 1;
            }
            else if (B.Parent == A.Parent && B.DrawInFrontOfSiblings)
            {
                return -1;
            }
            else if ((m_camera.Position - A.GlobalTransform.Translation).LengthSquared() < (m_camera.Position - B.GlobalTransform.Translation).LengthSquared())
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
