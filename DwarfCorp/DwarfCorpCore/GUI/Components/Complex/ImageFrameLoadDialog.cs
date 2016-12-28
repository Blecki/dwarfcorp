// ImageFrameLoadDialog.cs
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
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    /// <summary>
    ///     This GUI component displays a set of textures from a directory
    ///     which can be loaded.
    /// </summary>
    public class ImageFrameLoadDialog : Dialog
    {
        public delegate void TextureSelected(NamedImageFrame arg);

        public ImageFrameLoadDialog(DwarfGUI gui, GUIComponent parent, List<SpriteSheet> sprites) :
            base(gui, parent)
        {
            Sprites = sprites;
        }

        public NamedImageFrame DefaultTexture { get; set; }
        public List<NamedImageFrame> Images { get; set; }
        public List<SpriteSheet> Sprites { get; set; }

        public GridLayout SpriteLayout { get; set; }
        public event TextureSelected OnTextureSelected;

        public static ImageFrameLoadDialog Popup(DwarfGUI gui, List<SpriteSheet> sprites)
        {
            int w = gui.Graphics.Viewport.Width - 128;
            int h = gui.Graphics.Viewport.Height - 128;
            var toReturn = new ImageFrameLoadDialog(gui, gui.RootComponent, sprites)
            {
                LocalBounds =
                    new Rectangle(gui.Graphics.Viewport.Width/2 - w/2, gui.Graphics.Viewport.Height/2 - h/2, w, h)
            };
            toReturn.Initialize(ButtonType.Cancel, "Select Image", "");

            return toReturn;
        }

        public override void Initialize(ButtonType buttons, string title, string message)
        {
            base.Initialize(buttons, title, message);
            Initialize(Sprites);
        }

        public void Initialize(List<SpriteSheet> sprites)
        {
            Images = new List<NamedImageFrame>();
            foreach (SpriteSheet sprite in sprites)
            {
                Images.AddRange(sprite.GenerateFrames());
            }

            if (Images.Count > 0)
            {
                DefaultTexture = Images.First();
            }

            OnTextureSelected += TextureLoadDialog_OnTextureSelected;

            int rc = Math.Max((int) (Math.Round(Math.Sqrt(Images.Count) + 0.5f)), 2);

            if (SpriteLayout == null)
            {
                SpriteLayout = new GridLayout(GUI, Layout, rc, rc)
                {
                    WidthSizeMode = SizeMode.Fixed,
                    HeightSizeMode = SizeMode.Fixed
                };
            }
            else
            {
                RemoveChild(SpriteLayout);
                SpriteLayout = new GridLayout(GUI, Layout, rc, rc)
                {
                    WidthSizeMode = SizeMode.Fixed,
                    HeightSizeMode = SizeMode.Fixed
                };
            }

            Layout.SetComponentPosition(SpriteLayout, 0, 1, 4, 2);
            Layout.UpdateSizes();

            for (int i = 0; i < Images.Count; i++)
            {
                var img = new ImagePanel(GUI, SpriteLayout, Images[i])
                {
                    Highlight = true,
                    KeepAspectRatio = true,
                    AssetName = Images[i].AssetName,
                    ConstrainSize = true
                };
                int row = i/rc;
                int col = i%rc;
                NamedImageFrame texFile = Images[i];
                img.OnClicked += () => img_OnClicked(texFile);

                SpriteLayout.SetComponentPosition(img, col, row, 1, 1);
            }
        }

        private void img_OnClicked(NamedImageFrame image)
        {
            OnTextureSelected.Invoke(image);
            Close(ReturnStatus.Ok);
        }

        private void TextureLoadDialog_OnTextureSelected(NamedImageFrame arg)
        {
            //throw new NotImplementedException();
        }
    }
}