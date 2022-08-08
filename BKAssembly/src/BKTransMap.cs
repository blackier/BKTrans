using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKAssembly
{
    public class BKTransMap
    {
        public static readonly Dictionary<string, string> OCRBaiduLanguageType = new()
        {
            {"CHN_ENG", "中英文"},
            {"JAP", "日语"},
            {"ENG", "英语"},
            {"KOR", "韩语"},
            {"FRE", "法语"},
            {"GER", "德语"},
            {"RUS", "俄语"},
            {"SPA", "西班牙语"},
            {"POR", "葡萄牙语"},
            {"ITA", "意大利语"},
        };

        public static readonly Dictionary<string, string> TransBaiduLanguageType = new()
        {
            {"zh", "中文"},
            {"jp", "日语"},
            {"en", "英语"},
            {"kor","韩语"},
            {"fra", "法语"},
            {"de", "德语"},
            {"ru", "俄语"},
            {"spa", "西班牙语"},
            {"pt", "葡萄牙语"},
            {"it", "意大利语"},
        };

        public static readonly Dictionary<string, string> TransCaiyunLanguageType = new()
        {
            {"zh", "中文"},
            {"ja", "日语"},
            {"en", "英语"},
        };

        public static Dictionary<string, string> TransMapBaidu2Baidu = new() {
            {"CHN_ENG", "zh"},
            {"JAP", "jp"},
            {"ENG", "en"},
            {"KOR", "kor"},
            {"FRE", "fra"},
            {"GER", "de"},
            {"RUS", "ru"},
            {"SPA", "spa"},
            {"POR", "pt"},
            {"ITA", "it"},
        };

        public static Dictionary<string, string> TransMapBaidu2Caiyun = new() {
            {"CHN_ENG", "zh"},
            {"JAP", "ja"},
            {"ENG", "en"},
        };

        public static Dictionary<string, string> TransType = new() {
            {"baidu", "百度" },
            {"caiyun", "彩云" },
        };

        public static void GetLanguageType(string transType, int ocrLanTypeIndex, int transLanTypeIndex, ref string ocrLanTye, ref string transLantypeFrom, ref string transLanTypeTo)
        {
            if (transType == TransType.ElementAt(0).Key)
            {
                ocrLanTye = TransMapBaidu2Baidu.ElementAt(ocrLanTypeIndex).Key;
                transLantypeFrom = TransMapBaidu2Baidu.ElementAt(ocrLanTypeIndex).Value;
                transLanTypeTo = TransMapBaidu2Baidu.ElementAt(transLanTypeIndex).Value;
            }
            else if (transType == TransType.ElementAt(1).Key)
            {
                ocrLanTye = TransMapBaidu2Caiyun.ElementAt(ocrLanTypeIndex).Key;
                transLantypeFrom = TransMapBaidu2Caiyun.ElementAt(ocrLanTypeIndex).Value;
                transLanTypeTo = TransMapBaidu2Caiyun.ElementAt(transLanTypeIndex).Value;
            }
        }

        public static List<string> GetOCRLanguageTypeName(string transType)
        {
            List<string> lantype = new List<string>();

            if (transType == TransType.ElementAt(0).Key)
            {
                foreach (var ele in TransMapBaidu2Baidu)
                {
                    lantype.Add(OCRBaiduLanguageType[ele.Key]);
                }
            }
            else if (transType == TransType.ElementAt(1).Key)
            {
                foreach (var ele in TransMapBaidu2Caiyun)
                {
                    lantype.Add(OCRBaiduLanguageType[ele.Key]);
                }
            }

            return lantype;
        }

        public static string GetOCRLanguageTypeName(string transType, string mapValue)
        {
            string typename = "";

            if (transType == TransType.ElementAt(0).Key)
            {
                foreach (var ele in TransMapBaidu2Baidu)
                {
                    if (ele.Value == mapValue)
                    {
                        typename = OCRBaiduLanguageType[ele.Key];
                    }
                }
            }
            else if (transType == TransType.ElementAt(1).Key)
            {
                foreach (var ele in TransMapBaidu2Caiyun)
                {
                    if (ele.Value == mapValue)
                    {
                        typename = OCRBaiduLanguageType[ele.Key];
                    }
                }
            }

            return typename;
        }

        public static List<string> GetTransLanguageTypeName(string transType)
        {
            List<string> lantype = new List<string>();

            if (transType == TransType.ElementAt(0).Key)
            {
                foreach (var ele in TransMapBaidu2Baidu)
                {
                    lantype.Add(TransBaiduLanguageType[ele.Value]);
                }
            }
            else if (transType == TransType.ElementAt(1).Key)
            {
                foreach (var ele in TransMapBaidu2Caiyun)
                {
                    lantype.Add(TransCaiyunLanguageType[ele.Value]);
                }
            }
            return lantype;
        }

        public static string GetTransLanguageTypeName(string transType, string mapValue)
        {
            string typename = "";

            if (transType == TransType.ElementAt(0).Key)
            {
                typename = TransBaiduLanguageType[mapValue];
            }
            else if (transType == TransType.ElementAt(1).Key)
            {
                typename = TransCaiyunLanguageType[mapValue];
            }

            return typename;
        }
    }
}
