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

            Name = TextGenerator.GenerateRandom("$firstname", " ", "$lastname");
            List<string> justifications = new List<string>()
            {
                "I have many relevant qualities!",
                "My expereince is extensive!",
                "I really need this job!",
                "I need to get away from it all!",
                "I will be your loyal servant!",
                "I've always wanted to work at " + PlayState.PlayerCompany.Name + "!",
                "I am a very hard worker!",
                "I am an adventurous soul!"
            };
           CoverLetter =
                TextGenerator.GenerateRandom("Dear " + PlayState.PlayerCompany.Name + ",\n",
                "${Please,Do}"," consider ", "${my,this}"," application for the position of " + Level.Name +
                                             ". " + justifications[PlayState.Random.Next(justifications.Count)] +"\n", "${Thanks,Sincerely,Yours}", ",\n    " ,Name);

            if (level > 0)
            {
                FormerProfession = Class.Levels[level - 1].Name;
            }
            else
            {
                FormerProfession = TextGenerator.GenerateRandom("$profession");
            }

            List<string[]> templates = new List<string[]>
            {
                new[]
                {
                    "place",
                    " of the ",
                    "$color",
                    " ",
                    "$noun"
                },
                new[]
                {
                    "$place",
                    " of the ",
                    "$adjective",
                    " ",
                    "$noun"
                },
                new[]
                {
                    "$place",
                    " of the ",
                    "$material",
                    " ",
                    "$noun"
                },
                new[]
                {
                    "$place",
                    " of ",
                    "$noun"
                },
                new[]
                {
                    "$color",
                    " ",
                    "$material",
                    " ",
                    "$place"
                },
                new[]
                {
                    "$adjective",
                    " ",
                    "$place"
                },
                new []
                {
                    "$adjective",
                    "ville"
                },
                new []
                {
                    "$adjective",
                    "burg"
                },
                new []
                {
                    "$lastname",
                    "ton"
                }
               
            };
             HomeTown = TextGenerator.GenerateRandom(templates[PlayState.Random.Next(templates.Count)]); 
        }
    }
}
