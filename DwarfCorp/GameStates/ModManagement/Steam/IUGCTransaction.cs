using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Steamworks;

namespace DwarfCorp.AssetManagement.Steam
{
    public interface IUGCTransaction
    {
        String Message { get; }
        UGCStatus Status { get; }
        void Update();
    }
}
