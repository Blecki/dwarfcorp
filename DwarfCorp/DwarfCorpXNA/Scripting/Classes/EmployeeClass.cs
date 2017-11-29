// EmployeeClass.cs
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
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class EmployeeClassDef
    {
        public string Name { get; set; }
        public string Animations { get; set; }
        public List<EmployeeClass.Level> Levels { get; set; }
        public List<Attack> Attacks { get; set; }
        public List<string> Actions { get; set; } 
    }

    [JsonObject(IsReference = true)]
    public class EmployeeClass 
    {
        public class Level
        {
            public int Index;
            public string Name;
            public DwarfBux Pay;
            public int XP;
            public CreatureStats.StatNums BaseStats;
        }

        public Texture2D SpriteSheet { get; set; }
        public List<Animation> Animations { get; set; }
        public List<Attack> Attacks { get; set; }
        public List<Level> Levels { get; set; }
        public string Name { get; set; }
        public Task.TaskCategory Actions = Task.TaskCategory.None;

        [JsonIgnore]
        public static Dictionary<string, EmployeeClass> Classes { get; set; } 

        protected static bool staticClassInitialized = false;
        protected bool staticsInitiailized = false;

        public EmployeeClass()
        {
            if (!staticClassInitialized)
            {
                InitializeClassStatics();
            }
        }

        public EmployeeClass(EmployeeClassDef definition)
        {
            Name = definition.Name;
            Levels = definition.Levels;
            foreach (string s in definition.Actions)
            {
                var value = Task.TaskCategory.None;
                if (Enum.TryParse(s, true, out value))
                    Actions |= value;
            }

            CompositeAnimation.Descriptor descriptor = FileUtils.LoadJsonFromString<CompositeAnimation.Descriptor>(ContentPaths.GetFileAsString(definition.Animations));
            Animations = new List<Animation>();
            Animations.AddRange(descriptor.GenerateAnimations(Name));

            Attacks = definition.Attacks;
        }

        public static void AddClasses(string file)
        {
            if (!staticClassInitialized)
            {
                InitializeClassStatics();
            }
            List<EmployeeClassDef> defs = ContentPaths.LoadFromJson<List<EmployeeClassDef>>(file);

            foreach (EmployeeClassDef empClass in defs)
            {
                Classes[empClass.Name] = new EmployeeClass(empClass);
            }
        }

        protected virtual void InitializeStatics()
        {
            staticsInitiailized = true;
        }

        protected static void InitializeClassStatics()
        {
            if (!staticClassInitialized)
            {
                Classes = new Dictionary<string, EmployeeClass>();
                staticClassInitialized = true;
            }
        }

        public bool HasAction(Task.TaskCategory action)
        {
            return (Actions & action) == action;
        }
    }
}
