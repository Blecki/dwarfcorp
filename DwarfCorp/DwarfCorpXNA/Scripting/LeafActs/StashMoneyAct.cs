// StashResourcesAct.cs
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
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// A creature grabs a given item and puts it in their inventory
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class StashMoneyAct : CreatureAct
    {
        public DwarfBux Money { get { return GetMoney(); } set { SetMoney(value); } }
        [JsonIgnore]
        public Treasury Zone { get { return GetZone(); } set { SetZone(value); } }
        public Treasury GetZone()
        {
            return Agent.Blackboard.GetData<Treasury>(StockpileName);
        }

        public string StockpileName = "Treasury";
        public string MoneyName = "MoneyNeeded";
        public void SetZone(Treasury pile)
        {
            Agent.Blackboard.SetData(StockpileName, pile);
        }

        public DwarfBux GetMoney()
        {
            return Agent.Blackboard.GetData<DwarfBux>(MoneyName);
        }

        public void SetMoney(DwarfBux value)
        {
            Agent.Blackboard.SetData(MoneyName, value);
        }
        public StashMoneyAct()
        {

        }

        public StashMoneyAct(CreatureAI agent, string moneyAmountName, string zoneName) : 
            base(agent)
        {
            StockpileName = zoneName;
            MoneyName = moneyAmountName;
            Name = "Stash " + moneyAmountName;
        }

        public StashMoneyAct(CreatureAI agent, DwarfBux money) :
            base(agent)
        {
            Money = money;
            Name = "Stash " + Money;
        }

        public override IEnumerable<Status> Run()
        {
            Creature.IsCloaked = false;
            if (Zone == null)
            {
                yield return Act.Status.Fail;
                yield break;
            }
            Timer waitTimer = new Timer(1.0f, true);
            var moneyRemoved = Math.Min(Money, Zone.Money);
            bool removed = Zone.RemoveMoney(Agent.Position, moneyRemoved);

            if(!removed)
            {
                yield return Status.Fail;
            }
            else
            {
                Agent.AddMoney(Money);
                Money -= moneyRemoved;
                Agent.Creature.Sprite.ResetAnimations(Creature.AttackMode);
                while (!waitTimer.HasTriggered)
                {
                    Agent.Creature.CurrentCharacterMode = Creature.AttackMode;
                    waitTimer.Update(DwarfTime.LastTime);
                    yield return Status.Running;
                }
                Agent.Creature.CurrentCharacterMode = CharacterMode.Idle;
                yield return Status.Success;
            }

        }

    }

}

