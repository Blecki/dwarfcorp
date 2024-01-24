using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp.SteamPipes
{
    public class PipeSystem : EngineModule
    {
        [UpdateSystemFactory]
        private static EngineModule __factory(WorldManager World)
        {
            return new PipeSystem();
        }

        public override ModuleManager.UpdateTypes UpdatesWanted => ModuleManager.UpdateTypes.ComponentCreated 
            | ModuleManager.UpdateTypes.ComponentDestroyed 
            | ModuleManager.UpdateTypes.Update;

        private List<PipeNetworkObject> Objects = new List<PipeNetworkObject>();

        public override void ComponentCreated(GameComponent C)
        {
            if (C is PipeNetworkObject steamObject)
                Objects.Add(steamObject);
        }

        public override void ComponentDestroyed(GameComponent C)
        {
            if (C is PipeNetworkObject steamObject)
                Objects.Remove(steamObject);
        }

        public override void Update(DwarfTime GameTime, WorldManager World)
        {
            // Todo: Limit update rate.

            foreach (var pipeObject in Objects)
            {
                var currentVoxel = GlobalVoxelCoordinate.FromVector3(pipeObject.Position);
                if (currentVoxel != pipeObject.Coordinate)
                {
                    pipeObject.Coordinate = currentVoxel;
                    pipeObject.PropogateTransforms();
                    if (pipeObject.GlobalTransform.Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation))
                        pipeObject.Orientation = OrientationHelper.DetectOrientationFromRotation(rotation);

                    pipeObject.DetachFromNeighbors();
                    pipeObject.AttachToNeighbors();
                    pipeObject.Primitive = null;
                }

                pipeObject.OnPipeNetworkUpdate(World, this);

                //var total = pipeObject.Pressure;
                //var count = 1.0f;
                //foreach (var neighbor in pipeObject.NeighborPipes.Select(id => pipeObject.Manager.FindComponent(id)).OfType<PipeNetworkObject>())
                //{
                //    if (neighbor.CanSendSteam(pipeObject) && pipeObject.CanReceiveSteam(neighbor))
                //    {
                //        total += neighbor.Pressure;
                //        count += 1;
                //    }
                //}

                //pipeObject.Pressure = total / count;
                //pipeObject.Pressure *= 0.995f;
            }
        }

        private class OpenSearchNode
        {
            public PipeNetworkObject ParentCell;
            public PipeNetworkObject ThisCell;
            public int Cost;
        }

        private IEnumerable<OpenSearchNode> EnumerateOpenNeighbors(WorldManager World, OpenSearchNode Of)
        {
            if (Of.Cost > 25)
                yield break;

            foreach (var neighbor in Of.ThisCell.NeighborPipes.Select(n => World.ComponentManager.FindComponent(n)))
                if (neighbor.HasValue(out var v) && v is PipeNetworkObject pipe)
                    yield return new OpenSearchNode { ParentCell = Of.ThisCell, ThisCell = pipe, Cost = Of.Cost + 1 };
        }

        public PipeNetworkObject SearchNetwork(WorldManager World, PipeNetworkObject Source, Func<PipeNetworkObject, bool> Test)
        {
            var openNodes = new PriorityQueue<OpenSearchNode, int>();
            var closedNodes = new HashSet<uint>();
            openNodes.Enqueue(new OpenSearchNode { ParentCell = Source, ThisCell = Source, Cost = 0 }, 0);


            while (openNodes.Count > 0)
            {
                var current = openNodes.Dequeue();
                if (Test(current.ThisCell))
                    return current.ThisCell;

                foreach (var neighbor in EnumerateOpenNeighbors(World, current))
                {
                    if (closedNodes.Contains(neighbor.ThisCell.GlobalID)) continue;
                    closedNodes.Add(neighbor.ThisCell.GlobalID);

                   openNodes.Enqueue(neighbor, neighbor.Cost);
                }
            }

            return null;
        }
    }
}
