// GoTrainAct.cs
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

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    internal class GoTrainAct : CompoundCreatureAct
    {
        public bool Magical { get; set; }

        public GoTrainAct()
        {

        }


        public GoTrainAct(CreatureAI creature) :
            base(creature)
        {
            Name = "Train";
        }

        public IEnumerable<Act.Status> DoMagicResearch()
        {
            var obj = Agent.Blackboard.GetData<GameComponent>("Research");
            if (obj == null)
            {
                Agent.SetMessage("Failed to find magical object for research purposes.");
                yield return Act.Status.Fail;
                yield break;
            }

            float timer = 0;
            foreach (var status in Creature.HitAndWait(false, () => { return 10.0f;}, () => { return timer; }, () => { timer++; }, () => { return obj.Position; }, ContentPaths.Audio.Oscar.sfx_ic_dwarf_magic_research))
            {
                yield return Act.Status.Running;
            }
            Creature.AI.AddXP(10);
            yield return Act.Status.Success;
        }

        public override void Initialize()
        {
            var tag = Magical ? "Research" : "Train";
            Act trainAct = Magical ? new Wrap(DoMagicResearch) { Name = "Magic research" } as Act :
                new MeleeAct(Agent, "Train") { Training = true, Timeout = new Timer(10.0f, false) };
            Act unreserveAct = new Wrap(() => Creature.Unreserve(tag));
            Tree = new Sequence(
                new Wrap(() => Creature.FindAndReserve(tag, tag)),
                new Sequence
                    (
                        new GoToTaggedObjectAct(Agent) { Tag = tag, Teleport = false, TeleportOffset = new Vector3(1, 0, 0), ObjectName = tag },
                        trainAct,
                        unreserveAct
                    ) | new Sequence(unreserveAct, false)
                    ) | new Sequence(unreserveAct, false);
            base.Initialize();
        }


        public override void OnCanceled()
        {
            var tag = Magical ? "Research" : "Train";
            foreach (var statuses in Creature.Unreserve(tag))
            {
                continue;
            }
            base.OnCanceled();
        }


    }
}