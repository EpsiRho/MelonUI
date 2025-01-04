using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Enums
{
    public enum MessageSeverity
    {
        Debug, // Compiler debugging info
        Info, // Compiler status info 
        Warning, // This may be an issue but like it might be fine
        Error, // This is an issue and compilation cannot complete
        Success // Yippee
    }
}
