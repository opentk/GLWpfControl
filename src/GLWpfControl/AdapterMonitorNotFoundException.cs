using System;
using System.Collections.Generic;
using System.Text;

namespace OpenTK.Wpf
{

    [Serializable]
    class AdapterMonitorNotFoundException : Exception
    {
        public AdapterMonitorNotFoundException() { }
        public AdapterMonitorNotFoundException(string message) : base(message) { }
        public AdapterMonitorNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected AdapterMonitorNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
