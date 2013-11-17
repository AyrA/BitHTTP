using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace Server
{
    public static class Crypt
    {
        private static readonly byte[] SALT = new byte[] { 0x00, 0x7a, 0xee, 0xaf, 0x4d, 0x08, 0x22, 0x3c, 0xc5, 0x26, 0xdc, 0xff, 0xad, 0xed, 0xfe, 0x07 };

        private static byte[] getSizedByte(string source, int destinationLength)
        {
            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(source, SALT);
            return pdb.GetBytes(destinationLength);
        }

        ///<summary>
        /// Encrypts a file using Rijndael algorithm.
        ///</summary>
        ///<param name="input">unencrypted input data</param>
        ///<param name="password">password for encryption</param>
        public static byte[] Encrypt(byte[] input, string password)
        {
            RijndaelManaged RMCrypto = new RijndaelManaged();
            CryptoStream cs = null;
            MemoryStream fsCrypt = new MemoryStream();
            try
            {
                cs = new CryptoStream(fsCrypt, RMCrypto.CreateEncryptor(getSizedByte(password, 32), getSizedByte(password, 16)), CryptoStreamMode.Write);
            }
            catch
            {
                if (fsCrypt != null)
                {
                    fsCrypt.Close();
                    fsCrypt.Dispose();
                }
                RMCrypto.Clear();
                throw new Exception("Error creating encryptors");
            }
            cs.Write(input, 0, input.Length);
            cs.Close();
            byte[] bb = fsCrypt.ToArray();
            fsCrypt.Close();
            return bb;
        }

        ///<summary>
        /// Decrypts a file using Rijndael algorithm.
        ///</summary>
        ///<param name="input">encrypted input data</param>
        ///<param name="password">password for decryption</param>
        public static byte[] Decrypt(byte[] input,string password)
        {
            RijndaelManaged RMCrypto = new RijndaelManaged();
            CryptoStream cs = null;
            MemoryStream fsIn = new MemoryStream(input,false);

            try
            {
                cs = new CryptoStream(fsIn, RMCrypto.CreateDecryptor(getSizedByte(password, 32), getSizedByte(password, 16)), CryptoStreamMode.Read);
            }
            catch
            {
                if (fsIn != null)
                {
                    fsIn.Close();
                    fsIn.Dispose();
                }
            }

            byte[] output = new byte[input.Length];

            try
            {
                cs.Read(output, 0, output.Length);
            }
            catch
            {
                cs=null;
                fsIn.Close();
                fsIn.Dispose();
                return null;
            }
            cs.Close();
            cs.Dispose();
            fsIn.Close();
            fsIn.Dispose();
            return output;
        }
    }
}
