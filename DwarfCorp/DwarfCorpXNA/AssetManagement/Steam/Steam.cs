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
    }
}
