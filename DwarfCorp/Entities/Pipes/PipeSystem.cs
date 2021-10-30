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

                pipeObject.OnPipeNetworkUpdate();

                var total = pipeObject.Pressure;
                var count = 1.0f;
                foreach (var neighbor in pipeObject.NeighborPipes.Select(id => pipeObject.Manager.FindComponent(id)).OfType<PipeNetworkObject>())
                {
                    if (neighbor.CanSendSteam(pipeObject) && pipeObject.CanReceiveSteam(neighbor))
                    {
                        total += neighbor.Pressure;
                        count += 1;
                    }
                }

                pipeObject.Pressure = total / count;
                pipeObject.Pressure *= 0.995f;
            }
        }
    }
}
