using System;
using System.IO;
using System.Text;

namespace ProtoBufGenerator
{
    /// <summary>
    /// Handles a temporary file.
    /// </summary>
    public class TempFile : IDisposable
    {
        /// <summary>
        /// Path to the temporary file.
        /// </summary>
        private string path;

        /// <summary>
        /// Gets the temporary file path.
        /// </summary>
        public string Path
        {
            get
            {
                if (path == null)
                {
                    throw new ObjectDisposedException(GetType().Name);
                }
                return path;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TempFile"/> class.
        /// </summary>
        public TempFile() : this(System.IO.Path.GetTempFileName())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TempFile"/> class.
        /// </summary>
        /// <param name="path">A given filename.</param>
        public TempFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }
            this.path = path;
        }

        /// <summary>
        /// Disposes the temporary file.
        /// </summary>
        ~TempFile()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes the temporary file.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Disposes the temporary file.
        /// </summary>
        /// <param name="disposing">If we are disposing or not.</param>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                GC.SuppressFinalize(this);
            }
            if (path != null)
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                } // best effort
                path = null;
            }
        }

        /// <summary>
        /// Get the bytes from the file.
        /// </summary>
        /// <returns>The bytes from the file.</returns>
        public byte[] GetBytes()
        {
            StringBuilder output = new StringBuilder();

            using (var lineReader = File.OpenText(path))
            {
                string line;
                while ((line = lineReader.ReadLine()) != null)
                {
                    output.AppendLine(line);
                }
            }

            return Encoding.Default.GetBytes(output.ToString());
        }
    }
}


