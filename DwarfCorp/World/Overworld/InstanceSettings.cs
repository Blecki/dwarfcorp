using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace DwarfCorp
{
    public enum LoadType
    {
        CreateNew,
        LoadFromFile
    }

    public class InstanceSettings
    {
        public LoadType LoadType = LoadType.CreateNew;

        public Embarkment InitalEmbarkment = null;
        public List<BiomeData> SelectedBiomes = new List<BiomeData>();

        public InstanceSettings()
        {
        }

        public InstanceSettings(GameStates.Overworld Overworld)
        {
            InitalEmbarkment = new Embarkment(Overworld);
        }

        public DwarfBux TotalCreationCost()
        {
            return InitalEmbarkment.TotalCost();
        }

        public enum ValidationResult
        {
            Reject,
            Query,
            Pass
        }

        public static ValidationResult ValidateEmbarkment(GameStates.Overworld Settings, out String Message)
        {
            if (Settings.InstanceSettings.TotalCreationCost() > Settings.PlayerCorporationFunds)
            {
                Message = "You do not have enough funds.";
                return ValidationResult.Reject;
            }

            return Embarkment.ValidateEmbarkment(Settings, out Message);
        }
    }
}
