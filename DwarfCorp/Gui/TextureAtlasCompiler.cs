using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp.Gui.TextureAtlas
{
    internal static class BspSubdivision
    {
        /// <summary>
        /// Attempt to place textures into the space defined by Dimensions until no more can be fit. 
        /// Subdivide the space after each insertion. Assume textures are sorted by size.
        /// </summary>
        /// <param name="Dimensions"></param>
        /// <param name="Textures"></param>
        internal static void TryPlaceTextures(Rectangle Dimensions, List<SpriteAtlasEntry> Textures)
        {
            SpriteAtlasEntry tex = null;

            //Find largest entry that fits within the dimensions.
            for (int i = 0; i < Textures.Count; ++i)
            {
                var Entry = Textures[i];
                if (Entry.AtlasBounds.Width > Dimensions.Width || Entry.AtlasBounds.Height > Dimensions.Height)
                    continue;
                Textures.RemoveAt(i);
                tex = Entry;
                break;
            }

            // Quit if nothing fit.
            if (tex == null) return;

            tex.AtlasBounds.X = Dimensions.X;
            tex.AtlasBounds.Y = Dimensions.Y;

            //Subdivide remaining space.
            int HorizontalDifference = Dimensions.Width - tex.AtlasBounds.Width;
            int VerticalDifference = Dimensions.Height - tex.AtlasBounds.Height;

            if (HorizontalDifference == 0 && VerticalDifference == 0) //Perfect fit!
                return;

            Rectangle? ASpace = null;
            Rectangle? BSpace = null;            

            // Subdivide the space on the shortest axis of the texture we just placed.
            if (HorizontalDifference >= VerticalDifference)
            {
                ASpace = new Rectangle(Dimensions.X + tex.AtlasBounds.Width, Dimensions.Y, HorizontalDifference, Dimensions.Height);

                // Remember that this isn't a perfect split - a chunk belongs to the placed texture.
                if (VerticalDifference > 0)
                    BSpace = new Rectangle(Dimensions.X, Dimensions.Y + tex.AtlasBounds.Height, tex.AtlasBounds.Width, VerticalDifference);
            }
            else
            {
                ASpace = new Rectangle(Dimensions.X, Dimensions.Y + tex.AtlasBounds.Height, Dimensions.Width, VerticalDifference);
                if (HorizontalDifference > 0)
                    BSpace = new Rectangle(Dimensions.X + tex.AtlasBounds.Width, Dimensions.Y, HorizontalDifference, tex.AtlasBounds.Height);
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
        internal static Rectangle ExpandVertical(Rectangle TotalArea, Rectangle WorkingArea, List<SpriteAtlasEntry> Textures)
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
        internal static Rectangle ExpandHorizontal(Rectangle TotalArea, Rectangle WorkingArea, List<SpriteAtlasEntry> Textures)
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
        public static Rectangle Compile(IEnumerable<SpriteAtlasEntry> Entries)
        {
            var entries = Entries.ToList();

            entries.Sort((A, B) =>
            {
                return (B.AtlasBounds.Width * B.AtlasBounds.Height) - (A.AtlasBounds.Width * A.AtlasBounds.Height);
            });

            // Find smallest power of 2 sized texture that can hold the largest entry.
            var largestEntry = entries[0];
            var texSize = new Rectangle(0, 0, 1, 1);
            while (texSize.Width < largestEntry.AtlasBounds.Width)
                texSize.Width *= 2;
            while (texSize.Height < largestEntry.AtlasBounds.Height)
                texSize.Height *= 2;

            texSize = BspSubdivision.ExpandHorizontal(texSize, texSize, entries);
            return texSize;
        }
    }
}
