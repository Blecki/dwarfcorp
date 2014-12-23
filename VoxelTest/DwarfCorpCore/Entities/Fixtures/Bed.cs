using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Bed : Body
    {
        public Bed()
        {
            
        }

        public Bed(Vector3 position) :
            base("Bed", PlayState.ComponentManager.RootComponent, Matrix.CreateTranslation(position), new Vector3(0.75f, 0.5f, 1.5f), new Vector3(0.5f, 0.5f, 1.0f))
        {
            Texture2D spriteSheet = TextureManager.GetTexture(ContentPaths.Entities.Furniture.bedtex);
            Box bedModel = new Box(PlayState.ComponentManager, "bedbox", this, Matrix.Identity, new Vector3(1.0f, 1.0f, 2.0f), new Vector3(0.5f, 0.5f, 1.0f), PrimitiveLibrary.BoxPrimitives["bed"], spriteSheet);

            Voxel voxelUnder = new Voxel();


            if (PlayState.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
            {
                VoxelListener listener = new VoxelListener(PlayState.ComponentManager, this, PlayState.ChunkManager, voxelUnder);
            }

            Tags.Add("Bed");
            CollisionType = CollisionManager.CollisionType.Static;
        }
    }
}
