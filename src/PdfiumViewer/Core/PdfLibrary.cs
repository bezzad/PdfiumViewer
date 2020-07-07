using System;

namespace PdfiumViewer.Core
{
    internal class PdfLibrary : IDisposable
    {
        private static readonly object SyncRoot = new object();
        private static PdfLibrary _library;

        public static void EnsureLoaded()
        {
            lock (SyncRoot)
            {
                if (_library == null)
                    _library = new PdfLibrary();
            }
        }

        private bool _disposed;

        private PdfLibrary()
        {
            NativeMethods.FPDF_AddRef();
        }

        ~PdfLibrary()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                NativeMethods.FPDF_Release();

                _disposed = true;
            }
        }
    }
}
