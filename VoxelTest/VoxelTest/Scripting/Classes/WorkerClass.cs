using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class WorkerClass : EmployeeClass
    {
        public WorkerClass()
        {
            Name = "Miner";
            Levels = new List<Level>
            {
                new Level
                {
                    Index = 0,
                    Name = "Mining Intern",
                    Pay = 1,
                    XP = 0
                },
                new Level
                {
                    Index = 1,
                    Name = "Assistant Miner",
                    Pay = 5,
                    XP = 100
                },
                new Level
                {
                    Index = 2,
                    Name = "Miner",
                    Pay = 10,
                    XP = 250
                },
                new Level
                {
                    Index = 3,
                    Name = "Mine Specialist",
                    Pay = 20,
                    XP = 500
                },
                new Level
                {
                    Index = 4,
                    Name = "Senior Mine Specialist",
                    Pay = 50,
                    XP = 1000
                },
                new Level
                {
                    Index = 5,
                    Name = "Principal Mine Specialist",
                    Pay = 100,
                    XP = 5000
                },
                new Level
                {
                    Index = 6,
                    Name = "Vice President of Mine Operations",
                    Pay = 500,
                    XP = 10000
                },
                new Level
                {
                    Index = 7,
                    Name = "President of Mine Operations",
                    Pay = 1000,
                    XP = 20000

                },
                new Level
                {
                    Index = 8,
                    Name = "Ascended Mine Master",
                    Pay = 5000,
                    XP = 1000000
                },
                new Level
                {
                    Index = 9,
                    Name = "High Mine Lord",
                    Pay = 100000,
                    XP = 2000000
                },
                new Level
                {
                    Index = 10,
                    Name = "Father of All Miners",
                    Pay = 100000,
                    XP = 5000000
                }
            };
        }
    }
}
