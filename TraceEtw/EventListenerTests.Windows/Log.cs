using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventListenerTests.Windows
{
    [EventSource(Name = "Company-Product-Component")]
    sealed class Log : EventSource
    {
        public static Log Events = new Log();

        public class Tasks
        {
            public const EventTask ProcessImage = (EventTask)1;
        }

        [Event(1, Level = EventLevel.Verbose)]
        public void Message(string message) { Events.WriteEvent(1, message); }

        [Event(2, Task = Tasks.ProcessImage, Opcode = EventOpcode.Start, Level = EventLevel.Informational)]
        public void ProcessImageStart() { Events.WriteEvent(2); }

        [Event(3, Task = Tasks.ProcessImage, Opcode = EventOpcode.Stop, Level = EventLevel.Informational)]
        public void ProcessImageStop() { Events.WriteEvent(3); }
    }
}
