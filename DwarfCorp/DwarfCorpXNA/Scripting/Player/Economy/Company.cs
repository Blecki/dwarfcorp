// Company.cs
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
using DwarfCorp.GameStates;
using DwarfCorp.NewGui;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Company
    {
        public enum Sector
        {
            None = 0,
            Exploration = 1,
            Military = 2,
            Manufacturing = 4,
            Magic = 8,
            Finance = 16,
            All = Exploration | Military | Manufacturing | Magic | Finance
        };

        public Sector Industry { get; set; }
        public DwarfBux Assets { get; set; }
        public DwarfBux LastAssets { get; set; }
        public string TickerName { get { return GenerateTickerName(Information.Name); } }
        public List<DwarfBux> StockHistory { get; set; }

        public CompanyInformation Information;

        public static string GenerateTickerName(string longName)
        {
            string[] splits = longName.Split(' ', ',', ':', '&', ';', '.');
            int symbols = 0;
            string toReturn = "";
            foreach (string t in splits)
            {
                if (t == " " || t == "of" || t == "the" || t == "")
                {
                    continue;
                }
                if(t.Length > 0)
                    toReturn += t.First().ToString().ToUpper();
                symbols++;

                if (symbols >= 3)
                {
                    break;
                }
            }

            int k = 1;
            for (int j = symbols; j < 3; j++)
            {
                if(splits.Length > 0 && k < splits.Last().Length)
                {
                    toReturn += splits.Last()[k].ToString().ToUpper();
                }
                k++;
            }

        return toReturn;
        }

        public static string GenerateMotto()
        {
            var templates = TextGenerator.GetAtoms(ContentPaths.Text.Templates.mottos);
            return TextGenerator.GenerateRandom(Datastructures.SelectRandom(templates).ToArray());
        }

        public static string GenerateName(Sector sector)
        {

            string templateName = ContentPaths.Text.Templates.company_finance;
            if (sector == Sector.Magic)
            {
                templateName = ContentPaths.Text.Templates.company_magical;
            }
            else if (sector == Sector.Manufacturing)
            {
                templateName = ContentPaths.Text.Templates.company_industrial;
            }
            else if (sector == Sector.Military)
            {
                templateName = ContentPaths.Text.Templates.company_military;
            }
            var templates = TextGenerator.GetAtoms(templateName);
            return TextGenerator.GenerateRandom(Datastructures.SelectRandom(templates).ToArray());
        }

        public static List<DwarfBux> GenerateRandomStockHistory(DwarfBux current, int length)
        {
            List<DwarfBux> history = new List<DwarfBux>();
            DwarfBux startPrice = current + (float)MathFunctions.Random.NextDouble()*current*0.5f - current * 0.5f;
            double slope = (double)((current - startPrice).Value) / (double)length;
            for (int i = 0; i < length - 1; i++)
            {
                history.Add((decimal)(MathFunctions.Random.NextDouble() * 0.5 + slope * i + (double)startPrice.Value));
            }
            history.Add(current);
            return history;
        }

        public static Company GenerateRandom(DwarfBux assets, DwarfBux stockPrice, Sector industry)
        {
            // Todo: Find out how random companies are actually used and reimplement logo generation.
            /*
            Texture2D texture = TextureManager.GetTexture(ContentPaths.Logos.logos);

            int row = 0;
            switch (industry)
            {
                case Sector.Magic:
                    row = 3;
                    break;
                case Sector.Finance:
                    row = 3;
                    break;
                case Sector.Exploration:
                    row = 0;
                    break;
                case Sector.Manufacturing:
                    row = 1;
                    break;
                case Sector.Military:
                    row = 2;
                    break;
            }
            NamedImageFrame image = new NamedImageFrame(ContentPaths.Logos.logos, 32, MathFunctions.Random.Next(0, texture.Width / 32), row);

            Color c = new Color(MathFunctions.Random.Next(0, 255), MathFunctions.Random.Next(0, 255),
                MathFunctions.Random.Next(0, 255));
             * */
                
            return new Company()
            {
                Assets = assets,
                //StockPrice = stockPrice,
                Industry = industry,
                Information = new CompanyInformation
                {
                    Name = GenerateName(industry),
                    Motto = GenerateMotto()
                },
                StockHistory = GenerateRandomStockHistory(stockPrice, 10),
                LastAssets = assets
            };

        }

    }
}
