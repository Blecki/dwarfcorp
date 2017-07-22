using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.Gui;
using Microsoft.Xna.Framework;

namespace DwarfCorp.Gui.Widgets
{
    public class ApplicantInfo : Widget
    {
        private Applicant _applicant;
        public Applicant Applicant
        {
            get { return _applicant; }
            set { _applicant = value;  Invalidate(); }
        }

        private Widget NameLabel;
        private Widget ClassLabel;
        private Widget StartingWageLabel;
        private Widget SigningBonusLabel;
        private Widget Resume;
        private Widget LastJobLabel;
        private Widget LastJobLocation;
        private Widget Biography;
        private Widget Portrait;

        public override void Construct()
        {
            Widget topWidget = AddChild(new Widget()
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 48)
            });
            Portrait = topWidget.AddChild(new Widget()
            {
                MinimumSize = new Point(32, 48),
                AutoLayout = AutoLayout.DockLeft
            });
            NameLabel = topWidget.AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30),
                Font = "font-hires",
                TextVerticalAlign = VerticalAlign.Center
            });

            ClassLabel = AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            StartingWageLabel = AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30),
                Tooltip = "You must pay the dwarf this much per day."
            });

            SigningBonusLabel = AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30),
                Tooltip = "When hiring the dwarf, you have to pay this much."
            });

            LastJobLabel = AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            LastJobLocation = AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            Biography = AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(30, 30)
            });

            Resume = AddChild(new Gui.Widget
            {
                AutoLayout = AutoLayout.DockFill,
                MinimumSize = new Point(30, 30)
            });

            Border = "border-one";
            base.Construct();
        }
        
        protected override Gui.Mesh Redraw()
        {
            if (Applicant != null)
            {
                NameLabel.Text = Applicant.Name;
                ClassLabel.Text = Applicant.Class.Name;
                StartingWageLabel.Text = String.Format("Starts at {0}/day", Applicant.Level.Pay);
                SigningBonusLabel.Text = String.Format("{0} signing bonus", Applicant.Level.Pay * 4);
                LastJobLabel.Text = String.Format("Last job - {0}", Applicant.FormerProfession);
                LastJobLocation.Text = String.Format("Home town - {0}", Applicant.HomeTown);
                Biography.Text = Applicant.Biography;
                Resume.Text = Applicant.CoverLetter;
                var idx = EmployeePanel.GetIconIndex(Applicant.Class.Name);
                Portrait.Background = idx > 0 ? new TileReference("dwarves", idx) : null;
                Portrait.Invalidate();

            }

            return base.Redraw();
        }
        
    }
}
