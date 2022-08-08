using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace BKAssembly
{
    public class BKOCRBaidu
    {
        public class SettingBaiduOCR
        {
            public string grant_type { get; set; }
            public string client_id { get; set; }
            public string client_secret { get; set; }
            public string language_type { get; set; }
        }

        private SettingBaiduOCR _setting;
        private string _accessToken;
        private readonly string _akOauthUri;

        private readonly string _generalBasicUri;

        private Action<string> _ocrResultCallback;
        private Bitmap _ocrSrcImage;

        public BKOCRBaidu()
        {
            // 常量，参考：https://ai.baidu.com/ai-doc/REFERENCE/Ck3dwjhhu
            _akOauthUri = "https://aip.baidubce.com/oauth/2.0/token";

            // 常量，参考：https://ai.baidu.com/ai-doc/OCR/zk3h7xz52
            _generalBasicUri = "https://aip.baidubce.com/rest/2.0/ocr/v1/general_basic";
        }

        public async void DoOCR(Action<string> resultCallback, SettingBaiduOCR setting, Bitmap image)
        {
            _ocrResultCallback = resultCallback;
            _ocrSrcImage = (Bitmap)image.Clone();
            _setting = setting;
            if (string.IsNullOrEmpty(_setting.grant_type))
            {
                _setting.grant_type = "client_credentials";
            }
            await Task.Run(() =>
            {
                string result;
                do
                {
                    if (string.IsNullOrEmpty(_setting.client_id) || string.IsNullOrEmpty(_setting.client_secret))
                    {
                        result = string.Format("{{\"error\":\"{0}\"}}", "api key or secret key is empty.");
                        break;
                    }
                    if (string.IsNullOrEmpty(_accessToken) && !GetAccessToken())
                    {
                        result = string.Format("{{\"error\":\"{0}\"}}", "access token acquisition failed.");
                        break;
                    }
                    result = GeneralBasic();
                } while (false);
                _ocrResultCallback(result);
            });
        }

        private bool GetAccessToken()
        {
            bool retIsSuccess = false;
            do
            {
                string contentString = string.Format("grant_type={0}&client_id={1}&client_secret={2}",
                   _setting.grant_type, _setting.client_id, _setting.client_secret);

                HttpRequestMessage akReqMsg = new HttpRequestMessage(HttpMethod.Post, _akOauthUri)
                {
                    Content = new StringContent(contentString)
                };

                HttpClient akReq = BKMisc.GetHttpClient();
                HttpResponseMessage akRes = akReq.SendAsync(akReqMsg).Result;

                using (JsonDocument jdocAkResult = JsonDocument.Parse(akRes.Content.ReadAsStringAsync().Result))
                {

                    var rootElem = jdocAkResult.RootElement;
                    if (rootElem.TryGetProperty("error", out JsonElement errorElement) || !rootElem.TryGetProperty("access_token", out JsonElement tokenElement))
                    {
                        break;
                    }
                    _accessToken = tokenElement.GetString();
                    if (string.IsNullOrEmpty(_accessToken))
                    {
                        break;
                    }
                }
                retIsSuccess = true;
            } while (false);
            return retIsSuccess;
        }

        private string GeneralBasic()
        {
            string ocrResult;
            do
            {
                string contentString = string.Format("image={0}&language_type={1}",
                    HttpUtility.UrlEncode(BKMisc.Bitmap2Base64String(_ocrSrcImage)), _setting.language_type);

                HttpRequestMessage ocrReqMsg = new HttpRequestMessage(HttpMethod.Post, _generalBasicUri + "?access_token=" + _accessToken)
                {
                    Content = new StringContent(contentString, Encoding.UTF8, "application/x-www-form-urlencoded")
                };

                HttpClient ocrReq = BKMisc.GetHttpClient();
                HttpResponseMessage ocrRes = ocrReq.SendAsync(ocrReqMsg).Result;

                ocrResult = ocrRes.Content.ReadAsStringAsync().Result;

            } while (false);
            return ocrResult;
        }

    }
}
