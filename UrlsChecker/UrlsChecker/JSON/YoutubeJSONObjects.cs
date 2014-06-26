using System.Collections.Generic;

namespace JSONObjects
{
    public class Id
    {
        public string Z_t { get; set; }
    }

    public class Updated
    {
        public string Z_t { get; set; }
    }

    public class Category
    {
        public string scheme { get; set; }
        public string term { get; set; }
    }

    public class Title
    {
        public string Z_t { get; set; }
        public string type { get; set; }
    }


    public class Logo
    {
        public string Z_t { get; set; }
    }

    public class Link
    {
        public string rel { get; set; }
        public string type { get; set; }
        public string href { get; set; }
    }

    public class Name
    {
        public string Z_t { get; set; }
    }

    public class Uri
    {
        public string Z_t { get; set; }
    }

    public class Author
    {
        public Name name { get; set; }
        public Uri uri { get; set; }
    }

    public class Generator
    {
        public string Z_t { get; set; }
        public string version { get; set; }
        public string uri { get; set; }
    }

    public class OpenSearchZTotalResults
    {
        public int Z_t { get; set; }
    }

    public class OpenSearchZStartIndex
    {
        public int Z_t { get; set; }
    }

    public class OpenSearchZItemsPerPage
    {
        public int Z_t { get; set; }
    }


    public class Published
    {
        public string Z_t { get; set; }
    }



    public class Content
    {
        public string Z_t { get; set; }
        public string type { get; set; }
    }

    public class GdZFeedLink
    {
        public string rel { get; set; }
        public string href { get; set; }
        public int countHint { get; set; }
    }

    public class GdZComments
    {
        public GdZFeedLink gdZ_feedLink { get; set; }
    }

    public class MediaZCategory
    {
        public string Z_t { get; set; }
        public string label { get; set; }
        public string scheme { get; set; }
    }

    public class MediaZContent
    {
        public string url { get; set; }
        public string type { get; set; }
        public string medium { get; set; }
        public string isDefault { get; set; }
        public string expression { get; set; }
        public int duration { get; set; }
        public int ytZ_format { get; set; }
    }

    public class MediaZDescription
    {
        public string Z_t { get; set; }
        public string type { get; set; }
    }

    public class MediaZKeywords
    {
    }

    public class MediaZPlayer
    {
        public string url { get; set; }
    }

    public class MediaZThumbnail
    {
        public string url { get; set; }
        public int height { get; set; }
        public int width { get; set; }
        public string time { get; set; }
    }

    public class MediaZTitle
    {
        public string Z_t { get; set; }
        public string type { get; set; }
    }

    public class YtZDuration
    {
        public string seconds { get; set; }
    }

    public class MediaZGroup
    {
        public List<MediaZCategory> mediaZ_category { get; set; }
        public List<MediaZContent> mediaZ_content { get; set; }
        public MediaZDescription mediaZ_description { get; set; }
        public MediaZKeywords mediaZ_keywords { get; set; }
        public List<MediaZPlayer> mediaZ_player { get; set; }
        public List<MediaZThumbnail> mediaZ_thumbnail { get; set; }
        public MediaZTitle mediaZ_title { get; set; }
        public YtZDuration ytZ_duration { get; set; }
    }

    public class GdZRating
    {
        public double average { get; set; }
        public int max { get; set; }
        public int min { get; set; }
        public int numRaters { get; set; }
        public string rel { get; set; }
    }

    public class YtZStatistics
    {
        public string favoriteCount { get; set; }
        public string viewCount { get; set; }
    }

    public class Entry
    {
        public Id id { get; set; }
        public Published published { get; set; }
        public Updated updated { get; set; }
        public List<Category> category { get; set; }
        public Title title { get; set; }
        public Content content { get; set; }
        public List<Link> link { get; set; }
        public List<Author> author { get; set; }
        public GdZComments gdZ_comments { get; set; }
        public MediaZGroup mediaZ_group { get; set; }
        public GdZRating gdZ_rating { get; set; }
        public YtZStatistics ytZ_statistics { get; set; }
    }

    public class Feed
    {
        public string xmlns { get; set; }
        public string xmlnsZ_media { get; set; }
        public string xmlnsZ_openSearch { get; set; }
        public string xmlnsZ_gd { get; set; }
        public string xmlnsZ_yt { get; set; }
        public Id id { get; set; }
        public Updated updated { get; set; }
        public List<Category> category { get; set; }
        public Title title { get; set; }
        public Logo logo { get; set; }
        public List<Link> link { get; set; }
        public List<Author> author { get; set; }
        public Generator generator { get; set; }
        public OpenSearchZTotalResults openSearchZ_totalResults { get; set; }
        public OpenSearchZStartIndex openSearchZ_startIndex { get; set; }
        public OpenSearchZItemsPerPage openSearchZ_itemsPerPage { get; set; }
        public List<Entry> entry { get; set; }
    }

    public class RootObject
    {
        public string version { get; set; }
        public string encoding { get; set; }
        public Feed feed { get; set; }
    }
}