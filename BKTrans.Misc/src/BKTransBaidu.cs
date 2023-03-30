﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace BKTrans.Misc
{
    public class BKTransBaidu : BKTransBase
    {
        private readonly static Dictionary<string, string> LangMap = new()
        {
            {"zh-cn", "zh"},
            {"ja", "jp"},
            {"en-us", "en"},
            {"ko", "kor"},
            {"fr", "fra"},
            {"de", "de"},
            {"ru", "ru"},
            {"es", "spa"},
            {"pt", "pt"},
            {"it", "it"},
        };

        [Serializable]
        public class SettingBaiduTrans : BKTransSetting
        {
            public string appid { get; set; }
            public string secretkey { get; set; }
            public string salt { get; set; }

            public SettingBaiduTrans()
            {
                name = "baidu";
                appid = "";
                secretkey = "";
                salt = "";
                from = "ja";
                to = "zh-cn";
            }
        }

        private readonly string _translateUri;

        public BKTransBaidu()
        {
            // 常量，参考：http://api.fanyi.baidu.com/doc/21
            _translateUri = "https://fanyi-api.baidu.com/api/trans/vip/translate";
        }

        public override List<string> GetLangType()
        {
            return LangMap.Keys.ToList();
        }

        public override string Trans(BKSetting setting_, string srcText)
        {
            SettingBaiduTrans setting = (SettingBaiduTrans)setting_;
            string result = "";
            do
            {
                if (string.IsNullOrEmpty(setting.appid) || string.IsNullOrEmpty(setting.secretkey))
                {
                    result = "api id or secret key is empty.";
                    break;
                }
                if (string.IsNullOrEmpty(setting.salt))
                {
                    setting.salt = string.Format("{0}", (new Random()).Next(100000, 999999));
                }
                // 发起请求
                string contentString = string.Format("q={0}&from={1}&to={2}&appid={3}&salt={4}&sign={5}",
                                HttpUtility.UrlEncode(srcText), LangMap[setting.from], LangMap[setting.to], setting.appid, setting.salt,
                                BKMisc.CalculMD5(setting.appid + srcText + setting.salt + setting.secretkey));

                HttpRequestMessage transReqMsg = new HttpRequestMessage(HttpMethod.Post, _translateUri)
                {
                    Content = new StringContent(contentString, Encoding.UTF8, "application/x-www-form-urlencoded")
                };

                HttpClient transReq = BKHttpClient.DefaultHttpClient();
                HttpResponseMessage ocrRes = transReq.SendAsync(transReqMsg).Result;

                result = ocrRes.Content.ReadAsStringAsync().Result;

                // 解析结果
                using (JsonDocument jdocOcrResult = JsonDocument.Parse(result))
                {
                    string transResultText = "";
                    var root_element = jdocOcrResult.RootElement;
                    if (!root_element.TryGetProperty("trans_result", out JsonElement trasn_result))
                        break;

                    foreach (JsonElement dstElem in trasn_result.EnumerateArray())
                        transResultText += dstElem.GetProperty("dst").ToString();

                    result = transResultText;
                }

            } while (false);

            return result;
        }
    }
}
