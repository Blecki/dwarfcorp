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

            var supervisionCap = Settings.InstanceSettings.InitalEmbarkment.Employees.Where(e => e.Class.Managerial).Select(e => e.Level.BaseStats.Intelligence).Sum() + 4;
            var employeeCount = Settings.InstanceSettings.InitalEmbarkment.Employees.Where(e => !e.Class.Managerial).Count();

            if (employeeCount > supervisionCap)
            {
                Message = "You do not have enough supervision.";
                return InstanceSettings.ValidationResult.Reject;
            }

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
