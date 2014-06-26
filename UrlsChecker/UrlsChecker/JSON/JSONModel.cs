using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UrlsChecker
{
    public class MyEpisode
    {
        public int id { get; set; }
        public Dictionary<string, string> names { get; set; }
        public string youtube_id { get; set; }
    }

    //public class Season
    //{
    //    public int season { get; set; }
    //    public List<Episode> episodes { get; set; }
    //}

    public class RootObjectFlatEpisodes
    {
        public string statusmsg { get; set; }
        public string op { get; set; }
        public string targetVer { get; set; }
        public string value { get; set; }
        public List<MyEpisode> episodes { get; set; }
    }
}
