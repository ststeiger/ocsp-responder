// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace OCSPGuardian.ProxyProtocol
{


    // TODO: Use a proper parser: by Nazar Mishturak
    // https://github.com/nazar554/Scintillating.ProxyProtocol.Parser
    public static class ProxyProtocol
    {


        // The proxy protocol marker.
        private static System.ReadOnlySpan<byte> Preamble => new byte[] { 0x0D, 0x0A, 0x0D, 0x0A, 0x00, 0x0D, 0x0A, 0x51, 0x55, 0x49, 0x54, 0x0A };

        /// <summary>
        /// Proxy Protocol v2: https://www.haproxy.org/download/1.8/doc/proxy-protocol.txt Section 2.2
        /// Preamble(12 bytes) : 0D-0A-0D-0A-00-0D-0A-51-55-49-54-0A
        ///  -21                        Version + stream    12
        ///  -11                        TCP over IPv4       13
        ///  -00-14                     length              14
        ///  -AC-1C-00-04               src address         16
        ///  -01-02-03-04               dest address        20
        ///  -D7-9A                     src port            24
        ///  -13-88                     dest port           26
        ///  -EE                        PP2_TYPE_AZURE      28
        ///  -00-05                     length              29
        ///  -01                        LINKID type         31
        ///  -33-00-00-26               LINKID              32.
        /// </summary>
        public static async System.Threading.Tasks.Task ProcessAsync(
            Microsoft.AspNetCore.Connections.ConnectionContext connectionContext, 
            System.Func<System.Threading.Tasks.Task> next,
            Microsoft.Extensions.Logging.ILogger? logger = null
        )
        {
            System.IO.Pipelines.PipeReader input = connectionContext.Transport.Input;
            // Count how many bytes we've examined so we never go backwards, Pipes don't allow that.
            long minBytesExamined = 0L;
            while (true)
            {
                System.IO.Pipelines.ReadResult result = await input.ReadAsync();
                System.Buffers.ReadOnlySequence<byte> buffer = result.Buffer;
                System.SequencePosition examined = buffer.Start;

                try
                {
                    if (result.IsCompleted)
                    {
                        return;
                    }

                    if (buffer.Length == 0)
                    {
                        continue;
                    }

                    if (buffer.Length < Preamble.Length) // 12
                    {
                        // Buffer does not have enough data to make decision.
                        // Check for a partial match.
                        byte[] partial = System.Buffers.BuffersExtensions.ToArray(buffer);
                        if (!System.MemoryExtensions.StartsWith(Preamble, partial))
                        {
                            break;
                        }
                        minBytesExamined = buffer.Length;
                        examined = buffer.End;
                        continue;
                    }

                    byte[] bufferArray = NewMethod(buffer);

                    // if (!bufferArray.AsSpan().StartsWith(Preamble))
                    if (!System.MemoryExtensions.StartsWith(System.MemoryExtensions.AsSpan(bufferArray), Preamble))
                    {
                        // Break if it is not PPv2.
                        break;
                    }

                    if (HasEnoughPpv2Data(bufferArray))
                    {
                        // It is PPv2
                        ExtractPpv2Data(ref buffer, bufferArray, connectionContext, logger);
                        // We've consumed and sliced off the prefix.
                        minBytesExamined = 0; // Reset, we sliced off the examined bytes.
                        examined = buffer.Start;
                        break;
                    }

                    // It is PPv2, and we don't have enough data for PPv2
                    minBytesExamined = buffer.Length;
                    examined = buffer.End;
                }
                finally
                {
                    if (buffer.Slice(buffer.Start, examined).Length < minBytesExamined)
                    {
                        examined = buffer.Slice(buffer.Start, minBytesExamined).End;
                    }
                    input.AdvanceTo(buffer.Start, examined);
                }
            } // Whend 

            await next();
        } // End Task ProcessAsync 


        private static byte[] NewMethod(System.Buffers.ReadOnlySequence<byte> buffer)
        {
            return System.Buffers.BuffersExtensions.ToArray(buffer);
        } // End Function NewMethod 


        private static void ExtractPpv2Data(
            ref System.Buffers.ReadOnlySequence<byte> buffer, 
            byte[] bufferArray, 
            Microsoft.AspNetCore.Connections.ConnectionContext context,
            Microsoft.Extensions.Logging.ILogger? logger = null
        )
        {
            // Probe traffic does not have valid ppv2 data.
            try
            {
                short length = (short)(bufferArray[15] | (bufferArray[14] << 8));
                byte[] srcIpAddressArray = new byte[4];
                System.Array.Copy(bufferArray, 16, srcIpAddressArray, 0, 4);
                System.Net.IPAddress srcAddress = new System.Net.IPAddress(srcIpAddressArray);

                byte[] destIpAddressArray = new byte[4];
                System.Array.Copy(bufferArray, 20, destIpAddressArray, 0, 4);
                System.Net.IPAddress destAddress = new System.Net.IPAddress(destIpAddressArray);

                int srcPort = (int)(bufferArray[25] | (bufferArray[24] << 8));
                int destPort = (int)(bufferArray[27] | (bufferArray[26] << 8));

                ProxyProtocolFeature feature = new ProxyProtocolFeature()
                {
                    SourceIp = srcAddress,
                    DestinationIp = destAddress,
                    SourcePort = srcPort,
                    DestinationPort = destPort,
                };

                // Probe traffic does not have link ids.
                if (length > 12)
                {
                    long linkId = (long)(bufferArray[32] | (bufferArray[33] << 8) | (bufferArray[34] << 16) |
                                         (bufferArray[35] << 24));

                    feature.LinkId = linkId;
                }

                // Trim the buffer so the HTTP parser can pick up from there.
                buffer = buffer.Slice(length + 16);

                context.Features.Set<IProxyProtocolFeature>(feature);
            } // End Try 
            catch
            {
                if(logger != null)
                    Microsoft.Extensions.Logging.LoggerExtensions.LogDebug(logger, $"ExtractPpv2Data error. BufferArray: {System.BitConverter.ToString(bufferArray)}");

                throw;
            }
        } // End Sub ExtractPpv2Data 


        private static bool HasEnoughPpv2Data(System.Collections.Generic.IReadOnlyList<byte> bufferArray)
        {
            if (bufferArray.Count < 16)
                return false;

            short length = (short)(bufferArray[15] | (bufferArray[14] << 8));
            return bufferArray.Count >= 16 + length;
        } // End Function HasEnoughPpv2Data 


    } // End static class ProxyProtocol 


} // End Namespace 
