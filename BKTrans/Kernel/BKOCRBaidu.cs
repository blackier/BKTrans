using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using Windows.Media.Ocr;

namespace BKTrans.Kernel;

public class BKOCRBaidu : BKOCRBase
{
    private readonly static Dictionary<BKTransMap.LangType, string> LangMap = new()
    {
        {BKTransMap.LangType.zh_cn,  "CHN_ENG"},
        {BKTransMap.LangType.ja,     "JAP"},
        {BKTransMap.LangType.en_us,  "ENG"},
        {BKTransMap.LangType.ko,     "KOR"},
        {BKTransMap.LangType.fr,     "FRE"},
        {BKTransMap.LangType.de,     "GER"},
        {BKTransMap.LangType.ru,     "RUS"},
        {BKTransMap.LangType.es,     "SPA"},
        {BKTransMap.LangType.pt,     "POR"},
        {BKTransMap.LangType.it,     "ITA"},
    };

    public class SettingBaiduOCR : BKOCRSetting
    {
        public string grant_type { get; set; }
        public string client_id { get; set; }
        public string client_secret { get; set; }

        public SettingBaiduOCR()
        {
            name = "baidu";
            grant_type = "";
            client_id = "";
            client_secret = "";
            language = BKTransMap.LangType.ja;
        }
    }

    private SettingBaiduOCR _setting;
    private string _accessToken;
    private readonly string _akOauthUri;

    private readonly string _generalBasicUri;

    private Bitmap _ocrSrcImage;

    public BKOCRBaidu()
    {
        // 常量，参考：https://ai.baidu.com/ai-doc/REFERENCE/Ck3dwjhhu
        _akOauthUri = "https://aip.baidubce.com/oauth/2.0/token";

        // 常量，参考：https://ai.baidu.com/ai-doc/OCR/zk3h7xz52
        _generalBasicUri = "https://aip.baidubce.com/rest/2.0/ocr/v1/general_basic";
    }

    public override List<BKTransMap.LangType> GetLangType()
    {
        return LangMap.Keys.ToList();
    }

    public override bool OCR(BKBaseSetting setting, Bitmap image, out string result)
    {
        _ocrSrcImage = (Bitmap)image.Clone();
        _setting = (SettingBaiduOCR)setting;
        do
        {
            if (string.IsNullOrEmpty(_setting.grant_type))
            {
                _setting.grant_type = "client_credentials";
            }

            if (string.IsNullOrEmpty(_setting.client_id) || string.IsNullOrEmpty(_setting.client_secret))
            {
                result = "api key or secret key is empty.";
                break;
            }
            if (string.IsNullOrEmpty(_accessToken) && !GetAccessToken())
            {
                result = "access token acquisition failed.";
                break;
            }

            result = GeneralBasic();
        } while (false);
        return true;
    }

    public override string ParseResult(string result)
    {
        string ocrResultText = "";
        JsonDocument jdocOcrResult = JsonDocument.Parse(result);
        var rootElement = jdocOcrResult.RootElement;
        if (!rootElement.TryGetProperty("words_result", out JsonElement wordsResult))
        {
            ocrResultText = result;
        }
        else
        {
            foreach (JsonElement words_elem in wordsResult.EnumerateArray())
                ocrResultText += words_elem.GetProperty("words").GetString();
        }
        return ocrResultText;
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

            HttpClient akReq = BKHttpClient.DefaultHttpClient;
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
            string contentString = string.Format("image={0}&language_type={1}", HttpUtility.UrlEncode(_ocrSrcImage.ToBase64()), LangMap[_setting.language]);

            HttpRequestMessage ocrReqMsg = new HttpRequestMessage(HttpMethod.Post, _generalBasicUri + "?access_token=" + _accessToken)
            {
                Content = new StringContent(contentString, Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            HttpClient ocrReq = BKHttpClient.DefaultHttpClient;
            HttpResponseMessage ocrRes = ocrReq.SendAsync(ocrReqMsg).Result;

            ocrResult = ocrRes.Content.ReadAsStringAsync().Result;

        } while (false);
        return ocrResult;
    }

}
