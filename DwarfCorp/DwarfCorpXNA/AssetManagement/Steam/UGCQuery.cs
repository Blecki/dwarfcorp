using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Steamworks;

namespace DwarfCorp.AssetManagement.Steam
{
    public class UGCQuery
    {
        public String SearchString = "";
        public List<String> SearchTags = new List<string>();

        private UGCQueryHandle_t QueryHandle;
        private CallResult<SteamUGCQueryCompleted_t> QuerySubmitCallResult;

        public String Message { get; private set; }
        public QueryStatus Status { get; private set; }
        public List<SteamUGCMetaData> Results { get; private set; }

        public enum QueryStatus
        {
            Working,
            Success,
            Failure
        }

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
            Status = QueryStatus.Working;
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
                            Status = QueryStatus.Failure;
                            return;
                        }

                        if (callback.m_eResult != EResult.k_EResultOK)
                        {
                            State = States.Done;
                            Status = QueryStatus.Failure;
                            Message = String.Format("Query failed: {0}", callback.m_eResult);
                            return;
                        }

                        Results = new List<SteamUGCMetaData>();
                        for (uint i = 0; i < callback.m_unNumResultsReturned; ++i)
                        {
                            var details = new SteamUGCDetails_t();
                            SteamUGC.GetQueryUGCResult(QueryHandle, i, out details);
                            Results.Add(new SteamUGCMetaData
                            {
                                Name = details.m_rgchTitle,
                                Description = details.m_rgchDescription,
                                SteamID = (ulong)details.m_nPublishedFileId
                            });
                        }

                        State = States.Done;
                        Status = QueryStatus.Success;

                        Message = String.Format("Found {0} results.", callback.m_unNumResultsReturned);
                    });

                    QuerySubmitCallResult.Set(SteamUGC.SendQueryUGCRequest(QueryHandle));
                    State = States.Waiting;
                    Message = "Querying...";

                    return;

                case States.Waiting:
                    SteamAPI.RunCallbacks();
                    return;
                case States.Done:
                    return;
            }
        }
    }
}
