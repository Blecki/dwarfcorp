using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace DwarfCorp
{
    public class Embarkment
    {
        public List<Applicant> Employees = new List<Applicant>();
        public ResourceSet Resources = new ResourceSet();
        public DwarfBux Money;
    }
}
