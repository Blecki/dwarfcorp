// Economy.cs
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
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

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
        public DwarfBux CurrentMoney {
            get 
            {
                return
                Company != null ? Company.Assets : new DwarfBux(0m); 
            } 
            set { if(Company != null) {Company.Assets = value;} } }
        public Company Company { get; set; }
        public Faction Faction { get; set; }
        public List<Company> Market { get; set; }

        [JsonIgnore]
        public WorldManager WorldManager { get; set; }

        public Economy()
        {
            
        }

        public Economy(Faction faction, DwarfBux currentMoney, WorldManager worldManager, 
            CompanyInformation CompanyInformation)
        {
            this.WorldManager = worldManager;
            Company = Company.GenerateRandom(currentMoney, 1.0m, Company.Sector.Exploration);
            Company.Information = CompanyInformation;
            Company.Assets = currentMoney;

            CurrentMoney = currentMoney;
            Faction = faction;
            Market  = new List<Company>
            {
                Company,
                Company.GenerateRandom(1000m, 1.0m, Company.Sector.Exploration),
                Company.GenerateRandom(1200m, 5.0m, Company.Sector.Exploration),
                Company.GenerateRandom(1500m, 10.0m, Company.Sector.Exploration),
                Company.GenerateRandom(1300m, 10.0m, Company.Sector.Manufacturing),
                Company.GenerateRandom(1200m, 10.0m, Company.Sector.Manufacturing),
                Company.GenerateRandom(1500m, 15.0m, Company.Sector.Military),
                Company.GenerateRandom(1300m, 10.0m, Company.Sector.Military),
                Company.GenerateRandom(1200m, 15.0m, Company.Sector.Military),
                Company.GenerateRandom(1500m, 25.0m, Company.Sector.Magic),
                Company.GenerateRandom(1200m, 30.0m, Company.Sector.Magic),
                Company.GenerateRandom(1300m, 40.0m, Company.Sector.Magic),
                Company.GenerateRandom(1500m, 50.0m, Company.Sector.Finance),
                Company.GenerateRandom(1800m, 60.0m, Company.Sector.Finance)
            };

            if (worldManager != null)
             WorldManager.Time.NewDay += Time_NewDay;
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            WorldManager = ((WorldManager)context.Context);
        }

        public void UpdateStocks(DateTime time)
        {
            decimal marketBias = (decimal)Math.Sin(DwarfTime.LastTime.TotalGameTime.TotalSeconds * 0.001f) * 0.25m;
            DwarfBux originalStockPrice = Company.StockPrice;
            foreach (Company company in Market)
            {
                DwarfBux assetDiff = company.Assets - company.LastAssets;
                DwarfBux assetBonus = Math.Min(Math.Max(assetDiff * 0.01m, -1.5m), 1.5m);
                if (company.Assets <= 0m)
                {
                    assetBonus -= 1.5m;
                }
                DwarfBux newPrice = Math.Max(company.StockPrice + marketBias + (decimal)MathFunctions.Rand() * 0.5m - 0.25m + assetBonus, 0m);
                company.StockHistory.Add(newPrice);
                company.StockPrice = newPrice;
                company.LastAssets = company.Assets;
            }

            DwarfBux diff = Company.StockPrice - originalStockPrice;
            if (Company.StockPrice <= 0m)
            {
                WorldManager.InvokeLoss();
            }

            if (Company.Assets <= 0m)
            {
                WorldManager.MakeAnnouncement("We're bankrupt!", "If we don't make a profit by tomorrow, our stock will crash!");
                SoundManager.PlaySound(ContentPaths.Audio.Oscar.sfx_gui_negative_generic);
            }

            string symbol = diff > 0m ? "+" : "";

            WorldManager.MakeAnnouncement(String.Format("{0} {1} {2}{3}",
                Company.TickerName, Company.StockPrice, symbol, diff),
                String.Format("Our stock price changed by {0}{1} today.", symbol, diff));
        }

        void Time_NewDay(DateTime time)
        {
            UpdateStocks(time);   
        }

       
    }

}
