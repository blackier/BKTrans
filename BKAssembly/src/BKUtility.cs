using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BKAssembly
{
    public class BKUtility
    {
        private static HttpClient mHttpClient = null;
        private static readonly object mHttpClientLock = new object();

        public static string Bitmap2Base64String(Bitmap bmp)
        {
            MemoryStream buffStream = new MemoryStream();
            bmp.Save(buffStream, ImageFormat.Bmp);
            byte[] buffArr = new byte[buffStream.Length];
            buffStream.Position = 0;
            buffStream.Read(buffArr, 0, (int)buffStream.Length);
            buffStream.Close();
            return Convert.ToBase64String(buffArr);
        }

        public static string CalculMD5(string srcStr)
        {
            MD5 md5 = MD5.Create();

            byte[] byteSrc = Encoding.UTF8.GetBytes(srcStr);
            byte[] byteTarget = md5.ComputeHash(byteSrc);

            StringBuilder srcBuilder = new StringBuilder();
            foreach (byte b in byteTarget)
            {
                srcBuilder.Append(b.ToString("x2"));
            }

            return srcBuilder.ToString();
        }

        public static HttpClient GetHttpClient(WebProxy httpProxy = null)
        {
            lock (mHttpClientLock)
            {
                if (mHttpClient == null)
                {
                    if (httpProxy != null)
                    {
                        var hanlder = new HttpClientHandler()
                        {
                            Proxy = httpProxy,
                            UseProxy = true
                        };
                        mHttpClient = new(hanlder);
                    }
                    else
                    {
                        mHttpClient = new();
                    }
                }
            }
            return mHttpClient;
        }

        public static string JsonSerialize<T>(T obj, bool ignoreNullValues = true, bool writeIndented = true)
        {
            JsonSerializerOptions options = new()
            {
                IgnoreNullValues = ignoreNullValues,
                WriteIndented = writeIndented
            };
            return JsonSerializer.Serialize<T>(obj, options);
        }

        public static T JsonDeserialize<T>(string str)
        {
            return JsonSerializer.Deserialize<T>(str);
        }

        public static string LoadTextFile(string filePath)
        {
            string retStr = "";
            if (File.Exists(filePath))
            {
                retStr = File.ReadAllText(filePath);
            }
            return retStr;
        }

        public static bool SaveTextFile(string filePath, string text)
        {
            bool retBool = true;
            try
            {
                File.WriteAllText(filePath, text);
            }
            catch
            {
                retBool = false;
            }
            return retBool;
        }
    }
}
