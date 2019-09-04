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
        public string ExistingFile = null;
        public LoadType LoadType = LoadType.CreateNew;

        [JsonIgnore] public Embarkment InitalEmbarkment = new Embarkment();
        [JsonIgnore] public Vector2 Origin => new Vector2(Cell.Bounds.X, Cell.Bounds.Y);

        public ColonyCell Cell = null;

        public InstanceSettings()
        {

        }

        public InstanceSettings(ColonyCell Cell)
        {
            this.Cell = Cell;
        }

        public DwarfBux CalculateLandValue()
        {
            return Cell.Bounds.Width * Cell.Bounds.Height * 3;
        }
        
        public DwarfBux TotalCreationCost()
        {
            return CalculateLandValue() + InitalEmbarkment.TotalCost();
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
