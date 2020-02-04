using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using DwarfCorp.Gui;
using System.Linq;

namespace DwarfCorp.Gui.Widgets
{
    public class EmployeePortrait : Widget
    {
        public DwarfSprites.LayerStack Sprite;
        public AnimationPlayer AnimationPlayer;
        private TextureAtlas.SpriteAtlasEntry DynamicAtlasEntry = null;

        public override void Construct()
        {
            Root.RegisterForUpdate(this);
            base.Construct();

            this.OnUpdate = (sender, time) =>
            {
                if (Hidden || Transparent)
                    return;

                if (IsAnyParentHidden())
                    return;

                if (Sprite == null)
                    return;

                var texture = Sprite.GetCompositeTexture();
                if (texture != null)
                {
                    var sheet = new SpriteSheet(texture, 48, 40);
                    var frame = AnimationPlayer.GetCurrentAnimation().Frames[AnimationPlayer.CurrentFrame];
                    if (DynamicAtlasEntry == null)
                    {
                        var tex = new Texture2D(Root.RenderData.Device, 48, 40);
                        DynamicAtlasEntry = Root.SpriteAtlas.AddDynamicSheet(null,
                            new TileSheetDefinition
                            {
                                TileHeight = 40,
                                TileWidth = 48,
                                RepeatWhenUsedAsBorder = false,
                                Type = TileSheetType.TileSheet
                            }, 
                            tex);
                    }

                    var memTex = TextureTool.MemoryTextureFromTexture2D(texture, new Rectangle(frame.X * 48, frame.Y * 40, 48, 40));
                    DynamicAtlasEntry.ReplaceTexture(TextureTool.Texture2DFromMemoryTexture(Root.RenderData.Device, memTex));
                }

                this.Invalidate();
            };

            this.OnClose = (sender) =>
            {
                if (DynamicAtlasEntry != null)
                    DynamicAtlasEntry.Discard();
            };
        }

        protected override Mesh Redraw()
        {
            var r = base.Redraw();
            if (DynamicAtlasEntry != null)
                r.QuadPart()
                    .Scale(Rect.Width, Rect.Height)
                    .Translate(Rect.X, Rect.Y)
                    .Texture(DynamicAtlasEntry.TileSheet.TileMatrix(0));
            return r;
        }
    }
}
