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
        }
    }
}
