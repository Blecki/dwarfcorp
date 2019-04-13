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
            IEnumerable<Body> Renderables,
            DwarfTime time,
            ChunkManager chunks,
            Camera camera,
            SpriteBatch spriteBatch,
            GraphicsDevice graphics,
            Shader effect)
        {
            effect.CurrentTechnique = effect.Techniques["Selection"];
            foreach (var bodyToDraw in Renderables.OfType<Body>()) // Why does RenderSelectionBuffer belong to Body and not IRenderable?
                bodyToDraw.RenderSelectionBuffer(time, chunks, camera, spriteBatch, graphics, effect);
        }

        public static void Render(
            IEnumerable<Body> Renderables,
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
                foreach (var bodyToDraw in Renderables)
                {
                    if (!(bodyToDraw.GetBoundingBox().Min.Y > waterLevel - 2))
                        continue;

                    bodyToDraw.Render(gameTime, chunks, Camera, spriteBatch, graphicsDevice, effect, true);
                }
            }
            else
            {
                foreach (var bodyToDraw in Renderables)
                    bodyToDraw.Render(gameTime, chunks, Camera, spriteBatch, graphicsDevice, effect, false);
            }

            effect.EnableLighting = false;
        }
    }
}
