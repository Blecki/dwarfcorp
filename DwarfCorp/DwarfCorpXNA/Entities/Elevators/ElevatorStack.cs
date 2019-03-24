using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.Elevators
{
    public class ElevatorStack
    {
        public ElevatorPlatform Platform = null;
        public List<ElevatorShaft> Pieces = new List<ElevatorShaft>(); // Make private
        public BoundingBox BoundingBox;

        public static ElevatorStack Create(IEnumerable<ElevatorShaft> Pieces)
        {
            var r = new ElevatorStack();
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
