using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using System.Security;
using System.Security.Policy;
using System.Security.Permissions;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public static partial class TextureTool
    {
        public static Texture2D CropAndColorSprite(GraphicsDevice Device, Texture2D Sheet, Point FrameSize, Point Frame, Palette BasePalette, Palette Palette)
        {
            var sourceTex = MemoryTextureFromTexture2D(Sheet);
            var destTex = new MemoryTexture(FrameSize.X, FrameSize.Y);
            Blit(sourceTex, new Rectangle(FrameSize.X * Frame.X, FrameSize.Y * Frame.Y, FrameSize.X, FrameSize.Y), destTex, new Point(0, 0));
            var baseTex = DecomposeTexture(destTex, BasePalette);
            var recolored = ComposeTexture(baseTex, Palette);
            return Texture2DFromMemoryTexture(Device, recolored);
        }

        public static Texture2D CropSprite(GraphicsDevice Device, Texture2D Sheet, Point FrameSize, Point Frame)
        {
            // Same as above, except no palette swap.
            var sourceTex = MemoryTextureFromTexture2D(Sheet);
            var destTex = new MemoryTexture(FrameSize.X, FrameSize.Y);
            Blit(sourceTex, new Rectangle(FrameSize.X * Frame.X, FrameSize.Y * Frame.Y, FrameSize.X, FrameSize.Y), destTex, new Point(0, 0));
            return Texture2DFromMemoryTexture(Device, destTex);
        }
    }
}