using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using Mono.Options;
using Newtonsoft.Json;
using System.Threading;

namespace EventLog2Http
{
    class EventPump
    {
        static void Main(string[] args) {
            List<string> logs = new List<string>();
            List<int> event_ids = new List<int>();
            int verbose = 0;
            bool showHelp = false;

            var p = new OptionSet()
                .Add("v", "Verbosely print internal events.", v => ++verbose)
                .Add("l|log=", "Specify log to collect from, may be used multiple times.", l => logs.Add(l))
                .Add("i|id=", "Comma separated list of event IDs to filter on", id => {
                    string[] elements = id.Split(',');
                    foreach (var i in elements) {
                        int x = -1;
                        int.TryParse(i, out x);
                        if (x > 0) event_ids.Add(x);
                    }
                })
                .Add("h|?|help", "Show this help.", v => showHelp = true);
            p.Parse(args);

            if (showHelp) {
                var helpText = @"Usage: EventPump.exe -log Application -id 63,25
the -id option may contain multiple values separated by
commas but no whitespace.
";
                Console.WriteLine();
                Console.WriteLine(helpText);
                Console.WriteLine();
                p.WriteOptionDescriptions(Console.Out);
                Console.WriteLine();

                return;
            }

#if DEBUG
            foreach (var l in logs) Console.WriteLine("Log: {0}", l);
            foreach (var i in event_ids) Console.WriteLine("ID's: {0}", i);
#endif

            var watcher = new EventLogWatcher(logs, event_ids);
            Console.WriteLine($"Watcher status: {watcher.Status}");
            watcher.Start();

            bool go = true;
            do {
                Console.Write("Get status: s\nCancel/stop: c \nSelection> ");
                var choice = Console.ReadKey();
                Console.Write("\n");
                switch (choice.KeyChar) {
                    case 'c':
                    case 'C':
                        Console.WriteLine("Stopping...");
                        watcher.Stop();
                        Console.WriteLine($"Watcher status: {watcher.Status}");
                        go = false;
                        break;
                    case 's':
                    case 'S':
                        Console.WriteLine($"Watcher status: {watcher.Status}");
                        break;
                    default:
                        Console.WriteLine("S or C fool.");
                        break;
                }

            } while (go);

            while (watcher.Status == TaskStatus.Running) {
                Thread.Sleep(500);
                Console.WriteLine($"Watcher status: {watcher.Status}");
            }
        }

        class EventLogWatcher
        {
            List<string> logs = new List<string>();
            List<int> event_ids = new List<int>();
            List<EventLog> watching_logs = new List<EventLog>();
            BlockingCollection<EventLogEntry> events = new BlockingCollection<EventLogEntry>();
            CancellationTokenSource _tokenSource = new CancellationTokenSource();
            Task _watcherTask;

            public EventLogWatcher(List<string> Logs, List<int> Event_Ids) {
                logs.AddRange(Logs);
                event_ids.AddRange(Event_Ids);
                
            }

            private BlockingCollection<EventLogEntry> Events { get => events; set => events = value; }

            public TaskStatus Status { get => _watcherTask?.Status ?? TaskStatus.Created;  /* It's a lie, but so what */ }

            public List<string> WatchingLogs { get => watching_logs.Select(x => x.LogDisplayName).ToList(); }

            public void Start() {
                foreach (var l in logs) {
                    var el = new EventLog(l);
                    el.EntryWritten += (s, e) => {
                        if (event_ids.Any(x => x == e.Entry.InstanceId || x == e.Entry.EventID)) {
                            Events.TryAdd(e.Entry);
                        }
                        else if (event_ids.Count == 0) {
                            Events.TryAdd(e.Entry);
                        }
                    };
                    el.EnableRaisingEvents = true;
                    watching_logs.Add(el);
                }

                var _ctoken = _tokenSource.Token;
                _watcherTask = Task.Factory.StartNew(() => { DoTheThing(Events, _tokenSource.Token); }, _ctoken);
            }

            public void Stop() {
                _tokenSource.Cancel();
                Events.TryAdd(null);  // Enumerator will block until the next event otherwise
            }

            void DoTheThing (BlockingCollection<EventLogEntry> Events, CancellationToken Token) {

                foreach (var e in Events.GetConsumingEnumerable()) {

                    if (Token.IsCancellationRequested) {
                        Token.ThrowIfCancellationRequested();
                    }

                    if (e != null) {
                        var eventEntry = new Dictionary<string, dynamic>(); ;
                        eventEntry.Add("TimeGenerated", e.TimeGenerated);
                        eventEntry.Add("EventID", e.EventID);
                        eventEntry.Add("InstanceID", e.InstanceId);
                        eventEntry.Add("EntryType", e.EntryType);
                        eventEntry.Add("EventSource", e.Source);
                        eventEntry.Add("MachineName", e.MachineName);
                        eventEntry.Add("UserName", e.UserName);
                        eventEntry.Add("Message", e.Message);
                        Console.WriteLine(JsonConvert.SerializeObject(eventEntry));
                    }
                }
            }
        }
    }
}
