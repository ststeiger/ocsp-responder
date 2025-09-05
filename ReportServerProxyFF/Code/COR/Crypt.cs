
namespace ReportServerProxyFF
{


    internal static class AES
    {
        private static string s_key = "1b55ec1d96f637aa7b73c31765a12c2c8fb8b9f6ae8b14396475a20ed1a83dac";
        private static string s_IV = "d4e3381cdd39ddb70f85e96d11b667e5";


        public static string GetKey()
        {
            return s_key;
        } // End Sub GetKey


        public static string GetIV()
        {
            return s_IV;
        } // End Sub GetIV


        public static void SetKey(string inputKey)
        {
            s_key = inputKey;
        } // End Sub SetKey 


        public static void SetIV(string inputIV)
        {
            s_IV = inputIV;
        } // End Sub SetIV


        public static string GenerateKey()
        {
            string retValue = null;

            using (System.Security.Cryptography.Aes aes = System.Security.Cryptography.Aes.Create())
            {
                aes.GenerateKey();
                aes.GenerateIV();

                byte[] iv = aes.IV;
                byte[] key = aes.Key;
                aes.Clear();

                retValue = "IV: " + ByteArrayToHexString(iv)
                    + System.Environment.NewLine
                    + "Key: " + ByteArrayToHexString(key); ;
            } // End Using aes

            return retValue;
        } // End Function GenerateKey



        public static string Encrypt(string strPlainText)
        {
            string retValue = null;
            System.Text.Encoding enc = System.Text.Encoding.UTF8;

            using (System.Security.Cryptography.Aes aes =
               System.Security.Cryptography.Aes.Create())
            {
                byte[] cipherTextBuffer = null;
                byte[] plainTextBuffer = null;
                byte[] encryptionKey = null;
                byte[] initializationVector = null;

                // Create a new key and initialization vector.
                // aes.GenerateKey()
                // aes.GenerateIV()
                aes.Key = HexStringToByteArray(s_key);
                aes.IV = HexStringToByteArray(s_IV);


                //Get the key and initialization vector.
                encryptionKey = aes.Key;
                initializationVector = aes.IV;
                // kev = ByteArrayToHexString(encryptionKey); 
                // iv = ByteArrayToHexString(initializationVector); 

                //Get an encryptor.
                using (System.Security.Cryptography.ICryptoTransform encryptor = aes.CreateEncryptor(encryptionKey, initializationVector))
                {
                    // Encrypt the data.
                    using (System.IO.MemoryStream msEncrypt = new System.IO.MemoryStream())
                    {
                        using (System.Security.Cryptography.CryptoStream csEncrypt = new System.Security.Cryptography.CryptoStream(msEncrypt, encryptor, System.Security.Cryptography.CryptoStreamMode.Write))
                        {

                            // Convert the data to a byte array.
                            plainTextBuffer = enc.GetBytes(strPlainText);

                            // Write all data to the crypto stream and flush it.
                            csEncrypt.Write(plainTextBuffer, 0, plainTextBuffer.Length);
                            csEncrypt.FlushFinalBlock();

                            //Get encrypted array of bytes.
                            cipherTextBuffer = msEncrypt.ToArray();
                        } // End Using csEncrypt 

                    } // End Using msEncrypt

                } // End Using encryptor 

                retValue = ByteArrayToHexString(cipherTextBuffer);
            } // End Using aes 

            return retValue;
        } // End Function Encrypt


        public static string DeCrypt(string cypherText)
        {
            string retValue = null;

            if (string.IsNullOrEmpty(cypherText))
            {
                throw new System.ArgumentNullException(nameof(cypherText), nameof(cypherText) + " may not be string.Empty or NULL, because these are invid values.");
            }

            // System.Text.Encoding enc = System.Text.Encoding.ASCII;
            System.Text.Encoding enc = System.Text.Encoding.UTF8;

            using (System.Security.Cryptography.Aes aes =
                System.Security.Cryptography.Aes.Create())
            {
                byte[] cipherTextBuffer = HexStringToByteArray(cypherText);
                byte[] decryptionKey = HexStringToByteArray(s_key);
                byte[] initializationVector = HexStringToByteArray(s_IV);

                // This is where the message would be transmitted to a recipient
                // who already knows your secret key. Optionally, you can
                // also encrypt your secret key using a public key algorithm
                // and pass it to the mesage recipient along with the RijnDael
                // encrypted message.            
                //Get a decryptor that uses the same key and IV as the encryptor.
                using (System.Security.Cryptography.ICryptoTransform decryptor = aes.CreateDecryptor(decryptionKey, initializationVector))
                {
                    //Now decrypt the previously encrypted message using the decryptor
                    // obtained in the above step.
                    using (System.IO.MemoryStream msDecrypt = new System.IO.MemoryStream(cipherTextBuffer))
                    {
                        using (System.Security.Cryptography.CryptoStream csDecrypt = new System.Security.Cryptography.CryptoStream(msDecrypt, decryptor, System.Security.Cryptography.CryptoStreamMode.Read))
                        {
                            byte[] plainTextBuffer = new byte[cipherTextBuffer.Length];

                            //Read the data out of the crypto stream.
                            csDecrypt.Read(plainTextBuffer, 0, plainTextBuffer.Length);

                            //Convert the byte array back into a string.
                            retValue = enc.GetString(plainTextBuffer);
                        } // End Using csDecrypt 

                    } // End Using msDecrypt 

                    if (!string.IsNullOrEmpty(retValue))
                        retValue = retValue.Trim('\0');
                } // End Using decryptor 

            } // End Using aes 

            return retValue;
        } // End Function DeCrypt


