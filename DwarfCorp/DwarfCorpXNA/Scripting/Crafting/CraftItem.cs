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

        public string Name { get; set; }
        public List<Quantitiy<Resource.ResourceTags>> RequiredResources { get; set; }
        public Gum.TileReference Icon { get; set; }
        public float BaseCraftTime { get; set; }
        public string Description { get; set; }
        public CraftType Type { get; set; }
        public List<CraftPrereq> Prerequisites { get; set; }
        public ResourceLibrary.ResourceType ResourceCreated { get; set; }
        public List<ResourceAmount> SelectedResources { get; set; }
        public string CraftLocation { get; set; }
        public string Verb { get; set; }
        public string PastTeseVerb { get; set; }
        public string CurrentVerb { get; set; }
        public CraftItem()
        {
            Name = "";
            Prerequisites = new List<CraftPrereq>();
            RequiredResources = new List<Quantitiy<Resource.ResourceTags>>();
            BaseCraftTime = 0.0f;
            Description = "";
            Type = CraftType.Object;
            ResourceCreated = "";
            SelectedResources = new List<ResourceAmount>();
            CraftLocation = "Anvil";
            Verb = "Build";
            PastTeseVerb = "Built";
            CurrentVerb = "Building";
        }
    }
}
