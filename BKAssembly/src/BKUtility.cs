using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace BKAssembly
{
    public class BKUtility
    {
        #region 成员变量定义

        private static HttpClient http_client_ = new HttpClient();
        private static readonly object http_client_lock_ = new object();

        #endregion 成员变量定义

        #region 公有成员函数定义

        public static string Bitmap2Base64String(Bitmap bmp)
        {
            MemoryStream buff_stream = new MemoryStream();
            bmp.Save(buff_stream, ImageFormat.Bmp);
            byte[] buff_arr = new byte[buff_stream.Length];
            buff_stream.Position = 0;
            buff_stream.Read(buff_arr, 0, (int)buff_stream.Length);
            buff_stream.Close();
            return Convert.ToBase64String(buff_arr);
        }

        public static string CalculMD5(string src_str)
        {
            MD5 md5 = MD5.Create();

            byte[] byte_src = Encoding.UTF8.GetBytes(src_str);
            byte[] byte_target = md5.ComputeHash(byte_src);

            StringBuilder src_builder = new StringBuilder();
            foreach (byte b in byte_target)
            {
                src_builder.Append(b.ToString("x2"));
            }

            return src_builder.ToString();
        }

        public static HttpClient GetHttpClient()
        {
            lock (http_client_lock_)
            {
                if (http_client_ == null)
                {
                    //var handler = new HttpClientHandler();
                    //handler.Proxy = new WebProxy("https://127.0.0.1/",8888);
                    http_client_ = new HttpClient(/*handler*/);
                }
            }
            return http_client_;
        }

        #endregion 公有成员函数定义
    }
}
