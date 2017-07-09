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
        // Todo: Lifet out of class.
        private Box Model;

        public Bed()
        {
            Tags.Add("Bed");
            CollisionType = CollisionManager.CollisionType.Static;
        }

        public Bed(ComponentManager manager, Vector3 position) :
            base(manager, "Bed", Matrix.CreateTranslation(position), new Vector3(1.5f, 0.5f, 0.75f), new Vector3(-0.5f + 1.5f * 0.5f, -0.5f + 0.25f, -0.5f + 0.75f * 0.5f))
        {
            Tags.Add("Bed");
            CollisionType = CollisionManager.CollisionType.Static;

            Texture2D spriteSheet = TextureManager.GetTexture(ContentPaths.Entities.Furniture.bedtex);
            Model = AddChild(new Box(manager, "bedbox", Matrix.CreateTranslation(-0.5f, -0.5f, -0.5f) * Matrix.CreateRotationY((float)Math.PI * 0.5f), new Vector3(1.0f, 1.0f, 2.0f), new Vector3(0.5f, 0.5f, 1.0f), "bed", spriteSheet)) as Box;

            var voxelUnder = new VoxelHandle();

            if (manager.World.ChunkManager.ChunkData.GetFirstVoxelUnder(position, ref voxelUnder))
                AddChild(new VoxelListener(manager, manager.World.ChunkManager, voxelUnder));
        }
    }
}
