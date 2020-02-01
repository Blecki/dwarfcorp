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
        public List<TextureAtlas.Entry> CoreAtlasEntries = null;
        public Rectangle VirtualScreen { get; private set; }
        public Rectangle RealScreen { get; private set; }
        public int ScaleRatio { get { return CalculateScale(); } }


        private bool AtlasValid = false;
        private Dictionary<string, Func<GraphicsDevice, ContentManager, JsonTileSheet, Texture2D>> SheetGenerators = null;

        private List<TextureAtlas.Entry> DynamicAtlasEntries = new List<TextureAtlas.Entry>();

        private IEnumerable<TextureAtlas.Entry> EnumerateAllSheets()
        {
            foreach (var sheet in CoreAtlasEntries)
                yield return sheet;

            foreach (var sheet in DynamicAtlasEntries)
                yield return sheet;
        }

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

            var coreSheets = FileUtils.LoadJsonListFromMultipleSources<JsonTileSheet>(ContentPaths.GUI.Skin, null, (s) => s.Name);
            SheetGenerators = FindGenerators();

            CoreAtlasEntries = coreSheets.Select(s =>
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
                        realTexture = SheetGenerators[s.Texture](Device, Content, s);
                        break;
                }

                var r = new TextureAtlas.Entry
                {
                    Sheet = s,
                    Rect = new Rectangle(0, 0, realTexture.Width, realTexture.Height),
                    RealTexture = realTexture
                };

                r.TileSheet = MakeTileSheet(r, realTexture.Bounds);

                return r;
            }).ToList();

                Prerender(Content);
        }

        // This should return some kind of dynamic sheet handle that can be used to render with it, and to switch out it's texture.
        // - Employee Icon will be first to use this, and will replace the texture data //without// having to relayout the sheet.
        public TextureAtlas.Entry AddDynamicSheet(JsonTileSheet Sheet, Texture2D Texture)
        {
            Sheet.Name = System.Guid.NewGuid().ToString();
                
            var newEntry = new TextureAtlas.Entry
            {
                Sheet = Sheet,
                RealTexture = Texture,
                Rect = new Rectangle(0, 0, Texture.Width, Texture.Height)
            };

            newEntry.TileSheet = MakeTileSheet(newEntry, Texture.Bounds);

            DynamicAtlasEntries.Add(newEntry);

            AtlasValid = false;

            return newEntry;
        }

        public void Prerender(ContentManager Content)
        {
            if (AtlasValid)
            {
                foreach (var entry in DynamicAtlasEntries)
                    if (entry.NeedsBlit) BlitSheet(entry, Texture);
                return;
            }

            AtlasValid = true;

            TileSheets = new Dictionary<String, ITileSheet>();

            var atlasBounds = CompileAtlas();

            if (Texture == null || Texture.IsDisposed || Texture.Width != atlasBounds.Width || Texture.Height != atlasBounds.Height)
            {
                if (Texture != null && !Texture.IsDisposed)
                    Texture.Dispose();

                Texture = new Texture2D(Device, atlasBounds.Width, atlasBounds.Height, false, SurfaceFormat.Color);
            }

            foreach (var sheet in EnumerateAllSheets())
            {
                sheet.TileSheet.ResetAtlasBounds(sheet.Rect, Texture.Bounds);
                TileSheets.Upsert(sheet.Sheet.Name, sheet.TileSheet);

                BlitSheet(sheet, Texture);
            }
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

        private Rectangle CompileAtlas()
        {
            // Reset atlas entries.
            foreach (var entry in EnumerateAllSheets())
                entry.Rect = new Rectangle(0, 0, entry.RealTexture.Width, entry.RealTexture.Height);

            // Todo: Save a list of Atlas Entries at this top level.
            return TextureAtlas.Compiler.Compile(EnumerateAllSheets());
        }

        private void BlitSheet(TextureAtlas.Entry Sheet, Texture2D Into)
        {
            Sheet.NeedsBlit = false;

            var memTexture = TextureTool.MemoryTextureFromTexture2D(Sheet.RealTexture);

            if (Sheet.Sheet.Type == JsonTileSheetType.VariableWidthFont)
                memTexture.Filter(c => (c.R == 0 && c.G == 0 && c.B == 0) ? new Color(0, 0, 0, 0) : c);

            Into.SetData(0, Sheet.Rect, memTexture.Data, 0, memTexture.Width * memTexture.Height);
        }

        private ITileSheet MakeTileSheet(TextureAtlas.Entry Sheet, Rectangle AtlasBounds)
        {
            if (Sheet.Sheet.Type == JsonTileSheetType.VariableWidthFont)
                return new VariableWidthFont(Sheet.RealTexture, AtlasBounds.Width, AtlasBounds.Height, Sheet.Rect);
            else if (Sheet.Sheet.Type == JsonTileSheetType.JsonFont)
               return new JsonFont(Sheet.Sheet.Texture, AtlasBounds, Sheet.Rect);
            else
               return new TileSheet(AtlasBounds.Width, AtlasBounds.Height, Sheet.Rect, Sheet.Sheet.TileWidth, Sheet.Sheet.TileHeight, Sheet.Sheet.RepeatWhenUsedAsBorder);
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
