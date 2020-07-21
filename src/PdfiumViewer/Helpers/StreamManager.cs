using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace PdfiumViewer.Helpers
{
    internal static class StreamManager
    {
        private static int _nextId;
        private static readonly ConcurrentDictionary<int, Stream> Files = new ConcurrentDictionary<int, Stream>();

        public static int Register(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var id = Interlocked.Increment(ref _nextId);
            Files.TryAdd(id, stream);
            return id;
        }

        public static void UnRegister(int id)
        {
            Files.TryRemove(id, out var stream);
            stream?.Dispose();
        }

        public static Stream Get(int id)
        {
            Files.TryGetValue(id, out var stream);
            return stream;
        }
    }
}
