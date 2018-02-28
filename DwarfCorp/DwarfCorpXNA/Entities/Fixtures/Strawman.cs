// Strawman.cs
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
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Strawman : CraftedFixture
    {
        [EntityFactory("Strawman")]
        private static GameComponent __factory(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            switch (MathFunctions.RandInt(0, 3))
            {
                case 0:
                    return new Strawman(Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null));
                case 1:
                    return new WeightRack(Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null));
                case 2:
                    return new PunchingBag(Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null));
                default:
                    return new Strawman(Manager, Position, Data.GetData<List<ResourceAmount>>("Resources", null));
            }
        }


        public Strawman()
        {

        }

        public Strawman(ComponentManager manager, Vector3 position, List<ResourceAmount> Resources) :
            base(manager, position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(1, 5), new DwarfCorp.CraftDetails(manager, "Weight Rack", Resources))
        {
            Name = "Strawman";
            Tags.Add("Strawman");
            Tags.Add("Train");
            GetRoot().GetComponent<Health>().MaxHealth = 500;
            GetRoot().GetComponent<Health>().Hp = 500;
        }
    }
}
