using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKTrans.Misc
{
    public class BKTransMap
    {
        public readonly static List<string> LangType = new()
        {
            "zh-cn",
            "ja",
            "en-us",
            "ko",
            "fr",
            "de",
            "ru",
            "es",
            "pt",
            "it",
        };

        public readonly static List<string> OCRType = new()
        {
            "baidu",
            "microsoft",
        };

        public readonly static List<string> TransType = new()
        {
            "google",
            "baidu",
            "caiyun",
        };

        public static BKOCRBase CreateBKOCRClient(string ocrType)
        {
            if (ocrType == "baidu")
            {
                return new BKOCRBaidu();
            }
            else if (ocrType == "microsoft")
            {
                return new BKOCRMicrosoft();
            }
            return new BKOCRBaidu();
        }

        public static BKTransBase CreateBKTransClient(string transType)
        {
            if (transType == "baidu")
            {
                return new BKTransBaidu();
            }
            else if (transType == "caiyun")
            {
                return new BKTransCaiyun();
            }
            else if (transType == "google")
            {
                return new BKTransGoogle();
            }
            return new BKTransBaidu();
        }
    }
}
