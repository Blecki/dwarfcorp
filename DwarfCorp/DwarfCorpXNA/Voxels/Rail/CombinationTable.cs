using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Rail
{
    public class CombinationTable
    {
        public class Combination
        {
            public String BasePiece;
            public String OverlayPiece;
            public PieceOrientation OverlayRelativeOrientation;

            public String Result;
            public PieceOrientation ResultRelativeOrientation;
        }

        private List<Combination> Combinations = new List<Combination>();

        public Combination FindCombination(String Base, String Overlay, PieceOrientation OverlayRelativeOrientation)
        {
            return Combinations.FirstOrDefault(c => c.BasePiece == Base && c.OverlayPiece == Overlay && c.OverlayRelativeOrientation == OverlayRelativeOrientation);
        }

        public void AddCombination(Combination Combination)
        {
            Combinations.AddRange(EnumerateVariations(Combination));
        }

        private IEnumerable<Combination> EnumerateVariations(Combination Combination)
        {
            yield return Combination;

            // Switch base and overlay
            yield return new Combination
            {
                BasePiece = Combination.OverlayPiece,
                OverlayPiece = Combination.BasePiece,
                OverlayRelativeOrientation = OrientationHelper.Relative(Combination.OverlayRelativeOrientation, PieceOrientation.North),
                Result = Combination.Result,
                ResultRelativeOrientation = OrientationHelper.Relative(Combination.OverlayRelativeOrientation, Combination.ResultRelativeOrientation)
            };

            // Result with Base laid over it
            yield return new Combination
            {
                BasePiece = Combination.Result,
                OverlayPiece = Combination.BasePiece,
                OverlayRelativeOrientation = OrientationHelper.Relative(Combination.ResultRelativeOrientation, PieceOrientation.North),
                Result = Combination.Result,
                ResultRelativeOrientation = PieceOrientation.North
            };

            // Result with overlay laid over it
            yield return new Combination
            {
                BasePiece = Combination.Result,
                OverlayPiece = Combination.OverlayPiece,
                OverlayRelativeOrientation = OrientationHelper.Relative(Combination.ResultRelativeOrientation, Combination.OverlayRelativeOrientation),
                Result = Combination.Result,
                ResultRelativeOrientation = PieceOrientation.North
            };
        }

        private void ParseConfigurationLine(String Line)
        {
            if (String.IsNullOrEmpty(Line)) return;
            if (Line[0] == ';') return;

            var pieces = Line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (pieces.Length != 7) return;

            // base + overlay orient -> result orient
            // 0    1 2       3      4  5      6

            try
            {
                var combination = new Combination
                {
                    BasePiece = pieces[0],
                    OverlayPiece = pieces[2],
                    OverlayRelativeOrientation = (PieceOrientation)Enum.Parse(typeof(PieceOrientation), pieces[3]),
                    Result = pieces[5],
                    ResultRelativeOrientation = (PieceOrientation)Enum.Parse(typeof(PieceOrientation), pieces[6])
                };

                AddCombination(combination);
            }
            catch (Exception)
            {
                // Yum
            }
        }

        public void LoadConfiguration(String Filename)
        {
            foreach (var line in FileUtils.LoadConfigurationLinesFromMultipleSources(ContentPaths.rail_combinations))
                ParseConfigurationLine(line);
        }
    }
}
