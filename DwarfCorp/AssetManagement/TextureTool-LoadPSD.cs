using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using System.Security;
using System.Security.Policy;
using System.Security.Permissions;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public static partial class TextureTool
    {
        public class PhotoshopLayer
        {
            public String LayerName;
            public MemoryTexture Data;
        }

        public static List<PhotoshopLayer> LoadPSD(System.IO.FileStream Stream)
        {
            var psd = new PhotoshopFile.PsdFile(Stream, new PhotoshopFile.LoadContext());
            var r = new List<PhotoshopLayer>();

            foreach (var layer in psd.Layers)
            {
                var entry = new PhotoshopLayer();
                entry.LayerName = layer.Name;

                // Need to expand the PSD layer to the size of the image as transparent rows/columns are trimmed off in PSD.
                entry.Data = new MemoryTexture(psd.ColumnCount, psd.RowCount);
                TextureTool.Blit(PSDLayerToMemoryTexture(layer), new Rectangle(0, 0, layer.Rect.Width, layer.Rect.Height), entry.Data, new Point(layer.Rect.X, layer.Rect.Y));
                
                r.Add(entry);
            }

            return r;
        }

        private static MemoryTexture PSDLayerToMemoryTexture(PhotoshopFile.Layer Layer)
        {
            var channels = new List<PhotoshopFile.Channel>();
            channels.Add(Layer.Channels.Where(c => c.ID == 0).FirstOrDefault());
            channels.Add(Layer.Channels.Where(c => c.ID == 1).FirstOrDefault());
            channels.Add(Layer.Channels.Where(c => c.ID == 2).FirstOrDefault());
            channels.Add(Layer.AlphaChannel);

            var r = new MemoryTexture(Layer.Rect.Width, Layer.Rect.Height);

            for (var index = 0; index < Layer.Rect.Width * Layer.Rect.Height; ++index)
                r.Data[index] = new Color(channels[0].ImageData[index], channels[1].ImageData[index], channels[2].ImageData[index], channels[3].ImageData[index]);

            return r;
        }
    }
}