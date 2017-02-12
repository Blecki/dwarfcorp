// HireDialog.cs
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
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// This GUI component is a window which opens up on top
    /// of the GUI, and blocks the game until it gets user input.
    /// </summary>
    public class HireDialog : Dialog
    {

        public ListSelector ApplicantSelector { get; set; }
        public ApplicationPanel ApplicantPanel { get; set; }
        public Applicant CurrentApplicant { get; set; }
        public List<Applicant> Applicants { get; set; }
        public Button HireButton { get; set; }
        public Faction Faction { get; set; }
        public bool WasSomeoneHired { get; set; }
       
        public delegate void HiredDelegate(Applicant applicant);

        public CompanyInformation Company { get; set; }

        public event HiredDelegate OnHired;

        protected virtual void OnOnHired(Applicant applicant)
        {
            HiredDelegate handler = OnHired;
            if (handler != null) handler(applicant);
            
        }

        public static HireDialog Popup(DwarfGUI gui, Faction faction, CompanyInformation company)
        {
            int w = gui.Graphics.Viewport.Width - 64;
            int h = gui.Graphics.Viewport.Height - 64;
            HireDialog toReturn = new HireDialog(gui, gui.RootComponent)
            {
                Faction = faction,
                LocalBounds =
                    new Rectangle(gui.Graphics.Viewport.Width/2 - w/2, gui.Graphics.Viewport.Height/2 - h/2, w, h),
                Company = company
            };
            toReturn.Initialize(ButtonType.OK, "Hire new Employees", "");
            return toReturn;
        }

        public HireDialog(DwarfGUI gui, GUIComponent parent) 
            : base(gui, parent)
        {
        }

        public void GenerateApplicants(CompanyInformation info)
        {
            Applicants = new List<Applicant>();

            foreach (KeyValuePair<JobLibrary.JobType, EmployeeClass> employeeType in JobLibrary.Classes)
            {
                for (int i = 0; i < 5; i++)
                {
                    Applicant applicant = new Applicant();
                    applicant.GenerateRandom(employeeType.Value, 0, info);
                    Applicants.Add(applicant);   
                }
            }
           
        }

        public override void Initialize(Dialog.ButtonType buttons, string title, string message)
        {
            WasSomeoneHired = false;
            GenerateApplicants(Company);
            IsModal = true;
            OnClicked += HireDialog_OnClicked;
            OnClosed += HireDialog_OnClosed;

            int w = LocalBounds.Width;
            int h = LocalBounds.Height;
            int rows = h / 64;
            int cols = 8;
            
            GridLayout layout = new GridLayout(GUI, this, rows, cols);
            Title = new Label(GUI, layout, title, GUI.TitleFont);
            layout.SetComponentPosition(Title, 0, 0, 2, 1);


            GroupBox applicantSelectorBox = new GroupBox(GUI, layout, "");
            layout.SetComponentPosition(applicantSelectorBox, 0, 1, rows / 2 - 1, cols - 1);

            GridLayout selectorLayout = new GridLayout(GUI, applicantSelectorBox, 1, 1);
            ScrollView view = new ScrollView(GUI, selectorLayout);
            ApplicantSelector = new ListSelector(GUI, view)
            {
                Label = "-Applicants-"
            };
            
            selectorLayout.SetComponentPosition(view, 0, 0, 1, 1);

            foreach (Applicant applicant in Applicants)
            {
                ApplicantSelector.AddItem(applicant.Level.Name + " - " + applicant.Name);
            }

            ApplicantSelector.DrawPanel = false;
            ApplicantSelector.LocalBounds = new Rectangle(0, 0, 128, Applicants.Count * 24);

            ApplicantSelector.OnItemSelected += ApplicantSelector_OnItemSelected;

            CurrentApplicant = Applicants[0];

            GroupBox applicantPanel = new GroupBox(GUI, layout, "");
            layout.SetComponentPosition(applicantPanel, rows / 2 - 1, 1, rows / 2 - 1, cols - 1);

            GridLayout applicantLayout = new GridLayout(GUI, applicantPanel, 1, 1);

            ApplicantPanel = new ApplicationPanel(applicantLayout);
            applicantLayout.SetComponentPosition(ApplicantPanel, 0, 0, 1, 1);
            ApplicantPanel.SetApplicant(CurrentApplicant);

            bool createOK = false;
            bool createCancel = false;

            switch (buttons)
            {
                case ButtonType.None:
                    break;
                case ButtonType.OkAndCancel:
                    createOK = true;
                    createCancel = true;
                    break;
                case ButtonType.OK:
                    createOK = true;
                    break;
                case ButtonType.Cancel:
                    createCancel = true;
                    break;
            }

            if (createOK)
            {
                Button okButton = new Button(GUI, layout, "OK", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.Check));
                layout.SetComponentPosition(okButton, cols - 2, rows - 1 , 2, 1);
                okButton.OnClicked += okButton_OnClicked;
            }

            if (createCancel)
            {
                Button cancelButton = new Button(GUI, layout, "Cancel", GUI.DefaultFont, Button.ButtonMode.PushButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.Ex));
                layout.SetComponentPosition(cancelButton, cols - 4, rows - 1, 2, 1);
                cancelButton.OnClicked += cancelButton_OnClicked;
            }

            HireButton = new Button(GUI, layout, "Hire", GUI.DefaultFont, Button.ButtonMode.ToolButton, GUI.Skin.GetSpecialFrame(GUISkin.Tile.ZoomIn));
            layout.SetComponentPosition(HireButton, cols - 1, rows - 2, 1, 1);

            HireButton.OnClicked += HireButton_OnClicked;
        }

        void HireButton_OnClicked()
        {
            List<Room> rooms = Faction.GetRooms();

            bool hasBalloonPort = rooms.Any(room => room.RoomData.Name == "BalloonPort");

            if (CurrentApplicant.Level.Pay*4 > Faction.Economy.CurrentMoney)
            {
                Dialog.Popup(GUI, "Can't hire!",
                    "We can't afford the signing bonus. Our treasury: " + Faction.Economy.CurrentMoney.ToString("C"), ButtonType.OK, 500, 300, this, LocalBounds.Width / 2 - 250, LocalBounds.Height/2 - 150);
            }
            else if (!hasBalloonPort)
            {
                Dialog.Popup(GUI, "Can't hire!",
                  "We can't hire anyone when there are no balloon ports.", ButtonType.OK, 500, 300, this, LocalBounds.Width / 2 - 250, LocalBounds.Height / 2 - 150);
            }
            else
            {
                Applicants.Remove(CurrentApplicant);
                Faction.Hire(CurrentApplicant);
                SoundManager.PlaySound(ContentPaths.Audio.cash);
                OnOnHired(CurrentApplicant);
                CurrentApplicant = Applicants.FirstOrDefault();
                ApplicantSelector.ClearChildren();
                ApplicantSelector.Items.Clear();
                foreach (Applicant applicant in Applicants)
                {
                    ApplicantSelector.AddItem(applicant.Level.Name + " - " + applicant.Name);
                }

                ApplicantPanel.SetApplicant(Applicants.FirstOrDefault());
                WasSomeoneHired = true;
            }
        }

        void ApplicantSelector_OnItemSelected(int index, ListItem item)
        {
            CurrentApplicant = Applicants[index];
            ApplicantPanel.SetApplicant(CurrentApplicant);
        }

        void cancelButton_OnClicked()
        {
            Close(ReturnStatus.Ok);
        }

        void okButton_OnClicked()
        {
            if (WasSomeoneHired)
            {
                Faction.DispatchBalloon();
                WasSomeoneHired = false;
            }

            Close(ReturnStatus.Ok);
        }



        void HireDialog_OnClosed(Dialog.ReturnStatus status)
        {
          
        }

        void HireDialog_OnClicked()
        {
            if (IsMouseOver)
            {
                GUI.FocusComponent = this;
            }
            else if (!IsModal)
            {
                if (GUI.FocusComponent == this)
                {
                    GUI.FocusComponent = null;
                }
            }
        }
    }
}