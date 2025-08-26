
namespace SelfSignedCertificateGenerator
{

    using System; // for buffer.CopyTo, AsSpan 


    public abstract class NonBackdooredPrng
        : Org.BouncyCastle.Crypto.Prng.IRandomGenerator
    {
        public abstract void AddSeedMaterial(byte[] seed);
        public abstract void AddSeedMaterial(long seed);

        public abstract void NextBytes(System.Span<byte> bytes);

        public abstract void NextBytes(byte[] bytes);
        public abstract void NextBytes(byte[] bytes, int start, int len);

        public abstract void AddSeedMaterial(System.ReadOnlySpan<byte> seed);


        public static NonBackdooredPrng Create()
        {
            bool isWindows =
                System.Runtime.InteropServices.RuntimeInformation
                    .IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);

            if (isWindows)
                return new WindowsPrng();

            return new PosixPrng();
        } // End Function Create 


        public static Org.BouncyCastle.Security.SecureRandom SecureRandom
        {
            get
            {
                return new Org.BouncyCastle.Security.SecureRandom(
                    NonBackdooredPrng.Create()
                );
            } // End Getter 
        } // End Property SecureRandom 


    } // End Class NonBackdooredPrng 



    public class WindowsPrng
        : NonBackdooredPrng
    {
        protected readonly Org.BouncyCastle.Crypto.Prng.IRandomGenerator m_rnd;

        public WindowsPrng()
        {
            // this.m_rnd = new Org.BouncyCastle.Crypto.Prng.CryptoApiRandomGenerator();

            const string digestName = "SHA256";
            Org.BouncyCastle.Crypto.IDigest digest = Org.BouncyCastle.Security.DigestUtilities.GetDigest(digestName);

            if (digest == null)
                throw new System.InvalidOperationException($"Digest '{digestName}' not available.");

            Org.BouncyCastle.Crypto.Prng.DigestRandomGenerator prng =
                new Org.BouncyCastle.Crypto.Prng.DigestRandomGenerator(digest);

            const bool autoSeed = true;
            if (autoSeed)
            {
                // prng.AddSeedMaterial(NextCounterValue());
                // prng.AddSeedMaterial(GetNextBytes(Master, digest.GetDigestSize()));
            }

            this.m_rnd = prng;
        } // End Constructor 


        /// <summary>Add more seed material to the generator.</summary>
        /// <param name="seed">A byte array to be mixed into the generator's state.</param>
        public override void AddSeedMaterial(byte[] seed)
        {
            this.m_rnd.AddSeedMaterial(seed);
        } // End Sub AddSeedMaterial 


        public override void AddSeedMaterial(System.ReadOnlySpan<byte> seed)
        {
#if NETSTANDARD2_0_OR_GREATER
            byte[] buffer = seed.ToArray(); // Allocates on the heap
            this.m_rnd.AddSeedMaterial(buffer);
#else
            this.m_rnd.AddSeedMaterial(seed);
#endif
        } // End Sub AddSeedMaterial 



        /// <summary>Add more seed material to the generator.</summary>
        /// <param name="seed">A long value to be mixed into the generator's state.</param>
        public override void AddSeedMaterial(long seed)
        {
            this.m_rnd.AddSeedMaterial(seed);
        } // End Sub AddSeedMaterial 


        /// <summary>Fill byte array with random values.</summary>
        /// <param name="bytes">Array to be filled.</param>
        public override void NextBytes(byte[] bytes)
        {
            this.m_rnd.NextBytes(bytes);
        } // End Sub NextBytes 

        public override void NextBytes(System.Span<byte> bytes)
        {
#if NETSTANDARD2_0_OR_GREATER
            byte[] buffer = new byte[bytes.Length];
            this.m_rnd.NextBytes(buffer);
            buffer.CopyTo(bytes); // Copies from heap to span
#else
            this.m_rnd.NextBytes(bytes);
#endif 
        } // End Sub NextBytes 



        /// <summary>Fill byte array with random values.</summary>
        /// <param name="bytes">Array to receive bytes.</param>
        /// <param name="start">Index to start filling at.</param>
        /// <param name="len">Length of segment to fill.</param>
        public override void NextBytes(byte[] bytes, int start, int len)
        {
            this.m_rnd.NextBytes(bytes, start, len);
        } // End Sub NextBytes 

    } // End Class WindowsPrng


    public class PosixPrng
        : NonBackdooredPrng
    {
        // Early boot on a very low entropy device. 
        // The /dev/urandom device cannot guarantee 
        // that it has received enough initial entropy, 
        // while when using /dev/random that is guaranteed 
        // (even if it may block).
        // therefore, use /dev/urandom

        // /dev/random //  potentially blocking
        // /dev/urandom


        /// <summary>Add more seed material to the generator.</summary>
        /// <param name="seed">A byte array to be mixed into the generator's state.</param>
        public override void AddSeedMaterial(byte[] seed)
        {
            // throw new System.NotImplementedException();
            // Since your PosixPrng pulls randomness from /dev/urandom,
            // the concept of "adding seed material" is meaningless
            // in the sense of actually influencing the kernel PRNG.
            // The Linux kernel does not accept entropy injected into /dev/urandom from userland.
        } // End Sub AddSeedMaterial 

        public override void AddSeedMaterial(System.ReadOnlySpan<byte> seed)
        {
            // throw new System.NotImplementedException();
            // Since your PosixPrng pulls randomness from /dev/urandom,
            // the concept of "adding seed material" is meaningless
            // in the sense of actually influencing the kernel PRNG.
            // The Linux kernel does not accept entropy injected into /dev/urandom from userland.
        } // End Sub AddSeedMaterial 


        /// <summary>Add more seed material to the generator.</summary>
        /// <param name="seed">A long value to be mixed into the generator's state.</param>
        public override void AddSeedMaterial(long seed)
        {
            // throw new System.NotImplementedException();
            // Since your PosixPrng pulls randomness from /dev/urandom,
            // the concept of "adding seed material" is meaningless
            // in the sense of actually influencing the kernel PRNG.
            // The Linux kernel does not accept entropy injected into /dev/urandom from userland.
        } // End Sub AddSeedMaterial 


        /// <summary>Fill byte array with random values.</summary>
        /// <param name="bytes">Array to be filled.</param>
        public override void NextBytes(byte[] bytes)
        {
            using (System.IO.FileStream fs =
                new System.IO.FileStream(
                    "/dev/urandom"
                  , System.IO.FileMode.Open
                  , System.IO.FileAccess.Read
                  , System.IO.FileShare.Read
                  , bufferSize: 4096
                  , useAsync: false))
            {
                int totalRead = 0;
                while (totalRead < bytes.Length)
                {
                    int read = fs.Read(bytes, totalRead, bytes.Length - totalRead);
                    if (read <= 0)
                    {
                        throw new System.IO.EndOfStreamException(
                            $"Could not read the required {bytes.Length} bytes from /dev/urandom. Only got {totalRead}.");
                    }

                    totalRead += read;
                } // Whend 

            } // End Using fs 

        } // End Sub NextBytes 


        public override void NextBytes(System.Span<byte> bytes)
        {

#if NETSTANDARD2_0_OR_GREATER

            using (System.IO.FileStream fs = new System.IO.FileStream(
                "/dev/urandom",
                System.IO.FileMode.Open,
                System.IO.FileAccess.Read,
                System.IO.FileShare.Read,
                bufferSize: 4096,
                useAsync: false)
            )
            {
                int totalRead = 0;
                byte[] temp = new byte[4096]; // temporary buffer
                while (totalRead < bytes.Length)
                {
                    int toRead = System.Math.Min(temp.Length, bytes.Length - totalRead);
                    int read = fs.Read(temp, 0, toRead);
                    if (read <= 0)
                    {
                        throw new System.IO.EndOfStreamException(
                            $"Could not read the required {bytes.Length} bytes from /dev/urandom. Only got {totalRead}.");
                    }
                    temp.AsSpan(0, read).CopyTo(bytes.Slice(totalRead));
                    totalRead += read;
                }
            }

#else

            using (System.IO.FileStream fs =
                new System.IO.FileStream(
                    "/dev/urandom"
                  , System.IO.FileMode.Open
                  , System.IO.FileAccess.Read
                  , System.IO.FileShare.Read
                  , bufferSize: 4096
                  , useAsync: false))
            {
                int totalRead = 0;
                while (totalRead < bytes.Length)
                {
                    int read = fs.Read(bytes.Slice(totalRead));
                    if (read <= 0)
                    {
                        throw new System.IO.EndOfStreamException(
                            $"Could not read the required {bytes.Length} bytes from /dev/urandom. Only got {totalRead}.");
                    }
                    totalRead += read;
                } // Whend

            } // End Using fs 
#endif 
        } // End Sub NextBytes


        /// <summary>Fill byte array with random values.</summary>
        /// <param name="bytes">Array to receive bytes.</param>
        /// <param name="start">Index to start filling at.</param>
        /// <param name="len">Length of segment to fill.</param>
        public override void NextBytes(byte[] bytes, int start, int len)
        {

            using (System.IO.FileStream fs =
                new System.IO.FileStream(
                    "/dev/urandom"
                  , System.IO.FileMode.Open
                  , System.IO.FileAccess.Read
                  , System.IO.FileShare.Read
                  , bufferSize: 4096
                  , useAsync: false))
            {
                int totalRead = 0;
                while (totalRead < len)
                {
                    int read = fs.Read(bytes, start + totalRead, len - totalRead);
                    if (read <= 0)
                    {
                        throw new System.IO.EndOfStreamException(
                            $"Could not read the required {len} bytes from /dev/urandom. Only got {totalRead}.");
                    }
                    totalRead += read;
                } // Whend 

            } // End Using fs 
        } // End Sub NextBytes


    } // End Class LinuxPrng 


} // End Namespace 
