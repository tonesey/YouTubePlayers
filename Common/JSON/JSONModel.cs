using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Centapp.CartoonCommon.JSON
{
    public class Episody
    {
        public int id { get; set; }
        public string name { get; set; }
        public string url { get; set; }
        public string youtube_id { get; set; }
        public string free { get; set; }
    }

    public class Season
    {
        public int season { get; set; }
        public string title { get; set; }
        public string image { get; set; }
        public List<Episody> episodies { get; set; }
    }
}
