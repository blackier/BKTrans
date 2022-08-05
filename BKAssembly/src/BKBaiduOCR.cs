using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace BKAssembly
{
    public class BKBaiduOCR
    {
        public static readonly Dictionary<string, string> LanguageType = new()
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
            {"ITA", "意大利语"}
        };

        private readonly string mApiKey;
        private readonly string mSecretKey;

        private readonly string mAkGrantType;
        private readonly string mAkOauthUri;
        private string mAccessToken;

        private readonly string mGeneralBasicUri;
        private string mLanguageType;

        private Action<string> mOcrResultCallback;
        private Bitmap mOcrSrcImage;

        public BKBaiduOCR(string apikey, string secretkey)
        {
            mApiKey = apikey;
            mSecretKey = secretkey;

            // 常量，参考：https://ai.baidu.com/ai-doc/REFERENCE/Ck3dwjhhu
            mAkGrantType = "client_credentials";
            mAkOauthUri = "https://aip.baidubce.com/oauth/2.0/token";

            // 常量，参考：https://ai.baidu.com/ai-doc/OCR/zk3h7xz52
            mGeneralBasicUri = "https://aip.baidubce.com/rest/2.0/ocr/v1/general_basic";
        }

        public async void DoOCR(Action<string> resultCallback, Bitmap image, string languageType)
        {
            mOcrResultCallback = resultCallback;
            mOcrSrcImage = (Bitmap)image.Clone();
            mLanguageType = languageType;
            await Task.Run(() =>
            {
                string result;
                do
                {
                    if (string.IsNullOrEmpty(mApiKey) || string.IsNullOrEmpty(mSecretKey))
                    {
                        result = string.Format("{{\"error\":\"{0}\"}}", "api key or secret key is empty.");
                        break;
                    }
                    if (string.IsNullOrEmpty(mAccessToken) && !GetAccessToken())
                    {
                        result = string.Format("{{\"error\":\"{0}\"}}", "access token acquisition failed.");
                        break;
                    }
                    result = GeneralBasic();
                } while (false);
                mOcrResultCallback(result);
            });
        }

        private bool GetAccessToken()
        {
            bool retIsSuccess = false;
            do
            {
                string contentString = string.Format("grant_type={0}&client_id={1}&client_secret={2}",
                   mAkGrantType, mApiKey, mSecretKey);

                HttpRequestMessage akReqMsg = new HttpRequestMessage(HttpMethod.Post, mAkOauthUri)
                {
                    Content = new StringContent(contentString)
                };

                HttpClient akReq = BKUtility.GetHttpClient();
                HttpResponseMessage akRes = akReq.SendAsync(akReqMsg).Result;

                using (JsonDocument jdocAkResult = JsonDocument.Parse(akRes.Content.ReadAsStringAsync().Result))
                {

                    var rootElem = jdocAkResult.RootElement;
                    if (rootElem.TryGetProperty("error", out JsonElement errorElement) || !rootElem.TryGetProperty("access_token", out JsonElement tokenElement))
                    {
                        break;
                    }
                    mAccessToken = tokenElement.GetString();
                    if (string.IsNullOrEmpty(mAccessToken))
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
                    HttpUtility.UrlEncode(BKUtility.Bitmap2Base64String(mOcrSrcImage)), mLanguageType);

                HttpRequestMessage ocrReqMsg = new HttpRequestMessage(HttpMethod.Post, mGeneralBasicUri + "?access_token=" + mAccessToken)
                {
                    Content = new StringContent(contentString, Encoding.UTF8, "application/x-www-form-urlencoded")
                };

                HttpClient ocrReq = BKUtility.GetHttpClient();
                HttpResponseMessage ocrRes = ocrReq.SendAsync(ocrReqMsg).Result;

                ocrResult = ocrRes.Content.ReadAsStringAsync().Result;

            } while (false);
            return ocrResult;
        }

    }
}
