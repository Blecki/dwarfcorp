using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.Gui.TextureAtlas
{
    public class SpriteAtlasEntry
    {
        public TileSheetDefinition SourceDefinition;
        public Texture2D SourceTexture;

        public Rectangle AtlasBounds;
        public ITileSheet TileSheet;
        public bool NeedsBlitToAtlas = false;
        public int ReferenceCount = 0;


        public void ReplaceTexture(Texture2D NewTexture)
        {
            SourceTexture = NewTexture;
            NeedsBlitToAtlas = true;
        }

        public void Discard()
        {
            ReferenceCount -= 1;
        }
    }
}
