using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Linq;

namespace DwarfCorp
{
    public class Embarkment
    {
        public List<Applicant> Employees = new List<Applicant>();
        public ResourceSet Resources = new ResourceSet();
        public DwarfBux Money;

        public enum ValidationResult
        {
            Reject,
            Query,
            Pass
        }

        public ValidationResult ValidateEmbarkment(GameStates.OverworldGenerationSettings Settings, out String Message)
        {
            var cost = Settings.InitalEmbarkment.Money + Settings.InitalEmbarkment.Employees.Sum(e => e.SigningBonus);
            if (cost > Settings.PlayerCorporationFunds)
            {
                Message = "You do not have enough funds.";
                return ValidationResult.Reject;
            }

            var supervisionCap = Settings.InitalEmbarkment.Employees.Where(e => e.Class.Managerial).Select(e => e.Level.BaseStats.Intelligence).Sum() + 4;
            var employeeCount = Settings.InitalEmbarkment.Employees.Where(e => !e.Class.Managerial).Count();

            if (employeeCount > supervisionCap)
            {
                Message = "You do not have enough supervision.";
                return ValidationResult.Reject;
            }

            if (employeeCount == 0)
            {
                Message = "Are you sure you don't want any employees?";
                return ValidationResult.Query;
            }

            Message = "Looks good, let's go!";
            return ValidationResult.Pass;
        }
    }
}
