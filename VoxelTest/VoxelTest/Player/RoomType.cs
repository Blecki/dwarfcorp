using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class RoomType
    {
        public string Name { get; set; }
        public uint ID { get; set; }
        public Point FloorTexture { get; set; }
        public Dictionary<string, ResourceAmount> RequiredResources { get; set; }
        public List<RoomTemplate> Templates { get; set; }

        public RoomType(string name, uint id, Point floorTexture, Dictionary<string, ResourceAmount> requiredResources, List<RoomTemplate> templates)
        {
            Name = name;
            ID = id;
            FloorTexture = floorTexture;
            RequiredResources = requiredResources;
            Templates = templates;

            
        }

    }

}
