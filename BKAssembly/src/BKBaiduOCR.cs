using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace BKAssembly
{
    public class BKBaiduOCR
    {
        #region 成员变量定义

        public static readonly List<string> LanguageType = new()
        {
            "CHN_ENG", /*中英文*/
            "JAP", /*日语*/
            "ENG", /*英语*/
            "KOR", /*韩语*/
            "FRE", /*法语*/
            "GER", /*德语*/
            "RUS", /*俄语*/
            "SPA", /*西班牙语*/
            "POR", /*葡萄牙语*/
            "ITA", /*意大利语*/
        };

        private readonly string api_key_;
        private readonly string secret_key_;

        private readonly string ak_grant_type_;
        private readonly string ak_oauth_uri_;
        private string access_token_;

        private readonly string general_basic_uri_;
        private string language_type_;

        private Action<string> ocr_result_callback_;
        private Bitmap ocr_src_image_;

        #endregion 成员变量定义

        #region 公有成员函数定义

        public BKBaiduOCR(string api_key, string secret_key)
        {
            api_key_ = api_key;
            secret_key_ = secret_key;

            // 常量，参考：https://ai.baidu.com/ai-doc/REFERENCE/Ck3dwjhhu
            ak_grant_type_ = "client_credentials";
            ak_oauth_uri_ = "https://aip.baidubce.com/oauth/2.0/token";

            // 常量，参考：https://ai.baidu.com/ai-doc/OCR/zk3h7xz52
            general_basic_uri_ = "https://aip.baidubce.com/rest/2.0/ocr/v1/general_basic";
        }

        public async void DoOCR(Action<string> result_callback, Bitmap image, string language_type)
        {
            ocr_result_callback_ = result_callback;
            ocr_src_image_ = (Bitmap)image.Clone();
            language_type_ = language_type;
            await Task.Run(() =>
            {
                string result;
                do
                {
                    if (string.IsNullOrEmpty(api_key_) || string.IsNullOrEmpty(secret_key_))
                    {
                        result = string.Format("{{\"error\":\"{0}\"}}", "api key or secret key is empty.");
                        break;
                    }
                    if (string.IsNullOrEmpty(access_token_) && !GetAccessToken())
                    {
                        result = string.Format("{{\"error\":\"{0}\"}}", "access token acquisition failed.");
                        break;
                    }
                    result = GeneralBasic();
                } while (false);
                ocr_result_callback_(result);
            });
        }

        #endregion 公有成员函数定义

        #region 私有成员函数定义

        private bool GetAccessToken()
        {
            bool is_success = false;
            do
            {
                string content_string = string.Format("grant_type={0}&client_id={1}&client_secret={2}",
                   ak_grant_type_, api_key_, secret_key_);

                HttpRequestMessage ak_req_msg = new HttpRequestMessage(HttpMethod.Post, ak_oauth_uri_)
                {
                    Content = new StringContent(content_string)
                };

                HttpClient ak_request = BKUtility.GetHttpClient();
                HttpResponseMessage ak_response = ak_request.SendAsync(ak_req_msg).Result;

                using (JsonDocument jdoc_ak_result = JsonDocument.Parse(ak_response.Content.ReadAsStringAsync().Result))
                {

                    var root_elem = jdoc_ak_result.RootElement;
                    if (root_elem.TryGetProperty("error", out JsonElement error_element) || !root_elem.TryGetProperty("access_token", out JsonElement token_element))
                    {
                        break;
                    }
                    access_token_ = token_element.GetString();
                    if (string.IsNullOrEmpty(access_token_))
                    {
                        break;
                    }
                }
                is_success = true;
            } while (false);
            return is_success;
        }

        private string GeneralBasic()
        {
            string ocr_result;
            do
            {
                string content_string = string.Format("image={0}&language_type={1}",
                    HttpUtility.UrlEncode(BKUtility.Bitmap2Base64String(ocr_src_image_)), language_type_);

                HttpRequestMessage ocr_req_msg = new HttpRequestMessage(HttpMethod.Post, general_basic_uri_ + "?access_token=" + access_token_)
                {
                    Content = new StringContent(content_string, Encoding.UTF8, "application/x-www-form-urlencoded")
                };

                HttpClient ocr_request = BKUtility.GetHttpClient();
                HttpResponseMessage ocr_response = ocr_request.SendAsync(ocr_req_msg).Result;

                ocr_result = ocr_response.Content.ReadAsStringAsync().Result;

            } while (false);
            return ocr_result;
        }

        #endregion 私有成员函数定义
    }
}
