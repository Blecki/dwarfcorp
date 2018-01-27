// TextureManager.cs
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
using System.IO;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    /// <summary>
    /// This class exists to provide an abstract interface between asset tags and textures. 
    /// Technically, the ContentManager already does this for XNA, but ContentManager is missing a
    /// couple of important functions: modability, and storing the *inverse* lookup between tag
    /// and texture. Additionally, the TextureManager provides an interface to directly load
    /// resources from the disk (rather than going through XNAs content system)
    /// </summary>
    public class TextureManager
    {
        //Todo - don't need the instance nonsense.

        private static Dictionary<String, Texture2D> TextureCache = new Dictionary<string, Texture2D>();
        private static ContentManager Content;
        private static GraphicsDevice Graphics;

        public static void Initialize(ContentManager Content, GraphicsDevice Graphics)
        {
            TextureManager.Content = Content;
            TextureManager.Graphics = Graphics;

        }

        public static string ReverseLookup(Texture2D Texture)
        {
            var r = TextureCache.Where(p => p.Value == Texture).Select(p => p.Key).FirstOrDefault();
            if (r == null) return "";
            return r;
        }
        
        public static Texture2D GetContentTexture(string asset)
        {
            if (TextureCache.ContainsKey(asset))
                return TextureCache[asset];

            try
            {
                Texture2D toReturn = Content.Load<Texture2D>(asset);
                TextureCache[asset] = toReturn;
                return toReturn;
            }
            catch (ContentLoadException exception)
            {
                Console.Error.WriteLine(exception.ToString());
                var r = Content.Load<Texture2D>("newgui/error");
                TextureCache[asset] = r;
                return r;
            }

        }

        public static Texture2D LoadUnbuiltTextureFromAbsolutePath(string file)
        {
            using(var stream = new FileStream(file, FileMode.Open))
            {
                if (!stream.CanRead)
                {
                    Console.Out.WriteLine("Failed to read {0}, stream cannot be read.", file);
                    return null;
                }

                try
                {
                    return Texture2D.FromStream(GameState.Game.GraphicsDevice, stream);
                }
                catch (Exception exception)
                {
                    Console.Out.Write("Failed to load texture {0}: {1}", file, exception.ToString());
                    return null;
                }
           }
        }
    }

}