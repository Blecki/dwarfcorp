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
    public static class ComponentRenderer
    {
        public enum WaterRenderType
        {
            Reflective,
            None
        }

        public static void RenderSelectionBuffer(
            IEnumerable<IRenderableComponent> Renderables,
            DwarfTime time,
            ChunkManager chunks,
            Camera camera,
            SpriteBatch spriteBatch,
            GraphicsDevice graphics,
            Shader effect)
        {
            effect.CurrentTechnique = effect.Techniques["Selection"];
            foreach (var bodyToDraw in Renderables.OfType<Body>())
            {
                if (bodyToDraw.IsVisible)
                    bodyToDraw.RenderSelectionBuffer(time, chunks, camera, spriteBatch, graphics, effect);
            }
        }

        public static IEnumerable<IRenderableComponent> EnumerateVisibleRenderables(
            IEnumerable<IRenderableComponent> Renderables,
            ChunkManager chunks,
            Camera Camera)
        {
            var frustrum = Camera.GetFrustrum();

            var visibleComponents = Renderables.Where(r =>
            {
                if (!r.IsVisible) return false;
                if (r.IsAboveCullPlane(chunks)) return false;
                if (r.FrustrumCull)
                {
                    if ((r.GlobalTransform.Translation - Camera.Position).LengthSquared() >=
                        chunks.DrawDistanceSquared) return false;
                    if (!r.GetBoundingBox().Intersects(frustrum)) return false;
                }

                return true;
            }).ToList();

            // Need to identify transparent entities and draw them last. Alpha masked entities do not 
            //   count - they don't blend!
            //visibleComponents.Sort((A, B) =>
            //{
            //    if (A == B) return 0;
            //    return
            //    -(Camera.Position - A.GlobalTransform.Translation).LengthSquared()
            //        .CompareTo((Camera.Position - B.GlobalTransform.Translation).LengthSquared());
            //});

            return visibleComponents;
        }

        public static void Render(
            IEnumerable<IRenderableComponent> Renderables,
            DwarfTime gameTime,
            ChunkManager chunks,
            Camera Camera,
            SpriteBatch spriteBatch,
            GraphicsDevice graphicsDevice,
            Shader effect,
            WaterRenderType waterRenderMode, 
            float waterLevel)
        {
            effect.EnableLighting = GameSettings.Default.CursorLightEnabled;
            graphicsDevice.RasterizerState = RasterizerState.CullNone;

            if (waterRenderMode == WaterRenderType.Reflective)
            {
                foreach (IRenderableComponent bodyToDraw in Renderables)
                {
                    if (!(bodyToDraw.GetBoundingBox().Min.Y > waterLevel - 2))
                        continue;

                    bodyToDraw.Render(gameTime, chunks, Camera, spriteBatch, graphicsDevice, effect, true);
                }
            }
            else
            {
                foreach (IRenderableComponent bodyToDraw in Renderables)
                {
                    GamePerformance.Instance.StartTrackPerformance("Component Render - " + bodyToDraw.GetType().Name);
                    bodyToDraw.Render(gameTime, chunks, Camera, spriteBatch, graphicsDevice, effect, false);
                    GamePerformance.Instance.StopTrackPerformance("Component Render - " + bodyToDraw.GetType().Name);

                }
            }

            effect.EnableLighting = false;
        }
    }
}
