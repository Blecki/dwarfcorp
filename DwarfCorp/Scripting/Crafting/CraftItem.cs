// CraftItem.cs
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
using System.Text;
using Newtonsoft.Json;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class CraftItem
    {
        public enum CraftType
        {
            Object,
            Resource
        }

        public enum CraftPrereq
        {
            OnGround,
            NearWall
        }

        public enum CraftActBehaviors
        {
            Normal,
            Trinket,
            Meal,
            Ale,
            Bread,
            GemTrinket,
            Object
        }

        /// <summary>
        /// Unique ID of the craft item.
        /// </summary>
        public string Name = "";
        /// <summary>
        /// Entity factory handle that generates an object from this craft item.
        /// </summary>
        public string EntityName = "";
        /// <summary>
        /// When converting between object and resource representations of the craft item, this is the object corresponding to 
        /// an individual resource.
        /// </summary>
        public string ObjectName = "";

        public String DisplayName = null;
        public String ShortDisplayName = null;
        public String PluralDisplayName = null;

        public List<Quantitiy<Resource.ResourceTags>> RequiredResources = new List<Quantitiy<Resource.ResourceTags>>();
        public Gui.TileReference Icon = null;
        public float BaseCraftTime = 0.0f;
        public string Description = "";
        public CraftType Type = CraftType.Object;
        public List<CraftPrereq> Prerequisites = new List<CraftPrereq>();
        public int CraftedResultsCount = 1;
        public String ResourceCreated = "";
        public string CraftLocation = "Anvil";
        public string Verb = null;
        public string PastTeseVerb = null;
        public string CurrentVerb = null;
        public bool AllowHeterogenous = false;
        public Vector3 SpawnOffset = new Vector3(0.0f, 0.5f, 0.0f);
        public bool AddToOwnedPool = false;
        public bool Moveable = false;
        public bool Deconstructable = true;
        public CraftActBehaviors CraftActBehavior = CraftActBehaviors.Normal;
        public bool AllowRotation = false;
        public string Category = "";
        public bool IsMagical = false;
        public string Tutorial = "";

        /// <summary>
        /// If true, this will be displayed in the list of resources that the player can craft.
        /// </summary>
        public bool AllowUserCrafting = true;

        public void InitializeStrings()
        {
            DisplayName = StringLibrary.TransformDataString(DisplayName, Name);
            PluralDisplayName = StringLibrary.TransformDataString(PluralDisplayName, DisplayName + "s"); // Default to appending an s if the plural name is not specified.
            ShortDisplayName = StringLibrary.TransformDataString(ShortDisplayName, DisplayName);
            Verb = StringLibrary.TransformDataString(Verb, StringLibrary.GetString("build"));
            PastTeseVerb = StringLibrary.TransformDataString(PastTeseVerb, StringLibrary.GetString("built"));
            CurrentVerb = StringLibrary.TransformDataString(CurrentVerb, StringLibrary.GetString("building"));
            Description = StringLibrary.TransformDataString(Description, Description);
        }

        private IEnumerable<ResourceAmount> MergeResources(IEnumerable<ResourceAmount> resources)
        {
            Dictionary<String, int> counts = new Dictionary<String, int>();
            foreach(var resource in resources)
            {
                if(!counts.ContainsKey(resource.Type))
                {
                    counts.Add(resource.Type, 0);
                }
                counts[resource.Type] += resource.Count;
            }

            foreach(var count in counts)
            {
                yield return new ResourceAmount(count.Key, count.Value);
            }
        }

        public Resource ToResource(WorldManager world, List<ResourceAmount> selectedResources, string prefix = "")
        {
            var objectName = String.IsNullOrEmpty(ObjectName) ? Name : ObjectName;
            string resourceName = prefix + objectName + " (" + TextGenerator.GetListString(MergeResources(selectedResources).Select(r => (string)r.Type)) + ")";

            if (ResourceLibrary.Exists(resourceName))
                return ResourceLibrary.GetResourceByName(resourceName);

            var sheet = world.Gui.RenderData.SourceSheets[Icon.Sheet];

            var tex = AssetManager.GetContentTexture(sheet.Texture);
            var numTilesX = tex.Width / sheet.TileWidth;
            var numTilesY = tex.Height / sheet.TileHeight;
            var point = new Point(Icon.Tile % numTilesX, Icon.Tile / numTilesX);
            var toReturn = ResourceLibrary.GenerateResource();
            toReturn.Name = resourceName;
            toReturn.Tags = new List<Resource.ResourceTags>()
                    {
                        Resource.ResourceTags.CraftItem,
                        Resource.ResourceTags.Craft
                    };
            toReturn.MoneyValue = selectedResources.Sum(r => ResourceLibrary.GetResourceByName(r.Type).MoneyValue) * 2.0m;
            toReturn.CraftInfo = new Resource.CraftItemInfo
            {
                Resources = selectedResources,
                CraftItemType = objectName
            };
            toReturn.ShortName = Name;
            toReturn.Description = Description;
            toReturn.GuiLayers = new List<Gui.TileReference>() { Icon };
            toReturn.CompositeLayers = new List<Resource.CompositeLayer>() { new Resource.CompositeLayer() { Asset = sheet.Texture, Frame = point, FrameSize = new Point(sheet.TileWidth, sheet.TileHeight) } };
            toReturn.Tint = Color.White;
            ResourceLibrary.Add(toReturn);

            return toReturn;
        }

        public CraftItem ObjectAsCraftableResource()
        {
            string resourceName = Name + "...";
            CraftItem toReturn = CraftLibrary.GetCraftable(resourceName);
            if (toReturn == null)
            {
                toReturn = this.MemberwiseClone() as CraftItem;
                toReturn.Name = resourceName;
                toReturn.Type = CraftType.Resource;
                toReturn.CraftActBehavior = CraftActBehaviors.Object;
                toReturn.ResourceCreated = "Object";
                toReturn.CraftLocation = String.IsNullOrEmpty(CraftLocation) ? "Anvil" : CraftLocation;
                toReturn.ObjectName = Name;
                toReturn.AllowUserCrafting = false;
                CraftLibrary.Add(toReturn);
            }
            return toReturn;
        }
    }
}
