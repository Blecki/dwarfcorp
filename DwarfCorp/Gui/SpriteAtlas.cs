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
    public class SpriteAtlas : IDisposable
    {
        public enum PrerenderResult
        {
            RebuiltAtlas,
            AtlasWasValid
        }

        private GraphicsDevice Device;

        public Texture2D Texture { get; private set; }
        public Dictionary<String, ITileSheet> NamedTileSheets { get; private set; }

        public List<TextureAtlas.SpriteAtlasEntry> CoreAtlasEntries = null;
        private bool AtlasValid = false;
        private Dictionary<string, Func<GraphicsDevice, ContentManager, TileSheetDefinition, Texture2D>> SheetGenerators = null;
        private Dictionary<String, TextureAtlas.SpriteAtlasEntry> DynamicAtlasEntries = new Dictionary<String, TextureAtlas.SpriteAtlasEntry>();

        private IEnumerable<TextureAtlas.SpriteAtlasEntry> EnumerateAllSheets()
        {
            foreach (var sheet in CoreAtlasEntries)
                yield return sheet;

            foreach (var sheet in DynamicAtlasEntries)
                yield return sheet.Value;
        }

        public SpriteAtlas(GraphicsDevice Device, ContentManager Content)
        {
            this.Device = Device;

            var coreSheets = FileUtils.LoadJsonListFromMultipleSources<TileSheetDefinition>(ContentPaths.GUI.Skin, null, (s) => s.Name);
            SheetGenerators = FindGenerators();

            CoreAtlasEntries = coreSheets.Select(s =>
            {
                Texture2D realTexture = null;

                switch (s.Type)
                {
                    case TileSheetType.TileSheet:
                    case TileSheetType.VariableWidthFont:
                    case TileSheetType.JsonFont:
                        realTexture = AssetManager.GetContentTexture(s.Texture);
                        break;
                    case TileSheetType.Generated:
                        realTexture = SheetGenerators[s.Texture](Device, Content, s);
                        break;
                }

                var r = new TextureAtlas.SpriteAtlasEntry
                {
                    SourceDefinition = s,
                    AtlasBounds = new Rectangle(0, 0, realTexture.Width, realTexture.Height),
                    SourceTexture = realTexture
                };

                r.TileSheet = MakeTileSheet(r, realTexture.Bounds);

                return r;
            }).ToList();

            NamedTileSheets = new Dictionary<string, ITileSheet>();
            foreach (var coreSheet in CoreAtlasEntries)
                NamedTileSheets.Upsert(coreSheet.SourceDefinition.Name, coreSheet.TileSheet);

            Prerender();
        }

        public TextureAtlas.SpriteAtlasEntry AddDynamicSheet(String Name, TileSheetDefinition Sheet, Texture2D Texture)
        {
            if (!String.IsNullOrEmpty(Name) && DynamicAtlasEntries.ContainsKey(Name))
            {
                DynamicAtlasEntries[Name].ReferenceCount += 1;
                return DynamicAtlasEntries[Name];
            }

            if (String.IsNullOrEmpty(Name))
                Name = System.Guid.NewGuid().ToString();

            var newEntry = new TextureAtlas.SpriteAtlasEntry
            {
                SourceDefinition = Sheet,
                SourceTexture = Texture,
                AtlasBounds = new Rectangle(0, 0, Texture.Width, Texture.Height),
                ReferenceCount = 1
            };

            newEntry.TileSheet = MakeTileSheet(newEntry, Texture.Bounds);
            DynamicAtlasEntries.Add(Name, newEntry);
            AtlasValid = false;

            return newEntry;
        }

        public PrerenderResult Prerender()
        {
            var deadEntries = DynamicAtlasEntries.Where(e => e.Value.ReferenceCount <= 0).Select(e => e.Key).ToList();
            foreach (var entry in deadEntries)
                DynamicAtlasEntries.Remove(entry);

            if (AtlasValid)
            {
                foreach (var entry in DynamicAtlasEntries)
                    if (entry.Value.NeedsBlitToAtlas) BlitSheet(entry.Value, Texture);
                return PrerenderResult.AtlasWasValid;
            }

            AtlasValid = true;

            var atlasBounds = CompileAtlas();

            if (Texture == null || Texture.IsDisposed || Texture.Width != atlasBounds.Width || Texture.Height != atlasBounds.Height)
            {
                if (Texture != null && !Texture.IsDisposed)
                    Texture.Dispose();

                Texture = new Texture2D(Device, atlasBounds.Width, atlasBounds.Height, false, SurfaceFormat.Color);
            }

            foreach (var sheet in EnumerateAllSheets())
            {
                sheet.TileSheet.ResetAtlasBounds(sheet.AtlasBounds, Texture.Bounds);
                BlitSheet(sheet, Texture);
            }

            return PrerenderResult.RebuiltAtlas;
        }

        private static Dictionary<string, Func<GraphicsDevice, ContentManager, TileSheetDefinition, Texture2D>> FindGenerators()
        {
            var generators = new Dictionary<String, Func<GraphicsDevice, ContentManager, TileSheetDefinition, Texture2D>>();
            foreach (var method in AssetManager.EnumerateModHooks(typeof(TextureGeneratorAttribute), typeof(Texture2D), new Type[]
            {
                typeof(GraphicsDevice),
                typeof(ContentManager),
                typeof(TileSheetDefinition)
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
                entry.AtlasBounds = new Rectangle(0, 0, entry.SourceTexture.Width, entry.SourceTexture.Height);

            // Todo: Save a list of Atlas Entries at this top level.
            return TextureAtlas.Compiler.Compile(EnumerateAllSheets());
        }

        private void BlitSheet(TextureAtlas.SpriteAtlasEntry Sheet, Texture2D Into)
        {
            Sheet.NeedsBlitToAtlas = false;

            var memTexture = TextureTool.MemoryTextureFromTexture2D(Sheet.SourceTexture);

            if (Sheet.SourceDefinition.Type == TileSheetType.VariableWidthFont)
                memTexture.Filter(c => (c.R == 0 && c.G == 0 && c.B == 0) ? new Color(0, 0, 0, 0) : c);

            Into.SetData(0, Sheet.AtlasBounds, memTexture.Data, 0, memTexture.Width * memTexture.Height);
        }

        private ITileSheet MakeTileSheet(TextureAtlas.SpriteAtlasEntry Sheet, Rectangle AtlasBounds)
        {
            if (Sheet.SourceDefinition.Type == TileSheetType.VariableWidthFont)
                return new VariableWidthFont(Sheet.SourceTexture, AtlasBounds.Width, AtlasBounds.Height, Sheet.AtlasBounds);
            else if (Sheet.SourceDefinition.Type == TileSheetType.JsonFont)
               return new JsonFont(Sheet.SourceDefinition.Texture, AtlasBounds, Sheet.AtlasBounds);
            else
               return new TileSheet(AtlasBounds.Width, AtlasBounds.Height, Sheet.AtlasBounds, Sheet.SourceDefinition.TileWidth, Sheet.SourceDefinition.TileHeight, Sheet.SourceDefinition.RepeatWhenUsedAsBorder);
        }

        public void Dispose()
        {
            if (Texture != null && !Texture.IsDisposed)
                Texture.Dispose();
        }
    }
}
