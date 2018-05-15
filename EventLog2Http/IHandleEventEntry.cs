using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventLog2Http
{
    interface IHandleEventEntry
    {   
        void HandleEntry<T>(T input, CancellationToken Token);

        void HandleEntryAsync<T>(T input, CancellationToken Token);
    }
}
