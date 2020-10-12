using System.IO;

namespace PCRAgent
{

    public static class Extensions
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
    }
}
