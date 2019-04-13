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
    public static class ResourceType
    {
        public static String Wood = "Wood";
        public static String Stone = "Stone";
        public static String Dirt = "Dirt";
        public static String Mana = "Mana";
        public static String Gold = "Gold";
        public static String Iron = "Iron";
        public static String Berry = "Berry";
        public static String Mushroom = "Mushroom";
        public static String Grain = "Grain";
        public static String Sand = "Sand";
        public static String Coal = "Coal";
        public static String Charcoal = "Charcoal";
        public static String Meat = "Meat";
        public static String Bones = "Bone";
        public static String Gem = "Gem";
        public static String Meal = "Meal";
        public static String Ale = "Ale";
        public static String Bread = "Bread";
        public static String Trinket = "Trinket";
        public static String CaveMushroom = "Cave Mushroom";
        public static String GemTrinket = "Gem-set Trinket";
        public static String PineCone = "Pine Cone";
        public static String Peppermint = "Peppermint";
        public static String Coconut = "Coconut";
        public static String Pumkin = "Pumpkin";
        public static String Cactus = "Cactus";
        public static String Egg = "Egg";
        public static String Apple = "Apple";
        public static String Glass = "Glass";
        public static String Brick = "Brick";
        public static String Coins = "Coins";
        public static String EvilSeed = "Seed of Evil";
        public static String Ice = "Ice";
    }
}