// StockTicker.cs
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
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace DwarfCorp
{
    public class StockTicker : GUIComponent
    {
        public GridLayout Layout { get; set; }
        public ComboBox IndustryBox { get; set; }
        public Economy Economy { get; set; }
        public int Window { get; set; }
        public GUIComponent DrawSurface { get; set; }
        public DwarfBux MinStock = 0.0m;
        public DwarfBux MaxStock = 100.0m;
        public Color TickColor { get; set; }
        public List<Company> FilteredCompanies = new List<Company>();
        public string SelectedIndustry = "";
        public Company SelectedCompany = null;
        public struct StockIcon
        {
            public Company Company;
            public Rectangle DestRect;
        }

        public List<StockIcon> Icons { get; set; } 

        public StockTicker(DwarfGUI gui, GUIComponent parent, Economy economy) :
            base(gui, parent)
        {
            Icons = new List<StockIcon>();
            Economy = economy;
            Window = 30;
            TickColor = Color.Brown;
            Layout = new GridLayout(gui, this, 10, 4);
            Label displayLabel = new Label(gui, Layout, "Display: ", GUI.DefaultFont);
            Layout.SetComponentPosition(displayLabel, 0, 0, 1, 1);
            IndustryBox = new ComboBox(gui, Layout);
            IndustryBox.AddValue("Our Company");
            IndustryBox.AddValue("All");
            IndustryBox.AddValue("Average");
            IndustryBox.AddValue("Exploration");
            IndustryBox.AddValue("Military");
            IndustryBox.AddValue("Manufacturing");
            IndustryBox.AddValue("Magic");
            IndustryBox.AddValue("Finance");
            IndustryBox.CurrentIndex = 0;
            IndustryBox.CurrentValue = "Our Company";

            IndustryBox.OnSelectionModified += IndustryBox_OnSelectionModified;
            Layout.SetComponentPosition(IndustryBox, 1, 0, 1, 1);

            DrawSurface = new GUIComponent(gui, Layout);

            Layout.SetComponentPosition(DrawSurface, 0, 1, 4, 9);

            IndustryBox_OnSelectionModified("Our Company");
        }

        void IndustryBox_OnSelectionModified(string arg)
        {
            FilteredCompanies = GetCompaniesByIndustry(arg);
            Icons.Clear();

            if (arg != "Average")
            {
                CalculateIcons();
            }

            SelectedIndustry = arg;
        }

        public List<Company> GetCompaniesByIndustry(string industry)
        {
            List<Company> toReturn = new List<Company>();

            if (industry == "Our Company")
            {
                toReturn.Add(Economy.Company);
                return toReturn;
            }

            foreach (Company company in Economy.Market)
            {
                switch (industry)
                {
                    case  "Average":
                    case "All":
                        toReturn.Add(company);
                        break;
                    case "Exploration":
                        if (company.Industry == Company.Sector.Exploration) { toReturn.Add(company);}
                        break;
                    case "Military":
                        if (company.Industry == Company.Sector.Military) { toReturn.Add(company); }
                        break;
                    case "Manufacturing":
                        if (company.Industry == Company.Sector.Manufacturing) { toReturn.Add(company); }
                        break;
                    case "Magic":
                        if (company.Industry == Company.Sector.Magic) { toReturn.Add(company); }
                        break;
                    case "Finance":
                        if (company.Industry == Company.Sector.Finance) { toReturn.Add(company); }
                        break;
                }
            }

            return toReturn;
        }

        public void RenderCompanies(DwarfTime time, SpriteBatch batch)
        {
            int x = DrawSurface.GlobalBounds.X;
            int y = DrawSurface.GlobalBounds.Y;
            int w = DrawSurface.GlobalBounds.Width;
            int h = DrawSurface.GlobalBounds.Height;
            MaxStock = 0;
            MinStock = 9999;

            foreach (DwarfBux stock in FilteredCompanies.SelectMany(company => company.StockHistory))
            {
                MaxStock = Math.Max(stock, MaxStock);
                MinStock = Math.Min(stock, MinStock);
            }

            for (int i = 1; i < Window; i++)
            {
                int tick = (w / (Window + 1)) * (i + 1);
                int prevtick = (w / (Window + 1)) * (i);
                foreach (Company company in FilteredCompanies)
                {
                    if (company.StockHistory.Count < i + 1 || (company != SelectedCompany && SelectedCompany != null))
                    {
                        continue;
                    }
                    DwarfBux price1 = company.StockHistory[i - 1];
                    DwarfBux normalizedPrice1 = (price1 - MinStock) / (MaxStock - MinStock);
                    DwarfBux price0 = company.StockHistory[i];
                    DwarfBux normalizedPrice0 = (price0 - MinStock) / (MaxStock - MinStock);

                    Drawer2D.DrawLine(batch, new Vector2(x + tick, y + h - (float)((decimal)normalizedPrice0 * h)), 
                        new Vector2(x + prevtick, y + h - (float)((decimal)normalizedPrice1 * h)), new Color(company.Information.LogoBackgroundColor), 2);
                }
            }

            foreach (StockIcon icon in Icons)
            {
                if (icon.Company != SelectedCompany && SelectedCompany != null)
                    continue;

                // Todo: Change in company logo broke stock ticker.
                //batch.Draw(icon.Company.Logo.Image, icon.DestRect, icon.Company.Logo.SourceRect, icon.Company.SecondaryColor);
            }
        }

        public void RenderAverage(DwarfTime time, SpriteBatch batch)
        {
            int x = DrawSurface.GlobalBounds.X;
            int y = DrawSurface.GlobalBounds.Y;
            int w = DrawSurface.GlobalBounds.Width;
            int h = DrawSurface.GlobalBounds.Height;
            MaxStock = 0;
            MinStock = 9999;

            foreach (DwarfBux stock in FilteredCompanies.SelectMany(company => company.StockHistory))
            {
                MaxStock = Math.Max(stock, MaxStock);
                MinStock = Math.Min(stock, MinStock);
            }

            MaxStock *= (double)(FilteredCompanies.Count / 2);
            MinStock *= (double)(FilteredCompanies.Count / 2);

            for (int i = 1; i < Window; i++)
            {
                int tick = (w / (Window + 1)) * (i + 1);
                int prevtick = (w / (Window + 1)) * (i);
                DwarfBux prevAverage = 0.0m;
                DwarfBux currAverage = 0.0m;

                foreach (Company company in FilteredCompanies)
                {
                    if (company.StockHistory.Count < i + 1)
                    {
                        continue;
                    }
                    prevAverage += company.StockHistory[i];
                    currAverage += company.StockHistory[i - 1];
                    
                }

                DwarfBux normalizedPrice1 = (currAverage - MinStock) / (MaxStock - MinStock);
                DwarfBux normalizedPrice0 = (prevAverage - MinStock) / (MaxStock - MinStock);

                if (currAverage > 0)
                {
                    Drawer2D.DrawLine(batch, new Vector2(x + tick, y + h - (float)((decimal)normalizedPrice0*h)),
                        new Vector2(x + prevtick, y + h - (float)((decimal)normalizedPrice1*h)), Color.Black, 2);
                }
            }

        }


        public void CalculateIcons()
        {
            int x = DrawSurface.GlobalBounds.X;
            int y = DrawSurface.GlobalBounds.Y;
            int w = DrawSurface.GlobalBounds.Width;
            int h = DrawSurface.GlobalBounds.Height;
            MaxStock = 0;
            MinStock = 9999;

            foreach (DwarfBux stock in FilteredCompanies.SelectMany(company => company.StockHistory))
            {
                MaxStock = Math.Max(stock, MaxStock);
                MinStock = Math.Min(stock, MinStock);
            }

            foreach (Company company in FilteredCompanies)
            {
                Icons.Add(new StockIcon()
                {
                    Company = company,
                    DestRect = new Rectangle(x + w/2, y + h /2, 32, 32)
                });
            }

            for (int i = 1; i < Window; i++)
            {
                int tick = (w / (Window + 1)) * (i + 1);
                int prevtick = (w / (Window + 1)) * (i);
                int j = 0;
                foreach (Company company in FilteredCompanies)
                {
                    if (company.StockHistory.Count < i + 1)
                    {
                        continue;
                    }
                    DwarfBux price0 = company.StockHistory[i];
                    DwarfBux normalizedPrice0 = (price0 - MinStock) / (MaxStock - MinStock);

                    if (j % company.StockHistory.Count == (i - 1))
                    {
                        StockIcon icon = Icons[j];
                        icon.DestRect = new Rectangle((int) (x + tick), (int) (y + h - (float)((decimal)normalizedPrice0*h)) - 16, 32, 32);
                        Icons[j] = icon;
                    }
                    j++;
                }
            }
        }


        public void UpdateMouse()
        {
            MouseState mouseState = Mouse.GetState();
            SelectedCompany = null;
            foreach (StockIcon icon in Icons)
            {
                if (icon.DestRect.Contains(mouseState.X, mouseState.Y))
                {
                    SelectedCompany = icon.Company;
                    break;
                }
            }

            if (SelectedCompany != null)
            {
                ToolTip = SelectedCompany.Information.Name + " (" + SelectedCompany.TickerName + ")\n" +
                    " Share Price: " + SelectedCompany.StockPrice + "\n" +
                    " Industry: " + SelectedCompany.Industry.ToString() +"\n" +
                    " Motto: " + SelectedCompany.Information.Motto;
                if (FilteredCompanies.Contains(SelectedCompany))
                {
                    FilteredCompanies.Remove(SelectedCompany);
                    FilteredCompanies.Add(SelectedCompany);
                }
            }
            else
            {
                ToolTip = "";
            }
        }

        private bool firstIter = true;
        public override void Update(DwarfTime time)
        {
            UpdateMouse();
            base.Update(time);

            if (firstIter)
            {
                Layout.UpdateSizes();
                IndustryBox_OnSelectionModified("Our Company");
                firstIter = false;
            }
        }

        public override void Render(DwarfTime time, SpriteBatch batch)
        {
            int x = DrawSurface.GlobalBounds.X;
            int y = DrawSurface.GlobalBounds.Y;
            int w = DrawSurface.GlobalBounds.Width;
            int h = DrawSurface.GlobalBounds.Height;

            for (int i = 0; i < Window; i++)
            {
                int tick = (w/(Window + 1))*(i + 1);
                Vector2 start = new Vector2(x + tick, y + h);
                Vector2 end = new Vector2(x + tick, y);
                Drawer2D.DrawLine(batch, start, end, TickColor, 1);
            }

            if (SelectedIndustry == "Average")
            {
                RenderAverage(time, batch);
            }
            else
            {
                RenderCompanies(time, batch);
            }

            DwarfBux midStock = (MaxStock + MinStock) * 0.5m;

            Drawer2D.DrawAlignedText(batch, MinStock.ToString(), GUI.SmallFont, GUI.DefaultTextColor, Drawer2D.Alignment.Left, new Rectangle(x - 10, y + h - 30, 30, 30));
            Drawer2D.DrawAlignedText(batch, midStock.ToString(), GUI.SmallFont, GUI.DefaultTextColor, Drawer2D.Alignment.Left, new Rectangle(x - 10, y + (h / 2) - 30, 30, 30));
            Drawer2D.DrawAlignedText(batch, MaxStock.ToString(), GUI.SmallFont, GUI.DefaultTextColor, Drawer2D.Alignment.Left, new Rectangle(x - 10, y, 30, 30));
            base.Render(time, batch);
        }
    }
}
