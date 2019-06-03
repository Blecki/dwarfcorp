using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp.Tutorial
{
    public class TutorialSaveData
    {
        public bool TutorialEnabled;
        public Dictionary<String, bool> EntryShown;

        public TutorialSaveData()
        {
            TutorialEnabled = true;
            EntryShown = new Dictionary<string, bool>();
        }
    }
}