        // Convert a byte array into a hex string
        public static string ByteArrayToHexString(byte[] arrInput)
        {
            System.Text.StringBuilder output = new System.Text.StringBuilder(arrInput.Length);

            for (int i = 0; i <= arrInput.Length - 1; i++)
            {
                output.Append(arrInput[i].ToString("X2"));
            }

            return output.ToString().ToLower();
        } // End Function ByteArrayToHexString


        // Convert a hex string into a byte array 
        public static byte[] HexStringToByteArray(string strHexString)
        {
            int numChars = strHexString.Length;
            byte[] buffer = new byte[numChars / 2];
            for (int i = 0; i <= numChars - 1; i += 2)
            {
                buffer[i / 2] = System.Convert.ToByte(strHexString.Substring(i, 2), 16);
            }

            return buffer;
        } // End Function HexStringToByteArray


    } // End Class AES 


    public static class DES
    {

        // Any text can be used as symmetric key 
        private static string s_symmetricKey = "z67f3GHhdga78g3gZUIT(6/&ns289hsB_5Tzu6";


        // http://www.codeproject.com/KB/aspnet/ASPNET_20_Webconfig.aspx
        // http://www.codeproject.com/KB/database/Connection_Strings.aspx
        public static string DeCrypt(string SourceText)
        {
            string retValue = "";

            if (string.IsNullOrEmpty(SourceText))
            {
                return retValue;
            } // End if (string.IsNullOrEmpty(SourceText)) 


            using (System.Security.Cryptography.TripleDES des3 = System.Security.Cryptography.TripleDES.Create())
            {

                using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                {
                    des3.Key = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s_symmetricKey));
                    des3.Mode = System.Security.Cryptography.CipherMode.ECB;

                    using (System.Security.Cryptography.ICryptoTransform decryptor = des3.CreateDecryptor())
                    {
                        byte[] buff = System.Convert.FromBase64String(SourceText);
                        retValue = System.Text.Encoding.UTF8.GetString(decryptor.TransformFinalBlock(buff, 0, buff.Length));
                    } // End Using decryptor 

                } // End Using md5 

            } // End Using des3 

            return retValue;
        } // End Function DeCrypt


        public static string Crypt(string SourceText)
        {
            string retValue = "";

            using (System.Security.Cryptography.TripleDES des3 = System.Security.Cryptography.TripleDES.Create())
            {

                using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                {
                    des3.Key = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(s_symmetricKey));
                    des3.Mode = System.Security.Cryptography.CipherMode.ECB;
                    System.Security.Cryptography.ICryptoTransform desdencrypt = des3.CreateEncryptor();
                    byte[] buff = System.Text.Encoding.UTF8.GetBytes(SourceText);

                    retValue = System.Convert.ToBase64String(desdencrypt.TransformFinalBlock(buff, 0, buff.Length));
                } // End Using md5

            } // End Using des3 

            return retValue;
        } // End Function Crypt


        public static string GenerateKey()
        {
            string retVal = null;

            using (System.Security.Cryptography.TripleDES des3 =
                  System.Security.Cryptography.TripleDES.Create())
            {
                des3.GenerateKey();
                des3.GenerateIV();
                byte[] iv = des3.IV;
                byte[] key = des3.Key;

                retVal = "IV: " + AES.ByteArrayToHexString(iv)
                    + System.Environment.NewLine
                    + "Key: " + AES.ByteArrayToHexString(key);
            } // End Using des3 

            return retVal;
        } // End Function GenerateKey


        public static string GenerateHash(string SourceText)
        {
            string retValue = "";
            byte[] ByteSourceText = System.Text.Encoding.UTF8.GetBytes(SourceText);

            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] byteHash = md5.ComputeHash(ByteSourceText);
                retValue = System.Convert.ToBase64String(byteHash);
                byteHash = null;
            } // End Using md5 

            return retValue;
        } // End Function GenerateHash


    } // End Class DES


} // End Namespace COR.Tools.Cryptography
