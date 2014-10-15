using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class CraftItem
    {
        public string Name { get; set; }
        public List<ResourceAmount> RequiredResources { get; set; }
        public ImageFrame Image { get; set; }
        public float BaseCraftTime { get; set; }
        public string Description { get; set; }
        public CraftLibrary.CraftItemType CraftType { get; set; }
    }
}
