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
    public class RoomLibrary // Todo: Mono Library
    {
        private static List<RoomData> RoomTypes = null;
        private static Dictionary<string, Func<RoomData, Faction, WorldManager, Zone>> RoomFuncs { get; set; }
        private static bool RoomTypesInitialized = false;

        public static IEnumerable<string> GetRoomTypes()
        {
            InitializeRoomTypes();
            return RoomTypes.Select(r => r.Name);
        }

        public RoomLibrary()
        {
        }

        private static void InitializeRoomTypes()
        {
            if (RoomTypesInitialized)
                return;
            RoomTypesInitialized = true;

            RoomTypes = FileUtils.LoadJsonListFromDirectory<RoomData>(ContentPaths.room_types, null, d => d.Name);
            RoomFuncs = new Dictionary<string, Func<RoomData, Faction, WorldManager, Zone>>();

            foreach (var method in AssetManager.EnumerateModHooks(typeof(RoomFactoryAttribute), typeof(Zone), new Type[]
            {
                typeof(RoomData),
                typeof(Faction),
                typeof(WorldManager)
            }))
            {
                var attribute = method.GetCustomAttributes(false).FirstOrDefault(a => a is RoomFactoryAttribute) as RoomFactoryAttribute;
                if (attribute == null) continue;
                RoomFuncs[attribute.Name] = (data, faction, world) => method.Invoke(null, new Object[] { data, faction, world }) as Zone;
            }
        }

        public static RoomData GetData(string Name)
        {
            InitializeRoomTypes();
            return RoomTypes.Where(r => r.Name == Name).FirstOrDefault();
        }
      
        public static Zone CreateRoom(Faction faction, string name, WorldManager world)
        {
            InitializeRoomTypes();
            if (RoomFuncs.ContainsKey(name))
                return RoomFuncs[name](GetData(name), faction, world);
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
    }
}
