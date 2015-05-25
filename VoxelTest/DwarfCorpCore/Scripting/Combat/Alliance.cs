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
            SetRelationship("Player", "Herbivore", Relationship.Indifferent);
            SetRelationship("Player", "Carnivore", Relationship.Hates);
            SetRelationship("Herbivore", "Carnivore", Relationship.Hates);
            SetRelationship("Player", "Undead", Relationship.Hates);
            SetRelationship("Player", "Goblins", Relationship.Hates);
            SetRelationship("Goblins", "Undead", Relationship.Hates);
            SetRelationship("Herbivore", "Undead", Relationship.Indifferent);
            SetRelationship("Carnivore", "Undead", Relationship.Hates);
            SetRelationship("Goblins", "Carnivore", Relationship.Hates);
            SetRelationship("Goblins", "Herbivore", Relationship.Indifferent);
            SetRelationship("Elf", "Goblins", Relationship.Hates);
            SetRelationship("Elf", "Player", Relationship.Hates);
            SetRelationship("Elf", "Herbivore", Relationship.Loves);
            SetRelationship("Elf", "Carnivore", Relationship.Indifferent);
            SetRelationship("Elf", "Undead", Relationship.Hates);
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

            if (Relationships.ContainsKey(alliance))
                return Relationships[alliance];

            else return Relationship.Indifferent;
        }
    }

}