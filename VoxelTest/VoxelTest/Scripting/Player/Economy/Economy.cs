﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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
        public float CurrentMoney {
            get 
            {
                return
                Company != null ? Company.Assets : 0.0f; 
            } 
            set { if(Company != null) {Company.Assets = value;} } }
        public Company Company { get; set; }
        public Faction Faction { get; set; }
        public List<Company> Market { get; set; }

        [JsonIgnore]
        public PlayState PlayState { get; set; }

        public Economy()
        {
            
        }

        public Economy(Faction faction, float currentMoney, PlayState state, string companyName, string companyMotto, NamedImageFrame companyLogo, Color companyColor)
        {
            PlayState = state;
            Company = Company.GenerateRandom(currentMoney, 1.0f, Company.Sector.Exploration);
            Company.Name = companyName;
            Company.SecondaryColor = Color.White;
            Company.Logo = companyLogo;
            Company.Motto = companyMotto;
            Company.Assets = currentMoney;
            Company.BaseColor = companyColor;


            CurrentMoney = currentMoney;
            Faction = faction;
            Market  = new List<Company>
            {
                Company,
                Company.GenerateRandom(1000, 1.0f, Company.Sector.Exploration),
                Company.GenerateRandom(1200, 5.0f, Company.Sector.Exploration),
                Company.GenerateRandom(1500, 10.0f, Company.Sector.Exploration),
                Company.GenerateRandom(1300, 10.0f, Company.Sector.Manufacturing),
                Company.GenerateRandom(1200, 10.0f, Company.Sector.Manufacturing),
                Company.GenerateRandom(1500, 15.0f, Company.Sector.Military),
                Company.GenerateRandom(1300, 10.0f, Company.Sector.Military),
                Company.GenerateRandom(1200, 15.0f, Company.Sector.Military),
                Company.GenerateRandom(1500, 25.0f, Company.Sector.Magic),
                Company.GenerateRandom(1200, 30.0f, Company.Sector.Magic),
                Company.GenerateRandom(1300, 40.0f, Company.Sector.Magic),
                Company.GenerateRandom(1500, 50.0f, Company.Sector.Finance),
                Company.GenerateRandom(1800, 60.0f, Company.Sector.Finance)
            };

            PlayState.Time.NewDay += Time_NewDay;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            PlayState = GameState.Game.StateManager.GetState<PlayState>("PlayState");
        }
        public void UpdateStocks(DateTime time)
        {
            float marketBias = (float)Math.Sin(Act.LastTime.TotalGameTime.TotalSeconds * 0.001f) * 0.25f;
            float originalStockPrice = Company.StockPrice;
            foreach (Company company in Market)
            {
                float assetDiff = company.Assets - company.LastAssets;
                float assetBonus = Math.Min(Math.Max(assetDiff * 0.01f, -1.5f), 1.5f);
                if (company.Assets <= 0)
                {
                    assetBonus -= 1.5f;
                }
                float newPrice = Math.Max(company.StockPrice + marketBias + MathFunctions.Rand()*0.5f - 0.25f + assetBonus, 0);
                company.StockHistory.Add(newPrice);
                company.StockPrice = newPrice;
                company.LastAssets = company.Assets;
            }

            float diff = Company.StockPrice - originalStockPrice;
            if (Company.StockPrice <= 0)
            {
                PlayState.InvokeLoss();
            }

            if (Company.Assets <= 0)
            {
                PlayState.AnnouncementManager.Announce("We're bankrupt!", "If we don't make a profit by tomorrow, our stock will crash!");
            }

            string symbol = diff > 0 ? "+" : "";
           
            PlayState.AnnouncementManager.Announce(Company.TickerName + " " + Company.StockPrice.ToString("F2") + " " + symbol + diff.ToString("F2"), "Our stock price changed by " + symbol + " " + diff.ToString("F2") + " today.");
        }

        void Time_NewDay(DateTime time)
        {
            UpdateStocks(time);   
        }

       
    }

}