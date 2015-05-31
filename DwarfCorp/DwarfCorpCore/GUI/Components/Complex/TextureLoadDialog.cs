// TextureLoadDialog.cs
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// This GUI component displays a set of textures from a directory
    /// which can be loaded.
    /// </summary>
    public class TextureLoadDialog : GUIComponent
    {
        public TextureLoader TextureLoader { get; set; }
        public Texture2D DefaultTexture { get; set; }
        public List<TextureLoader.TextureFile> Textures { get; set; }
        public Label DirLabel { get; set; }

        public delegate void TextureSelected(TextureLoader.TextureFile arg);

        public event TextureSelected OnTextureSelected;
        public GridLayout Layout { get; set; }

        public TextureLoadDialog(DwarfGUI gui, GUIComponent parent, string directory, Texture2D image) :
            base(gui, parent)
        {
            Initialize(image, directory);
        }

        public void Initialize(Texture2D image, string directory)
        {
            DefaultTexture = image;
            TextureLoader = new TextureLoader(directory, GUI.Graphics);

            Textures = TextureLoader.GetTextures();
            TextureLoader.TextureFile defaultFile = new TextureLoader.TextureFile(DefaultTexture, "Default");
            Textures.Insert(0, defaultFile);

            OnTextureSelected += TextureLoadDialog_OnTextureSelected;

            int rc = Math.Max((int) (Math.Sqrt(Textures.Count)), 2);


            if(Layout == null)
            {
                Layout = new GridLayout(GUI, this, rc + 1, rc);
            }
            else
            {
                RemoveChild(Layout);
                Layout = new GridLayout(GUI, this, rc + 1, rc);
            }

            if(DirLabel == null)
            {
                Label dirLabel = new Label(GUI, Layout, "Images from: " + TextureLoader.Folder, GUI.DefaultFont);
                DirLabel = dirLabel;
            }
            else
            {
                DirLabel.Text = "Images from: " + TextureLoader.Folder;
                Layout.AddChild(DirLabel);
            }
            Layout.SetComponentPosition(DirLabel, 0, 0, 1, 1);

            for(int i = 0; i < Textures.Count; i++)
            {
                ImagePanel img = new ImagePanel(GUI, Layout, Textures[i].Texture);
                img.Highlight = true;
                img.KeepAspectRatio = true;
                int row = i / rc;
                int col = i % rc;
                TextureLoader.TextureFile texFile = Textures[i];
                img.OnClicked += delegate { img_OnClicked(texFile); };

                Layout.SetComponentPosition(img, col, row + 1, 1, 1);
            }
        }

        private void img_OnClicked(TextureLoader.TextureFile image)
        {
            OnTextureSelected.Invoke(image);
        }

        private void TextureLoadDialog_OnTextureSelected(TextureLoader.TextureFile arg)
        {
            //throw new NotImplementedException();
        }
    }

}