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
            "baidu",
            "caiyun",
        };

        public static BKOCRBase CreateBKOCRClient(string ocrType)
        {
            if (ocrType == OCRType[0])
            {
                return new BKOCRBaidu();
            }
            else if (ocrType == OCRType[1])
            {
                return new BKOCRMicrosoft();
            }
            return new BKOCRBaidu();
        }

        public static BKTransBase CreateBKTransClient(string transType)
        {
            if (transType == TransType[0])
            {
                return new BKTransBaidu();
            }
            else if (transType == TransType[1])
            {
                return new BKTransCaiyun();
            }
            return new BKTransBaidu();
        }
    }
}
