using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Centapp.CartoonCommon.JSON
{
    //====================================================
    //Grouped
    public class Episode
    {
        public int id { get; set; }
        public string name { get; set; }
        public string youtube_id { get; set; }
    }

    public class Season
    {
        public int season { get; set; }
        public List<Episode> episodes { get; set; }
    }

    public class RootObjectEpisodesGroupedBySeasons
    {
        public string statusmsg { get; set; }
        public string op { get; set; }
        public string targetVer { get; set; }
        public string value { get; set; }
        public List<Season> seasons { get; set; }
    }

    //====================================================
    //Flat

    public class MyEpisode
    {
        public int id { get; set; }
        public Dictionary<string, string> names { get; set; }
        public string youtube_id { get; set; }
    }

    public class RootObjectFlatEpisodes
    {
        public string statusmsg { get; set; }
        public string op { get; set; }
        public string targetVer { get; set; }
        public string value { get; set; }
        public List<MyEpisode> episodes { get; set; }
    }

}
