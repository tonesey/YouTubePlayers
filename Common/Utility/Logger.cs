using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Centapp.CartoonCommon.Utility
{
    internal class Logger
    {
        StringBuilder _sb = new StringBuilder();

        public void Reset()
        {
            _sb.Clear();
        }

        public void Log(string text)
        {
            _sb.AppendLine(string.Format("{0} - {1}", DateTime.Now.ToShortTimeString(), text));
        }

        public string GetLog() {

            return _sb.ToString();
        }

    }
}
