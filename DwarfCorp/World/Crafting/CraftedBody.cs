using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System.Linq;

namespace DwarfCorp
{
    public class CraftedBody : GameComponent
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
            CraftDetails details) :
            base(Manager, name, localTransform, bboxExtents, bboxPos)
        {
            this.SetFlag(Flag.ShouldSerialize, true);
            AddChild(details);
        }

    }
}
