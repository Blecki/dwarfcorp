using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class EngineModule
    {
        public virtual void Update(DwarfTime GameTime, WorldManager World) { }
        public virtual void ComponentCreated(GameComponent C) { }
        public virtual void ComponentDestroyed(GameComponent C) { }
        public virtual void Render(DwarfTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect) { }
    }
}
