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
            PositionLabel.Text = applicant.Level.Name;
            PayLabel.Text = "Wage: " + applicant.Level.Pay.ToString("C") + " / day";
            BonusLabel.Text = "Signing Bonus: " + (applicant.Level.Pay*4).ToString("C");
            LetterLabel.Text = applicant.CoverLetter;
            HomeTownLabel.Text = "Hometown: " + applicant.HomeTown;
            FormerPositionLabel.Text = "Last Job: " + applicant.FormerProfession;
        }

    }
}
