using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp
{



    public class Stockpile : Zone
    {
        private static uint maxID = 0;

        public static uint NextID()
        {
            maxID++;
            return maxID;
        }


        public Stockpile(string id, ChunkManager chunk) :
            base(id, chunk)
        {
            ReplacementType = VoxelLibrary.GetVoxelType("Stockpile");
        }

    }
}
