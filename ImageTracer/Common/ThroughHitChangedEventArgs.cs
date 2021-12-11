using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MonitorBlind.Common
{
    public class ThroughHitChangedEventArgs : EventArgs
    {
        public bool NewValue { get; set; }
    }
}
