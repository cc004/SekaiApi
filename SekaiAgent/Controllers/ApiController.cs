using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MessagePack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PCRAgent.Controllers
{
    public class Ref<T>
    {
        public T value;
    }

    [ApiController]
    [Route("api")]
    public class ApiController : ControllerBase
    {
        private readonly ILogger<ApiController> _logger;
        private readonly HttpClient client;
        private static TextWriter filelog;

        private void LogInformation(string info)
        {
            if (filelog == null) filelog = new StreamWriter(new FileStream("api.log", FileMode.Append, FileAccess.Write));
            filelog.WriteLine(info);
            filelog.Flush();
            Console.WriteLine(info);
        }

        public ApiController(ILogger<ApiController> logger)
        {
            _logger = logger;
            client = new HttpClient();
        }

        [HttpGet("{*route}")]
        [HttpPut("{*route}")]
        [HttpPost("{*route}")]
        [HttpPatch("{*route}")]
        public FileResult Api(string route)
        {
            var ms = Request.Body as MemoryStream;
            var body = ms.ToArray();

            var aes = Aes.Create();
            aes.Key = Encoding.ASCII.GetBytes("g2fcC0ZczN9MTJ61");
            aes.IV = Encoding.ASCII.GetBytes("msx3IV0i9XE5uYZ1");
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.ISO10126;

            var decrypted = aes.CreateDecryptor().TransformFinalBlock(body, 0, body.Length);

            LogInformation("//////////////////////////////REQEUST////////////////////////");
            LogInformation($"{Request.Method} {Request.Path}");
            foreach (var header in Request.Headers)
                LogInformation($"{header.Key} {header.Value}");

            if (decrypted.Length > 0)
                LogInformation(JToken.Parse(MessagePackSerializer.ConvertToJson(decrypted)).ToString(Formatting.Indented));

            LogInformation("//////////////////////////////RESPONSE////////////////////////");
            if (Request.Path.Value.EndsWith("a0030f53-41e9-4b52-a8a4-993b807d5869"))
            {
                LogInformation("bypassing param api");
                return File(Array.Empty<byte>(), "application/octet-stream");
            }

            WebRequest.DefaultWebProxy = null;
            var req = WebRequest.CreateHttp($"http://production-game-api.sekai.colorfulpalette.org{Request.Path}" + (Request.QueryString.HasValue ? Request.QueryString.Value : ""));
            req.Method = Request.Method;
            req.Headers.Clear();

            foreach (var header in Request.Headers)
                req.Headers.Add(header.Key, header.Value.ToString());

            if (Request.Method != "GET")
                req.GetRequestStream().Write(body, 0, body.Length);
            
            var res = req.GetResponse();
            
            foreach (var header in res.Headers.AllKeys)
                LogInformation($"{header} {res.Headers[header]}");

            var lb = new List<byte>();
            var s = res.GetResponseStream();

            for (; ; )
            {
                int b = s.ReadByte();
                if (b == -1) break;
                lb.Add((byte)b);
            }

            var resp = lb.ToArray();

            aes = Aes.Create();
            aes.Key = Encoding.ASCII.GetBytes("g2fcC0ZczN9MTJ61");
            aes.IV = Encoding.ASCII.GetBytes("msx3IV0i9XE5uYZ1");
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.ISO10126;
            decrypted = aes.CreateDecryptor().TransformFinalBlock(resp, 0, resp.Length);

            if (decrypted.Length > 0)
                LogInformation(JToken.Parse(MessagePackSerializer.ConvertToJson(decrypted)).ToString(Formatting.Indented));

            Response.Headers.Clear();

            foreach (var header in res.Headers.AllKeys)
                if (header != "Content-Type" && header != "Transfer-Encoding")
                    Response.Headers.Add(header, res.Headers[header]);

            return File(resp, "application/octet-stream");
        }
    }
}
