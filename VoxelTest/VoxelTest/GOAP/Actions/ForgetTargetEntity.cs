using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{

    public class ForgetTargetEntity : Action
    {
        public ForgetTargetEntity()
        {
            Name = "ForgetTargetEntity";
            PreCondition = new WorldState();
            PreCondition[GOAPStrings.TargetType] = GOAP.TargetType.Entity;

            Effects = new WorldState();
            Effects[GOAPStrings.AtTarget] = false;
            Effects[GOAPStrings.TargetType] = GOAP.TargetType.None;
            Effects[GOAPStrings.TargetTags] = null;
            Effects[GOAPStrings.TargetEntity] = null;
            Cost = 1.0f;
        }
    }

}