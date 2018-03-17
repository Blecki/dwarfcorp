using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp.Rail
{
    public partial class RailHelper
    {
		private static Vector3? FindConnectionPoint(RailEntity A, RailEntity B)
        {
            if (!A.NeighborRails.Any(n => n.NeighborID == B.GlobalID)) return null;
            return A.NeighborRails.First(n => n.NeighborID == B.GlobalID).Position;
        }

        private static RailConnection FindConnectionFromTransformedEntrancePoint(RailPiece Piece, Matrix TransformToEntitySpace, Vector3 EntrancePoint)
        {
            foreach (var connection in Piece.Connections)
            {
                var transformedEntrance = Vector3.Transform(connection.Entrance, TransformToEntitySpace);
                if ((EntrancePoint - transformedEntrance).LengthSquared() < 0.01f)
                    return connection;
            }

            return null;
        }

        public static IEnumerable<uint> EnumerateForwardNetworkConnections(RailEntity Leaving, RailEntity Entering)
        {
            if (Leaving == null)
            {
                foreach (var neighbor in Entering.NeighborRails)
                    yield return neighbor.NeighborID;
                yield break;
            }
            var connectionPoint = FindConnectionPoint(Leaving, Entering);
            if (connectionPoint.HasValue)
            {
                var piece = RailLibrary.GetRailPiece(Entering.GetPiece().RailPiece);
                var transformToEntitySpace = Matrix.CreateRotationY((float)Math.PI * 0.5f * (float)Entering.GetPiece().Orientation) * Entering.GlobalTransform;
                var connection = FindConnectionFromTransformedEntrancePoint(piece, transformToEntitySpace, connectionPoint.Value);
                if (connection != null)
                    foreach (var exit in connection.Exits)
                    {
                        var transformedExit = Vector3.Transform(exit, transformToEntitySpace);
                        foreach (var neighbor in Entering.NeighborRails)
                            if ((neighbor.Position - transformedExit).LengthSquared() < 0.01f)
                                yield return neighbor.NeighborID;
                    }
            }
        }
    }
}
