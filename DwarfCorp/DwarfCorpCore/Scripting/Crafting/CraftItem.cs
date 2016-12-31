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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A CraftItem is a kind of thing that a Dwarf can create from raw resources.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class CraftItem
    {
        /// <summary>
        /// Items can only be built in certain locations.
        /// </summary>
        public enum CraftPrereq
        {
            /// <summary>
            /// Items must be placed on top of another voxel.
            /// </summary>
            OnGround,
            /// <summary>
            /// Items must be placed adjacent to a wall.
            /// </summary>
            NearWall
        }

        /// <summary>
        /// The type of the object to be crafted.
        /// </summary>
        public enum CraftType
        {
            /// <summary>
            /// These things are placed into the world as physical objects (like traps or doors)
            /// </summary>
            Object,
            /// <summary>
            /// These become intermediate resources that get placed into stockpiles.
            /// </summary>
            Resource
        }

        public CraftItem()
        {
            Name = "";
            Prerequisites = new List<CraftPrereq>();
            RequiredResources = new List<Quantitiy<Resource.ResourceTags>>();
            Image = null;
            BaseCraftTime = 0.0f;
            Description = "";
            Type = CraftType.Object;
            ResourceCreated = "";
            SelectedResources = new List<ResourceAmount>();
            CraftLocation = "Anvil";
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the required resources (as tags)
        /// </summary>
        /// <value>
        /// The required resources.
        /// </value>
        public List<Quantitiy<Resource.ResourceTags>> RequiredResources { get; set; }
        /// <summary>
        /// Gets or sets the image to display in GUIs.
        /// </summary>
        /// <value>
        /// The image.
        /// </value>
        public ImageFrame Image { get; set; }
        /// <summary>
        /// For level 1 dwarves, this is how long (in seconds) the item takes to craft.
        /// </summary>
        /// <value>
        /// The base craft time.
        /// </value>
        public float BaseCraftTime { get; set; }
        /// <summary>
        /// A longer description of the item to display to the player.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public string Description { get; set; }
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public CraftType Type { get; set; }
        /// <summary>
        /// Gets or sets the prerequisites.
        /// </summary>
        /// <value>
        /// The prerequisites.
        /// </value>
        public List<CraftPrereq> Prerequisites { get; set; }
        /// <summary>
        /// A resource of this type gets created by the action of crafting this item.
        /// </summary>
        /// <value>
        /// The resource created.
        /// </value>
        public ResourceLibrary.ResourceType ResourceCreated { get; set; }
        /// <summary>
        /// These are the resources the player has selected to craft the item with.
        /// </summary>
        /// <value>
        /// The selected resources.
        /// </value>
        public List<ResourceAmount> SelectedResources { get; set; }
        /// <summary>
        /// The player must have already built an object with this tag in order to build the item.
        /// </summary>
        /// <value>
        /// The craft location.
        /// </value>
        public string CraftLocation { get; set; }
    }
}