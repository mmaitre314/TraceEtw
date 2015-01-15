using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventProviderGeneratorTests
{
    [EventSource(Name = "Company-Product-Component")]
    sealed class Logger : EventSource
    {
        public static Logger Events = new Logger();

        [Event(1, Level = EventLevel.Verbose)]
        public void Message(string message) { Events.WriteEvent(1, message); }
    }
}
