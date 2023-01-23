using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BKTrans.Misc
{
    public class BKTransMap
    {
        public readonly static Dictionary<string, string> TransMapBaidu2Baidu = new() {
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

        public readonly static Dictionary<string, string> TransMapBaidu2Caiyun = new() {
            {"CHN_ENG", "zh"},
            {"JAP", "ja"},
            {"ENG", "en"},
        };

        public readonly static Dictionary<string, string> TransMapBaidu2Microsoft = new() {
            {"CHN_ENG", "zh-cn"},
            {"JAP", "ja"},
            {"ENG", "en-us"},
            {"KOR", "ko"},
            {"FRE", "fr"},
            {"GER", "de"},
            {"RUS", "ru"},
            {"SPA", "es"},
            {"POR", "pt"},
            {"ITA", "it"},
        };

        public readonly static List<string> TransType = new() {
            "baidu",
            "caiyun",
        };

        public static void GetLanguageType(string transType, int ocrLanTypeIndex, int transLanTypeIndex, ref string ocrLanTye, ref string transLantypeFrom, ref string transLanTypeTo)
        {
            if (transType == TransType[0])
            {
                ocrLanTye = TransMapBaidu2Baidu.ElementAt(ocrLanTypeIndex).Key;
                transLantypeFrom = TransMapBaidu2Baidu.ElementAt(ocrLanTypeIndex).Value;
                transLanTypeTo = TransMapBaidu2Baidu.ElementAt(transLanTypeIndex).Value;
            }
            else if (transType == TransType[1])
            {
                ocrLanTye = TransMapBaidu2Caiyun.ElementAt(ocrLanTypeIndex).Key;
                transLantypeFrom = TransMapBaidu2Caiyun.ElementAt(ocrLanTypeIndex).Value;
                transLanTypeTo = TransMapBaidu2Caiyun.ElementAt(transLanTypeIndex).Value;
            }
        }

        public static List<string> GetOCRLanguageTypeName(string transType)
        {
            List<string> lantype = new List<string>();

            if (transType == TransType[0])
            {
                foreach (var ele in TransMapBaidu2Baidu)
                {
                    lantype.Add(ele.Key);
                }
            }
            else if (transType == TransType[1])
            {
                foreach (var ele in TransMapBaidu2Caiyun)
                {
                    lantype.Add(ele.Key);
                }
            }

            return lantype;
        }

        public static string GetOCRLanguageTypeName(string transType, string mapValue)
        {
            string typename = "";

            if (transType == TransType[0])
            {
                foreach (var ele in TransMapBaidu2Baidu)
                {
                    if (ele.Value == mapValue)
                    {
                        typename = ele.Key;
                    }
                }
            }
            else if (transType == TransType[1])
            {
                foreach (var ele in TransMapBaidu2Caiyun)
                {
                    if (ele.Value == mapValue)
                    {
                        typename = ele.Key;
                    }
                }
            }

            return typename;
        }

        public static List<string> GetTransLanguageTypeName(string transType)
        {
            List<string> lantype = new List<string>();

            if (transType == TransType[0])
            {
                foreach (var ele in TransMapBaidu2Baidu)
                {
                    lantype.Add(ele.Value);
                }
            }
            else if (transType == TransType[1])
            {
                foreach (var ele in TransMapBaidu2Caiyun)
                {
                    lantype.Add(ele.Value);
                }
            }
            return lantype;
        }

    }
}
