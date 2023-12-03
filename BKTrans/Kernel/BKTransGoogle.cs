using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace BKTrans.Kernel;

public class BKTransGoogle : BKTransBase
{
    private readonly static Dictionary<BKTransMap.LangType, string> LangMap = new()
    {
        {BKTransMap.LangType.zh_cn,  "zh"},
        {BKTransMap.LangType.ja,     "ja"},
        {BKTransMap.LangType.en_us,  "en"},
        {BKTransMap.LangType.ko,     "ko"},
        {BKTransMap.LangType.fr,     "fr"},
        {BKTransMap.LangType.de,     "de"},
        {BKTransMap.LangType.ru,     "ru"},
        {BKTransMap.LangType.es,     "es"},
        {BKTransMap.LangType.pt,     "pt"},
        {BKTransMap.LangType.it,     "it"},
    };

    [Serializable]
    public class SettingGoogleTrans : BKTransSetting
    {
        public string api_key { get; set; }

        public SettingGoogleTrans()
        {
            name = "google";
            api_key = "";
            from = BKTransMap.LangType.ja;
            to = BKTransMap.LangType.zh_cn;
        }
    }

    private string _translateUri;

    public BKTransGoogle()
    {
        // https://cloud.google.com/translate/docs/basic/translating-text?hl=zh-cn
        _translateUri = "https://translation.googleapis.com/language/translate/v2";
    }

    public override List<BKTransMap.LangType> GetLangType()
    {
        return LangMap.Keys.ToList();
    }

    public override string Trans(BKBaseSetting setting_, string srcText)
    {
        SettingGoogleTrans setting = (SettingGoogleTrans)setting_;
        string result = "";
        do
        {
            if (string.IsNullOrEmpty(setting.api_key))
            {
                result = "api key is empty.";
                break;
            }

            // 请求
            JsonObject jobjContent = new();
            jobjContent.Add("q", new JsonArray(srcText));
            jobjContent.Add("source", LangMap[setting.from]);
            jobjContent.Add("target", LangMap[setting.to]);
            jobjContent.Add("key", setting.api_key);

            string oo = jobjContent.ToJsonString();

            HttpRequestMessage transReqMsg = new HttpRequestMessage(HttpMethod.Post, $"{_translateUri}?key={setting.api_key}")
            {
                Content = new StringContent(jobjContent.ToJsonString(), Encoding.UTF8, "application/json")
            };

            HttpClient transReq = BKHttpClient.DefaultHttpClient;
            HttpResponseMessage ocrRes = transReq.SendAsync(transReqMsg).Result;

            result = ocrRes.Content.ReadAsStringAsync().Result;

            // 解析结果
            using (JsonDocument jdocOcrResult = JsonDocument.Parse(result))
            {
                string transResultText = "";
                if (!jdocOcrResult.RootElement.TryGetProperty("data", out JsonElement jelemData))
                    break;

                if (!jelemData.TryGetProperty("translations", out JsonElement jeleTranslations))
                    break;

                foreach (JsonElement jeleResult in jeleTranslations.EnumerateArray())
                    transResultText += jeleResult.GetProperty("translatedText").ToString();

                result = transResultText;
            }

        } while (false);
        return result;
    }
}
