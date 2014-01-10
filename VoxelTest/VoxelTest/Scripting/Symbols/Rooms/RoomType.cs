using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// A room type has a Name, alters the apperance of voxels, requires resources to build,
    /// and has item templates.
    /// </summary>
    public class RoomType
    {
        public string Name { get; set; }
        public uint ID { get; set; }
        public string FloorType { get; set; }
        public Dictionary<string, ResourceAmount> RequiredResources { get; set; }
        public List<RoomTemplate> Templates { get; set; }

        public RoomType(string name, uint id, string floorTexture, Dictionary<string, ResourceAmount> requiredResources, List<RoomTemplate> templates)
        {
            Name = name;
            ID = id;
            FloorType = floorTexture;
            RequiredResources = requiredResources;
            Templates = templates;
        }
    }

}