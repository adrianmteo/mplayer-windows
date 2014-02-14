using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace mPlayer.Model
{
    public class LastFMWrapper
    {
        public const string ARTIST_INFO_URL = "http://ws.audioscrobbler.com/2.0/?method=artist.getinfo&artist={0}&api_key=4e1a230c7c6a1257ef84fa05fb9dfbd5&format=json";
        public const string ALBUM_ART_URL = "http://ws.audioscrobbler.com/2.0/?method=track.getInfo&api_key=4e1a230c7c6a1257ef84fa05fb9dfbd5&artist={0}&track={1}&format=json";

        public class ArtistInfo
        {
            public string Image
            { get; set; }

            public string Biography
            { get; set; }

            public List<SimilarArtist> Similar;
        }

        public static async Task<ArtistInfo> GetArtistInfo(string artist)
        {
            var x = new ArtistInfo();

            var data = await DownloadString(string.Format(ARTIST_INFO_URL, artist));
            if (data == null) return x;

            try
            {
                var json = await JsonConvert.DeserializeObjectAsync(data) as JContainer;
                var root = json["artist"];

                if (root != null)
                {
                    x.Image = root["image"].Last.Value<string>("#text");
                    if (x.Image.Contains("getyourtagsright"))
                    {
                        x.Image = "";
                    }

                    var bio = root["bio"].Value<string>("content");

                    var doc = new HtmlDocument();
                    doc.LoadHtml(HtmlEntity.DeEntitize(bio));

                    var text = doc.DocumentNode.InnerText;

                    var texts = text.Split('\n');
                    var sb = new StringBuilder();

                    foreach (var line in texts)
                    {
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            sb.AppendLine(line.Trim());
                        }
                    }

                    x.Biography = sb.ToString().Trim();


                    x.Similar = new List<SimilarArtist>();

                    try
                    {
                        var s = root["similar"]["artist"] as JArray;
                        foreach (var ss in s)
                        {
                            var si = new SimilarArtist()
                            {
                                Name = ss.Value<string>("name"),
                                Image = (ss["image"] as JArray)[2].Value<string>("#text")
                            };
                            if (string.IsNullOrEmpty(si.Image)) si.Image = "../Images/no_image.png";

                            x.Similar.Add(si);
                        }
                    }
                    catch { }
                }
            }
            catch
            {
            }

            return x;
        }

        public static async Task<Uri> GetAlbumArt(string artist, string title)
        {
            var data = await DownloadString(string.Format(ALBUM_ART_URL, artist, title));
            if (data == null) return null;

            try
            {
                var json = await JsonConvert.DeserializeObjectAsync(data) as JContainer;
                var images = json["track"]["album"]["image"] as JArray;

                if (images != null)
                {
                    return new Uri(images.Last.Value<string>("#text"));
                }
            }
            catch { }

            return null;
        }

        public static async Task<string> DownloadString(string url)
        {
            var client = new WebClient();

            try { return await client.DownloadStringTaskAsync(url); }
            catch { return null; }
        }
    }
}
