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
    public class RenderData
    {
        public GraphicsDevice Device { get; private set; }
        public Point ActualScreenBounds { get { return new Point(Device.Viewport.Width, Device.Viewport.Height); } }
        public Effect Effect { get; private set; }
        public Texture2D Texture { get; private set; }
        public Dictionary<String, ITileSheet> TileSheets { get; private set; }
        public Rectangle VirtualScreen { get; private set; }
        public Rectangle RealScreen { get; private set; }
        public int ScaleRatio { get { return DwarfCorp.GameSettings.Default.GuiScale; } }

        public RenderData(GraphicsDevice Device, ContentManager Content, String Effect, String Skin)
        {
            this.Device = Device;
            this.Effect = Content.Load<Effect>(Effect);

            CalculateScreenSize();

            // Load skin from disc. The skin is a set of tilesheets.
            var skin = Newtonsoft.Json.JsonConvert.DeserializeObject<JsonTileSheetSet>(
                System.IO.File.ReadAllText(Skin));

            // Pack skin into a single texture - Build atlas information from texture sizes.
            var atlas = TextureAtlas.Compiler.Compile(skin.Sheets.Select(s =>
                {
                    var realTexture = Content.Load<Texture2D>(s.Texture);
                    return new TextureAtlas.Entry
                    {
                        Sheet = s,
                        Rect = new Rectangle(0, 0, realTexture.Width, realTexture.Height)
                    };
                }).ToList());

            // Create the atlas texture
            Texture = new Texture2D(Device, atlas.Dimensions.Width, atlas.Dimensions.Height);

            TileSheets = new Dictionary<String, ITileSheet>();

            foreach (var texture in atlas.Textures)
            {
                // Copy source texture into the atlas
                var realTexture = Content.Load<Texture2D>(texture.Sheet.Texture);
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
                else
                {
                    // Create a tilesheet pointing into the atlas texture.
                    TileSheets.Upsert(texture.Sheet.Name, new TileSheet(Texture.Width,
                        Texture.Height, texture.Rect, texture.Sheet.TileWidth, texture.Sheet.TileHeight, texture.Sheet.RepeatWhenUsedAsBorder));
                }

                // Paste texture data into atlas.
                Texture.SetData(0, texture.Rect, textureData, 0, realTexture.Width * realTexture.Height);

            }
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
    }
}
