using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BKAssembly
{
    public class BKMisc
    {
        public static string Bitmap2Base64String(Bitmap bmp)
        {
            MemoryStream buffStream = new MemoryStream();
            bmp.Save(buffStream, ImageFormat.Bmp);
            return Convert.ToBase64String(buffStream.ToArray());
        }

        public static Bitmap BitmapResize(Bitmap bmp, int width, int height)
        {
            Bitmap img = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(img);
            g.Clear(Color.White);
            g.DrawImage(bmp, 0, 0, width, height);

            return img;
        }

        public static string BitmapDHash(Bitmap bmp)
        {
            string dhash = "";
            for (int i = 0; i < bmp.Height; i++)
            {
                for (int j = 0; j < bmp.Width - 1; j++)
                {
                    var l = bmp.GetPixel(j, i);
                    var r = bmp.GetPixel(j + 1, i);
                    if ((l.R + l.G + l.B) > (r.R + r.G + r.B))
                        dhash += '1';
                    else
                        dhash += '0';
                }
            }
            return dhash;
        }

        /// <summary>
        /// bmp1/bmp2
        /// </summary>
        /// <param name="bmp1"></param>
        /// <param name="bmp2"></param>
        /// <returns></returns>
        public static float BitmapDHashCompare(Bitmap bmp11, Bitmap bmp22)
        {
            if (bmp11 == null || bmp22 == null)
                return 0;

            if (bmp11.Size != bmp22.Size)
                return 0;

            string p1_dhash = BitmapDHash(BitmapResize(bmp11, 9, 8));
            string p2_dhash = BitmapDHash(BitmapResize(bmp22, 9, 8));

            float same_count = 0;
            for (int i = 0; i < p1_dhash.Length; i++)
            {
                if (p1_dhash[i] == p2_dhash[i])
                    same_count++;
            }
            return same_count / p1_dhash.Length;
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

        public static string JsonSerialize<T>(T obj, bool ignoreNullValues = true, bool writeIndented = true, JavaScriptEncoder charsetEncoder = null)
        {
            JsonSerializerOptions options = new()
            {
                DefaultIgnoreCondition = ignoreNullValues ? JsonIgnoreCondition.WhenWritingDefault : JsonIgnoreCondition.Never,
                WriteIndented = writeIndented,
                Encoder = charsetEncoder
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
