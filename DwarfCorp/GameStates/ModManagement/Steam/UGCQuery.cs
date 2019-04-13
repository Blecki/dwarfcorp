using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Steamworks;

namespace DwarfCorp.AssetManagement.Steam
{
    public class UGCQuery : IUGCTransaction
    {
        public String SearchString = "";
        public List<String> SearchTags = new List<string>();

        private UGCQueryHandle_t QueryHandle;
        private CallResult<SteamUGCQueryCompleted_t> QuerySubmitCallResult;

        public String Message { get; private set; }
        public UGCStatus Status { get; private set; }
        public List<SteamUGCDetails_t> Results { get; private set; }

        private enum States
        {
            Creating,
            Waiting,
            Done
        }

        private States State = States.Creating;

        public UGCQuery()
        {
            State = States.Creating;
            Status = UGCStatus.Working;
        }

        public void Update()
        {
            switch (State)
            {
                case States.Creating:
                    QueryHandle = SteamUGC.CreateQueryAllUGCRequest(EUGCQuery.k_EUGCQuery_RankedByVotesUp,
                        EUGCMatchingUGCType.k_EUGCMatchingUGCType_All, Steam.AppID, Steam.AppID, 1);
                    if (!String.IsNullOrEmpty(SearchString))
                        SteamUGC.SetSearchText(QueryHandle, SearchString);
                    QuerySubmitCallResult = CallResult<SteamUGCQueryCompleted_t>.Create((callback, IOFailure) =>
                    {
                        if (callback.m_handle != QueryHandle) return;

                        if (IOFailure)
                        {
                            State = States.Done;
                            Message = "There was an error communicating with steam.";
                            Status = UGCStatus.Failure;
                            return;
                        }

                        if (callback.m_eResult != EResult.k_EResultOK)
                        {
                            State = States.Done;
                            Status = UGCStatus.Failure;
                            Message = String.Format("Query failed: {0}", callback.m_eResult);
                            return;
                        }

                        Results = new List<SteamUGCDetails_t>();
                        for (uint i = 0; i < callback.m_unNumResultsReturned; ++i)
                        {
                            var details = new SteamUGCDetails_t();
                            SteamUGC.GetQueryUGCResult(QueryHandle, i, out details);
                            Results.Add(details);
                        }

                        State = States.Done;
                        Status = UGCStatus.Success;

                        Message = String.Format("Found {0} results.", callback.m_unNumResultsReturned);
                    });

                    QuerySubmitCallResult.Set(SteamUGC.SendQueryUGCRequest(QueryHandle));
                    State = States.Waiting;
                    Message = "Querying...";

                    return;

                case States.Waiting:
                    return;
                case States.Done:
                    return;
            }
        }
    }
}
