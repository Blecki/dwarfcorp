using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class DepositMoney : CreatureAct
    {
        public DwarfBux Money { get; set; }

        public DepositMoney(CreatureAI agent, DwarfBux money) :
            base(agent)
        {
            Money = money;
            Name = "Stash money in zone";
        }

        public override IEnumerable<Status> Run()
        {
            if (Money <= 0)
            {
                yield return Status.Success;
                yield break;
            }

            Creature.Faction.AddMoney(Money);
            Creature.AI.AddMoney(-Money);
            Creature.NoiseMaker.MakeNoise("Stockpile", Creature.AI.Position);
            Creature.Stats.NumItemsGathered++;
            Creature.AI.AddXP(1);

            if (Creature.Stats.CurrentClass.HasValue(out var c))
            {
                Creature.CurrentCharacterMode = c.AttackMode;
                Creature.Sprite.ResetAnimations(c.AttackMode);
                Creature.Sprite.PlayAnimations();
            }

            while (!Creature.Sprite.IsDone())
            {
                yield return Status.Running;
            }

            Creature.CurrentCharacterMode = CharacterMode.Idle;
            yield return Status.Success;
        }
    }
}