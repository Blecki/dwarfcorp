// EntityFactory.cs
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
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;

namespace DwarfCorp
{
    public static class DebugDactories
    {
        [EntityFactory("RandTrinket")]
        private static GameComponent __factory0(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            var randResource = ResourceLibrary.GenerateTrinket(Datastructures.SelectRandom(ResourceLibrary.Resources.Where(r => r.Value.Tags.Contains(Resource.ResourceTags.Material))).Key, MathFunctions.Rand(0.1f, 3.5f));

            if (MathFunctions.RandEvent(0.5f))
                randResource = ResourceLibrary.EncrustTrinket(randResource.Name, Datastructures.SelectRandom(ResourceLibrary.Resources.Where(r => r.Value.Tags.Contains(Resource.ResourceTags.Gem))).Key);

            return new ResourceEntity(Manager, new ResourceAmount(randResource.Name), Position);
        }

        [EntityFactory("RandFood")]
        private static GameComponent __factory1(ComponentManager Manager, Vector3 Position, Blackboard Data)
        {
            IEnumerable<Resource> foods = ResourceLibrary.GetResourcesByTag(Resource.ResourceTags.RawFood);
            Resource randresource = ResourceLibrary.CreateMeal(Datastructures.SelectRandom(foods).Name, Datastructures.SelectRandom(foods).Name);
            return new ResourceEntity(Manager, new ResourceAmount(randresource.Name), Position);
        }
    }
}
