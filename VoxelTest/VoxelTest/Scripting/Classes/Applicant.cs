using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class Applicant
    {
        public EmployeeClass Class { get; set; }
        public EmployeeClass.Level Level { get; set; }
        public string Name { get; set; }
        public string CoverLetter { get; set; }
        public string FormerProfession { get; set; }
        public string HomeTown { get; set; }

        public Applicant()
        {
            
        }

        public void GenerateRandom(EmployeeClass employeeClass, int level)
        {
            Class = employeeClass;
            Level = Class.Levels[level];

            Name = TextGenerator.GenerateRandom("$DwarfName", " ", "$DwarfFamily");
            List<string> justifications = new List<string>()
            {
                "I have many relevant qualities!",
                "My expereince is extensive!",
                "I really need this job!",
                "I need to get away from it all!",
                "I will be your loyal servant!",
                "I've always wanted to work at " + PlayerSettings.Default.CompanyName + "!",
                "I am a very hard worker!",
                "I am an adventurous soul!"
            };
           CoverLetter =
                TextGenerator.GenerateRandom("Dear " + PlayerSettings.Default.CompanyName + ",\n",
                "${Please,Do}"," consider ", "${my,this}"," application for the position of " + Level.Name +
                                             ". " + justifications[PlayState.Random.Next(justifications.Count)] +"\n", "${Thanks,Sincerely,Yours}", ",\n    " ,Name);

            if (level > 0)
            {
                FormerProfession = Class.Levels[level - 1].Name;
            }
            else
            {
                FormerProfession = TextGenerator.GenerateRandom("$Professions");
            }

            List<string[]> templates = new List<string[]>
            {
                new[]
                {
                    "$Place",
                    " of the ",
                    "$Color",
                    " ",
                    "$Animal"
                },
                new[]
                {
                    "$Place",
                    " of the ",
                    "$Material",
                    " ",
                    "$Animal"
                },
                new[]
                {
                    "$Place",
                    " of ",
                    "$Material"
                },
                new []
                {
                    "$Material",
                    " ",
                    "$Place"
                },
                new[]
                {
                    "$Color",
                    " ",
                    "$Material",
                    " ",
                    "$Place"
                }
               
            };

             HomeTown = TextGenerator.GenerateRandom(templates[PlayState.Random.Next(templates.Count)]); 
        }
    }
}
