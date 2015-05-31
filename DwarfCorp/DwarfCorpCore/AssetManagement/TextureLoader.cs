// TextureLoader.cs
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
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp
{

    /// <summary>
    /// This is a helper class designed to help load textures from disk. Can load entire directories of textures.
    /// </summary>
    public class TextureLoader
    {
        public string Folder { get; set; }
        public string FileTypes { get; set; }
        public GraphicsDevice Graphics { get; set; }

        public class TextureFile
        {
            public Texture2D Texture;
            public string File;
            
            public TextureFile(Texture2D texture, string file)
            {
                Texture = texture;
                File = file;
            }
        }

        public TextureLoader(string folder, GraphicsDevice graphics)
        {
            string assemblyLocation = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + ProgramData.DirChar + "DwarfCorp";
            string relativePath = assemblyLocation + ProgramData.DirChar + folder;
            FileTypes = "*.png";
            Folder = relativePath;
            Graphics = graphics;
        }

        public List<TextureFile> GetTextures()
        {
            DirectoryInfo di = new DirectoryInfo(Folder);

            if(!di.Exists)
            {
                di.Create();
            }
            FileInfo[] files = di.GetFiles(FileTypes, SearchOption.TopDirectoryOnly);

            List<TextureFile> toReturn = new List<TextureFile>();
            foreach(FileInfo file in files)
            {
                Texture2D texture = null;
                FileStream stream = new FileStream(file.FullName, FileMode.Open);
                try
                {
                    texture = Texture2D.FromStream(Graphics, stream);
                }
                catch(IOException e)
                {
                    Console.Error.WriteLine(e.Message);
                    stream.Close();
                    continue;
                }

                if(texture != null)
                {
                    toReturn.Add(new TextureFile(texture, file.FullName));
                }
                stream.Close();
            }

            return toReturn;
        }
    }

}