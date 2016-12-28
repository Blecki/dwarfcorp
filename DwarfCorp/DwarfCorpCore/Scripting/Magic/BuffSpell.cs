// BuffSpell.cs
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

using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    ///     This is a spell that applies buffs to a creature. Buff it up!
    /// </summary>
    [JsonObject(IsReference = true)]
    public class BuffSpell : Spell
    {
        public BuffSpell()
        {
        }

        public BuffSpell(params Creature.Buff[] buffs)
        {
            Buffs = buffs.ToList();
            Mode = SpellMode.SelectEntities;
            Name = "Buff spell";
            Description = "Apply buffs to selected creatures";
            Hint = "Click and drag to select creatures";
            ManaCost = 20;
            Image = new NamedImageFrame(ContentPaths.GUI.icons, 32, 0, 2);
        }

        /// <summary>
        ///     Buffs to apply to the creature.
        /// </summary>
        public List<Creature.Buff> Buffs { get; set; }


        public override void OnEntitiesSelected(SpellTree tree, List<Body> entities)
        {
            foreach (Body body in entities)
            {
                // Only select creatures.
                Creature creature = body.GetChildrenOfType<Creature>().FirstOrDefault();

                if (creature == null) continue;
                // Try casting all the buffs on each of the creatures that were selected.
                foreach (Creature.Buff buff in Buffs)
                {
                    if (OnCast(tree))
                    {
                        Vector3 p = creature.AI.Position + Vector3.Up;
                        IndicatorManager.DrawIndicator("-" + ManaCost + " M", p, 1.0f, Color.Red);
                        creature.AddBuff(buff.Clone());
                    }
                }
            }
            base.OnEntitiesSelected(tree, entities);
        }
    }
}