using MelonUI.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MelonUI.Base
{
    public class CompilerMessage
    {
        public string Message { get; private set; }
        public MessageSeverity Severity { get; private set; }
        public DateTime DateTime { get; private set; } = DateTime.Now;
        public (int Line, int Position) MxmlLineNumber { get; set; }
        public int CompilerLineNumber { get; set; }
        public string CompilerFile { get; set; }
        public CompilerMessage(string m, MessageSeverity s, (int line, int position)? mxmlLine = null, [CallerLineNumber] int compilerLine = 0, [CallerFilePath] string compilerFile = "")
        {
            Message = m;
            Severity = s;
            MxmlLineNumber = mxmlLine.HasValue ? mxmlLine.Value : (-1,-1);
            CompilerLineNumber = compilerLine;
            CompilerFile = Path.GetFileName(compilerFile);
        }
        public override string ToString()
        {
            string s = Severity switch
            {
                MessageSeverity.Debug => "@",
                MessageSeverity.Info => "#",
                MessageSeverity.Warning => "-",
                MessageSeverity.Error => "!",
                MessageSeverity.Success => "+",
                _ => "Info"
            };
            // [!] (12/23/24 00:00:00): blah blah blah
            string LineInfo = $"(MUIC:{CompilerLineNumber.ToString("0000")}";
            if(MxmlLineNumber.Line != -1) LineInfo += $" MXML:{MxmlLineNumber.Line}:{MxmlLineNumber.Position})\t";
            else LineInfo += ")";
            return $"[{s}] {LineInfo} {Message}";
        }
    }
}
