using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UrlsChecker
{

    public enum IndexType { 
        Xml,
        JSONFlat,
        JSONGrouped
    }

    public class AppItem
    {
        public string Name { get; set; }
        public string IndexFile { get; set; }
        public string YouTubeSearchHint { get; set; }
        public IndexType IndexType { get; set; }

        public AppItem() {
            IndexType = UrlsChecker.IndexType.Xml;
        }
    }
}
