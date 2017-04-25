using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum;
using Microsoft.Xna.Framework;

namespace DwarfCorp.NewGui
{
    public class HireEmployeeDialog : TwoColumns
    {
        public Faction Faction;
        private Gum.Widgets.ListView ApplicantList;
        private List<Applicant> Applicants;

        public IEnumerable<Applicant> GenerateApplicants(CompanyInformation info)
        {
            foreach (KeyValuePair<JobLibrary.JobType, EmployeeClass> employeeType in JobLibrary.Classes)
            {
                for (int i = 0; i < 5; i++)
                {
                    Applicant applicant = new Applicant();
                    applicant.GenerateRandom(employeeType.Value, 0, info);
                    yield return applicant;
                }
            }
        }

        public HireEmployeeDialog(CompanyInformation Company)
        {
            Applicants = GenerateApplicants(Company).ToList();
        }

        public override void Construct()
        {
            Border = "border-fancy";
            Rect = Root.VirtualScreen;

            var left = AddChild(new Widget());
            var right = AddChild(new Widget());

            var buttonRow = right.AddChild(new Widget
            {
                AutoLayout = AutoLayout.DockBottom,
                MinimumSize = new Point(0, 30)
            });

            var applicantInfo = right.AddChild(new ApplicantInfo
            {
                AutoLayout = AutoLayout.DockFill
            }) as ApplicantInfo;
            
            left.AddChild(new Widget
            {
                Text = "Applicants",
                AutoLayout = AutoLayout.DockTop,
                MinimumSize = new Point(0, 30)
            });
            
            ApplicantList = left.AddChild(new Gum.Widgets.ListView
            {
                AutoLayout = AutoLayout.DockFill,
                Items = Applicants.Select(a => a.Name).ToList(),
                OnSelectedIndexChanged = (sender) =>
                {
                    if ((sender as Gum.Widgets.ListView).SelectedIndex >= 0 &&
                        (sender as Gum.Widgets.ListView).SelectedIndex < Applicants.Count)
                    {
                        applicantInfo.Hidden = false;
                        applicantInfo.Applicant = Applicants[(sender as Gum.Widgets.ListView).SelectedIndex];
                    }
                    else
                        applicantInfo.Hidden = true;
                }
            }) as Gum.Widgets.ListView;

            ApplicantList.SelectedIndex = 0;

            buttonRow.AddChild(new Widget
            {
                Text = "Back",
                Border = "border-button",
                AutoLayout = AutoLayout.DockLeft,
                OnClick = (sender, args) =>
                {
                    this.Close();
                }
            });

            buttonRow.AddChild(new Widget
            {
                Text = "Hire",
                Border = "border-button",
                AutoLayout = AutoLayout.DockRight,
                OnClick = (sender, args) =>
                {
                    var applicant = applicantInfo.Applicant;
                    if (applicant != null)
                    {
                        if (applicant.Level.Pay * 4 > Faction.Economy.CurrentMoney)
                        {
                            Root.ShowPopup(Root.ConstructWidget(new NewGui.Popup
                            {
                                Text = "We can't afford the signing bonus!",
                            }), Root.PopupExclusivity.DestroyExistingPopups);
                        }
                        else if (!Faction.GetRooms().Any(r => r.RoomData.Name == "BalloonPort"))
                        {
                            Root.ShowPopup(Root.ConstructWidget(new NewGui.Popup
                            {
                                Text = "We need a balloon port to hire someone.",
                            }), Root.PopupExclusivity.DestroyExistingPopups);
                        }
                        else
                        {
                            Applicants.Remove(applicant);
                            Faction.Hire(applicant);
                            SoundManager.PlaySound(ContentPaths.Audio.cash);
                            ApplicantList.Items = Applicants.Select(a => a.Name).ToList();
                            ApplicantList.SelectedIndex = 0;
                        }
                    }
                }
            });

            this.Layout();
        }
    }
}
