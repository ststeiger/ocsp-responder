
namespace ReportServerProxy
{


    public static class GzipHelper
    {

        public static async System.Threading.Tasks.Task<byte[]> Compress(
            string input,
            System.Text.Encoding enc
        )
        {
            // 3. Recompress the modified content
            byte[] compressedBytes;
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                using (System.IO.Compression.GZipStream gzip = new System.IO.Compression.GZipStream(ms, 
                    System.IO.Compression.CompressionMode.Compress, 
                    true)
                )
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(gzip, enc))
                {
                    await writer.WriteAsync(input);
                    await writer.WriteAsync(input);
                }
                compressedBytes = ms.ToArray();
            }

            return compressedBytes;
        }




        /// <summary>
        /// Decompresses GZIP compressed bytes into a string using a specified encoding.
        /// </summary>
        /// <param name="gzipBytes">The byte array containing GZIP compressed data.</param>
        /// <param name="encoding">The encoding to use for the decompressed text (e.g., Encoding.UTF8).</param>
        /// <returns>The decompressed string, or an error message if decompression fails.</returns>
        public static string DecompressGzipBytesToString(byte[] gzipBytes, System.Text.Encoding encoding)
        {
            if (gzipBytes == null || gzipBytes.Length == 0)
            {
                return string.Empty;
            }

            // Use UTF8 as a common default if encoding is null
            System.Text.Encoding textEncoding = encoding ?? System.Text.Encoding.UTF8;

            try
            {
                // 1. Wrap the compressed byte array in a MemoryStream
                using (System.IO.MemoryStream compressedStream = new System.IO.MemoryStream(gzipBytes))
                {
                    // 2. Create a GZipStream in Decompress mode, wrapping the MemoryStream
                    using (System.IO.Compression.GZipStream gzipStream = 
                        new System.IO.Compression.GZipStream(compressedStream,
                        System.IO.Compression.CompressionMode.Decompress)
                    )
                    {
                        // 3. Use a StreamReader to read the decompressed bytes as text
                        // Pass the specified encoding
                        using (System.IO.StreamReader reader = 
                            new System.IO.StreamReader(gzipStream, textEncoding)
                        )
                        {
                            // 4. Read the entire stream to get the decompressed string
                            return reader.ReadToEnd();
                        } // StreamReader is automatically disposed
                    } // GZipStream is automatically disposed
                } // MemoryStream is automatically disposed
            }
            catch (System.IO.InvalidDataException ex)
            {
                // This exception is thrown if the data is not in a valid GZIP format
                System.Diagnostics.Debug.WriteLine($"Error: Data does not appear to be valid GZIP. {ex.Message}");
                return $"Decompression Error: Invalid GZIP data - {ex.Message}";
            }
            catch (System.Exception ex)
            {
                // Catch other potential exceptions during decompression
                System.Diagnostics.Debug.WriteLine($"An unexpected error occurred during decompression: {ex.Message}");
                return $"Decompression Error: Unexpected error - {ex.Message}";
            }
        }

    } // End Class GzipHelper 


}
