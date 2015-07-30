// BalloonPort.cs
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
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class BalloonPort : Stockpile
    {
        public static string BalloonPortName { get { return "BalloonPort"; } }
        public static RoomData BalloonPortData { get { return RoomLibrary.GetData(BalloonPortName); } }

        public new static RoomData InitializeData()
        {
            Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>> balloonPortResources = new Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>>();
            balloonPortResources[Resource.ResourceTags.Stone] = new Quantitiy<Resource.ResourceTags>()
            {
                ResourceType = Resource.ResourceTags.Stone,
                NumResources = 1
            };

            RoomTile[,] flagTemplate =
            {
                {
                    RoomTile.None,
                    RoomTile.Wall | RoomTile.Edge
                },
                {
                    RoomTile.Wall | RoomTile.Edge,
                    RoomTile.Flag
                }
            };

            RoomTile[,] flagAccesories =
            {
                {
                    RoomTile.None,
                    RoomTile.None
                },
                {
                    RoomTile.None,
                    RoomTile.None
                }
            };

            RoomTemplate flag = new RoomTemplate(PlacementType.All, flagTemplate, flagAccesories);


            List<RoomTemplate> balloonTemplates = new List<RoomTemplate>
            {
                flag
            };
            Texture2D roomIcons = TextureManager.GetTexture(ContentPaths.GUI.room_icons);
            return new RoomData(BalloonPortName, 0, "Stockpile", balloonPortResources, balloonTemplates, new ImageFrame(roomIcons, 16, 1, 0))
            {
                Description = "Balloons pick up / drop off resources here.",
                CanBuildBelowGround = false
            };
        }

        public BalloonPort()
        {
        }

        public BalloonPort(Faction faction, bool designation, IEnumerable<Voxel> designations, ChunkManager chunks) :
            base(faction, designation, designations, BalloonPortData, chunks)
        {
        }

        public BalloonPort(Faction faction, IEnumerable<Voxel> voxels, ChunkManager chunks) :
            base(faction, voxels, BalloonPortData, chunks)
        {
            OnBuilt();
        }

    }
}
