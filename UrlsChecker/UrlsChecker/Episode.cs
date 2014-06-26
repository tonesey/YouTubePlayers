using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UrlsChecker
{
    class EpisodeInfo
    {
        public int Id { get; set; }

        public string OrigId { get; set; }

        public string Url { get; set; }

        public string Desc { get; set; }

        public string AppName { get; set; }

        public string YouTubeSearchHint { get; set; }
        
        public string YouTubeId { get; set; }

        public double Accuracy { get; set; }

        public string OrigTitle { get; set; }

        public override string ToString()
        {
            return string.Format("Desc: {0} -  OrigYoutubeTitle: {1} - Accuracy: {2}", new object[] { Desc, OrigTitle, Accuracy });
        }
    }
}
