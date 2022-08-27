using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace BKAssembly
{
    public class BKTransCaiyun : BKTransBase
    {
        [Serializable]
        public class SettingCaiyunTrans : BKSetting
        {
            public string token { get; set; }
            public string request_id { get; set; }
            public string from { get; set; }
            public string to { get; set; }

            public SettingCaiyunTrans()
            {
                token = "";
                request_id = "";
                from = "ja";
                to = "zh";
            }
        }

        private string _translateUri;

        public BKTransCaiyun()
        {
            // https://docs.caiyunapp.com/blog/2018/09/03/lingocloud-api/
            _translateUri = "http://api.interpreter.caiyunai.com/v1/translator";
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
                    setting.request_id = "BKTrans";
                }

                // 请求
                JsonObject jobjContent = new();
                jobjContent.Add("source", srcText);
                jobjContent.Add("trans_type", string.Format("{0}2{1}", setting.from, setting.to));
                jobjContent.Add("request_id", setting.request_id);
                jobjContent.Add("detect", true);

                HttpRequestMessage transReqMsg = new HttpRequestMessage(HttpMethod.Post, _translateUri)
                {
                    Content = new StringContent(jobjContent.ToJsonString(), Encoding.UTF8, "application/json"),
                    Headers = {
                            { "X-Authorization", "token " + setting.token }
                        }
                };

                HttpClient transReq = BKMisc.GetHttpClient();
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
