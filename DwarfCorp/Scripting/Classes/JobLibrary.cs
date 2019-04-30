using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class JobLibrary
    {
        public static Dictionary<String, EmployeeClass> Classes { get; set; }

        public static void Initialize()
        {
            var list = FileUtils.LoadJsonListFromDirectory<EmployeeClass>(ContentPaths.employee_classes, null, c => c.Name);

            Classes = new Dictionary<String, EmployeeClass>();
            foreach (var item in list)
                Classes.Add(item.Name, item);

            FileUtils.SaveBasicJson(new SkeletonClass(), "Classes\\Skeleton.json");
            FileUtils.SaveBasicJson(new TrollClass(), "Classes\\Troll.json");
            FileUtils.SaveBasicJson(new MolemanClass(), "Classes\\MoleMan.json");
            FileUtils.SaveBasicJson(new GremlinClass(), "Classes\\Gremlin.json");
            FileUtils.SaveBasicJson(new DemonClass(), "Classes\\Demon.json");
            FileUtils.SaveBasicJson(new ElfClass(), "Classes\\Elf.json");
            FileUtils.SaveBasicJson(new SnowGolemClass(), "Classes\\SnowGolem.json");
            FileUtils.SaveBasicJson(new MudGolemClass(), "Classes\\MudGolem.json");
            FileUtils.SaveBasicJson(new KoboldClass(), "Classes\\Kobold.json");
            FileUtils.SaveBasicJson(new GoblinClass(), "Classes\\Goblin.json");
            FileUtils.SaveBasicJson(new NecromancerClass(), "Classes\\Necromancer.json");
            FileUtils.SaveBasicJson(new FairyClass(), "Classes\\Fairy.json");
        }
    }
}
