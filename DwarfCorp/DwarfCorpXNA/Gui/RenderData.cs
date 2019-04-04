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
        public Dictionary<String, JsonTileSheet> SourceSheets { get; private set; }
        public Rectangle VirtualScreen { get; private set; }
        public Rectangle RealScreen { get; private set; }
        public int ScaleRatio { get { return CalculateScale(); } }

        public int CalculateScale()
        {
            if (!DwarfCorp.GameSettings.Default.GuiAutoScale)
                return DwarfCorp.GameSettings.Default.GuiScale;

            float scaleX = ActualScreenBounds.X/1920.0f;
            float scaleY = ActualScreenBounds.Y/1080.0f;
            float maxScale = Math.Max(scaleX, scaleY);
            var scale = (int) MathFunctions.Clamp((int)Math.Ceiling(maxScale), 1, 10);
            GameSettings.Default.GuiScale = scale;
            return scale;
        }

        public RenderData(GraphicsDevice Device, ContentManager Content)
        {
            this.Effect = Content.Load<Effect>(ContentPaths.GUI.Shader);

            CalculateScreenSize();

            // Load skin from disc. The skin is a set of tilesheets.
            var sheets = FileUtils.LoadJsonListFromMultipleSources<JsonTileSheet>(ContentPaths.GUI.Skin, null, (s) => s.Name);

            var generators = new Dictionary<String, Func<GraphicsDevice, ContentManager, JsonTileSheet, Texture2D>>();
            foreach (var method in AssetManager.EnumerateModHooks(typeof(TextureGeneratorAttribute), typeof(Texture2D), new Type[]
            {
                typeof(GraphicsDevice),
                typeof(ContentManager),
                typeof(JsonTileSheet)
            }))
            {
                var attribute = method.GetCustomAttributes(false).FirstOrDefault(a => a is TextureGeneratorAttribute) as TextureGeneratorAttribute;
                if (attribute == null) continue;
                generators[attribute.GeneratorName] = (device, content, sheet) => method.Invoke(null, new Object[] { device, content, sheet }) as Texture2D;
            }

            // Pack skin into a single texture - Build atlas information from texture sizes.
            var atlas = TextureAtlas.Compiler.Compile(sheets.Select(s =>
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
                }).ToList());

            SourceSheets = new Dictionary<string, JsonTileSheet>();
            foreach(var sheet in sheets)
            {
                SourceSheets[sheet.Name] = sheet;
            }
            // Create the atlas texture
            Texture = new Texture2D(Device, atlas.Dimensions.Width, atlas.Dimensions.Height, false, SurfaceFormat.Color);

            TileSheets = new Dictionary<String, ITileSheet>();

            foreach (var texture in atlas.Textures)
            {
                Console.Out.WriteLine("Loading {0}", texture.Sheet.Name);
                // Copy source texture into the atlas
                var realTexture = texture.RealTexture;
                if (realTexture == null || realTexture.IsDisposed || realTexture.GraphicsDevice.IsDisposed)
                {
                    texture.RealTexture = AssetManager.GetContentTexture(texture.Sheet.Texture);
                    realTexture = texture.RealTexture;
                }
                var textureData = new Color[realTexture.Width * realTexture.Height];
                realTexture.GetData(textureData);

                if (texture.Sheet.Type == JsonTileSheetType.VariableWidthFont)
                {
                    for (int i = 0; i < textureData.Length; ++i)
                    {
                        if (textureData[i].R == 0 &&
                            textureData[i].G == 0 &&
                            textureData[i].B == 0)
                            textureData[i] = new Color(0, 0, 0, 0);
                    }

                    TileSheets.Upsert(texture.Sheet.Name, new VariableWidthFont(realTexture, Texture.Width,
                        Texture.Height, texture.Rect));
                }
                else if (texture.Sheet.Type == JsonTileSheetType.JsonFont)
                {
                    TileSheets.Upsert(texture.Sheet.Name, new JsonFont(texture.Sheet.Texture, atlas.Dimensions, texture.Rect));
                }
                else
                {
                    // Create a tilesheet pointing into the atlas texture.
                    TileSheets.Upsert(texture.Sheet.Name, new TileSheet(Texture.Width,
                        Texture.Height, texture.Rect, texture.Sheet.TileWidth, texture.Sheet.TileHeight, texture.Sheet.RepeatWhenUsedAsBorder));
                }

                // Paste texture data into atlas.
                Texture.SetData(0, texture.Rect, textureData, 0, realTexture.Width * realTexture.Height);

            }
            Console.Out.WriteLine("Done with texture atlas.");
        }

        public void CalculateScreenSize()
        {
            var screenSize = ActualScreenBounds;
            VirtualScreen = new Rectangle(0, 0,
                screenSize.X / ScaleRatio,
                screenSize.Y / ScaleRatio);
            
            RealScreen = new Rectangle(0, 0, VirtualScreen.Width * ScaleRatio, VirtualScreen.Height * ScaleRatio);
            RealScreen = new Rectangle((screenSize.X - RealScreen.Width) / 2,
                (screenSize.Y - RealScreen.Height) / 2,
                RealScreen.Width, RealScreen.Height);
        }

        public void Dispose()
        {
            if (Texture != null && !Texture.IsDisposed)
                Texture.Dispose();
        }
    }
}
