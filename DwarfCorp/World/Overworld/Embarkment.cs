using System.Collections.Generic;
using System;
using System.Linq;

namespace DwarfCorp
{
    public class Embarkment
    {
        public List<Applicant> Employees = new List<Applicant>();
        public ResourceSet Resources = new ResourceSet();
        public DwarfBux Funds;

        public Embarkment(GameStates.Overworld Overworld)
        {
            foreach (var dwarf in Overworld.Difficulty.Dwarves)
                Employees.Add(Applicant.Random(Library.GetLoadout(dwarf), Overworld.Company));
        }

        public DwarfBux TotalCost()
        {
            return Funds + Employees.Sum(e => e.SigningBonus);
        }

        public static InstanceSettings.ValidationResult ValidateEmbarkment(GameStates.Overworld Settings, out String Message)
        {
            if (Settings.InstanceSettings.TotalCreationCost() > Settings.PlayerCorporationFunds)
            {
                Message = "You do not have enough funds. Save Anyway?";
                return InstanceSettings.ValidationResult.Query;
            }

            var employeeCount = Settings.InstanceSettings.InitalEmbarkment.Employees.Count();

            if (employeeCount == 0)
            {
                Message = "Are you sure you don't want any employees?";
                return InstanceSettings.ValidationResult.Query;
            }

            Message = "Looks good, let's go!";
            return InstanceSettings.ValidationResult.Pass;
        }
    }
}
