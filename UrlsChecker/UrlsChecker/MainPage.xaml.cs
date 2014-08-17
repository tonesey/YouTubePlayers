using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.IO;
using System.Xml;
using System.Threading;
using MyToolkit.Multimedia;
using System.Text;
using Microsoft.Phone.Tasks;
using MyToolkit.Networking;


using System.Text.RegularExpressions;
using Wp7Shared.Utility;
using System.Threading.Tasks;
using System.Diagnostics;
using JSONObjects;
using Newtonsoft.Json;
using Wp7Shared.Helpers;


namespace UrlsChecker
{
    // https://developers.google.com/api-client-library/dotnet/apis/youtube/v3
    // https://code.google.com/p/youtube-api-samples/source/browse/samples/dotnet/search.cs

    public class YouTubeVideo
    {
        public string Title { get; set; }
        public string VideoImageUrl { get; set; }
        public string VideoId { get; set; }
        public int Duration { get; set; }

        public override string ToString()
        {
            if (Accuracy > 0) return string.Format("{0} => {1}", Title, Accuracy);
            return Title;
        }

        public double Accuracy { get; set; }

        public string OrigTitle { get; set; }
    }

    public partial class MainPage : PhoneApplicationPage
    {

        private static ManualResetEvent _mre = new ManualResetEvent(false);


        XNamespace atomns = XNamespace.Get("http://www.w3.org/2005/Atom");
        XNamespace medians = XNamespace.Get("http://search.yahoo.com/mrss/");
        XNamespace ytns = XNamespace.Get("http://gdata.youtube.com/schemas/2007");

        Queue<AppItem> _appsToCheck = new Queue<AppItem>();
        Queue<EpisodeInfo> _episodesToCheck = new Queue<EpisodeInfo>();

        Queue<EpisodeInfo> _episodesQueue = new Queue<EpisodeInfo>();
        List<EpisodeInfo> _episodesList = new List<EpisodeInfo>();

        DateTime _startTime = DateTime.Now;
        DateTime _endTime = DateTime.Now;

        List<EpisodeInfo> _wrongEpisodes = new List<EpisodeInfo>();
        StringBuilder _report = new StringBuilder();
        List<string> _titleWords = null;
        AppItem _curApp;

        private int _episodeCount = 0;
        private int _episodeTot = 0;

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            ProgBar.Value = 0;

            ////valid link: http://www.youtube.com/watch?v=sb_hKB38QRw
            //YouTube.GetVideoUri("2323423423423", YouTubeQuality.Quality480P, (uri, ex) =>
            //{
            //    if (ex != null)
            //    {
            //    }
            //});

            //TestYoutubeApis();

            //Task.Run(() => Test());
            //Test();

            //ConvertXmlToJSON();
        }

        private void ConvertXmlToJSON()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            Stream stream = asm.GetManifestResourceStream("UrlsChecker.pingu_full.xml");
            var doc = XDocument.Load(stream);

            int index = 0;

            //<item id="2">
            //  <url>http://www.youtube.com/watch?v=lnKi4qDdLmQ</url>
            //  <origId>46</origId>
            //  <desc>
            //    <descItem lang="es-ES" value="Pingu y el regalo" />
            //    <descItem lang="it-IT" value="Pingu e il dono" />
            //    <descItem lang="en-US" value="Pingu and the gift" />
            //    <descItem lang="pt-BR" value="Pingu eo dom" />
            //  </desc>
            //</item>

            //_episodesList = new List<Episode>((from item in doc.Element("root").Descendants("item")
            //                                   select new MyEpisode()
            //                                   {
            //                                       id = ++index,
            //                                       youtube_id = GetYoutubeID(item.Element("url").Value),
            //                                       names = new Dictionary<string, string>() 
            //                                   }).ToList());


            RootObjectFlatEpisodes r = new RootObjectFlatEpisodes();
            r.episodes = new List<MyEpisode>();

            var status = doc.Element("root").Element("status");

            //<status value="warn" targetVer="3.1.10" op="lt" defMsg="WARNING: this app is obsolete, please install latest version! Thanks">
            //</status>

            r.statusmsg = status.Attribute("defMsg").Value;
            r.value = status.Attribute("value").Value;
            r.targetVer = status.Attribute("targetVer").Value;
            r.op = status.Attribute("op").Value;

