using SekaiClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SekaiDownloader
{
    class Program
    {

        public static void Main(string[] args)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("Accept", "*/*");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "deflate, gzip");
            client.DefaultRequestHeaders.Add("User-Agent", "UnityPlayer/2019.4.3f1 (UnityWebRequest/1.0, libcurl/7.52.0-DEV)");
            client.DefaultRequestHeaders.Add("X-Unity-Version", "2019.4.3f1");

            File.WriteAllText("assetinfo.json", PackHelper.Unpack(client.GetByteArrayAsync($"https://assetbundle-info.sekai.colorfulpalette.org/api/version/1.0.30/os/android?t={DateTime.Now:yyyyMMddHHmmss}").Result).ToString(Newtonsoft.Json.Formatting.Indented));
        }
    }
}
