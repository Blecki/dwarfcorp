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
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class RoomFactoryAttribute : Attribute
    {
        public String Name;

        public RoomFactoryAttribute(String Name)
        {
            this.Name = Name;
        }
    }

    /// <summary>
    /// A static class describing all the kinds of rooms. Can create rooms using templates.
    /// </summary>
    public class RoomLibrary
    {
        private static List<RoomData> RoomTypes = null;
        private static Dictionary<string, Func<RoomData, Faction, WorldManager, Room>> RoomFuncs { get; set; }

        public static IEnumerable<string> GetRoomTypes()
        {
            InitializeStatics();
            return RoomTypes.Select(r => r.Name);
        }

        public RoomLibrary()
        {
        }

        private static void InitializeStatics()
        {
            if (RoomTypes != null) return;

            RoomTypes = FileUtils.LoadJsonListFromDirectory<RoomData>(ContentPaths.room_types, null, d => d.Name);
            RoomFuncs = new Dictionary<string, Func<RoomData, Faction, WorldManager, Room>>();

            foreach (var method in AssetManager.EnumerateModHooks(typeof(RoomFactoryAttribute), typeof(Room), new Type[]
            {
                typeof(RoomData),
                typeof(Faction),
                typeof(WorldManager)
            }))
            {
                var attribute = method.GetCustomAttributes(false).FirstOrDefault(a => a is RoomFactoryAttribute) as RoomFactoryAttribute;
                if (attribute == null) continue;
                RoomFuncs[attribute.Name] = (data, faction, world) => method.Invoke(null, new Object[] { data, faction, world }) as Room;
            }
        }

        public static RoomData GetData(string Name)
        {
            InitializeStatics();
            return RoomTypes.Where(r => r.Name == Name).FirstOrDefault();
        }
      
        public static Room CreateRoom(Faction faction, string name, WorldManager world)
        {
            InitializeStatics();
            if (RoomFuncs.ContainsKey(name))
                return RoomFuncs[name](GetData(name), faction, world);
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
