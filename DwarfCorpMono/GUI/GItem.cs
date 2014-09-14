using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace DwarfCorp
{
    public class GItem
    {
        public string Name { get; set; }
        public ImageFrame Image { get; set; }
        public int MaxAmount { get; set; }
        public int MinAmount { get; set; }
        public List<string> Tags { get; set; }
        public int CurrentAmount { get; set; }
        public float Price { get; set; }

        public GItem(string name, ImageFrame imag, int min, int max, int currentAmount, float price, IEnumerable<string> tags)
        {
            Name = name;
            Image = imag;
            MinAmount = min;
            MaxAmount = max;
            Tags = new List<string>();
            Tags.AddRange(tags);
            CurrentAmount = currentAmount;
            Price = price;
        }
    }
}
