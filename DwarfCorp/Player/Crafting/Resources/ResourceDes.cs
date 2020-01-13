using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;

namespace DwarfCorp
{
    public class ResourceDes
    {
        public bool Finished = false;
        public float Progress = 0.0f;
        public bool HasResources = false;
        public CreatureAI ResourcesReservedFor = null;
    }
}