using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace DwarfCorp
{
    public class ResourceAmount
    {
        public Resource ResourceType { get; set; }
        public float NumResources { get; set; }
    }

    public class Resource
    {
        public string ResourceName { get; set; }
        public float MoneyValue { get; set; }
        public string Description { get; set; }
        public ImageFrame Image { get; set; }
        public List<string> Tags { get; set; }

        public Resource(string name, float money, string description, ImageFrame image, params string[] tags)
        {
            ResourceName = name;
            MoneyValue = money;
            Description = description;
            Image = image;
            Tags = new List<string>();
            Tags.AddRange(tags);
        }
    }
}
