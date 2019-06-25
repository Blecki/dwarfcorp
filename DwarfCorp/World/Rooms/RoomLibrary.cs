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
    public static partial class Library
    {
        private static List<RoomType> RoomTypes = null;
        private static Dictionary<string, Func<RoomType, WorldManager, Zone>> RoomFuncs = null;
        private static bool RoomTypesInitialized = false;

        private static void InitializeRoomTypes()
        {
            if (RoomTypesInitialized)
                return;
            RoomTypesInitialized = true;

            RoomTypes = FileUtils.LoadJsonListFromDirectory<RoomType>(ContentPaths.room_types, null, d => d.Name);
            RoomFuncs = new Dictionary<string, Func<RoomType, WorldManager, Zone>>();

            foreach (var method in AssetManager.EnumerateModHooks(typeof(RoomFactoryAttribute), typeof(Zone), new Type[]
            {
                typeof(RoomType),
                typeof(WorldManager)
            }))
            {
                var attribute = method.GetCustomAttributes(false).FirstOrDefault(a => a is RoomFactoryAttribute) as RoomFactoryAttribute;
                if (attribute == null) continue;
                RoomFuncs[attribute.Name] = (data, world) => method.Invoke(null, new Object[] { data, world }) as Zone;
            }
        }

        public static RoomType GetRoomData(string Name)
        {
            InitializeRoomTypes();
            return RoomTypes.Where(r => r.Name == Name).FirstOrDefault();
        }
      
        public static Zone CreateRoom(string name, WorldManager world)
        {
            InitializeRoomTypes();
            if (RoomFuncs.ContainsKey(name))
                return RoomFuncs[name](GetRoomData(name), world);
            return null;
        }

        // Todo: Does not belong here.
        public static void CompleteRoomImmediately(Zone Room, List<VoxelHandle> Voxels)
        {
            foreach (var voxel in Voxels)
                Room.AddVoxel(voxel);
            Room.IsBuilt = true;
            Room.OnBuilt();
        }

        public static IEnumerable<string> EnumerateRoomTypeNames()
        {
            InitializeRoomTypes();
            return RoomTypes.Select(r => r.Name);
        }
    }
}
