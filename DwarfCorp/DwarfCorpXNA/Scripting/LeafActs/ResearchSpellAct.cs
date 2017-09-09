// ResearchSpellAct.cs
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
using DwarfCorp;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace DwarfCorp
{
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class ResearchSpellAct : CreatureAct
    {
        public SpellTree.Node Spell { get; set; }

        public ResearchSpellAct(CreatureAI agent, SpellTree.Node spell) :
            base(agent)
        {
            Spell = spell;
        }

        public override void OnCanceled()
        {
            Creature.OverrideCharacterMode = false;
            base.OnCanceled();
        }

        public override IEnumerable<Act.Status> Run()
        {
            if (Spell.IsResearched)
            {
                Creature.CurrentCharacterMode = CharacterMode.Idle;
                Creature.OverrideCharacterMode = false;
                yield return Status.Success;
                yield break;
            }
            Timer starParitcle = new Timer(0.5f, false);
            float totalResearch = 0.0f;
            Sound3D sound = null;
            while (!Spell.IsResearched)
            {
                Creature.CurrentCharacterMode = CharacterMode.Attacking;
                Creature.OverrideCharacterMode = true;
                Creature.Sprite.ReloopAnimations(CharacterMode.Attacking);
                float research = Creature.Stats.BuffedInt * 0.25f * DwarfTime.Dt;
                Spell.ResearchProgress += research;
                totalResearch += research;
                Creature.Physics.Velocity *= 0;
                Drawer2D.DrawLoadBar(Creature.World.Camera, Creature.Physics.Position, Color.Cyan, Color.Black, 64, 4, Spell.ResearchProgress / Spell.ResearchTime);
                if ((int) totalResearch > 0)
                {
                    if (sound == null || sound.EffectInstance.IsDisposed ||  sound.EffectInstance.State == SoundState.Stopped)
                    {
                        sound = SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_ic_dwarf_magic_research,
                            Creature.AI.Position,
                            true);
                    }
                    //SoundManager.PlaySound(ContentPaths.Audio.tinkle, Creature.AI.Position, true);
                    Creature.AI.AddXP((int)(totalResearch));
                    totalResearch = 0.0f;
                }

                if (Spell.ResearchProgress >= Spell.ResearchTime)
                {
                    Creature.Manager.World.MakeAnnouncement(
                        new Gui.Widgets.QueuedAnnouncement
                        {
                            Text = String.Format("{0} ({1}) discovered the {2} spell!",
                                Creature.Stats.FullName,
                                Creature.Stats.CurrentLevel.Name, Spell.Spell.Name),
                            ClickAction = (gui, sender) => Agent.ZoomToMe()
                        });

                    SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_positive_generic, 0.15f);
                }

                starParitcle.Update(DwarfTime.LastTime);
                if(starParitcle.HasTriggered)
                   Creature.Manager.World.ParticleManager.Trigger("star_particle", Creature.AI.Position, Color.White, 3);
                yield return Status.Running;
            }

            if (sound != null)
            {
                sound.Stop();
            }
            Creature.AI.AddThought(Thought.ThoughtType.Researched);
            Creature.OverrideCharacterMode = false;
            Creature.CurrentCharacterMode = CharacterMode.Idle;
            yield return Status.Success;
            yield break;
        }
    }
}
