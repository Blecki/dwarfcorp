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
        private Gui.Mesh SpriteMesh;
        public DwarfSprites.LayerStack Sprite;
        public AnimationPlayer AnimationPlayer;
        private TextureAtlas.Entry DynamicAtlasEntry = null;

        public override void Construct()
        {
            Root.RegisterForPostdraw(this);
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

                if (SpriteMesh == null)
                    Layout();

                var texture = Sprite.GetCompositeTexture();
                if (texture != null)
                {
                    var sheet = new SpriteSheet(texture, 48, 40);
                    var frame = AnimationPlayer.GetCurrentAnimation().Frames[AnimationPlayer.CurrentFrame];
                    if (DynamicAtlasEntry == null)
                    {
                        var tex = new Texture2D(Root.RenderData.Device, 48, 40);
                        DynamicAtlasEntry = Root.RenderData.AddDynamicSheet(new JsonTileSheet
                        {
                            TileHeight = 40,
                            TileWidth = 48,
                            RepeatWhenUsedAsBorder = false,
                            Type = JsonTileSheetType.TileSheet
                        }, tex);

                        Root.InvalidateRenderData();

                    }

                    var memTex = TextureTool.MemoryTextureFromTexture2D(texture, new Rectangle(frame.X * 48, frame.Y * 40, 48, 40));
                    DynamicAtlasEntry.ReplaceTexture(TextureTool.Texture2DFromMemoryTexture(Root.RenderData.Device, memTex));
                }

                this.Invalidate();
            };

            // Todo: Cleanup the dynamic tile sheet!
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

        public override void Layout()
        {
            base.Layout();
            SpriteMesh = Mesh.EmptyMesh();
            SpriteMesh.QuadPart()
                .Scale(Rect.Width, Rect.Height)
                .Translate(Rect.X, Rect.Y);
        }
       
        public override void PostDraw(GraphicsDevice device)
        {
            if (Hidden || Transparent)
                return;

            if (IsAnyParentHidden())
                return;

            if (Sprite == null)
                return;

            if (SpriteMesh == null)
                Layout();

            var texture = Sprite.GetCompositeTexture();
            if (texture != null)
            {
                var sheet = new SpriteSheet(texture, 48, 40);
                var frame = AnimationPlayer.GetCurrentAnimation().Frames[AnimationPlayer.CurrentFrame];
                SpriteMesh.EntireMeshAsPart().ResetQuadTexture().Texture(sheet.TileMatrix(frame.X, frame.Y));
                //Root.DrawMesh(SpriteMesh, texture);
            }

            base.PostDraw(device);
        }
    }

}
