using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    public enum Relationship
    {
        Indifferent,
        Loves,
        Hates
    }
    
    /// <summary>
    /// Entitites can belong to alliances. Alliances either love, hate, or are indifferent to other
    /// entities in different alliances.
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class Alliance
    {
        public struct AlliancePair : IEquatable<AlliancePair>
        {
            public string AllianceA;
            public string AllianceB;

            public bool Equals(AlliancePair other)
            {
                return (AllianceA == other.AllianceA && AllianceB == other.AllianceB) || (AllianceA == other.AllianceB && AllianceB == other.AllianceA);
            }

            public override int GetHashCode()
            {
                return AllianceA.GetHashCode() ^ (AllianceB.GetHashCode());
            }

            public override bool Equals(object obj)
            {
                if(!(obj is AlliancePair))
                {
                    return false;
                }
                else
                {
                    return Equals((AlliancePair) obj);
                }
            }
        }

        public Alliance()
        {
            Relationships = InitializeRelationships();
        }

        public static Dictionary<AlliancePair, Relationship> Relationships { get; set; }

        public static Dictionary<AlliancePair, Relationship> InitializeRelationships()
        {
            Relationships = new Dictionary<AlliancePair, Relationship>();
            SetRelationship("Dwarf", "Herbivore", Relationship.Indifferent);
            SetRelationship("Dwarf", "Carnivore", Relationship.Hates);
            SetRelationship("Herbivore", "Carnivore", Relationship.Hates);
            SetRelationship("Dwarf", "Undead", Relationship.Hates);
            SetRelationship("Dwarf", "Goblin", Relationship.Hates);
            SetRelationship("Goblin", "Undead", Relationship.Hates);
            SetRelationship("Goblin", "Carnivore", Relationship.Hates);
            SetRelationship("Goblin", "Herbivore", Relationship.Indifferent);


            SetSelfClassLove();

            return Relationships;
        }

        public static void SetSelfClassLove()
        {
            List<AlliancePair> relationships = new List<AlliancePair>();
            relationships.AddRange(Relationships.Keys);
            foreach(AlliancePair a in relationships)
            {
                SetRelationship(a.AllianceA, a.AllianceA, Relationship.Loves);
                SetRelationship(a.AllianceB, a.AllianceB, Relationship.Loves);
            }
        }

        public static void SetRelationship(string a, string b, Relationship relationship)
        {
            AlliancePair alliance;
            alliance.AllianceA = a;
            alliance.AllianceB = b;
            Relationships[alliance] = relationship;
        }

        public static Relationship GetRelationship(string a, string b)
        {
            AlliancePair alliance;
            alliance.AllianceA = a;
            alliance.AllianceB = b;
            return Relationships[alliance];
        }
    }

}