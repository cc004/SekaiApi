using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SekaiClient
{
    public static class Utils
    {
        public static byte[] ReadToEnd(this Stream stream)
        {
            const int bufferSize = 4096;
            using var ms = new MemoryStream();
            byte[] buffer = new byte[bufferSize];
            int count;
            while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
                ms.Write(buffer, 0, count);
            return ms.ToArray();
        }

        public static string PadRightEx(this string str, int length)
        {
            return str + new string(Enumerable.Range(0, Math.Max(0, length - str.Sum(c => c > 127 ? 2 : 1))).Select(_ => ' ').ToArray());
        }

        public static long GetTimestamp(this DateTime time)
        {
            return (time.ToUniversalTime().Ticks - 621355968000000000) / 10000;
        }

        public static List<T> SortRandom<T>(this IEnumerable<T> collection)
        {
            List<T> result = new List<T>();
            Random rand = new Random();
            foreach (var t in collection)
                result.Insert(rand.Next(0, result.Count), t);
            return result;
        }
        public static byte[] ReadAll(this WebResponse response)
        {
            List<byte> buffer = new List<byte>();
            int b;
            while ((b = response.GetResponseStream().ReadByte()) != -1)
                buffer.Add((byte)b);
            return buffer.ToArray();
        }

        public static void Log(string from, object msg)
        {
            Console.WriteLine("[" + from + "] " + msg);
        }

        public static void sleep(int millis)
        {
            Thread.Sleep(millis);
        }

        public static string SendGet(string url)
        {
            HttpWebRequest request = WebRequest.CreateHttp(url);
            request.Method = "GET";
            request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1;SV1)";
            var response = request.GetResponse();
            return Encoding.UTF8.GetString(response.ReadAll());
        }

    }
}
