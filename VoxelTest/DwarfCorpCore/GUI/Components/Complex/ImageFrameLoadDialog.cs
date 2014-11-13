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
    public class ImageFrameLoadDialog : Dialog
    {
        public NamedImageFrame DefaultTexture { get; set; }
        public List<NamedImageFrame> Images { get; set; }
        public List<SpriteSheet> Sprites { get; set; } 
        public delegate void TextureSelected(NamedImageFrame arg);
        public event TextureSelected OnTextureSelected;
        public GridLayout SpriteLayout { get; set; }

        public static ImageFrameLoadDialog Popup(DwarfGUI gui, List<SpriteSheet> sprites)
        {
            int w = gui.Graphics.Viewport.Width - 128;
            int h = gui.Graphics.Viewport.Height - 128;
            ImageFrameLoadDialog toReturn = new ImageFrameLoadDialog(gui, gui.RootComponent, sprites)
            {
                LocalBounds =
                    new Rectangle(gui.Graphics.Viewport.Width / 2 - w / 2, gui.Graphics.Viewport.Height / 2 - h / 2, w, h)
            };
            toReturn.Initialize(ButtonType.Cancel, "Select Image", "");

            return toReturn;
        }

        public ImageFrameLoadDialog(DwarfGUI gui, GUIComponent parent, List<SpriteSheet> sprites) :
            base(gui, parent)
        {
            Sprites = sprites;
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

            int rc = Math.Max((int)(Math.Round(Math.Sqrt(Images.Count) + 0.5f)), 2);

            if (SpriteLayout == null)
            {
                SpriteLayout = new GridLayout(GUI, Layout, rc, rc) {WidthSizeMode = SizeMode.Fixed, HeightSizeMode = SizeMode.Fixed};
            }
            else
            {
                RemoveChild(SpriteLayout);
                SpriteLayout = new GridLayout(GUI, Layout, rc, rc) { WidthSizeMode = SizeMode.Fixed, HeightSizeMode = SizeMode.Fixed };
            }

            Layout.SetComponentPosition(SpriteLayout, 0, 1, 4, 2);
            Layout.UpdateSizes();

            for (int i = 0; i < Images.Count; i++)
            {
                ImagePanel img = new ImagePanel(GUI, SpriteLayout, Images[i]) { Highlight = true, KeepAspectRatio = true, AssetName = Images[i].AssetName, ConstrainSize = true};
                int row = i / rc;
                int col = i % rc;
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