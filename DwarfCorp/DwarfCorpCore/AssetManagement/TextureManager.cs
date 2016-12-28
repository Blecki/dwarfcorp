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

using System.Collections.Generic;
using System.IO;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    ///     This class exists to provide an abstract interface between asset tags and textures.
    ///     Technically, the ContentManager already does this for XNA, but ContentManager is missing a
    ///     couple of important functions: modability, and storing the *inverse* lookup between tag
    ///     and texture. Additionally, the TextureManager provides an interface to directly load
    ///     resources from the disk (rather than going through XNAs content system)
    /// </summary>
    public class TextureManager
    {
        private static bool staticsInitialized;

        public TextureManager(ContentManager content, GraphicsDevice graphics)
        {
            Content = content;
            Graphics = graphics;
            AssetMap = new Dictionary<Texture2D, string>();
            if (!staticsInitialized)
            {
                InitializeStatics();
                Instance = this;
            }
        }

        public static Dictionary<Texture2D, string> AssetMap { get; set; }

        public ContentManager Content { get; set; }
        public GraphicsDevice Graphics { get; set; }

        public static TextureManager Instance { get; set; }


        public static void InitializeStatics()
        {
            staticsInitialized = true;
        }

        public static Texture2D GetTexture(string asset)
        {
            Texture2D toReturn = Instance.GetInstanceTexture(asset);
            return toReturn;
        }

        public static Texture2D LoadTexture(string asset)
        {
            Texture2D toReturn = LoadInstanceTexture(asset);
            return toReturn;
        }

        public Texture2D GetInstanceTexture(string asset)
        {
            var toReturn = Content.Load<Texture2D>(asset);
            AssetMap[toReturn] = asset;
            return toReturn;
        }

        public static Texture2D LoadInstanceTexture(string file)
        {
            Texture2D texture = null;
            var stream = new FileStream(file, FileMode.Open);
            texture = Texture2D.FromStream(GameState.Game.GraphicsDevice, stream);
            stream.Close();
            AssetMap[texture] = file;
            return texture;
        }
    }
}