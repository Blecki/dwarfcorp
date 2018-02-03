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
            GemTrinket
        }

        public string Name = "";
        public string EntityName = "";
        public List<Quantitiy<Resource.ResourceTags>> RequiredResources = new List<Quantitiy<Resource.ResourceTags>>();
        public Gui.TileReference Icon = null;
        public float BaseCraftTime = 0.0f;
        public string Description = "";
        public CraftType Type = CraftType.Object;
        public List<CraftPrereq> Prerequisites = new List<CraftPrereq>();
        public ResourceType ResourceCreated = "";
        public string CraftLocation = "Anvil";
        public string Verb = "Build";
        public string PastTeseVerb = "Built";
        public string CurrentVerb = "Building";
        public bool AllowHeterogenous = false;
        public Vector3 SpawnOffset = new Vector3(0.0f, 0.5f, 0.0f);
        public bool AddToOwnedPool = false;
        public bool Moveable = false;
        public CraftActBehaviors CraftActBehavior = CraftActBehaviors.Normal;
    }
}
