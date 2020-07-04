using System.Text.Json;
using System.Linq;
using System.Collections.Generic;
using System;

namespace Dsevents
{
    class Program
    {
        static Model model = null;

        static void Main(string[] args)
        {
            string file = GetFileString();
            model = JsonSerializer.Deserialize<Model>(file);

            string mode = args[0];
            string id = args[1];

            Event point = GetEvent(id);

            List<Event> events = new List<Event>();
            if (mode == "past")
            {
                events = GetPasts(point);
            }
            else if (mode == "future")
            {
                events = GetFutures(point);
            }
            else if (mode == "concurrent")
            {
                List<Event> pasts = GetPasts(point);
                List<Event> futures = GetFutures(point);

                List<Event> allEvents = new List<Event>();
                allEvents.AddRange(pasts);
                allEvents.AddRange(futures);

                events = GetConcurrent(allEvents, point);
            }

            Console.WriteLine(
              String.Join(' ', events
                .Select(v => v.ID)
                .ToList()
              )
            );
        }

        private static string GetFileString()
        {
            string file = "";
            string line = "";
            while ((line = Console.ReadLine()) != null)
            {
                file += line;
            }

            if (file.Length == 0)
            {
                return "";
            }
            return file;
        }

        private static Event GetEvent(string id)
        {
            return model.Events.Find(v => v.ID == id);
        }

        public static List<Event> GetConcurrent(List<Event> allEvents, Event point)
        {
            List<Event> events = new List<Event>();

            model.Events.ForEach(e =>
            {
                if (allEvents.Find(v => v.ID == e.ID) == null && e.ID != point.ID)
                {
                    events.Add(e);
                }
            });

            return events;
        }

        private static List<Event> GetPasts(Event point, int depth = 0)
        {
            List<Event> events = GetPastEvents(point);

            if (depth != 0)
            {
                events.Add(point);
            }

            Channel channel = GetChannel(point);
            if (channel == null)
            {
                return events;
            }

            events.AddRange(AddEventsFrom(channel, point));

            events = UniqEvents(events);


            List<Event> eventsList = new List<Event>(events);
            eventsList.Add(point);

            List<Event> tempEventsList = new List<Event>();

            events.AddRange(PastRecursion(eventsList, tempEventsList, depth));

            return UniqEvents(events);
        }

        private static List<Event> PastRecursion(List<Event> eventsList, List<Event> tempEventsList, int depth)
        {
            eventsList.ForEach(e =>
            {
                if (e.ChannelID == null)
                {
                    return;
                }

                List<Channel> channels = GetPastChannels(e);

                channels.ForEach(channel =>
                {
                    List<Event> events = GetEvents(e, channel);

                    events.ForEach(x => tempEventsList.AddRange(GetPasts(x, depth + 1)));
                });
            });

            return tempEventsList;
        }

        private static List<Channel> GetPastChannels(Event e)
        {
            List<Channel> channels = new List<Channel>();

            model.Channels.ForEach(c =>
            {
                if (e.ChannelID == c.ID && c.From != e.ProcessID)
                {
                    channels.Add(c);
                }
            });

            return channels;
        }

        private static List<Event> GetPastEvents(Event point)
        {
            List<Event> events = new List<Event>();

            model.Events.ForEach(e =>
            {
                if (e.ProcessID == point.ProcessID &&
                        e.Seq < point.Seq)
                {
                    events.Add(e);
                }
            });

            return events;
        }

        private static Channel GetChannel(Event point)
        {
            return model.Channels.Find(v => v.ID == point.ChannelID);
        }

        private static List<Event> AddEventsFrom(Channel channel, Event point)
        {
            List<Event> events = new List<Event>();

            Event eventFrom = model.Events.Find(e =>
                e.ProcessID == channel.From &&
                e.ChannelID == point.ChannelID
            );

            model.Events.ForEach(e =>
            {
                if (e.ID != point.ID &&
                    e.ProcessID == channel.From &&
                    e.Seq <= eventFrom.Seq)
                {
                    events.Add(e);
                }
            });

            return events;
        }

        private static List<Event> GetFutures(Event point, int depth = 0)
        {
            List<Event> events = GetFutureEvents(point);

            if (depth != 0)
            {
                events.Add(point);
            }

            events = UniqEvents(events);

            List<Event> eventsList = new List<Event>(events);
            eventsList.Add(point);

            List<Event> tempEventsList = new List<Event>();

            events.AddRange(FutureRecursion(eventsList, tempEventsList, depth));

            return UniqEvents(events);
        }

        private static List<Event> GetFutureEvents(Event point)
        {
            List<Event> events = new List<Event>();

            model.Events.ForEach(e =>
            {
                if (e.ProcessID == point.ProcessID &&
                        e.Seq > point.Seq)
                {
                    events.Add(e);
                }
            });

            return events;
        }

        private static List<Event> FutureRecursion(List<Event> eventsList, List<Event> tempEventsList, int depth)
        {
            eventsList.ForEach(e =>
            {
                if (e.ChannelID == null)
                {
                    return;
                }

                List<Channel> channels = GetFutureChannels(e);

                channels.ForEach(channel =>
                {
                    List<Event> events = GetEvents(e, channel);

                    events.ForEach(x => tempEventsList.AddRange(GetFutures(x, depth + 1)));
                });
            });

            return tempEventsList;
        }

        private static List<Channel> GetFutureChannels(Event e)
        {
            List<Channel> channels = new List<Channel>();

            model.Channels.ForEach(c =>
            {
                if (e.ChannelID == c.ID && c.To != e.ProcessID)
                {
                    channels.Add(c);
                }
            });

            return channels;
        }

        private static List<Event> GetEvents(Event e, Channel channel)
        {
            List<Event> events = new List<Event>();

            model.Events.ForEach(v =>
            {
                if (v.ID != e.ID &&
                    v.ChannelID == channel.ID &&
                    (v.ProcessID == channel.From || v.ProcessID == channel.To))
                {
                    events.Add(v);
                }
            });

            return events;
        }

        private static List<Event> UniqEvents(List<Event> events)
        {
            return events.GroupBy(v => v.ID).Select(g => g.First()).ToList();
        }
    }
}