using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Steamworks;

namespace DwarfCorp.AssetManagement.Steam
{
    public static class Steam
    {
        public static AppId_t AppID { get; private set; }
        public static bool SteamAvailable { get; private set; }
        private static List<UGCTransactionProcessor> PendingTransactions = new List<UGCTransactionProcessor>();


        public enum SteamInitializationResult
        {
            Continue,
            QuitImmediately
        }

        public static SteamInitializationResult InitializeSteam()
        {
            AppID = new AppId_t(252390);

            // Todo: Don't even try this for non-steam builds.
            if (SteamAPI.RestartAppIfNecessary(AppID))
            {
                // Todo: Quit if this fails.
                return SteamInitializationResult.QuitImmediately;
            }

            SteamAvailable = SteamAPI.Init();

            //// Set up our callback to recieve warning messages from Steam.
            //// You must launch with "-debug_steamapi" in the launch args to recieve warnings.
            //var m_SteamAPIWarningMessageHook = new SteamAPIWarningMessageHook_t((severity, message) =>
            //{
            //    throw new Exception(message.ToString());
            //});
            //SteamClient.SetWarningMessageHook(m_SteamAPIWarningMessageHook);

            return SteamInitializationResult.Continue;
        }

        public static void Update()
        {
            SteamAPI.RunCallbacks();

            foreach (var transaction in PendingTransactions)
                transaction.Update();
            PendingTransactions.RemoveAll(t => t.Complete);
        }

        public static void AddTransaction(UGCTransactionProcessor Processor)
        {
            PendingTransactions.Add(Processor);
        }

        public static bool HasTransactionOfType<T>()
        {
            return PendingTransactions.Any(t => t.Transaction is T);
        }
    }
}
