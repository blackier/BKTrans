using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace BKTrans.Misc
{
    public class BKTransCaiyun : BKTransBase
    {
        private readonly static Dictionary<string, string> LangMap = new()
        {
            {"zh-cn", "zh"},
            {"ja", "ja"},
            {"en-us", "en"},
        };

        [Serializable]
        public class SettingCaiyunTrans : BKTransSetting
        {
            public string token { get; set; }
            public string request_id { get; set; }

            public SettingCaiyunTrans()
            {
                name = "caiyun";
                token = "";
                request_id = "";
                from = "ja";
                to = "zh-cn";
            }
        }

        private string _translateUri;

        public BKTransCaiyun()
        {
            // https://docs.caiyunapp.com/blog/2018/09/03/lingocloud-api/
            _translateUri = "http://api.interpreter.caiyunai.com/v1/translator";
        }

        public override List<string> GetLangType()
        {
            return LangMap.Keys.ToList();
        }

        public override string Trans(BKSetting setting_, string srcText)
        {
            SettingCaiyunTrans setting = (SettingCaiyunTrans)setting_;
            string result = "";
            do
            {
                if (string.IsNullOrEmpty(setting.token))
                {
                    result = "token is empty.";
                    break;
                }
                if (string.IsNullOrEmpty(setting.request_id))
                {
                    setting.request_id = string.Format("{0}", (new Random()).Next(100000, 999999));
                }

                // 请求
                JsonObject jobjContent = new();
                jobjContent.Add("source", srcText);
                jobjContent.Add("trans_type", string.Format("{0}2{1}", LangMap[setting.from], LangMap[setting.to]));
                jobjContent.Add("request_id", setting.request_id);
                jobjContent.Add("detect", true);

                HttpRequestMessage transReqMsg = new HttpRequestMessage(HttpMethod.Post, _translateUri)
                {
                    Content = new StringContent(jobjContent.ToJsonString(), Encoding.UTF8, "application/json"),
                    Headers = {
                            { "X-Authorization", "token " + setting.token }
                        }
                };

                HttpClient transReq = BKHttpClient.DefaultHttpClient();
                HttpResponseMessage ocrRes = transReq.SendAsync(transReqMsg).Result;

                result = ocrRes.Content.ReadAsStringAsync().Result;

                // 解析结果
                using (JsonDocument jdocOcrResult = JsonDocument.Parse(result))
                {
                    var root_element = jdocOcrResult.RootElement;
                    if (!root_element.TryGetProperty("target", out JsonElement trasn_result))
                        break;

                    result = trasn_result.ToString();
                }

            } while (false);
            return result;
        }
    }
}
