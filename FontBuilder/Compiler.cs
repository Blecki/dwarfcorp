using System.Collections.Generic;

namespace FontBuilder
{
    internal static class BspSubdivision
    {
        /// <summary>
        /// Attempt to place textures into the space defined by Dimensions until no more can be fit. 
        /// Subdivide the space after each insertion. Assume textures are sorted by size.
        /// </summary>
        /// <param name="Dimensions"></param>
        /// <param name="Textures"></param>
        internal static void TryPlaceGlyphs(Rectangle Dimensions, List<Glyph> Textures)
        {
            Glyph tex = null;

            //Find largest entry that fits within the dimensions.
            for (int i = 0; i < Textures.Count; ++i)
            {
                var Entry = Textures[i];
                if (Entry.Width > Dimensions.Width || Entry.Height > Dimensions.Height)
                    continue;
                Textures.RemoveAt(i);
                tex = Entry;
                break;
            }

            // Quit if nothing fit.
            if (tex == null) return;

            tex.X = Dimensions.X;
            tex.Y = Dimensions.Y;

            //Subdivide remaining space.
            int HorizontalDifference = Dimensions.Width - tex.Width;
            int VerticalDifference = Dimensions.Height - tex.Height;

            if (HorizontalDifference == 0 && VerticalDifference == 0) //Perfect fit!
                return;

            Rectangle? ASpace = null;
            Rectangle? BSpace = null;            

            // Subdivide the space on the shortest axis of the texture we just placed.
            if (HorizontalDifference >= VerticalDifference)
            {
                ASpace = new Rectangle(Dimensions.X + tex.Width, Dimensions.Y, HorizontalDifference, Dimensions.Height);

                // Remember that this isn't a perfect split - a chunk belongs to the placed texture.
                if (VerticalDifference > 0)
                    BSpace = new Rectangle(Dimensions.X, Dimensions.Y + tex.Height, tex.Width, VerticalDifference);
            }
            else
            {
                ASpace = new Rectangle(Dimensions.X, Dimensions.Y + tex.Height, Dimensions.Width, VerticalDifference);
                if (HorizontalDifference > 0)
                    BSpace = new Rectangle(Dimensions.X + tex.Width, Dimensions.Y, HorizontalDifference, tex.Height);
            }

           TryPlaceGlyphs(ASpace.Value, Textures);
            if (BSpace.HasValue) TryPlaceGlyphs(BSpace.Value, Textures);
        }

        /// <summary>
        /// Attempt to fit textures into working space, and if any are left, double vertical size of working space.
        /// </summary>
        /// <param name="TotalArea"></param>
        /// <param name="WorkingArea"></param>
        /// <param name="Textures"></param>
        /// <returns></returns>
        internal static Rectangle ExpandVertical(Rectangle TotalArea, Rectangle WorkingArea, List<Glyph> Textures)
        {
            TryPlaceGlyphs(WorkingArea, Textures);

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
        internal static Rectangle ExpandHorizontal(Rectangle TotalArea, Rectangle WorkingArea, List<Glyph> Textures)
        {
            TryPlaceGlyphs(WorkingArea, Textures);

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

    public class AtlasCompiler
    {
        public static Atlas Compile(List<Glyph> Entries)
        {
            Entries.Sort((A, B) =>
            {
                return (B.Width * B.Height) - (A.Width * A.Height);
            });

            // Find smallest power of 2 sized texture that can hold the largest entry.
            var largestEntry = Entries[0];
            var texSize = new Rectangle(0, 0, 1, 1);
            while (texSize.Width < largestEntry.Width)
                texSize.Width *= 2;
            while (texSize.Height < largestEntry.Height)
                texSize.Height *= 2;

            // Be sure to pass a copy of the list since the algorithm modifies it.
            texSize = BspSubdivision.ExpandHorizontal(texSize, texSize, new List<Glyph>(Entries));
            return new Atlas { Dimensions = texSize, Glyphs = Entries };
        }
    }
}
