using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Newtonsoft.Json;

namespace DwarfCorp
{

    /// <summary>
    /// Controls how much money the player has, and whether the player can
    /// buy and sell certain things. Controls balloon shipments.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Economy
    {
        public float CurrentMoney { get { return Company.Assets; } set { Company.Assets = value; } }
        public Company Company { get; set; }
        public Faction Faction { get; set; }
        public List<Company> Market { get; set; } 

        public Economy(Faction faction, float currentMoney, float buyMultiplier, float sellMulitiplier)
        {
            Company = new Company();
            Company.InitializeFromPlayer();
            CurrentMoney = currentMoney;
            Faction = faction;
            Market  = new List<Company>
            {
                Company,
                Company.GenerateRandom(1000, 1.0f, Company.Sector.Exploration),
                Company.GenerateRandom(1000, 5.0f, Company.Sector.Exploration),
                Company.GenerateRandom(1000, 10.0f, Company.Sector.Exploration),
                Company.GenerateRandom(1000, 10.0f, Company.Sector.Manufacturing),
                Company.GenerateRandom(1000, 10.0f, Company.Sector.Manufacturing),
                Company.GenerateRandom(1000, 15.0f, Company.Sector.Military),
                Company.GenerateRandom(1000, 10.0f, Company.Sector.Military),
                Company.GenerateRandom(1000, 15.0f, Company.Sector.Military),
                Company.GenerateRandom(1000, 25.0f, Company.Sector.Magic),
                Company.GenerateRandom(1000, 30.0f, Company.Sector.Magic),
                Company.GenerateRandom(1000, 50.0f, Company.Sector.Magic),
                Company.GenerateRandom(1000, 100.0f, Company.Sector.Finance),
                Company.GenerateRandom(1000, 115.0f, Company.Sector.Finance)
            };
        }

       
    }

}