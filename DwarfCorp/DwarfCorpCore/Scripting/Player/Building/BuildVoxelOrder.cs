// BuildVoxelOrder.cs
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

namespace DwarfCorp
{
    /// <summary>
    /// This designation specifies that a given voxel from a given BuildRoom should be built.
    /// A BuildRoom build designation is actually a colletion of these.
    /// </summary>
    public class BuildVoxelOrder
    {
        public Room ToBuild { get; set; }
        public Voxel Voxel { get; set; }
        public BuildRoomOrder Order { get; set; }

        public BuildVoxelOrder(BuildRoomOrder order, Room toBuild, Voxel voxel)
        {
            Order = order;
            ToBuild = toBuild;
            Voxel = voxel;
        }



        public void Build()
        {
            Order.Build();
        }

        public Resource.ResourceTags GetNextRequiredResource()
        {
 
            foreach (var s in ToBuild.RoomData.RequiredResources.Keys)
            {
                if(!Order.PutResources.ContainsKey(s))
                {
                    return ToBuild.RoomData.RequiredResources[s].ResourceType;
                }
                else if(Order.PutResources[s].NumResources < Math.Max((int) (ToBuild.RoomData.RequiredResources[s].NumResources * Order.VoxelOrders.Count * 0.25f), 1))
                {
                    return ToBuild.RoomData.RequiredResources[s].ResourceType;
                }
            }

            return Resource.ResourceTags.None;
        }


    }

}