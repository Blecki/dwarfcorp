// Alliance.cs
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

namespace DwarfCorp
{

    public enum Relationship
    {
        Indifferent,
        Loving,
        Hateful
    }
    /*
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
            SetRelationship("Player", "Carnivore", Relationship.Hateful);
            SetRelationship("Herbivore", "Carnivore", Relationship.Hateful);
            SetRelationship("Player", "Undead", Relationship.Hateful);
            SetRelationship("Player", "Goblins", Relationship.Hateful);
            SetRelationship("Goblins", "Undead", Relationship.Hateful);
            SetRelationship("Herbivore", "Undead", Relationship.Indifferent);
            SetRelationship("Carnivore", "Undead", Relationship.Hateful);
            SetRelationship("Goblins", "Carnivore", Relationship.Hateful);
            SetRelationship("Goblins", "Herbivore", Relationship.Indifferent);
            SetRelationship("Elf", "Goblins", Relationship.Hateful);
            SetRelationship("Elf", "Player", Relationship.Hateful);
            SetRelationship("Elf", "Herbivore", Relationship.Loving);
            SetRelationship("Elf", "Carnivore", Relationship.Indifferent);
            SetRelationship("Elf", "Undead", Relationship.Hateful);
            SetRelationship("Molemen", "Goblins", Relationship.Hateful);
            SetRelationship("Molemen", "Player", Relationship.Hateful);
            SetRelationship("Molemen", "Undead", Relationship.Hateful);
            SetSelfClassLove();

            return Relationships;
        }

        public static void SetSelfClassLove()
        {
            List<AlliancePair> relationships = new List<AlliancePair>();
            relationships.AddRange(Relationships.Keys);
            foreach(AlliancePair a in relationships)
            {
                SetRelationship(a.AllianceA, a.AllianceA, Relationship.Loving);
                SetRelationship(a.AllianceB, a.AllianceB, Relationship.Loving);
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
     */

}