            var items = doc.Element("root").Descendants("item");
            foreach (var itemXml in items)
            {
                var id = (itemXml.Attribute("id").Value);
                var youtube_id = GetYoutubeID(itemXml.Element("url").Value);
                var descs = itemXml.Element("desc").Descendants("descItem");

                Dictionary<string, string> descsDict = new Dictionary<string, string>();
                foreach (var desc in descs)
                {
                    descsDict.Add(desc.Attribute("lang").Value, desc.Attribute("value").Value);
                }

                var jsonEpisode = new MyEpisode { id = int.Parse(id), youtube_id = youtube_id, names = descsDict };
                r.episodes.Add(jsonEpisode);
            }

            string objTostr = Newtonsoft.Json.JsonConvert.SerializeObject(r);


            RootObjectFlatEpisodes rTest = Newtonsoft.Json.JsonConvert.DeserializeObject<RootObjectFlatEpisodes>(objTostr);

            //RootObject r = Newtonsoft.Json.JsonConvert.DeserializeObject<RootObject>(result.Replace("$", "Z_"));



        }

        //#region async/await
        ////http://msdn.microsoft.com/en-us/library/hh191443(VS.110).aspx
        ////http://msdn.microsoft.com/en-us/library/hh156513(v=vs.110).aspx
        //private async void Test()
        //{
        //    //string x = await TaskEx.Run(() => YouTube.GetVideoTitleAsync("sb_hKB38QRw"));
        //    string x = await YouTube.GetVideoTitleAsync("sb_hKB38QRw");
        //    await TaskEx.Run(() => GetResult());
        //}

        //private string GetResult()
        //{
        //    // Simula una elaborazione che dura diversi secondi (potrebbe essere la lettura di un file, l'accesso ad un risorsa di rete, ecc.).
        //    System.Threading.Thread.Sleep(5000);
        //    return DateTime.Now.ToString();
        //}
        //#endregion

        async Task FindYoutubeId(EpisodeInfo ep)
        {
            //Debug.WriteLine("<-- " + ep.Desc);
            string id = "not found";
            string title = ep.Desc;
            DateTime requestStartTime = DateTime.Now;
            string query = null;

            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            bool json = true;
            ////////////////////////////////////////////////////////////////////////////////////////////////////////
            if (json)
            {
                query = string.Format("http://gdata.youtube.com/feeds/api/videos?alt=json&max-results=1&start-index=1&q={0}&format=6", HttpUtility.UrlEncode(title));
            }
            else
            {
                query = string.Format("http://gdata.youtube.com/feeds/api/videos?max-results=1&start-index=1&q={0}&format=6", HttpUtility.UrlEncode(title));
            }
            var res = await Http.GetAsync(query);
            DateTime requestEndTime = DateTime.Now;
            TimeSpan duration = requestEndTime - requestStartTime;
            //Debug.WriteLine("request time: " + duration.Milliseconds);
            if (!res.Successful)
            {
                return;
            }
            var foundYouTubeEpisodes = GetYoutubeItems(json, res.Response);
            id = foundYouTubeEpisodes.First().VideoId;
            ep.YouTubeId = id;
            //Debug.WriteLine(ep.Desc + "-->");
        }

        async Task<string> FindYoutubeId(string title)
        {
            //byte[] byteArray = await client.GetByteArrayAsync(url);
            //DisplayResults(url, byteArray);
            //return byteArray.Length;

            string id = "not found";
            DateTime requestStartTime = DateTime.Now;
            string query = null;
            bool json = false;

            //http://gdata.youtube.com/demo/index.html
            if (json)
            {
                query = string.Format("http://gdata.youtube.com/feeds/api/videos?alt=json&max-results=1&start-index=1&q={0}&format=6", HttpUtility.UrlEncode(title));
            }
            else
            {
                query = string.Format("http://gdata.youtube.com/feeds/api/videos?max-results=1&start-index=1&q={0}&format=6", HttpUtility.UrlEncode(title));
            }
            // var res = await Http.Get(new HttpGetRequest(query), null);
            var res = await Http.GetAsync(query);
            DateTime requestEndTime = DateTime.Now;
            TimeSpan duration = requestEndTime - requestStartTime;
            //Debug.WriteLine("request time: " + duration.Milliseconds);
            if (!res.Successful)
            {
                return id;
            }
            // Dictionary<string, string> test = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string,string>>(res.Response);
            var foundYouTubeEpisodes = GetYoutubeItems(json, res.Response);
            id = foundYouTubeEpisodes.First().VideoId;
            return id;
        }

        //async Task<int> ProcessURL(string url, HttpClient client)
        //{
        //    byte[] byteArray = await client.GetByteArrayAsync(url);
        //    DisplayResults(url, byteArray);
        //    return byteArray.Length;
        //}

        private async void TestYoutubeApis()
        {
            await TestXmlProcessing_HttpReq("http://centapp.altervista.org/peppa.xml");

            // string searchterm = "peppa pig italiano";
            //string searchterm = "unit testing";
            //RetrieveAll(searchterm);
        }

        private async Task TestXmlProcessing_HttpReq(string indexFileUrl)
        {
            var res = await Http.GetAsync(indexFileUrl);
            if (!res.Successful)
            {
                return;
            }

            string errMsg = string.Empty;
            XDocument doc = new XDocument();

            try
            {
                //XmlReaderSettings settings = new XmlReaderSettings();
                //settings.DtdProcessing = DtdProcessing.Ignore;
                //Stream s = null;
                //s = res.ResponseStream;
                //using (XmlReader reader = XmlReader.Create(s, settings))
                //{
                //    doc = XDocument.Load(reader);
                //}

                string xml = res.Response.Substring(res.Response.IndexOf("<root"));
                doc = XDocument.Parse(xml);
            }
            catch (Exception ex)
            {
                throw;
            }

            int index = 0;
            _episodesList = new List<EpisodeInfo>((from item in doc.Element("root").Descendants("item")
                                                   select new EpisodeInfo()
                                                   {
                                                       Id = ++index,
                                                       OrigId = item.Element("origId") != null ? item.Element("origId").Value : string.Empty,
                                                       Desc = item.Element("desc") == null ? "" : item.Element("desc").Elements("descItem").Count() == 0 ? item.Element("desc").Value : item.Element("desc").Elements("descItem").First().Attribute("value").Value.ToString()
                                                   }).ToList());

            _episodesList.ForEach(ep => _episodesQueue.Enqueue(ep));

            ////////////////////////////////////////////////////////////////////////////////
            //1. completamento con query unica - inaffidabile, richiede troppi episodi per fare il match..................
            //_titleWords = new List<string>() { "Cartone", "Cartoni", "Episodio", "Episodi", "Stagione", "Animati", "Animato", "Nuovi", "2013", "Completi", "Serie" };
            //_titleWords = _titleWords.OrderByDescending(w => w.Length).ToList();
            //RetrieveAllItems_OneShot("peppa pig italiano");
            ////////////////////////////////////////////////////////////////////////////////

            ////////////////////////////////////////////////////////////////////////////////
            //2. completamento con query per singolo episodio (circa 20" x 50 episodi)
            // RetrieveAllItems_OneRequestForEach();
            ////////////////////////////////////////////////////////////////////////////////

            ////////////////////////////////////////////////////////////////////////////////
            //3.completamento con query per singolo episodio con async/await (circa 7" x 50 episodi)
            await RetrieveAllItems_Parallel(_episodesList);
            ////////////////////////////////////////////////////////////////////////////////


        }

        //private async Task TestXmlProcessing_WebClient(string indexFileUrl)
        //{
        //    _startTime = DateTime.Now;
        //    WebClient client = new WebClient();
        //    //client.OpenReadCompleted += new OpenReadCompletedEventHandler(client_OpenReadCompleted);
        //    client.OpenReadAsync(new Uri(indexFileUrl + "?" + Guid.NewGuid()), UriKind.Absolute);
        //    client.OpenReadCompleted += (s, e) =>
        //    {
        //        string errMsg = string.Empty;
        //        XDocument doc = new XDocument();
        //        Stream webStream = null;
        //        webStream = e.Result;
        //        XmlReaderSettings settings = new XmlReaderSettings();
        //        settings.DtdProcessing = DtdProcessing.Ignore;
        //        using (XmlReader reader = XmlReader.Create(webStream, settings))
        //        {
        //            doc = XDocument.Load(reader);
        //        }
        //        if (webStream != null)
        //        {
        //            webStream.Close();
        //        }
        //        int index = 0;
        //        _episodesList = new List<Episode>((from item in doc.Element("root").Descendants("item")
        //                                           select new Episode()
        //                                           {
        //                                               Id = ++index,
        //                                               OrigId = item.Element("origId") != null ? item.Element("origId").Value : string.Empty,
        //                                               Desc = item.Element("desc") == null ? "" : item.Element("desc").Elements("descItem").Count() == 0 ? item.Element("desc").Value : item.Element("desc").Elements("descItem").First().Attribute("value").Value.ToString()
        //                                           }).ToList());
        //        _episodesList.ForEach(ep => _episodesQueue.Enqueue(ep));
        //        ////////////////////////////////////////////////////////////////////////////////
        //        //1. completamento con query unica - inaffidabile, richiede troppi episodi per fare il match..................
        //        //_titleWords = new List<string>() { "Cartone", "Cartoni", "Episodio", "Episodi", "Stagione", "Animati", "Animato", "Nuovi", "2013", "Completi", "Serie" };
        //        //_titleWords = _titleWords.OrderByDescending(w => w.Length).ToList();
        //        RetrieveAllItems_OneShot("peppa pig italiano");
        //        ////////////////////////////////////////////////////////////////////////////////
        //        ////////////////////////////////////////////////////////////////////////////////
        //        //2. completamento con query per singolo episodio (circa 20" x 50 episodi)
        //        RetrieveAllItems_OneRequestForEach();
        //        ////////////////////////////////////////////////////////////////////////////////
        //        ////////////////////////////////////////////////////////////////////////////////
        //        //3.completamento con query per singolo episodio con async/await (circa 7" con xml e 4" con json x 50 episodi)
        //        RetrieveAllItems_Parallel(_episodesList);
        //        ////////////////////////////////////////////////////////////////////////////////
        //    };
        //}

        private async Task RetrieveAllItems_Parallel(List<EpisodeInfo> episodes)
        {
            //IEnumerable<Task<string>> downloadTasksQuery = from ep in _episodesList select FindYoutubeId(ep.Desc);
            //Task<string>[] downloadTasks = downloadTasksQuery.ToArray();
            //_startTime = DateTime.Now;
            //string[] urls = await TaskEx.WhenAll(downloadTasks);
            //_endTime = DateTime.Now;
            //TimeSpan duration = _endTime - _startTime;
            //Console.WriteLine("seconds:" + duration.Seconds);

            IEnumerable<Task> downloadTasksQuery = from ep in _episodesList select FindYoutubeId(ep);
            Task[] downloadTasks = downloadTasksQuery.ToArray();
#if DEBUG
            _startTime = DateTime.Now;
#endif
            await TaskEx.WhenAll(downloadTasks);
#if DEBUG
            _endTime = DateTime.Now;
            TimeSpan duration = _endTime - _startTime;

            StringBuilder sb = new StringBuilder();
            _episodesList.ForEach(ep => sb.AppendLine(ep.Desc + " - " + ep.YouTubeId));
            string epToStr = sb.ToString();
            Console.WriteLine("seconds:" + duration.Seconds);
            Console.WriteLine("epToStr:" + epToStr);
#endif
        }

        private void RetrieveAllItems_OneRequestForEach()
        {
            if (!_episodesQueue.Any())
            {
                //fine
                _endTime = DateTime.Now;
                TimeSpan duration = _endTime - _startTime;
                Console.WriteLine("seconds:" + duration.Seconds);
            }
            else
            {
                var ep = _episodesQueue.Dequeue();
                Console.WriteLine("Processing: " + ep.Desc);

                var query = string.Format("http://gdata.youtube.com/feeds/api/videos?max-results=1&start-index=1&q={0}&format=6", HttpUtility.UrlEncode(ep.Desc));
                var request = new HttpGetRequest(query);
                Http.Get(request, result =>
                {
                    if (!result.Successful)
                    {
                        return;
                    }
                    var foundYouTubeEpisodes = GetYoutubeItems(false, result.Response);

                    ep.Url = foundYouTubeEpisodes.First().VideoId;
                    RetrieveAllItems_OneRequestForEach();
                });
            }
        }

        private void RetrieveAllItems_OneShot(string searchterm)
        {
            //wc.DownloadStringCompleted += DownloadStringCompleted;
            // http://gdata.youtube.com/feeds/api/videos?max-results=50&start-index=3&q=visual studio 2013&format=6&safeSearch=strict
            //var searchUri = string.Format("http://gdata.youtube.com/feeds/api/videos?max-results=50&q={0}&format=6&safeSearch=strict", HttpUtility.UrlEncode(searchterm));
            //ok
            //var searchUri = string.Format("http://gdata.youtube.com/feeds/api/videos?max-results=50&q={0}&format=6", HttpUtility.UrlEncode(searchterm));

            var foundYouTubeEpisodes = new List<YouTubeVideo>();

            //ricerca #1 - 50 episodi
            var searchUri = BuildUri(searchterm, 1);
            var wc1 = new WebClient();
            wc1.DownloadStringAsync(new System.Uri(searchUri));
            wc1.DownloadStringCompleted += (sender1, e1) =>
            {
                foundYouTubeEpisodes.AddRange(GetYoutubeItems(false, e1.Result));

                //ricerca #2 - 50 + 50 = 100 episodi
                searchUri = BuildUri(searchterm, 51);
                var wc2 = new WebClient();
                wc2.DownloadStringAsync(new System.Uri(searchUri));
                wc2.DownloadStringCompleted += (sender2, e2) =>
                {
                    foundYouTubeEpisodes.AddRange(GetYoutubeItems(false, e2.Result));

                    //ricerca #3 - 50 + 50 + 50 = 150 episodi
                    searchUri = BuildUri(searchterm, 101);
                    var wc3 = new WebClient();
                    wc3.DownloadStringAsync(new System.Uri(searchUri));
                    wc3.DownloadStringCompleted += (sender3, e3) =>
                    {
                        foundYouTubeEpisodes.AddRange(GetYoutubeItems(false, e3.Result));

                        var finalList = foundYouTubeEpisodes.Where(ep => ep.Duration >= 240 && ep.Duration <= 600).OrderBy(ep => ep.Title).ToList<YouTubeVideo>();

                        StringBuilder sb = new StringBuilder();
                        finalList.ForEach(ep => sb.AppendLine(ep.Title));
                        string epToStrBefore = sb.ToString();

                        //pulizia dei titoli di youtube
                        foreach (var it in finalList)
                        {
                            string cleantitle = it.Title;
                            foreach (var item in searchterm.Split(' '))
                            {
                                cleantitle = GenericUtility.ReplaceEx(cleantitle, item, string.Empty);
                            }

                            foreach (var item in _titleWords)
                            {
                                cleantitle = GenericUtility.ReplaceEx(cleantitle, item, string.Empty);
                            }

                            //http://msdn.microsoft.com/en-us/library/az24scfc(v=vs.110).aspx
                            Regex rgx;

                            //if (cleantitle.Contains("3x51") || cleantitle.Contains("3x39") || cleantitle.Contains("3x21"))
                            //{
                            //    rgx = new Regex("[0-9{1}x0-9{2}]");
                            //    cleantitle = rgx.Replace(cleantitle, "").Trim();
                            //}

                            rgx = new Regex("[^a-zA-Z ']");
                            cleantitle = rgx.Replace(cleantitle, "").Trim();

                            it.OrigTitle = it.Title;
                            it.Title = cleantitle;
                        }

                        sb = new StringBuilder();
                        finalList.ForEach(ep => sb.AppendLine(ep.Title));
                        string epToStrAfter = sb.ToString();

                        foreach (var currentEpisode in _episodesList)
                        {
                            List<YouTubeVideo> matchingList = new List<YouTubeVideo>();
                            foreach (var it in finalList)
                            {
                                YouTubeVideo v = new YouTubeVideo();
                                v.Title = it.Title;
                                v.VideoId = it.VideoId;
                                v.OrigTitle = it.OrigTitle;
                                v.VideoImageUrl = it.VideoImageUrl;
                                v.Accuracy = it.Title.DiceCoefficient(currentEpisode.Desc);
                                matchingList.Add(v);
                            }

                            List<YouTubeVideo> sortedList = matchingList.OrderByDescending(el => el.Accuracy).ToList();
                            YouTubeVideo bestMatchingEpisode = sortedList.First();

                            currentEpisode.Accuracy = bestMatchingEpisode.Accuracy;
                            currentEpisode.Url = bestMatchingEpisode.VideoId;
                            currentEpisode.OrigTitle = bestMatchingEpisode.OrigTitle;
                        }

                        _endTime = DateTime.Now;
                        TimeSpan duration = _endTime - _startTime;
                        Console.WriteLine("seconds:" + duration.Seconds);
                    };
                };
            };
        }

        private static string BuildUri(string searchterm, int index)
        {
            int MAX_RESULTS = 50;
            return string.Format("http://gdata.youtube.com/feeds/api/videos?max-results={0}&start-index={1}&q={2}&format=6",
                                    new object[] { 
                                        MAX_RESULTS, 
                                        index, 
                                        HttpUtility.UrlEncode(searchterm) 
                                    });
        }

        private List<YouTubeVideo> GetYoutubeItems(bool json, string result)
        {
            List<YouTubeVideo> foundYouTubeEpisodes = new List<YouTubeVideo>();

            if (!json)
            {
                var xml = XElement.Parse(result);
                foreach (var entry in xml.Descendants(atomns.GetName("entry")))
                {
                    YouTubeVideo v = new YouTubeVideo();
                    string videoId = entry.Element(atomns.GetName("id")).Value;
                    string title = entry.Element(atomns.GetName("title")).Value;
                    int duration = int.Parse(entry.Element(medians.GetName("group")).Element(ytns.GetName("duration")).Attribute("seconds").Value);
                    string videoImageUrl = (from thumbnail in entry.Descendants(medians.GetName("thumbnail"))
                                            where thumbnail.Attribute("height").Value == "240"
                                            select thumbnail.Attribute("url").Value).FirstOrDefault();
                    v.VideoId = videoId.Split('/').Last(); ;
                    v.VideoImageUrl = videoImageUrl;
                    v.Title = title;
                    v.Duration = duration;
                    foundYouTubeEpisodes.Add(v);
                }
            }
            else
            {
                RootObject r = Newtonsoft.Json.JsonConvert.DeserializeObject<RootObject>(result.Replace("$", "Z_"));
                YouTubeVideo v = new YouTubeVideo();
                string title = r.feed.entry[0].title.Z_t;
                string duration = r.feed.entry[0].mediaZ_group.ytZ_duration.seconds;
                string id = r.feed.entry[0].id.Z_t;
                string videoImageUrl = r.feed.entry[0].mediaZ_group.mediaZ_thumbnail.First(i => i.width >= 120).url;

                v.Title = title;
                v.Duration = int.Parse(duration);
                v.VideoImageUrl = videoImageUrl;
                v.VideoId = id.Split('/').Last();
                foundYouTubeEpisodes.Add(v);
            }
            foundYouTubeEpisodes = foundYouTubeEpisodes.OrderBy(e => e.Title).ToList();
            return foundYouTubeEpisodes;
        }

        //private void VideoListSelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    var video = ResultsList.SelectedItem as YouTubeVideo;
        //    if (video != null)
        //    {
        //        var parsed = video.VideoId.Split('/');
        //        var id = parsed[parsed.Length - 1];
        //        var playbackUrl = "vnd.youtube:" + id;

        //        var task = new WebBrowserTask { URL = playbackUrl };
        //        task.Show();
        //    }
        //}

        private void Init()
        {
            bool test = false;
            _appsToCheck.Clear();
            _episodesToCheck.Clear();

            if (!test)
            {
                // _appsToCheck.Enqueue(new AppItem() { IndexFile = "http://centapp.altervista.org/MyLittlePony_en.xml", Name = "My Little Pony", YouTubeSearchHint = "My Little Pony" });
                //_appsToCheck.Enqueue(new AppItem() { IndexFile = "http://centapp.altervista.org/peppa.xml", Name = "Peppa Pig (XML)", YouTubeSearchHint = "Peppa Pig" });
                _appsToCheck.Enqueue(new AppItem() { IndexFile = "http://centapp.altervista.org/peppa_it_src_json.xml", Name = "Peppa Pig (JSON)", YouTubeSearchHint = "Peppa Pig", IndexType = IndexType.JSONGrouped });
                //_appsToCheck.Enqueue(new AppItem() { IndexFile = "http://centapp.altervista.org/peppa_en-US.xml", Name = "Peppa Pig (en)", YouTubeSearchHint = "Peppa Pig" });
                //_appsToCheck.Enqueue(new AppItem() { IndexFile = "http://centapp.altervista.org/peppa_es.xml", Name = "Peppa Pig (es)", YouTubeSearchHint = "Peppa Pig" });
                _appsToCheck.Enqueue(new AppItem() { IndexFile = "http://centapp.altervista.org/peppa_pt.xml", Name = "Peppa Pig (pt)", YouTubeSearchHint = "Peppa Pig" });
                //_appsToCheck.Enqueue(new AppItem() { IndexFile = "http://centapp.altervista.org/peppa_ru.xml", Name = "Свинка Пеппа", YouTubeSearchHint = "Свинка Пеппа" });
                //_appsToCheck.Enqueue(new AppItem() { IndexFile = "http://centapp.altervista.org/pingu_full.xml", Name = "Pingu", YouTubeSearchHint = "Pingu" });
                _appsToCheck.Enqueue(new AppItem() { IndexFile = "http://centapp.altervista.org/pingu_src_json.xml", Name = "Pingu (JSON)", YouTubeSearchHint = "Pingu", IndexType = IndexType.JSONFlat });
                //_appsToCheck.Enqueue(new AppItem() { IndexFile = "http://centapp.altervista.org/puffi.xml", Name = "Puffi", YouTubeSearchHint = "Puffi" });
                // _appsToCheck.Enqueue(new AppItem() { IndexFile = "http://centapp.altervista.org/banane.xml", Name = "Banane in pigiama", YouTubeSearchHint = "Banane in pigiama" });
                //_appsToCheck.Enqueue(new AppItem() { IndexFile = "http://centapp.altervista.org/cinico.xml", Name = "Cinico TV", YouTubeSearchHint = "Cinico TV" });
            }
            else
            {
                _appsToCheck.Enqueue(new AppItem() { IndexFile = "http://centapp.altervista.org/peppa_test.xml", Name = "Peppa Pig Test" });
            }
            ProgBar.Value = 0;
        }

        internal static string GetYoutubeID(string uri)
        {
            ////http://www.youtube.com/watch?v=1CuGUN_rmpE
            //string id = "Uh_tZEkIVS4";
            if (string.IsNullOrEmpty(uri)) throw new ArgumentException("uri not valid");
            return uri.Substring(uri.IndexOf('=') + 1);
        }

        //private void Button_Tap_Load(object sender, GestureEventArgs e)
        //{

        //}

        private void Button_Tap_Send(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var email = new EmailComposeTask();
            email.To = "centapp@hotmail.com";
            email.Subject = "url checker report";
            email.Body = _report.ToString();
            email.Show();
        }

        private void Button_Tap_Check(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Init();
            _report.Clear();
            ButtonCheck.IsEnabled = false;
            ButtonSend.IsEnabled = false;
            ProcessAppIndex();
        }

        private void ProcessAppIndex()
        {
            if (_appsToCheck.Any())
            {
                _curApp = _appsToCheck.Dequeue();
                // _report.AppendLine("loading: " + url);
                DownloadItemsAsynch(_curApp);
            }
            else
            {
                _episodeTot = _episodesToCheck.Count;
                //fine
                Log("Starting URLs check...");
                CheckUrls();
            }
        }

        public void DownloadItemsAsynch(AppItem app)
        {
            Log("Loading: " + app.IndexFile);
            WebClient client = new WebClient();
            //client.OpenReadCompleted += new OpenReadCompletedEventHandler(client_OpenReadCompleted);
            client.OpenReadAsync(new System.Uri(app.IndexFile + "?" + Guid.NewGuid()), UriKind.Absolute);

            client.OpenReadCompleted += (s, e) =>
            {
                string errMsg = string.Empty;

                if (app.IndexType == IndexType.JSONFlat || app.IndexType == IndexType.JSONGrouped)
                {
                    RootObjectEpisodesGroupedBySeasons rootGrouped = null;
                    RootObjectFlatEpisodes rootFlat = null;

                    Stream webStream = null;
                    webStream = e.Result;
                    string data = null;
                    using (StreamReader reader = new StreamReader(webStream))
                    {
                        data = reader.ReadToEnd();
                    }

                    if (app.IndexType == IndexType.JSONFlat)
                    {
                        rootFlat = JsonConvert.DeserializeObject<RootObjectFlatEpisodes>(data);
                        foreach (var episode in rootFlat.episodes)
                        {
                            var item = new EpisodeInfo()
                            {
                                AppName = app.Name,
                                Id = episode.id,
                                Url = YouTubeHelper.BuildYoutubeID(episode.youtube_id),
                                Desc = episode.names.FirstOrDefault(n => n.Key.Contains("en")).Value
                            };
                            _episodesToCheck.Enqueue(item);
                        }
                    }
                    else
                    {
                        rootGrouped = JsonConvert.DeserializeObject<RootObjectEpisodesGroupedBySeasons>(data);
                        foreach (var season in rootGrouped.seasons)
                        {
                            foreach (var episode in season.episodes)
                            {
                                var item = new EpisodeInfo()
                                {
                                    AppName = app.Name,
                                    Id = episode.id,
                                    Url = YouTubeHelper.BuildYoutubeID(episode.youtube_id),
                                    Desc = episode.name
                                };
                                _episodesToCheck.Enqueue(item);
                            }
                        }
                    }
                }
                else if (app.IndexType == IndexType.Xml)
                {
                    XDocument doc = new XDocument();
                    Stream webStream = null;
                    webStream = e.Result;
                    XmlReaderSettings settings = new XmlReaderSettings();
                    settings.DtdProcessing = DtdProcessing.Ignore;
                    using (XmlReader reader = XmlReader.Create(webStream, settings))
                    {
                        doc = XDocument.Load(reader);
                    }

                    if (webStream != null)
                    {
                        webStream.Close();
                    }

                    //AppName = xmlUrl.Substring(xmlUrl.LastIndexOf("/") + 1).Replace(".xml", "");

                    int index = 0;
                    var episodes = new List<EpisodeInfo>((from item in doc.Element("root").Descendants("item")
                                                          select new EpisodeInfo()
                                                                   {
                                                                       AppName = app.Name,
                                                                       YouTubeSearchHint = app.YouTubeSearchHint,
                                                                       Id = ++index,
                                                                       OrigId = item.Element("origId") != null ? item.Element("origId").Value : string.Empty,
                                                                       Url = item.Element("url").Value,
                                                                       Desc = item.Element("desc") == null ? "" : item.Element("desc").Elements("descItem").Count() == 0 ? item.Element("desc").Value : item.Element("desc").Elements("descItem").First().Attribute("value").Value.ToString()
                                                                   }).ToList());

                    episodes.ForEach(it => _episodesToCheck.Enqueue(it));
                }

                //_dict.Add(url, episodes);
                //Log(">>> " + xmlUrl + " loaded");
                ProcessAppIndex();

            };

        }

        private void Log(string p)
        {
            Dispatcher.BeginInvoke(() =>
            {
                TextBlockLogger.Text = p + Environment.NewLine + TextBlockLogger.Text;
            });
        }

        //HTML based
        private bool Fix(string titleOfMissingEpisode, out string newYoutTubeId)
        {

            newYoutTubeId = null;
            string t = "<html></html>";
            //TODO richiesta HTTP

            //string searchTerm = "Peppa Pig papà perde gli occhiali";
            string query = string.Format("http://www.youtube.com/results?search_query={0}", titleOfMissingEpisode.Replace(" ", "+").Replace("'", ""));

            var request = new HttpGetRequest(query);
            // request.RequestGZIP = false; // default is true
            //request.Query.Add("name", "value");
            //request.Credentials = new NetworkCredential("username", "password");
            // the url is now: "http://www.server.com?name=value"
            Http.Get(request, result =>
            {
                if (result.Successful)
                {
                    t = result.Response;
                }
            });


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

            List<EpisodeInfo> episodesList = new List<EpisodeInfo>();
            //http://www.tsjensen.com/blog/post/2011/05/27/Four+Functions+For+Finding+Fuzzy+String+Matches+In+C+Extensions.aspx
            foreach (var item in episodesElements)
            {
                episodesList.Add(new EpisodeInfo()
                {
                    Desc = item.Attribute("data-context-item-title").Value,
                    YouTubeId = item.Attribute("data-context-item-id").Value,
                    Accuracy = item.Attribute("data-context-item-title").Value.DiceCoefficient(titleOfMissingEpisode)
                });
            }

            var sortedList = episodesList.OrderByDescending(el => el.Accuracy);
            newYoutTubeId = sortedList.First().YouTubeId;
            return true;
        }

        private void OnRequestFinished(HttpResponse obj)
        {
            throw new NotImplementedException();
        }

        private async void CheckUrls()
        {
            if (_episodesToCheck.Any())
            {
                EpisodeInfo ep = _episodesToCheck.Dequeue();
                _episodeCount++;

                Dispatcher.BeginInvoke(() =>
                {
                    ProgBar.Value = (_episodeCount * 100) / _episodeTot;
                });


                //valid link: http://www.youtube.com/watch?v=sb_hKB38QRw
                var uri = await YouTube.GetVideoUriAsync(GetYoutubeID(ep.Url), YouTubeQuality.Quality480P);
                if (uri == null)
                {
                    _wrongEpisodes.Add(ep);
                    Log(string.Format("{0}: {1}", ep.AppName, ep.Desc));
                }
                CheckUrls();
            }
            else
            {
                //fine

                Dispatcher.BeginInvoke(() =>
                {
                    ProgBar.Value = 0;
                    ButtonCheck.IsEnabled = true;
                    ButtonSend.IsEnabled = true;
                    Log("check finished");
                });

                if (_wrongEpisodes.Any())
                {
                    var groupedBadEpisodes = from ep in _wrongEpisodes
                                             group ep by ep.AppName into g
                                             select new { AppName = g.Key, Episodes = g };

                    foreach (var g in groupedBadEpisodes)
                    {
                        _report.AppendLine("----------------");
                        _report.AppendLine(g.AppName);
                        _report.AppendLine("----------------");
                        foreach (var ep in g.Episodes)
                        {
                            //_report.AppendLine(string.Format("{0}: {1}", string.IsNullOrEmpty(ep.Desc) ? ep.Id.ToString() : ep.Desc, ep.Url));
                            //_report.AppendLine(ep.Desc);
                            _report.AppendLine(string.Format("{0} - {1}", ep.Id.ToString(), ep.Desc));
                            var searchTerm = ep.YouTubeSearchHint + " " + ep.Desc;
                            _report.AppendLine(string.Format("http://www.youtube.com/results?search_query={0}", searchTerm.Replace(" ", "+").Replace("'", "")));
                            _report.AppendLine();
                        }
                    }
                }
                else
                {
                    _report.AppendLine("EVERYTHING OK!");
                }
                _report.AppendLine("=============");
            }
        }
    }
}