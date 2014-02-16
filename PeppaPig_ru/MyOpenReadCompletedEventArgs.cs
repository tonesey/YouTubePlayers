using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Centapp.CartoonCommon
{
    class MyOpenReadCompletedEventArgs
    {
        public MyOpenReadCompletedEventArgs() { }

        public Exception Error { get; set; }

        public Stream Result { get; set; }
    }
}
