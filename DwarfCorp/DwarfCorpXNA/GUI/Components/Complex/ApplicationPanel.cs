// ApplicationPanel.cs
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
    public class ApplicationPanel : GUIComponent
    {
        public Label NameLabel { get; set; }
        public Label PositionLabel { get; set; }
        public Label PayLabel { get; set; }
        public Label BonusLabel { get; set; }
        public Label LetterLabel { get; set; }
        public Label FormerPositionLabel { get; set; }
        public Label HomeTownLabel { get; set; }

        public ApplicationPanel(GUIComponent parent) :
            base(parent.GUI, parent)
        {
            Initialize();
        }

        public void Initialize()
        {
            GridLayout layout = new GridLayout(GUI, this, 6, 4);
            NameLabel = new Label(GUI, layout, "", GUI.TitleFont)
            {
                WordWrap = true
            };
            PositionLabel = new Label(GUI, layout, "", GUI.DefaultFont)
            {
                WordWrap = true
            };
            PayLabel = new Label(GUI, layout, "", GUI.DefaultFont)
            {
                WordWrap = true
            };
            BonusLabel = new Label(GUI, layout, "", GUI.DefaultFont)
            {
                WordWrap = true
            };
            LetterLabel = new Label(GUI, layout, "", GUI.DefaultFont)
            {
                WordWrap = true
            };
            FormerPositionLabel = new Label(GUI, layout, "", GUI.SmallFont);
            HomeTownLabel = new Label(GUI, layout, "", GUI.SmallFont);

            layout.SetComponentPosition(NameLabel, 0, 0, 2, 1);
            layout.SetComponentPosition(PositionLabel, 0, 1, 1, 1);
            layout.SetComponentPosition(FormerPositionLabel, 0, 5, 2, 1);
            layout.SetComponentPosition(HomeTownLabel, 2, 5, 2, 1);
            layout.SetComponentPosition(PayLabel , 0, 2, 1, 1);
            layout.SetComponentPosition(BonusLabel, 2, 2, 1, 1);
            layout.SetComponentPosition(LetterLabel, 0, 3, 3, 2);

        }

        public void SetApplicant(Applicant applicant)
        {
            NameLabel.Text = applicant.Name;
            PositionLabel.Text = applicant.Class.Name;
            PayLabel.Text = "Wage: " + applicant.Level.Pay + " / day";
            BonusLabel.Text = "Signing Bonus: " + new DwarfBux(applicant.Level.Pay*4.0m);
            LetterLabel.Text = applicant.CoverLetter;
            HomeTownLabel.Text = "Hometown: " + applicant.HomeTown;
            FormerPositionLabel.Text = "Last Job: " + applicant.FormerProfession;
        }

    }
}
