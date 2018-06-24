// ResourceType.cs
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
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Text;
using System;

namespace DwarfCorp
{
    public struct ResourceType : IEquatable<ResourceType>
    {
        [JsonProperty]
        private string _value;

        public static ResourceType Wood = "Wood";
        public static ResourceType Stone = "Stone";
        public static ResourceType Dirt = "Dirt";
        public static ResourceType Mana = "Mana";
        public static ResourceType Gold = "Gold";
        public static ResourceType Iron = "Iron";
        public static ResourceType Berry = "Berry";
        public static ResourceType Mushroom = "Mushroom";
        public static ResourceType Grain = "Grain";
        public static ResourceType Sand = "Sand";
        public static ResourceType Coal = "Coal";
        public static ResourceType Meat = "Meat";
        public static ResourceType Bones = "Bone";
        public static ResourceType Gem = "Gem";
        public static ResourceType Meal = "Meal";
        public static ResourceType Ale = "Ale";
        public static ResourceType Bread = "Bread";
        public static ResourceType Trinket = "Trinket";
        public static ResourceType CaveMushroom = "Cave Mushroom";
        public static ResourceType GemTrinket = "Gem-set Trinket";
        public static ResourceType PineCone = "Pine Cone";
        public static ResourceType Peppermint = "Peppermint";
        public static ResourceType Coconut = "Coconut";
        public static ResourceType Pumkin = "Pumpkin";
        public static ResourceType Cactus = "Cactus";
        public static ResourceType Egg = "Egg";
        public static ResourceType Apple = "Apple";
        public static ResourceType Glass = "Glass";
        public static ResourceType Brick = "Brick";
        public static ResourceType Coins = "Coins";
        public static ResourceType EvilSeed = "Seed of Evil";
        public static ResourceType Ice = "Ice";

        public static implicit operator ResourceType(string value)
        {
            if (value == null)
            {
                return new ResourceType { _value = null };
            }
            return new ResourceType { _value = new string(value.ToCharArray()) };
        }

        public static implicit operator string(ResourceType value)
        {
            if (value == null)
                return null;

            return value._value;
        }

        public override string ToString()
        {
            return _value;
        }

        public Resource GetResource()
        {
            if (_value == null)
            {
                return null;
            }
            return ResourceLibrary.GetResourceByName(_value);
        }

        public static bool operator ==(ResourceType A, ResourceType B)
        {
            return A._value == B._value;
        }

        public static bool operator !=(ResourceType A, ResourceType B)
        {
            return A._value != B._value;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ResourceType)) return false;
            return this == (ResourceType)obj;
        }

        public bool Equals(ResourceType other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            if (_value == null)
            {
                return 0;
            }
            return _value.GetHashCode();
        }
    }
}