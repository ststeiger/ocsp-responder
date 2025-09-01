
namespace TestHttpsProxy
{


    public static class CustomBufferedStreamExtensions
    {


        /// <summary>
        /// Peek the first <paramref name="count"/> bytes without consuming them.
        /// Returns fewer if the stream ends early.
        /// </summary>
        public static async System.Threading.Tasks.Task<byte[]> PeekBytesAsync(
            this StreamExtended.Network.CustomBufferedStream stream,
            int count,
            System.Threading.CancellationToken cancel = default
        )
        {
            byte[] buffer = new byte[count];
            int i = 0;

            for (; i < count; i++)
            {
                int b = await stream.PeekByteAsync(i, cancel).ConfigureAwait(false);
                if (b == -1)
                {
                    break; // end of stream
                }
                buffer[i] = (byte)b;
            }

            if (i < count)
            {
                // Trim to actual length if less than requested was available
                System.Array.Resize(ref buffer, i);
            }

            return buffer;
        } // End Task PeekBytesAsync 


    } // End Class CustomBufferedStreamExtensions 


} // End Namespace 
