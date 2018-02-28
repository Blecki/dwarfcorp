// RailLibrary.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp.Rail
{
    public class RailLibrary
    {
        private static List<JunctionPattern> Patterns;
        private static List<JunctionPattern> ChainPatterns;
        private static List<RailPiece> Pieces;
        public static CombinationTable CombinationTable;

        private static void Initialize()
        {
            if (Patterns == null)
            {
                Pieces = FileUtils.LoadJsonListFromMultipleSources<RailPiece>(ContentPaths.rail_pieces, null, p => p.Name);
                Patterns = FileUtils.LoadJsonListFromMultipleSources<JunctionPattern>(ContentPaths.rail_patterns, null, p => p.Name);

                CombinationTable = new CombinationTable();
                CombinationTable.LoadConfiguration(ContentPaths.rail_combinations);

                foreach (var piece in Pieces)
                    piece.ComputeConnections();

                ChainPatterns = Patterns
                    // Only pieces with well defined entrance and exits can be used in chains.
                    .Where(p => p.Entrance != null && p.Exit != null)
                    // We need them in every orientation - not worried about redundancies.
                    .SelectMany(p =>
                    {
                        return new JunctionPattern[]
                        {
                            p.Rotate(Orientation.North),
                            p.Rotate(Orientation.East),
                            p.Rotate(Orientation.South),
                            p.Rotate(Orientation.West)
                        };
                    })
                    // We need them with the endpoints switched as well.
                    .SelectMany(p =>
                    {
                        return new JunctionPattern[]
                        {
                            p,
                            new JunctionPattern
                            {
                                Pieces = p.Pieces,
                                Entrance = p.Exit,
                                Exit = p.Entrance,
                            }
                        };
                    })
                    // And they must be positioned so the entrance is at 0,0
                    .Select(p =>
                    {
                        if (p.Entrance.Offset.X == 0 && p.Entrance.Offset.Y == 0) return p;
                        else return new JunctionPattern
                        {
                            Entrance = new JunctionPortal
                            {
                                Direction = p.Entrance.Direction,
                                Offset = Point.Zero
                            },
                            Exit = new JunctionPortal
                            {
                                Direction = p.Exit.Direction,
                                Offset = new Point(p.Exit.Offset.X - p.Entrance.Offset.X, p.Exit.Offset.Y - p.Entrance.Offset.Y)
                            },
                            Pieces = p.Pieces.Select(piece =>
                                new JunctionPiece
                                {
                                    RailPiece = piece.RailPiece,
                                    Orientation = piece.Orientation,
                                    Offset = new Point(piece.Offset.X - p.Entrance.Offset.X, piece.Offset.Y - p.Entrance.Offset.Y)
                                }).ToList(),
                        };
                    })
                    .ToList();
            }
        }

        public static IEnumerable<JunctionPattern> EnumeratePatterns()
        {
            Initialize();
            return Patterns;
        }

        public static IEnumerable<JunctionPattern> EnumerateChainPatterns()
        {
            Initialize();
            return ChainPatterns;
        }

        public static IEnumerable<RailPiece> EnumeratePieces()
        {
            Initialize();
            return Pieces;
        }

        public static RailPiece GetRailPiece(String Name)
        {
            Initialize();
            return Pieces.FirstOrDefault(p => p.Name == Name);
        }
    }
}