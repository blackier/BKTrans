using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace BKTrans.Core;

public class BKTransDeepL : BKTransBase
{
    private readonly static Dictionary<BKTransMap.LangType, string> LangMap = new()
    {
        {BKTransMap.LangType.zh_cn,  "ZH"},
        {BKTransMap.LangType.ja,     "JA"},
        {BKTransMap.LangType.en_us,  "EN"},
        {BKTransMap.LangType.ko,     "KO"},
        {BKTransMap.LangType.fr,     "FR"},
        {BKTransMap.LangType.de,     "DE"},
        {BKTransMap.LangType.ru,     "RU"},
        {BKTransMap.LangType.es,     "ES"},
        {BKTransMap.LangType.pt,     "PT"},
        {BKTransMap.LangType.it,     "IT"},
    };

    [Serializable]
    public class SettingDeepLTrans : BKTransSetting
    {
        public string auth_key { get; set; }

        public SettingDeepLTrans()
        {
            name = "deepl";
            auth_key = "";
            from = BKTransMap.LangType.ja;
            to = BKTransMap.LangType.zh_cn;
        }
    }

    private string _translateUri;

    public BKTransDeepL()
    {
        // https://developers.deepl.com/docs/v/zh/api-reference/translate
        _translateUri = "https://api-free.deepl.com/v2/translate";
    }

    public override List<BKTransMap.LangType> GetLangType()
    {
        return LangMap.Keys.ToList();
    }

    public override string Trans(BKBaseSetting setting_, string srcText)
    {
        SettingDeepLTrans setting = (SettingDeepLTrans)setting_;
        string result = "";
        do
        {
            if (string.IsNullOrEmpty(setting.auth_key))
            {
                result = "auth key is empty.";
                break;
            }

            // 请求
            JsonObject jobjContent = new();
            jobjContent.Add("text", new JsonArray(srcText));
            jobjContent.Add("source_lang", LangMap[setting.from]);
            jobjContent.Add("target_lang", LangMap[setting.to]);

            string oo = jobjContent.ToJsonString();

            HttpRequestMessage transReqMsg = new HttpRequestMessage(HttpMethod.Post, $"{_translateUri}")
            {
                Content = new StringContent(jobjContent.ToJsonString(), Encoding.UTF8, "application/json")
            };
            transReqMsg.Headers.Add("Authorization", setting.auth_key);

            HttpClient transReq = BKHttpClient.DefaultHttpClient;
            HttpResponseMessage ocrRes = transReq.Send(transReqMsg);

            result = ocrRes.Content.ReadAsStringAsync().Result;

            // 解析结果
            using (JsonDocument jdocOcrResult = JsonDocument.Parse(result))
            {
                string transResultText = "";
                if (!jdocOcrResult.RootElement.TryGetProperty("translations", out JsonElement jelemData))
                    break;

                if (!jelemData.TryGetProperty("translations", out JsonElement jeleTranslations))
                    break;

                foreach (JsonElement jeleResult in jeleTranslations.EnumerateArray())
                    transResultText += jeleResult.GetProperty("text").ToString();

                result = transResultText;
            }

        } while (false);
        return result;
    }
}
