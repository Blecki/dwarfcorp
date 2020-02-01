using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.Gui.TextureAtlas
{
    public class Entry
    {
        public JsonTileSheet Sheet;
        public Rectangle Rect;
        public Texture2D RealTexture;
        public ITileSheet TileSheet;
        public bool NeedsBlit = false;

        public void ReplaceTexture(Texture2D NewTexture)
        {
            RealTexture = NewTexture;
            NeedsBlit = true;
        }
    }

    internal static class BspSubdivision
    {
        /// <summary>
        /// Attempt to place textures into the space defined by Dimensions until no more can be fit. 
        /// Subdivide the space after each insertion. Assume textures are sorted by size.
        /// </summary>
        /// <param name="Dimensions"></param>
        /// <param name="Textures"></param>
        internal static void TryPlaceTextures(Rectangle Dimensions, List<Entry> Textures)
        {
            Entry tex = null;

            //Find largest entry that fits within the dimensions.
            for (int i = 0; i < Textures.Count; ++i)
            {
                var Entry = Textures[i];
                if (Entry.Rect.Width > Dimensions.Width || Entry.Rect.Height > Dimensions.Height)
                    continue;
                Textures.RemoveAt(i);
                tex = Entry;
                break;
            }

            // Quit if nothing fit.
            if (tex == null) return;

            tex.Rect.X = Dimensions.X;
            tex.Rect.Y = Dimensions.Y;

            //Subdivide remaining space.
            int HorizontalDifference = Dimensions.Width - tex.Rect.Width;
            int VerticalDifference = Dimensions.Height - tex.Rect.Height;

            if (HorizontalDifference == 0 && VerticalDifference == 0) //Perfect fit!
                return;

            Rectangle? ASpace = null;
            Rectangle? BSpace = null;            

            // Subdivide the space on the shortest axis of the texture we just placed.
            if (HorizontalDifference >= VerticalDifference)
            {
                ASpace = new Rectangle(Dimensions.X + tex.Rect.Width, Dimensions.Y, HorizontalDifference, Dimensions.Height);

                // Remember that this isn't a perfect split - a chunk belongs to the placed texture.
                if (VerticalDifference > 0)
                    BSpace = new Rectangle(Dimensions.X, Dimensions.Y + tex.Rect.Height, tex.Rect.Width, VerticalDifference);
            }
            else
            {
                ASpace = new Rectangle(Dimensions.X, Dimensions.Y + tex.Rect.Height, Dimensions.Width, VerticalDifference);
                if (HorizontalDifference > 0)
                    BSpace = new Rectangle(Dimensions.X + tex.Rect.Width, Dimensions.Y, HorizontalDifference, tex.Rect.Height);
            }

           TryPlaceTextures(ASpace.Value, Textures);
            if (BSpace.HasValue) TryPlaceTextures(BSpace.Value, Textures);
        }

        /// <summary>
        /// Attempt to fit textures into working space, and if any are left, double vertical size of working space.
        /// </summary>
        /// <param name="TotalArea"></param>
        /// <param name="WorkingArea"></param>
        /// <param name="Textures"></param>
        /// <returns></returns>
        internal static Rectangle ExpandVertical(Rectangle TotalArea, Rectangle WorkingArea, List<Entry> Textures)
        {
            TryPlaceTextures(WorkingArea, Textures);

            if (Textures.Count > 0)
            {
                WorkingArea = new Rectangle(0, TotalArea.Height, TotalArea.Width, TotalArea.Height);
                TotalArea.Height *= 2;
                return ExpandHorizontal(TotalArea, WorkingArea, Textures);
            }
            else
                return TotalArea;
        }

        /// <summary>
        /// Attempt to fit textures into working space, and if any are left, double horizontal size of working space.
        /// </summary>
        /// <param name="TotalArea"></param>
        /// <param name="WorkingArea"></param>
        /// <param name="Textures"></param>
        /// <returns></returns>
        internal static Rectangle ExpandHorizontal(Rectangle TotalArea, Rectangle WorkingArea, List<Entry> Textures)
        {
            TryPlaceTextures(WorkingArea, Textures);

            if (Textures.Count > 0)
            {
                WorkingArea = new Rectangle(TotalArea.Width, 0, TotalArea.Width, TotalArea.Height);
                TotalArea.Width *= 2;
                return ExpandVertical(TotalArea, WorkingArea, Textures);
            }
            else
                return TotalArea;
        }
    }

    public class Compiler
    {
        public static Rectangle Compile(IEnumerable<Entry> Entries)
        {
            var entries = Entries.ToList();

            entries.Sort((A, B) =>
            {
                return (B.Rect.Width * B.Rect.Height) - (A.Rect.Width * A.Rect.Height);
            });

            // Find smallest power of 2 sized texture that can hold the largest entry.
            var largestEntry = entries[0];
            var texSize = new Rectangle(0, 0, 1, 1);
            while (texSize.Width < largestEntry.Rect.Width)
                texSize.Width *= 2;
            while (texSize.Height < largestEntry.Rect.Height)
                texSize.Height *= 2;

            texSize = BspSubdivision.ExpandHorizontal(texSize, texSize, entries);
            return texSize;
        }
    }
}
