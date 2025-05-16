namespace ReportServerProxy
{
    using System.IO;
    using System.IO.Compression;
    using System.Text;
    using System; // Needed for Exception

    public static class GzipHelper
    {
        /// <summary>
        /// Decompresses GZIP compressed bytes into a string using a specified encoding.
        /// </summary>
        /// <param name="gzipBytes">The byte array containing GZIP compressed data.</param>
        /// <param name="encoding">The encoding to use for the decompressed text (e.g., Encoding.UTF8).</param>
        /// <returns>The decompressed string, or an error message if decompression fails.</returns>
        public static string DecompressGzipBytesToString(byte[] gzipBytes, Encoding encoding)
        {
            if (gzipBytes == null || gzipBytes.Length == 0)
            {
                return string.Empty;
            }

            // Use UTF8 as a common default if encoding is null
            Encoding textEncoding = encoding ?? Encoding.UTF8;

            try
            {
                // 1. Wrap the compressed byte array in a MemoryStream
                using (MemoryStream compressedStream = new MemoryStream(gzipBytes))
                {
                    // 2. Create a GZipStream in Decompress mode, wrapping the MemoryStream
                    using (GZipStream gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                    {
                        // 3. Use a StreamReader to read the decompressed bytes as text
                        // Pass the specified encoding
                        using (StreamReader reader = new StreamReader(gzipStream, textEncoding))
                        {
                            // 4. Read the entire stream to get the decompressed string
                            return reader.ReadToEnd();
                        } // StreamReader is automatically disposed
                    } // GZipStream is automatically disposed
                } // MemoryStream is automatically disposed
            }
            catch (InvalidDataException ex)
            {
                // This exception is thrown if the data is not in a valid GZIP format
                System.Diagnostics.Debug.WriteLine($"Error: Data does not appear to be valid GZIP. {ex.Message}");
                return $"Decompression Error: Invalid GZIP data - {ex.Message}";
            }
            catch (Exception ex)
            {
                // Catch other potential exceptions during decompression
                System.Diagnostics.Debug.WriteLine($"An unexpected error occurred during decompression: {ex.Message}");
                return $"Decompression Error: Unexpected error - {ex.Message}";
            }
        }
    }
}
