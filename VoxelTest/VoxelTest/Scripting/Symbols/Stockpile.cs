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

    /// <summary>
    /// A stockpile is a kind of zone which contains items on top of it.
    /// </summary>
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