using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Steamworks;

namespace DwarfCorp.AssetManagement.Steam
{
    public class Steam
    {
        public AppId_t AppID { get; private set; }

        public Steam()
        {
            AppID = new AppId_t(252390);

            if (SteamAPI.RestartAppIfNecessary(AppID))
            {
                // Todo: Quit if this fails.
                //Application.Quit();
                return;
            }

            if (!SteamAPI.Init())
                return;

            //// Set up our callback to recieve warning messages from Steam.
            //// You must launch with "-debug_steamapi" in the launch args to recieve warnings.
            //var m_SteamAPIWarningMessageHook = new SteamAPIWarningMessageHook_t((severity, message) =>
            //{
            //    throw new Exception(message.ToString());
            //});
            //SteamClient.SetWarningMessageHook(m_SteamAPIWarningMessageHook);

        }
    }
}
