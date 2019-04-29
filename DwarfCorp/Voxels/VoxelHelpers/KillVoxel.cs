using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using System.Threading;
using System.Collections.Concurrent;

namespace DwarfCorp
{
    public partial class VoxelHelpers
    {
        public static List<GameComponent> KillVoxel(WorldManager World, VoxelHandle Voxel)
        {
            if (World.Master != null)
                World.Master.Faction.OnVoxelDestroyed(Voxel);

            if (!Voxel.IsValid || Voxel.IsEmpty)
                return null;

            if (World.ParticleManager != null)
            {
                World.ParticleManager.Trigger(Voxel.Type.ParticleType, 
                    Voxel.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 20);
                World.ParticleManager.Trigger("puff", 
                    Voxel.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f), Color.White, 20);
            }

            Voxel.Type.ExplosionSound.Play(Voxel.WorldPosition);

            List<GameComponent> emittedResources = null;
            if (Voxel.Type.ReleasesResource)
            {
                if (MathFunctions.Rand() < Voxel.Type.ProbabilityOfRelease)
                {
                    emittedResources = new List<GameComponent>
                    {
                        EntityFactory.CreateEntity<GameComponent>(Voxel.Type.ResourceToRelease + " Resource",
                            Voxel.WorldPosition + new Vector3(0.5f, 0.5f, 0.5f))
                    };
                }
            }

            Voxel.Type = VoxelLibrary.EmptyType;

            return emittedResources;
        }
    }
}
