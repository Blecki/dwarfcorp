using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.Elevators
{
    public class ElevatorShaft
    {
        public List<ElevatorTrack> Pieces = new List<ElevatorTrack>(); // Make private
        public BoundingBox BoundingBox;

        public static ElevatorShaft Create(ElevatorTrack FirstPiece)
        {
            var r = new ElevatorShaft();
            r.Pieces.Add(FirstPiece);
            r.UpdateBoundingBox();
            return r;
        }

        public static ElevatorShaft Create(IEnumerable<ElevatorTrack> Pieces)
        {
            var r = new ElevatorShaft();
            r.Pieces.AddRange(Pieces);
            r.UpdateBoundingBox();
            return r;
        }

        private void UpdateBoundingBox()
        {
            BoundingBox = Pieces[0].BoundingBox;
            for (var i = 1; i < Pieces.Count; ++i)
                BoundingBox = BoundingBox.CreateMerged(BoundingBox, Pieces[i].BoundingBox);
        }
    }
}
