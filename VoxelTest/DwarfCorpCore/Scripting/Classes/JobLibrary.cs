using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public class JobLibrary
    {
        public enum JobType
        {
            Worker,
            AxeDwarf,
            Wizard,
            CraftsDwarf
        }

        public static Dictionary<JobType, EmployeeClass> Classes { get; set; }

        public static void Initialize()
        {
            Classes = new Dictionary<JobType, EmployeeClass>();
            Classes[JobType.Worker] = new WorkerClass();
            Classes[JobType.AxeDwarf] = new AxeDwarfClass();
            Classes[JobType.CraftsDwarf] = new CraftDwarfClass();
            Classes[JobType.Wizard] = new WizardClass();
        }
    }
}
