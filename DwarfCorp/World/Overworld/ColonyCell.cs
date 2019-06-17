using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Collections;

namespace DwarfCorp
{
    public class ColonyCell : IEquatable<ColonyCell>
    {
        public Rectangle Bounds;
        public OverworldFaction Faction;

        bool IEquatable<ColonyCell>.Equals(ColonyCell other)
        {
            return Object.ReferenceEquals(other, this);
        }
    }

    public class CellSet
    {
        [JsonProperty] private List<ColonyCell> Cells;
        [JsonProperty] private int[,] CellMap;

        public CellSet(String Filename)
        {
            var texture = AssetManager.GetContentTexture(Filename);
            var rawTexture = TextureTool.MemoryTextureFromTexture2D(texture);

            var cells = new Dictionary<Color, Rectangle>();
            for (var x = 0; x < rawTexture.Width; ++x)
                for (var y = 0; y < rawTexture.Height; ++y)
                {
                    var c = rawTexture.Data[rawTexture.Index(x, y)];
                    if (!cells.ContainsKey(c))
                        cells.Add(c, new Rectangle(x, y, 1, 1));
                    else
                    {
                        var r = cells[c];
                        if (x >= r.X + r.Width) r = new Rectangle(r.X, r.Y, x - r.X + 1, r.Height);
                        if (y >= r.Y + r.Height) r = new Rectangle(r.X, r.Y, r.Width, y - r.Y + 1);
                        cells[c] = r;
                    }
                }

            Cells = cells.Values.Select(r => new ColonyCell { Bounds = r }).ToList();

            CellMap = new int[rawTexture.Width, rawTexture.Height];

            for (var i = 0; i < Cells.Count; ++i)
                for (var x = Cells[i].Bounds.Left; x < Cells[i].Bounds.Right; ++x)
                    for (var y = Cells[i].Bounds.Top; y < Cells[i].Bounds.Bottom; ++y)
                        CellMap[x, y] = i;
        }

        public ColonyCell GetCellAt(int X, int Y)
        {
            if (X >= 0 && X < CellMap.GetLength(0) && Y >= 0 && Y < CellMap.GetLength(1))
                return Cells[CellMap[X, Y]];
            return null;
        }

        public IEnumerable<ColonyCell> EnumerateCells()
        {
            return Cells;
        }

        public IEnumerable<ColonyCell> EnumerateManhattanNeighbors(ColonyCell Cell)
        {
            // Cells can have multiple neighbors on any edge.
            var r = new HashSet<ColonyCell>();
            EnumerateLine(new Point(Cell.Bounds.Left - 1, Cell.Bounds.Top), new Point(0, 1), Cell.Bounds.Height, r);
            EnumerateLine(new Point(Cell.Bounds.Right, Cell.Bounds.Top), new Point(0, 1), Cell.Bounds.Height, r);
            EnumerateLine(new Point(Cell.Bounds.Left, Cell.Bounds.Top - 1), new Point(1, 0), Cell.Bounds.Width, r);
            EnumerateLine(new Point(Cell.Bounds.Left, Cell.Bounds.Bottom), new Point(1, 0), Cell.Bounds.Width, r);
            return r;
        }

        private void EnumerateLine(Point Start, Point Step, int Steps, HashSet<ColonyCell> Into)
        {
            for (var i = 0; i < Steps; ++i)
            {
                var c = GetCellAt(Start.X, Start.Y);
                if (c != null) Into.Add(c);
                Start = new Point(Start.X + Step.X, Start.Y + Step.Y);
            }
        }
    }
}
