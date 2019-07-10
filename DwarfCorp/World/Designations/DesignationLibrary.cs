using System;
using System.Collections.Generic;
using System.Linq;

namespace DwarfCorp
{
    public static partial class Library
    {
        private static Dictionary<DesignationType, DesignationTypeProperties> Designations = null;
        private static DesignationTypeProperties DefaultDesignation = null;
        private static bool DesignationInitialized = false;
        private static Object DesignationLock = new object();

        private static void InitializeDesignations()
        {
            lock (DesignationLock)
            {
                if (DesignationInitialized)
                    return;

                DesignationInitialized = true;

                Designations = new Dictionary<DesignationType, DesignationTypeProperties>();
                foreach (var des in FileUtils.LoadJsonListFromDirectory<DesignationTypeProperties>("World/Designations", null, r => r.Name))
                {
                    if (des.Name == "default")
                        DefaultDesignation = des;
                    else
                        Designations.Add((DesignationType)Enum.Parse(typeof(DesignationType), des.Name), des);
                }

                Console.WriteLine("Loaded Designation Library.");
            }
        }

        public static DesignationTypeProperties GetDesignationTypeProperties(DesignationType Of)
        {
            InitializeDesignations();

            if (Designations.ContainsKey(Of))
                return Designations[Of];
            return DefaultDesignation;
        }
    }
}
