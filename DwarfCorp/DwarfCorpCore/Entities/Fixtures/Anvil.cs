// Anvil.cs
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
    [JsonObject(IsReference = true)]
    public class Anvil : Fixture
    {

        public Anvil()
        {

        }

        public Anvil(Vector3 position) :
            base(position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(0, 3), PlayState.ComponentManager.RootComponent)
        {
            Name = "Anvil";
            Tags.Add("Anvil");
        }
    }

    [JsonObject(IsReference = true)]
    public class Stove : Fixture
    {

        public Stove()
        {

        }

        public Stove(Vector3 position) :
            base(position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(3, 4), PlayState.ComponentManager.RootComponent)
        {
            Name = "Stove";
            Tags.Add("Stove");


            /*
            new LightEmitter("light", this, Matrix.Identity, new Vector3(0.1f, 0.1f, 0.1f), Vector3.Zero, 5, 4)
            {
                HasMoved = true
            };
             */
        }

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            if (MathFunctions.RandEvent(0.01f))
            {
                PlayState.ParticleManager.Trigger("smoke", GlobalTransform.Translation + Vector3.Up * .5f, Color.White, 1);
            }
            base.Update(gameTime, chunks, camera);
        }
    }


    [JsonObject(IsReference = true)]
    public class Barrel : Fixture
    {

        public Barrel()
        {

        }

        public Barrel(Vector3 position) :
            base(position, new SpriteSheet(ContentPaths.Entities.Furniture.interior_furniture, 32, 32), new Point(1, 0), PlayState.ComponentManager.RootComponent)
        {
            Name = "Barrel";
            Tags.Add("Barrel");
        }
    }
}
