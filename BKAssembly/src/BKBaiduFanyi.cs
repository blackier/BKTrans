using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace BKAssembly
{
    public class BKBaiduFanyi
    {
        public static readonly List<string> mLanguageType = new()
        {
            "zh", /*中文*/
            "jp", /*日语*/
            "en", /*英语*/
            "kor", /*韩语*/
            "fra", /*法语*/
            "de", /*德语*/
            "ru", /*俄语*/
            "spa", /*西班牙语*/
            "pt", /*葡萄牙语*/
            "it", /*意大利语*/
        };

        private readonly string mAppId;
        private readonly string mSecretKey;
        private string mSalt;

        private readonly string mTranslateUri;

        private Action<string> mFanyiResultCallback;

        public BKBaiduFanyi(string appId, string secretKey, string salt)
        {
            mAppId = appId;
            mSecretKey = secretKey;

            mSalt = salt;
            if (string.IsNullOrEmpty(mSalt))
            {
                mSalt = string.Format("{0}", (new Random()).Next(100000, 999999));
            }

            // 常量，参考：http://api.fanyi.baidu.com/doc/21
            mTranslateUri = "https://fanyi-api.baidu.com/api/trans/vip/translate";
        }

        public async void DoFanyi(Action<string> resultCallback, string srcText, string fromType, string toType)
        {
            mFanyiResultCallback = resultCallback;
            await Task.Run(() =>
            {

                string result;
                do
                {
                    if (string.IsNullOrEmpty(mAppId) || string.IsNullOrEmpty(mSecretKey))
                    {
                        result = string.Format("{{\"error\":\"{0}\"}}", "api id or secret key is empty.");
                        break;
                    }
                    result = Translate(srcText, fromType, toType);
                } while (false);

                mFanyiResultCallback(result);
            });
        }

        private string Translate(string srcText, string fromType, string toType)
        {
            string transResult = "";
            do
            {
                if (string.IsNullOrEmpty(mAppId) || string.IsNullOrEmpty(mSecretKey))
                {
                    break;
                }

                string contentString = string.Format("q={0}&from={1}&to={2}&appid={3}&salt={4}&sign={5}",
                                HttpUtility.UrlEncode(srcText), fromType, toType, mAppId, mSalt,
                                BKUtility.CalculMD5(mAppId + srcText + mSalt + mSecretKey));

                HttpRequestMessage transReqMsg = new HttpRequestMessage(HttpMethod.Post, mTranslateUri)
                {
                    Content = new StringContent(contentString, Encoding.UTF8, "application/x-www-form-urlencoded")
                };

                HttpClient transReq = BKUtility.GetHttpClient();
                HttpResponseMessage ocrRes = transReq.SendAsync(transReqMsg).Result;

                transResult = ocrRes.Content.ReadAsStringAsync().Result;

            } while (false);
            return transResult;
        }

    }
}
