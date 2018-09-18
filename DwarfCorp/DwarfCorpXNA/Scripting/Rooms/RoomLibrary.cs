// RoomLibrary.cs
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
using System.Security.Cryptography;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// A static class describing all the kinds of rooms. Can create rooms using templates.
    /// </summary>
    public class RoomLibrary
    {
        private static Dictionary<string, RoomData> roomTypes = new Dictionary<string, RoomData>();
        private static bool staticIntialized = false;

        public static IEnumerable<string> GetRoomTypes()
        {
            return roomTypes.Keys;
        }

        public RoomLibrary()
        {
            if(!staticIntialized)
                InitializeStatics();
        }

        public static void InitializeStatics()
        {
            RegisterType(Stockpile.InitializeData());
            RegisterType(BalloonPort.InitializeData());
            RegisterType(Graveyard.InitializeData());
            RegisterType(AnimalPen.InitializeData());
            RegisterType(Treasury.InitializeData());
            staticIntialized = true;
        }

        public static void RegisterType(RoomData t)
        {
            roomTypes[t.Name] = t;
        }

        public static RoomData GetData(string name)
        {
            return roomTypes.ContainsKey(name) ? roomTypes[name] : null;
        }
      
        public static Room CreateRoom(Faction faction, string name, WorldManager world)
        {
            if (name == BalloonPort.BalloonPortName)
                return new BalloonPort(faction, world);
            else if (name == Stockpile.StockpileName)
                return new Stockpile(faction, world);
            else if (name == Graveyard.GraveyardName)
                return new Graveyard(faction, world);
            else if (name == AnimalPen.AnimalPenName)
                return new AnimalPen(world, faction);
            else if (name == Treasury.TreasuryName)
                return new Treasury(faction, world);
            else
                return null;
        }

        public static void CompleteRoomImmediately(Room Room, List<VoxelHandle> Voxels)
        {
            foreach (var voxel in Voxels)
                Room.AddVoxel(voxel);
            Room.IsBuilt = true;
            Room.OnBuilt();
        }
    }
}
