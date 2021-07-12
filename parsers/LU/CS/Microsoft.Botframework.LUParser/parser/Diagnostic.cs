using System;
using System.Collections.Generic;
using System.Text;
using Antlr4.Runtime;

namespace Microsoft.Botframework.LUParser.parser
{
    enum DiagnosticSeverity
    {
        Error,
        Warn
    }
    class Diagnostic: Error
    {

        public Diagnostic(Range range, string message, string severity = "ERROR")
        {
            Range = range;
            Message = message;
            Severity = severity;
        }

        public string StringMessage()
        {
            var secondMessage = Range == null ? Message : Range.StringMessage() + ":" + Message;
            return $"[{Severity}] { secondMessage }";
        }

        public static Diagnostic BuildDiagnostic(string message, DiagnosticSeverity severity = DiagnosticSeverity.Error, Range range = null, ParserRuleContext context = null)
        {
            Range actualRange = null;

            if (range != null)
            {
                var startPosition = new Position { Line = range.Start.Line, Character = range.Start.Character };
                var stopPosition = new Position { Line = range.End.Line, Character = range.End.Character };
                actualRange = new Range { Start = startPosition, End = stopPosition };
            } else if (context != null)
            {
                var startPosition = new Position { Line = context.Start.Line, Character = context.Start.Column };
                var stopPosition = new Position { Line = context.Stop.Line, Character = context.Stop.Column + context.Stop.Text.Length };
                actualRange = new Range { Start = startPosition, End = stopPosition };
            }

            return new Diagnostic(actualRange, message, severity.ToString().ToUpper());
        }
    }
}
