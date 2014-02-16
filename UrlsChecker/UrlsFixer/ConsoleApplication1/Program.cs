using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    public class Episode
    {
        public string Title { get; set; }
        public string Id { get; set; }
        public string Tag { get; set; }
        public double Accuracy { get; set; }

        public override string ToString()
        {
            return Title + " - " + Id + " -> " + Accuracy;
        }

    }


    class Program
    {


        static void Main(string[] args)
        {
            //////////////////////////////////////////////
            string origTitle = "peppa pig papà appende di foto";
            //////////////////////////////////////////////

            string t = Resource1.test;
            int start = t.IndexOf("<ol id=\"search-results\"");
            int end = t.IndexOf("</ol>");
            string searchResults = t.Substring(start, end - start + "</ol>".Length);

            StringBuilder sb = new StringBuilder();
            foreach (var line in searchResults.Split('\n'))
            {
                if (!line.Trim().StartsWith("<img"))
                {
                    sb.AppendLine(line);
                }
            }
            string newRes = sb.ToString();
            XDocument doc = XDocument.Parse(newRes);
            var episodesElements = doc.Descendants("li").Where(d => d.Attribute("data-context-item-id") != null && d.Attribute("data-context-item-title") != null);

            List<Episode> episodesList = new List<Episode>();
            //http://www.tsjensen.com/blog/post/2011/05/27/Four+Functions+For+Finding+Fuzzy+String+Matches+In+C+Extensions.aspx
            foreach (var item in episodesElements)
            {
                episodesList.Add(new Episode()
                {
                    Title = item.Attribute("data-context-item-title").Value,
                    Id = item.Attribute("data-context-item-id").Value,
                    Accuracy = item.Attribute("data-context-item-title").Value.DiceCoefficient(origTitle)
                });
            }

           var sortedList = episodesList.OrderByDescending(el => el.Accuracy);


            //data-context-item-title="Peppa Pig - S01E45 - Papa' Appende una Foto" data-context-item-id="v2KmltRBQ28"
            //<img data-group-key="thumb-group-4" src="peppa%20pig%20papa%20appende%20una%20foto%20-%20YouTube_files/pixel-vfl3z5WfW.gif" alt="Miniatura" data-thumb="//i1.ytimg.com/vi/i9czkcYTW_c/mqdefault.jpg" width="185">
        }
    }
}
