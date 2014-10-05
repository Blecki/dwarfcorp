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
        public float LastAssets { get; set; }
        public NamedImageFrame Logo { get; set; }
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

        public static string GenerateName(Sector sector)
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
            string[] magical_color_place =
            {
                "$Color",
                " ",
                "$Place",
                " ",
                "$Magical"
            };
            string[] animal_magical =
            {
                "$Animal",
                " ",
                "$Magical"
            };
            string[] materialPlaceMilitary =
            {
                "$Material",
                " ",
                "$Place",
                " ",
                "$Military"
            };
            string[] personMilitary =
            {
                "$MaleName",
                "'s",
                " ",
                "$Military"
            };
            string[] colorPlaceIndustry =
            {
                "$Color",
                " ",
                "$Place",
                " ",
                "$Industry"
            };
            string[] colorAnimalIndustry =
            {
                "$Color",
                " ",
                "$Animal",
                " ",
                "$Industry"
            };
            string[] materialAnimalIndustry =
            {
                "$Material",
                " ",
                "$Animal",
                " ",
                "$Industry"
            };
            string[] materialBodyIndustry =
            {
                "$Material",
                " ",
                "$Bodypart",
                " ",
                "$Industry"
            };
            List<string[]> genericTemplates = new List<string[]>
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
            List<string[]> magicalTemplates = new List<string[]>
            {
                magical_color_place,
                animal_magical
            };
            List<string[]> militaryTemplates = new List<string[]>
            {
                materialPlaceMilitary,
                personMilitary
            };
            List<string[]> industralTemplates = new List<string[]>
            {
                colorPlaceIndustry,
                colorAnimalIndustry,
                materialAnimalIndustry,
                materialBodyIndustry
            };

            List<string[]> templates = genericTemplates;
            if (sector == Sector.Magic)
            {
                templates = magicalTemplates;
            }
            else if (sector == Sector.Manufacturing)
            {
                templates = industralTemplates;
            }
            else if (sector == Sector.Military)
            {
                templates = militaryTemplates;
            }
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
            NamedImageFrame image = new NamedImageFrame(ContentPaths.Logos.logos, 32, PlayState.Random.Next(0, texture.Width / 32), row);

            Color c = new Color(PlayState.Random.Next(0, 255), PlayState.Random.Next(0, 255),
                PlayState.Random.Next(0, 255));
                
            return new Company()
            {
                Assets = assets,
                StockPrice = stockPrice,
                Industry = industry,
                Logo = image,
                Name = GenerateName(industry),
                Motto = GenerateMotto(),
                BaseColor = c,
                SecondaryColor  = c,
                StockHistory = GenerateRandomStockHistory(stockPrice, 10),
                LastAssets = assets
            };

        }

    }
}
