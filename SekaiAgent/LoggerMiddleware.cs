using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCRAgent
{
    public class LoggerMiddleware
    {
        private readonly RequestDelegate _next;
        public static byte[] data;

        public LoggerMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            IEnumerable<byte> requestContent = new byte[0];
            int b;

            do
            {
                var t = new byte[1024];
                b = await context.Request.Body.ReadAsync(t, 0, 1024);
                if (b > 0)
                    requestContent = requestContent.Concat(t.Take(b));
            } while (b == 1024);

            data = requestContent.ToArray();
            context.Request.Body = new MemoryStream(data);

            await _next(context);

        }
    }
}
