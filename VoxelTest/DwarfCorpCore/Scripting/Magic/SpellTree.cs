using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class SpellTree
    {
        [JsonObject(IsReference = true)]
        public class Node
        {
            public Spell Spell { get; set; }
            public float ResearchTime { get; set; }
            public float ResearchProgress { get; set; }
            public List<Node> Children { get; set; }
            public Node Parent { get; set; }
            public bool IsResearched { get { return ResearchProgress >= ResearchTime; }}


            public void GetKnownSpellsRecursive(List<Spell> spells)
            {
                if (ResearchProgress >= ResearchTime)
                {
                    spells.Add(Spell);
                }

                foreach (Node node in Children)
                {
                    node.GetKnownSpellsRecursive(spells);
                }
            }

            public void SetupParentsRecursive()
            {
                foreach (Node node in Children)
                {
                    node.Parent = this;
                    node.SetupParentsRecursive();
                }
            }

            public Node()
            {
                Spell = null;
                Children = new List<Node>();
                ResearchTime = 0.0f;
                ResearchProgress = 0.0f;
            }
        }

        public List<Node> RootSpells { get; set; }
        public float Mana { get; set; }
        public float MaxMana { get; set; }
      

        public SpellTree()
        {
            RootSpells = new List<Node>();
            Mana = 100.0f;
            MaxMana = 100.0f;
        }

        public bool CanCast(Spell spell)
        {
            return spell.ManaCost <= Mana;
        }

        public void Recharge(float amount)
        {
            Mana = Math.Min(Mana + amount, MaxMana);
        }

        public void UseMagic(float amount)
        {
            Mana = Math.Max(Mana - amount, 0.0f);
        }

        public List<Spell> GetKnownSpells()
        {
            List<Spell> toReturn = new List<Spell>();
            foreach (Node node in RootSpells)
            {
                node.GetKnownSpellsRecursive(toReturn);
            }

            return toReturn;
        }

        
    }
}
