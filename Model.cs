using System.Collections.Generic;

namespace Dsevents
{
    public class Model
    {
        public List<Process> Processes { get; set; }
        public List<Channel> Channels { get; set; }
        public List<Message> Messages { get; set; }
        public List<Event> Events { get; set; }
    }
}