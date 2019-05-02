namespace DwarfCorp
{
    public class Company
    {
        public DwarfBux Funds = 0;
        public CompanyInformation Information;

        public Faction Faction { get; set; }

        public Company()
        {
            
        }

        public Company(Faction OwnerFaction, DwarfBux StartupFunds, CompanyInformation CompanyInformation)
        {
            Funds = StartupFunds;
            Faction = OwnerFaction;
            Information = CompanyInformation;
        }
    }
}
