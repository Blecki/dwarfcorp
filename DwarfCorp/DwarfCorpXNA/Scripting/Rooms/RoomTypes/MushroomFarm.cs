// MushroomFarm.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /*
    [JsonObject(IsReference = true)]
    public class MushroomFarm : Farm
    {
        public static string MushroomFarmName { get { return "MushroomFarm"; } }
        public static RoomData MushroomFarmData { get { return RoomLibrary.GetData(MushroomFarmName); } }

        public static RoomData InitializeData()
        {
            
            Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>> mushroomFarmResources = new Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>>();
            mushroomFarmResources[Resource.ResourceTags.Fungus] = new Quantitiy<Resource.ResourceTags>()
            {
                ResourceType = Resource.ResourceTags.Fungus,
                NumResources = 1
            };

            List<RoomTemplate> mushroomTemplates = new List<RoomTemplate>();
            Texture2D roomIcons = TextureManager.GetTexture(ContentPaths.GUI.room_icons);
            return new RoomData(MushroomFarmName, 6, "TilledSoil", mushroomFarmResources, mushroomTemplates, new ImageFrame(roomIcons, 16, 3, 1))
            {
                Description = "Dwarves can grow mushrooms below ground here",
                CanBuildAboveGround = false,
                MustBeBuiltOnSoil = false,
                MinimumSideLength = 3,
                MinimumSideWidth = 2
            };
        }




        public MushroomFarm()
        {
            RoomData = MushroomFarmData;
        }

        public MushroomFarm(bool designation, IEnumerable<Voxel> designations, ChunkManager chunks) :
            base(designation, designations, MushroomFarmData, chunks)
        {
        }

        public MushroomFarm(IEnumerable<Voxel> voxels, ChunkManager chunks) :
            base(voxels, MushroomFarmData, chunks)
        {
     
        }
        public override Body CreatePlant(Vector3 position)
        {
            return (Body)EntityFactory.CreateEntity<Body>("Mushroom", position);
        }
        
    }
     * */
}
