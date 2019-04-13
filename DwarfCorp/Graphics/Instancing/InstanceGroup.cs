using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using LibNoise.Modifiers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{
    public abstract class InstanceGroup
    {
        public String Name;
        public abstract void RenderInstance(NewInstanceData Instance, GraphicsDevice Device, Shader Effect, Camera Camera, InstanceRenderMode Mode);
        public abstract void Flush(GraphicsDevice Device, Shader Effect, Camera Camera, InstanceRenderMode Mode);
        public abstract void Initialize();
    }
}