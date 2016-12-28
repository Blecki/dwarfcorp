// GoResearchSpellAct.cs
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
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     A creature goes to a voxel location, and places an object with the desired tags there to build it.
    /// </summary>
    [JsonObject(IsReference = true)]
    internal class GoResearchSpellAct : CompoundCreatureAct
    {
        public GoResearchSpellAct()
        {
        }


        public GoResearchSpellAct(CreatureAI creature, SpellTree.Node node) :
            base(creature)
        {
            Spell = node;
            Name = "Research spell " + node.Spell.Name;
        }

        public SpellTree.Node Spell { get; set; }

        public override void Initialize()
        {
            Act unreserveAct = new Wrap(() => Creature.Unreserve("Research"));
            Tree = new Sequence(
                new Wrap(() => Creature.FindAndReserve("Research", "Research")),
                new Sequence
                    (
                    new GoToTaggedObjectAct(Agent)
                    {
                        Tag = "Research",
                        Teleport = false,
                        TeleportOffset = new Vector3(1, 0, 0),
                        ObjectName = "Research"
                    },
                    new ResearchSpellAct(Agent, Spell),
                    unreserveAct
                    ) | new Sequence(unreserveAct, false)
                ) | new Sequence(unreserveAct, false);
            base.Initialize();
        }


        public override void OnCanceled()
        {
            foreach (Status statuses in Creature.Unreserve("Research"))
            {
            }
            base.OnCanceled();
        }
    }
}