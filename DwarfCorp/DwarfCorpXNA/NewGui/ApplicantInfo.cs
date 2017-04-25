using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
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
        
        public override void Construct()
        {
            NameLabel = AddChild(new Gum.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            ClassLabel = AddChild(new Gum.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            StartingWageLabel = AddChild(new Gum.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            SigningBonusLabel = AddChild(new Gum.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            LastJobLabel = AddChild(new Gum.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            LastJobLocation = AddChild(new Gum.Widget
            {
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });

            Resume = AddChild(new Gum.Widget
            {
                AutoLayout = AutoLayout.DockFill,
                MinimumSize = new Point(0, 30)
            });

            base.Construct();
        }
        
        protected override Gum.Mesh Redraw()
        {
            if (Applicant != null)
            {
                NameLabel.Text = Applicant.Name;
                ClassLabel.Text = Applicant.Class.Name;
                StartingWageLabel.Text = String.Format("Starts at {0}/day", Applicant.Level.Pay);
                SigningBonusLabel.Text = String.Format("${0} signing bonus", Applicant.Level.Pay * 4);
                LastJobLabel.Text = String.Format("Last job - {0}", Applicant.FormerProfession);
                LastJobLocation.Text = String.Format("Home town - {0}", Applicant.HomeTown);
                Resume.Text = Applicant.CoverLetter;
            }

            return base.Redraw();
        }
        
    }
}
