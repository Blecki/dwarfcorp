// Applicant.cs
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
                "I've always wanted to work at " + WorldManager.PlayerCompany.Information.Name + "!",
                "I am a very hard worker!",
                "I am an adventurous soul!"
            };
           CoverLetter =
                TextGenerator.GenerateRandom("Dear " + WorldManager.PlayerCompany.Information.Name + ",\n",
                "${Please,Do}"," consider ", "${my,this}"," application for the position of " + Level.Name +
                                             ". " + justifications[MathFunctions.Random.Next(justifications.Count)] +"\n", "${Thanks,Sincerely,Yours}", ",\n    " ,Name);

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
             HomeTown = TextGenerator.GenerateRandom(templates[MathFunctions.Random.Next(templates.Count)]); 
        }
    }
}
