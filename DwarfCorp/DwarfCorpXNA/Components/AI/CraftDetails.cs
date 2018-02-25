// Creature.cs
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
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class CraftDetails : GameComponent
    {
        // Corresponds to a type in the CraftLibrary
        public string CraftType = "";
        // The Resources used to craft the item.
        public List<ResourceAmount> Resources = new List<ResourceAmount>();

        public CraftDetails()
        {
            this.SetFlag(Flag.ShouldSerialize, true);
        }

        public CraftDetails(ComponentManager manager) :
            base(manager)
        {
            this.SetFlag(Flag.ShouldSerialize, true);
        }

        public CraftDetails(ComponentManager manager, string craftType, List<ResourceAmount> resources = null) :
            this(manager)
        {
            CraftType = craftType;
            var libraryType = CraftLibrary.CraftItems[craftType];
            if (resources == null)
            {
                Resources = new List<ResourceAmount>();
                var required = libraryType.RequiredResources;
                foreach (var requirement in required)
                {
                    Resources.Add(new ResourceAmount(ResourceLibrary.GetLeastValuableWithTag(requirement.ResourceType), requirement.NumResources));
                }
            }
            else
            {
                Resources = resources;
            }
        }



        public override void Die()
        {
            var body = Parent.GetRoot().GetComponent<Body>();
            if (body != null)
            {
                var bounds = body.GetBoundingBox();
                foreach(var resource in Resources)
                {
                    for (int i = 0; i < resource.NumResources; i++)
                    {
                        Vector3 pos = MathFunctions.RandVector3Box(bounds);
                        EntityFactory.CreateEntity<Body>(resource.ResourceType + " Resource", pos);
                    }
                }
            }
            base.Die();
        }
    }

    [JsonObject(IsReference = true)]
    public class FixtureCraftDetails : CraftDetails
    {
        // Defines a mapping from specific resource tags in a crafted
        // resource to frames in a sprite sheet.
        [JsonIgnore]
        public Dictionary<Resource.ResourceTags, Point> Sprites;
        // The default sprite sheet to use if there is no such mapping.
        [JsonIgnore]
        public Point DefaultSpriteFrame;

        public Point GetSpritesheetFrame(ResourceLibrary.ResourceType resourceType)
        {
            var resource = ResourceLibrary.GetResourceByName(resourceType);
            foreach (var tag in resource.Tags)
            {
                if (Sprites.ContainsKey(tag))
                {
                    return Sprites[tag];
                }
            }

            return DefaultSpriteFrame;
        }

        public FixtureCraftDetails()
        {
            this.SetFlag(Flag.ShouldSerialize, true);
        }

        public FixtureCraftDetails(ComponentManager manager) :
            base(manager)
        {
            this.SetFlag(Flag.ShouldSerialize, true);
        }

        public FixtureCraftDetails Clone()
        {
            var details = new FixtureCraftDetails(Manager)
            {
                Resources = new List<ResourceAmount>(),
                Sprites = new Dictionary<Resource.ResourceTags, Point>(),
                DefaultSpriteFrame = DefaultSpriteFrame,
                CraftType = CraftType
            };
            details.Resources.AddRange(Resources);
            foreach (var pair in Sprites)
            {
                details.Sprites[pair.Key] = pair.Value;
            }
            return details;
        }

    }

    [JsonObject(IsReference = true)]
    public class CraftedFixture : Fixture
    {
        [JsonIgnore]
        public FixtureCraftDetails CraftDetails {  get { return GetRoot().GetComponent<FixtureCraftDetails>(); } }

        public CraftedFixture()
        {
            this.SetFlag(Flag.ShouldSerialize, true);
        }

        public CraftedFixture(ComponentManager manager, Vector3 position, SpriteSheet sheet, Point frame, CraftDetails details, SimpleSprite.OrientMode OrientMode = SimpleSprite.OrientMode.Spherical) :
            base(manager, position, sheet, frame, OrientMode)
        {
            this.SetFlag(Flag.ShouldSerialize, true);
            AddChild(details);
        }

        public CraftedFixture(
            ComponentManager Manager,
            Vector3 position,
            SpriteSheet asset,
            FixtureCraftDetails details,
            SimpleSprite.OrientMode OrientMode = SimpleSprite.OrientMode.Spherical) :
            base(Manager, position, asset, details.GetSpritesheetFrame(details.Resources[0].ResourceType), OrientMode)
        {
            this.SetFlag(Flag.ShouldSerialize, true);
            AddChild(details);
        }

    }

    [JsonObject(IsReference = true)]
    public class CraftedBody : Body
    {
        [JsonIgnore]
        public FixtureCraftDetails CraftDetails { get { return GetRoot().GetComponent<FixtureCraftDetails>(); } }

        public CraftedBody()
        {
            this.SetFlag(Flag.ShouldSerialize, true);
        }

        public CraftedBody(
            ComponentManager Manager,
            string name,
            Matrix localTransform,
            Vector3 bboxExtents,
            Vector3 bboxPos,
            CraftDetails details,
            bool addToCollisionManager=true) :
            base(Manager, name, localTransform, bboxExtents, bboxPos, addToCollisionManager)
        {
            this.SetFlag(Flag.ShouldSerialize, true);
            AddChild(details);
        }

    }
}
