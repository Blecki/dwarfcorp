using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace DwarfCorp.DwarfSprites
{
    // Todo: Want to make this not dwarf specific.
    public class FixDwarfSprites
    {
        public static int FindIndexOfNearestColor(Vector3 Input, List<Vector3> Palette)
        {
            var index = 0;
            var nearest = int.MaxValue;
            for (var i = 0; i < Palette.Count; ++i)
            {
                var delta = (Input - Palette[i]);
                var distance = (int)(delta.X + delta.Y + delta.Z);
                if (distance < nearest)
                {
                    nearest = distance;
                    index = i;
                }
            }
            return index;
        }

        public static IndexedTexture DecomposeTextureNearest(MemoryTexture Source, DwarfCorp.Palette Palette)
        {
            if (Source == null)
                return null;

            var colorVectors = Palette.Select(c => new Vector3(c.R, c.G, c.B)).ToList();

            var r = new IndexedTexture(Source.Width, Source.Height);
            for (var i = 0; i < Source.Data.Length; ++i)
            {
                var sourcePixel = Source.Data[i];
                var sourceColorVector = new Vector3(sourcePixel.R, sourcePixel.G, sourcePixel.B);

                var index = Palette.IndexOf(sourcePixel);
                if (index == -1)
                    index = FindIndexOfNearestColor(sourceColorVector, colorVectors);

                if (index >= 0)
                    r.Data[i] = (byte)index;
                else
                    r.Data[i] = 0;
            }
            return r;
        }

        public static bool HasPixels(MemoryTexture Texture, Rectangle Rect)
        {
            for (var x = Rect.X; x < Rect.Right && x < Texture.Width; ++x)
                for (var y = Rect.Y; y < Rect.Bottom && y < Texture.Height; ++y)
                    if (Texture.Data[Texture.Index(x, y)].A != 0)
                        return true;
            return false;
        }

        public static void Process()
        {
            var stream = System.IO.File.OpenRead("Content/Entities/Dwarf/Layers/dwarf-layers.psd");
            var psd = new PhotoshopFile.PsdFile(stream, new PhotoshopFile.LoadContext());

            var attackRects = new Rectangle[]
            {
                new Rectangle(0, 321, 192, 158),
                new Rectangle(192, 321, 192, 158),
                new Rectangle(192, 481, 192, 158)
            };

            var hasAttacks = new bool[3] { false, false, false };

            if (LayerLibrary.FindPalette("Base").HasValue(out var conversionPalette))
            {
                foreach (var layer in psd.Layers)
                {
                    // Grab the data from the PSD
                    var channels = new List<PhotoshopFile.Channel>();
                    channels.Add(layer.Channels.Where(c => c.ID == 0).FirstOrDefault());
                    channels.Add(layer.Channels.Where(c => c.ID == 1).FirstOrDefault());
                    channels.Add(layer.Channels.Where(c => c.ID == 2).FirstOrDefault());
                    channels.Add(layer.AlphaChannel);

                    // Copy to a memory texture
                    var rawMemText = new MemoryTexture(layer.Rect.Width, layer.Rect.Height);

                    for (var index = 0; index < layer.Rect.Width * layer.Rect.Height; ++index)
                        if (channels[3].ImageData[index] == 0)
                            rawMemText.Data[index] = new Color(0, 0, 0, 0);
                        else
                            rawMemText.Data[index] = new Color(channels[0].ImageData[index], channels[1].ImageData[index], channels[2].ImageData[index], channels[3].ImageData[index]);

                    // PSD cuts off transparent pixels - Expand to real size texture.
                    var memTex = new MemoryTexture(psd.ColumnCount, psd.RowCount);
                    TextureTool.Blit(rawMemText, new Rectangle(0, 0, layer.Rect.Width, layer.Rect.Height), memTex, new Point(layer.Rect.X, layer.Rect.Y));

                    // Smack it down to indexed - and then convert it back. This maps it to the exact palette colors.
                    var decomposed = DecomposeTextureNearest(memTex, conversionPalette.CachedPalette);
                    var composed = TextureTool.ComposeTexture(decomposed, conversionPalette.CachedPalette);

                    // Copy clothing to the other attack animations if they are incomplete. Might not line up but oh well.
                    for (var i = 0; i < 3; ++i)
                        hasAttacks[i] = HasPixels(composed, attackRects[i]);

                    for (var i = 0; i < 3; ++i)
                        if (!hasAttacks[i])
                            for (var j = 2; j > -1; --j) // Backwards so it prefers the earlier animation.
                                if (i != j && hasAttacks[j])
                                    TextureTool.Blit(composed, attackRects[j], composed, attackRects[i]);

                    // Put data back into the PSD
                    layer.Rect = new System.Drawing.Rectangle(0, 0, composed.Width, composed.Height);
                    for (var i = 0; i < 4; ++i)
                        channels[i].ImageData = new byte[composed.Width * composed.Height];

                    for (var index = 0; index < composed.Width * composed.Height; ++index)
                    {
                        channels[0].ImageData[index] = composed.Data[index].R;
                        channels[1].ImageData[index] = composed.Data[index].G;
                        channels[2].ImageData[index] = composed.Data[index].B;
                        channels[3].ImageData[index] = composed.Data[index].A;
                    }

                    foreach (var channel in layer.Channels)
                        channel.ImageDataRaw = null;
                }
            }

            psd.PrepareSave();
            psd.Save("processed.psd", Encoding.Unicode);
        }

    }
}
