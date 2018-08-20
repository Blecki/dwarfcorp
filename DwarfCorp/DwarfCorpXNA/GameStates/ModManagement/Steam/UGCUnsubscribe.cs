using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Steamworks;

namespace DwarfCorp.AssetManagement.Steam
{
    public class UGCUnsubscribe : IUGCTransaction
    {
        public PublishedFileId_t FileID;
        private CallResult<RemoteStorageUnsubscribePublishedFileResult_t> UnsubscribeCallResult;

        public String Message { get; private set; }
        public UGCStatus Status { get; private set; }

        private enum States
        {
            Creating,
            Waiting,
            Done
        }

        private States State = States.Creating;

        public UGCUnsubscribe(PublishedFileId_t FileID)
        {
            State = States.Creating;
            Status = UGCStatus.Working;
            this.FileID = FileID;
        }

        public void Update()
        {
            switch (State)
            {
                case States.Creating:
                    UnsubscribeCallResult = CallResult<RemoteStorageUnsubscribePublishedFileResult_t>.Create((callback, IOFailure) =>
                    {
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
                            Message = String.Format("Unsubscribe failed: {0}", callback.m_eResult);
                            return;
                        }

                        Message = "Unsubscribed!";
                        Status = UGCStatus.Success;
                        State = States.Done;
                    });

                    UnsubscribeCallResult.Set(SteamUGC.UnsubscribeItem(FileID));

                    State = States.Waiting;
                    Message = "Unsubscribing...";

                    return;

                case States.Waiting:
                    return;
                case States.Done:
                    return;
            }
        }
    }
}
