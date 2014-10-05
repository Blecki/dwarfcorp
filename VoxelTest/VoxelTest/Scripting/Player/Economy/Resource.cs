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
    /// <summary>
    /// A resource is a kind of item that can be bought or sold, and can be used
    /// to build things.
    /// </summary>
    public class Resource
    {
        public ResourceLibrary.ResourceType Type { get; set; }
        public string ResourceName { get { return ResourceLibrary.ResourceNames[Type]; }}
        public float MoneyValue { get; set; }
        public string Description { get; set; }
        public ImageFrame Image { get; set; }
        public List<ResourceTags> Tags { get; set; }
        public float FoodContent { get; set; }
        public bool SelfIlluminating { get; set; }

        public enum ResourceTags
        {
            Food,
            Material,
            Precious
        }

        public Resource()
        {
            
        }

        public Resource(ResourceLibrary.ResourceType type, float money, string description, ImageFrame image, params ResourceTags[] tags)
        {
            Type = type;
            MoneyValue = money;
            Description = description;
            Image = image;
            Tags = new List<ResourceTags>();
            Tags.AddRange(tags);
            FoodContent = 0;
        }
    }

}