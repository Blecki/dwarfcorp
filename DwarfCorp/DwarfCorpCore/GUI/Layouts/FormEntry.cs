using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    /// <summary>
    /// A row of a form layout.
    /// </summary>
    public class FormEntry
    {
        public Label Label { get; set; }
        public GUIComponent Component { get; set; }
    }
}
