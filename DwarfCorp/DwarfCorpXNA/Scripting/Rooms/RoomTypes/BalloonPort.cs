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
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class BalloonPort : Stockpile
    {
        [JsonIgnore]
        public static string BalloonPortName { get { return "BalloonPort"; } }
        [JsonIgnore]
        public static RoomData BalloonPortData { get { return RoomLibrary.GetData(BalloonPortName); } }

        public new static RoomData InitializeData()
        {
            Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>> balloonPortResources = new Dictionary<Resource.ResourceTags, Quantitiy<Resource.ResourceTags>>();
            balloonPortResources[Resource.ResourceTags.Stone] = new Quantitiy<Resource.ResourceTags>()
            {
                ResourceType = Resource.ResourceTags.Stone,
                NumResources = 4
            };

            return new RoomData(BalloonPortName, 0, "Stockpile", balloonPortResources, new List<RoomTemplate>(), new Gui.TileReference("rooms", 1))
            {
                Description = "Balloons pick up / drop off resources here.",
                CanBuildBelowGround = false,
                MaxNumRooms = 1
            };
        }

        public BalloonPort()
        {

        }

        public BalloonPort(Faction faction, WorldManager world) :
            base(faction, BalloonPortData, world)
        {
        }

        private void CreateFlag(Vector3 At)
        {
            var flag = new Flag(World.ComponentManager, At + new Vector3(0.5f, 0.5f, 0.5f), World.PlayerCompany.Information);
            AddBody(flag, true);
        }

        public override void OnBuilt()
        {
            var box = GetBoundingBox();
            CreateFlag(new Vector3(box.Min.X, box.Max.Y, box.Min.Z));
            CreateFlag(new Vector3(box.Max.X - 1, box.Max.Y, box.Min.Z));
            CreateFlag(new Vector3(box.Min.X, box.Max.Y, box.Max.Z - 1));
            CreateFlag(new Vector3(box.Max.X - 1, box.Max.Y, box.Max.Z - 1));

        }
    }
}
