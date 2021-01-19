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
        #region 成员变量定义

        public static readonly List<string> LanguageType = new()
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

        private readonly string app_id_;
        private readonly string secret_key_;
        private string salt_;

        private readonly string translate_uri_;

        Action<string> fanyi_result_callback_;

        #endregion 成员变量定义

        #region 公有成员函数定义

        public BKBaiduFanyi(string app_id, string secret_key, string salt)
        {
            app_id_ = app_id;
            secret_key_ = secret_key;

            salt_ = salt;
            if (string.IsNullOrEmpty(salt_))
            {
                salt_ = string.Format("{0}", (new Random()).Next(100000, 999999));
            }

            // 常量，参考：http://api.fanyi.baidu.com/doc/21
            translate_uri_ = "https://fanyi-api.baidu.com/api/trans/vip/translate";
        }

        public async void DoFanyi(Action<string> result_callback, string src_text, string from_type, string to_type)
        {
            fanyi_result_callback_ = result_callback;
            await Task.Run(() =>
            {

                string result;
                do
                {
                    if (string.IsNullOrEmpty(app_id_) || string.IsNullOrEmpty(secret_key_))
                    {
                        result = string.Format("{{\"error\":\"{0}\"}}", "api id or secret key is empty.");
                        break;
                    }
                    result = Translate(src_text, from_type, to_type);
                } while (false);

                fanyi_result_callback_(result);
            });
        }

        #endregion 公有成员函数定义

        #region 私有成员函数定义

        private string Translate(string src_text, string from_type, string to_type)
        {
            string trans_result = "";
            do
            {
                if (string.IsNullOrEmpty(app_id_) || string.IsNullOrEmpty(secret_key_))
                {
                    break;
                }

                string content_string = string.Format("q={0}&from={1}&to={2}&appid={3}&salt={4}&sign={5}",
                                HttpUtility.UrlEncode(src_text), from_type, to_type, app_id_, salt_,
                                BKUtility.CalculMD5(app_id_ + src_text + salt_ + secret_key_));

                HttpRequestMessage trans_req_msg = new HttpRequestMessage(HttpMethod.Post, translate_uri_)
                {
                    Content = new StringContent(content_string, Encoding.UTF8, "application/x-www-form-urlencoded")
                };

                HttpClient trans_request = BKUtility.GetHttpClient();
                HttpResponseMessage ocr_response = trans_request.SendAsync(trans_req_msg).Result;

                trans_result = ocr_response.Content.ReadAsStringAsync().Result;

            } while (false);
            return trans_result;
        }

        #endregion 私有成员函数定义
    }
}
