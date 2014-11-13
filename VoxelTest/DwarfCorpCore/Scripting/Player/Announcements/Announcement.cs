using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Announcement
    {
        public string Name { get; set; }
        public string Message { get; set; }
        public Color Color { get; set; }
        public ImageFrame Icon { get; set; }

        public Announcement()
        {
            
        }


    }
}
