using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// This component dynamically lights up voxels around it with torch light.
    /// </summary>
    [JsonObject(IsReference = true)]
    internal class LightEmitter : Body
    {
        public byte Intensity { get; set; }
        public byte Range { get; set; }
        public DynamicLight Light { get; set; }


        public LightEmitter()
        {
            
        }

        public LightEmitter(string name, GameComponent parent, Matrix localTransform, Vector3 boundingBoxExtents, Vector3 boundingBoxPos, byte intensity, byte range) :
            base(name, parent, localTransform, boundingBoxExtents, boundingBoxPos)
        {
            Intensity = intensity;
            Range = range;
            Light = null;
        }

        public void UpdateLight(ChunkManager chunks)
        {
            if(Light == null)
            {
                Light = chunks.ChunkData.GetVoxelChunkAtWorldLocation(GlobalTransform.Translation).AddLight(GlobalTransform.Translation, Range, Intensity);
            }
            else
            {
                Voxel vox = new Voxel();
                if (chunks.ChunkData.GetVoxelerenceAtWorldLocation(GlobalTransform.Translation, ref vox))
                {
                    Light.Voxel = vox;
                    chunks.ChunkData.ChunkMap[Light.Voxel.ChunkID].ShouldRebuild = true;
                    chunks.ChunkData.ChunkMap[Light.Voxel.ChunkID].ShouldRecalculateLighting = true;
                }
            }
        }

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            if(HasMoved || Light == null)
            {
                UpdateLight(chunks);
            }


            base.Update(gameTime, chunks, camera);
        }

        public override void Die()
        {
            Light.Destroy();
            base.Die();
        }
    }

}