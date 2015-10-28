using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Security.Cryptography;
using System.IO;
using System;
namespace eWallet.Common
{
    public class Security
    {
        /// <summary>
        /// Tên hàm     : CreateMD5Hash
        /// </summary>
        /// <param name="input">chuỗi cần mã hóa</param>
        /// <returns>kết quả mã hóa</returns>
        public static string GenMd5Hash(string input)
        {
            // Use input string to calculate MD5 hash
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            // Convert the byte array to hexadecimal string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }
            return sb.ToString();
        }

        public static string GenMd5HashGNC(string input)
        {
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] outputBytes = md5.ComputeHash(inputBytes);
            md5 = null;
            return (BitConverter.ToString(outputBytes).Replace("-", string.Empty));
        }
        public static string GenPasswordHash(string user_id, string clear_pass)
        {
            return GenMd5Hash(String.Join("|", user_id, clear_pass));
        }

        private string Encrypt(string key, string data)
        {
            try
            {
                data = data.Trim();
                byte[] keydata = Encoding.UTF8.GetBytes(key);
                string md5String = BitConverter.ToString(new
                MD5CryptoServiceProvider().ComputeHash(keydata)).Replace("-", "").ToLower();
                byte[] tripleDesKey = Encoding.UTF8.GetBytes(md5String.Substring(0, 24));
                TripleDES tripdes = TripleDESCryptoServiceProvider.Create();
                tripdes.Mode = CipherMode.ECB;
                tripdes.Key = tripleDesKey;
                tripdes.GenerateIV();
                MemoryStream ms = new MemoryStream();
                CryptoStream encStream = new CryptoStream(ms, tripdes.CreateEncryptor(),
                CryptoStreamMode.Write);
                encStream.Write(Encoding.UTF8.GetBytes(data), 0, Encoding.UTF8.GetByteCount(data));
                encStream.FlushFinalBlock();
                byte[] cryptoByte = ms.ToArray();
                ms.Close();
                encStream.Close();
                return Convert.ToBase64String(cryptoByte, 0, cryptoByte.GetLength(0)).Trim();
            }
            catch
            {
                return "";
            }

        }
        private string Decrypt(string key, string data)
        {
            try
            {
                byte[] keydata = Encoding.UTF8.GetBytes(key);
                string md5String = BitConverter.ToString(new
                MD5CryptoServiceProvider().ComputeHash(keydata)).Replace("-", "").ToLower();
                byte[] tripleDesKey = Encoding.UTF8.GetBytes(md5String.Substring(0, 24));
                TripleDES tripdes = TripleDESCryptoServiceProvider.Create();
                tripdes.Mode = CipherMode.ECB;
                tripdes.Key = tripleDesKey;
                byte[] cryptByte = Convert.FromBase64String(data);
                MemoryStream ms = new MemoryStream(cryptByte, 0, cryptByte.Length);
                ICryptoTransform cryptoTransform = tripdes.CreateDecryptor();
                CryptoStream decStream = new CryptoStream(ms, cryptoTransform,
                CryptoStreamMode.Read);
                StreamReader read = new StreamReader(decStream);
                return (read.ReadToEnd());
            }
            catch
            {
                return "";
            }
        }

    }
}
