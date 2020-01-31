using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp.Gui
{
    /// <summary>
    /// Encapsulates rendering data that GUI instances can share. This data is expensive to create
    /// so we only want to do it once.
    /// </summary>
    public class RenderData : IDisposable
    {
        public GraphicsDevice Device { get { return GameStates.GameState.Game.GraphicsDevice; } }

        public Point ActualScreenBounds
        {
            get
            {
                if (Device != null)
                    return new Point(Device.Viewport.Width, Device.Viewport.Height);
                throw new InvalidOperationException("Graphics device was null.");
            }
        }

        public Effect Effect { get; private set; }
        public Texture2D Texture { get; private set; }
        public Dictionary<String, ITileSheet> TileSheets { get; private set; }
        public Rectangle VirtualScreen { get; private set; }
        public Rectangle RealScreen { get; private set; }
        public int ScaleRatio { get { return CalculateScale(); } }


        private bool AtlasValid = false;
        private List<JsonTileSheet> CoreSheets = null;
        private Dictionary<string, Func<GraphicsDevice, ContentManager, JsonTileSheet, Texture2D>> SheetGenerators = null;

        private class DynamicTileSheet
        {
            public Texture2D Texture;
            public JsonTileSheet Sheet;
        }

        private List<DynamicTileSheet> DynamicSheets = new List<DynamicTileSheet>();

        public int CalculateScale()
        {
            if (!DwarfCorp.GameSettings.Current.GuiAutoScale)
                return DwarfCorp.GameSettings.Current.GuiScale;

            float scaleX = ActualScreenBounds.X/1920.0f;
            float scaleY = ActualScreenBounds.Y/1080.0f;
            float maxScale = Math.Max(scaleX, scaleY);
            var scale = MathFunctions.Clamp((int)Math.Ceiling(maxScale), 1, 10);
            GameSettings.Current.GuiScale = scale;
            return scale;
        }

        public RenderData(GraphicsDevice Device, ContentManager Content)
        {
            this.Effect = Content.Load<Effect>(ContentPaths.GUI.Shader);
            CalculateScreenSize();

            CoreSheets = FileUtils.LoadJsonListFromMultipleSources<JsonTileSheet>(ContentPaths.GUI.Skin, null, (s) => s.Name);
            SheetGenerators = FindGenerators();

            Prerender(Content);
        }

        // This should return some kind of dynamic sheet handle that can be used to render with it, and to switch out it's texture.
        // - Employee Icon will be first to use this, and will replace the texture data //without// having to relayout the sheet.
        public void AddDynamicSheet(JsonTileSheet Sheet, Texture2D Texture)
        {
            DynamicSheets.Add(new DynamicTileSheet { Sheet = Sheet, Texture = Texture });
            AtlasValid = false;
        }

        public void Prerender(ContentManager Content)
        {
            if (AtlasValid) return;
            AtlasValid = true;

            TileSheets = new Dictionary<String, ITileSheet>();

            var atlas = CompileAtlas(Content, CoreSheets, SheetGenerators);

            if (Texture == null || Texture.IsDisposed || Texture.Width != atlas.Dimensions.Width || Texture.Height != atlas.Dimensions.Height)
            {
                if (Texture != null && !Texture.IsDisposed)
                    Texture.Dispose();

                Texture = new Texture2D(Device, atlas.Dimensions.Width, atlas.Dimensions.Height, false, SurfaceFormat.Color);
            }
            
            BuildTilesheetsFromPackedAtlas(atlas);
        }

        private static Dictionary<string, Func<GraphicsDevice, ContentManager, JsonTileSheet, Texture2D>> FindGenerators()
        {
            var generators = new Dictionary<String, Func<GraphicsDevice, ContentManager, JsonTileSheet, Texture2D>>();
            foreach (var method in AssetManager.EnumerateModHooks(typeof(TextureGeneratorAttribute), typeof(Texture2D), new Type[]
            {
                typeof(GraphicsDevice),
                typeof(ContentManager),
                typeof(JsonTileSheet)
            }))
            {
                if (!(method.GetCustomAttributes(false).FirstOrDefault(a => a is TextureGeneratorAttribute) is TextureGeneratorAttribute attribute))
                    continue;
                generators[attribute.GeneratorName] = (device, content, sheet) => method.Invoke(null, new Object[] { device, content, sheet }) as Texture2D;
            }

            return generators;
        }

        private TextureAtlas.Atlas CompileAtlas(ContentManager Content, List<JsonTileSheet> sheets, Dictionary<string, Func<GraphicsDevice, ContentManager, JsonTileSheet, Texture2D>> generators)
        {
            // Todo: Save a list of Atlas Entries at this top level.
            return TextureAtlas.Compiler.Compile(CoreSheets.Select(s =>
            {
                Texture2D realTexture = null;

                switch (s.Type)
                {
                    case JsonTileSheetType.TileSheet:
                    case JsonTileSheetType.VariableWidthFont:
                    case JsonTileSheetType.JsonFont:
                        realTexture = AssetManager.GetContentTexture(s.Texture);
                        break;
                    case JsonTileSheetType.Generated:
                        realTexture = generators[s.Texture](Device, Content, s);
                        break;
                }

                return new TextureAtlas.Entry
                {
                    Sheet = s,
                    Rect = new Rectangle(0, 0, realTexture.Width, realTexture.Height),
                    RealTexture = realTexture
                };
            }).Concat(DynamicSheets.Select(s =>
            {
                return new TextureAtlas.Entry
                {
                    Sheet = s.Sheet,
                    Rect = new Rectangle(0, 0, s.Texture.Width, s.Texture.Height),
                    RealTexture = s.Texture
                };
            })).ToList());
        }

        private void BuildTilesheetsFromPackedAtlas(TextureAtlas.Atlas atlas)
        {
            // Todo: Use the tile sheet saved in the atlas entry if it exists. Otherwise, make a new one.
            foreach (var texture in atlas.Textures)
            {
                // Copy source texture into the atlas
                var realTexture = texture.RealTexture;
                var memTexture = TextureTool.MemoryTextureFromTexture2D(realTexture);

                if (texture.Sheet.Type == JsonTileSheetType.VariableWidthFont)
                {
                    // Make black pixels transparent.
                    memTexture.Filter(c => (c.R == 0 && c.G == 0 && c.B == 0) ? new Color(0, 0, 0, 0) : c);

                    TileSheets.Upsert(texture.Sheet.Name, new VariableWidthFont(realTexture, Texture.Width,
                        Texture.Height, texture.Rect));
                }
                else if (texture.Sheet.Type == JsonTileSheetType.JsonFont)
                    TileSheets.Upsert(texture.Sheet.Name, new JsonFont(texture.Sheet.Texture, atlas.Dimensions, texture.Rect));
                else
                    TileSheets.Upsert(texture.Sheet.Name, new TileSheet(Texture.Width,
                        Texture.Height, texture.Rect, texture.Sheet.TileWidth, texture.Sheet.TileHeight, texture.Sheet.RepeatWhenUsedAsBorder));

                Texture.SetData(0, texture.Rect, memTexture.Data, 0, realTexture.Width * realTexture.Height);
            }
        }

        public void CalculateScreenSize()
        {
            VirtualScreen = new Rectangle(0, 0, ActualScreenBounds.X / ScaleRatio, ActualScreenBounds.Y / ScaleRatio);
            RealScreen = new Rectangle(0, 0, VirtualScreen.Width * ScaleRatio, VirtualScreen.Height * ScaleRatio);
            RealScreen = new Rectangle((ActualScreenBounds.X - RealScreen.Width) / 2, (ActualScreenBounds.Y - RealScreen.Height) / 2, RealScreen.Width, RealScreen.Height);
        }

        public void Dispose()
        {
            if (Texture != null && !Texture.IsDisposed)
                Texture.Dispose();
        }
    }
}
