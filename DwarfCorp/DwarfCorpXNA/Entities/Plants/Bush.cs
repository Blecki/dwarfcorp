// Bush.cs
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
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class Bush : Plant
    {
        public Bush() { }

        public Bush(ComponentManager componentManager, Vector3 position, string asset, float bushSize) :
            base(componentManager, "Berry Bush", position, MathFunctions.Rand(-0.1f, 0.1f), new Vector3(bushSize, bushSize, bushSize), asset, bushSize)
        {
            AddChild(new Health(componentManager, "HP", 30 * bushSize, 0.0f, 30 * bushSize));
            AddChild(new Flammable(componentManager, "Flames"));

            var particles = AddChild(new ParticleTrigger("Leaves", Manager, "LeafEmitter", Matrix.Identity, LocalBoundingBoxOffset, GetBoundingBox().Extents())
            {
                SoundToPlay = ContentPaths.Audio.Oscar.sfx_env_bush_harvest_1
            }) as ParticleTrigger;

            Tags.Add("Vegetation");
            Tags.Add("Bush");
            Tags.Add("EmitsFood");

            Inventory inventory = AddChild(new Inventory(componentManager, "Inventory", BoundingBox.Extents(), LocalBoundingBoxOffset)) as Inventory;

            inventory.AddResource(new ResourceAmount()
            {
                NumResources = 3,
                ResourceType = ResourceType.Berry
            });

            CollisionType = CollisionType.Static;
            PropogateTransforms();
        }

        public override void CreateCosmeticChildren(ComponentManager Manager)
        {
            base.CreateCosmeticChildren(Manager);
        }
    }
}
