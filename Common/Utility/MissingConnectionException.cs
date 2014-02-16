using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Centapp.CartoonCommon.Utility
{
    public class MissingConnectionException : Exception
    {
        public MissingConnectionException()
            : base()
        {

        }

        public MissingConnectionException(string msg)
            : base(msg)
        {

        }
    }
}
