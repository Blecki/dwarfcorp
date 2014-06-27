using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
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
            Exploration,
            Military,
            Manufacturing,
            Magic,
            Finance
        };

        public string Name { get; set; }
        public string Motto { get; set; }
        public Sector Industry { get; set; }
        public float StockPrice { get; set; }
        public float Assets { get; set; }
        public ImageFrame Logo { get; set; }
        public Color BaseColor { get; set; }
        public Color SecondaryColor { get; set; }
        public string TickerName { get { return GenerateTickerName(Name); } }
        public List<float> StockHistory { get; set; } 

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
            List<string[]> templates = new List<string[]>();
            string[] adverbweverb =
            {
                "$Adverb",
                ", we ",
                "$Verb",
                "!"
            };
            string[] thing2 =
            {
                "$Interjection",
                ", the ",
                "$Color",
                " ",
                "$Animal",
                "s ",
                "$Verb",
                "!"
            };
            string[] thing3 =
            {
                "$Verb",
                " my ",
                "$Bodypart",
                ", my ",
                "$Family",
                "!"
            };
            string[] thing4 =
            {
                "$Verb",
                "!"
            };
            string[] thing5 =
            {
                "You can't ",
                "$Verb",
                " until you ",
                "$Verb",
                "!"
            };
            string[] thing6 =
            {
                "$Interjection",
                " ... the ",
                "$Material",
                " ",
                "$Place",
                "!"
            };
            templates.Add(adverbweverb);
            templates.Add(thing2);
            templates.Add(thing3);
            templates.Add(thing5);
            templates.Add(thing4);
            templates.Add(thing6);

            return TextGenerator.GenerateRandom(templates[PlayState.Random.Next(0, templates.Count)]);
        }

        public static string GenerateName()
        {
            string[] partners =
            {
                "$MaleName",
                " ",
                "&",
                " ",
                "$MaleName",
                ",",
                " ",
                "$Corp"
            };
            string[] animalCorp =
            {
                "$Animal",
                " ",
                "$Corp"
            };
            string[] animalPart =
            {
                "$Animal",
                " ",
                "$Bodypart"
            };
            string[] nameAndSons =
            {
                "$MaleName",
                " ",
                "&",
                " ",
                "$Family",
                "s"
            };
            string[] colorPart =
            {
                "$Color",
                " ",
                "$Bodypart",
                " ",
                "&",
                " ",
                "$Family",
                "s"
            };
            string[] colorPlace =
            {
                "$Color",
                " ",
                "$Place",
                " ",
                "$Corp"
            };
            string[] colorAnimal =
            {
                "$Color",
                " ",
                "$Animal",
                " ",
                "$Corp"
            };
            string[] materialAnimal =
            {
                "$Material",
                " ",
                "$Animal",
                " ",
                "$Corp"
            };
            string[] materialBody =
            {
                "$Material",
                " ",
                "$Bodypart",
                " ",
                "$Corp"
            };
            string[] reversed =
            {
                "$Corp",
                " of the ",
                "$Material",
                " ",
                "$Place",
                "s"
            };
            List<string[]> templates = new List<string[]>
            {
                partners,
                animalCorp,
                animalPart,
                nameAndSons,
                colorPart,
                colorPlace,
                colorAnimal,
                materialAnimal,
                materialBody,
                reversed
            };
            return TextGenerator.GenerateRandom(templates[PlayState.Random.Next(0, templates.Count)]);
        }

        public static List<float> GenerateRandomStockHistory(float current, int length)
        {
            List<float> history = new List<float>();
            float startPrice = current + (float)PlayState.Random.NextDouble()*current*0.5f - current * 0.5f;
            float slope = (current - startPrice) / length;
            for (int i = 0; i < length - 1; i++)
            {
                history.Add((float)PlayState.Random.NextDouble() * 0.5f + slope * i + startPrice);
            }
            history.Add(current);
            return history;
        }

        public static Company GenerateRandom(float assets, float stockPrice, Sector industry)
        {
            Texture2D texture = TextureManager.GetTexture(ContentPaths.Logos.logos);
            ImageFrame image = new ImageFrame(texture, 32, PlayState.Random.Next(0, texture.Width / 32), PlayState.Random.Next(0, texture.Height / 32));

            Color c = new Color(PlayState.Random.Next(0, 255), PlayState.Random.Next(0, 255),
                PlayState.Random.Next(0, 255));
                
            return new Company()
            {
                Assets = assets,
                StockPrice = stockPrice,
                Industry = industry,
                Logo = image,
                Name = GenerateName(),
                Motto = GenerateMotto(),
                BaseColor = c,
                SecondaryColor  = c,
                StockHistory = GenerateRandomStockHistory(stockPrice, 10)
            };

        }

        public void InitializeFromPlayer()
        {
            Name = PlayerSettings.Default.CompanyName;
            Motto = PlayerSettings.Default.CompanyMotto;
            Logo = new ImageFrame(TextureManager.GetTexture("CorpLogo"));
            Industry = Sector.Exploration;
            Assets = 100.0f;
            StockPrice = 1.0f;
            BaseColor = Color.DarkRed;
            SecondaryColor = Color.White;
            StockHistory = GenerateRandomStockHistory(1.0f, 10);
        }

    }
}
