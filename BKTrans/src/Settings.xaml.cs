using BKAssembly;
using System;
using System.IO;
using System.Windows;

namespace BKTrans
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        [Serializable]
        public class Options
        {
            // 翻译API
            public string trans_type { get; set; }
            // ocr参数
            public BKOCRBaidu.SettingBaiduOCR ocr_baidu { get; set; }
            // 翻译参数
            public BKTransBaidu.SettingBaiduTrans trans_baidu { get; set; }
            public BKTransCaiyun.SettingCaiyunTrans trans_caiyun { get; set; }

            // 截图翻译事件间隔
            public int auto_captrue_trans_interval { get; set; }
            public int auto_captrue_trans_countdown { get; set; }
            public bool auto_captrue_trans_open { get; set; }
            public float auto_captrue_trans_similarity { get; set; }

            public Options()
            {
                trans_type = "baidu";
                ocr_baidu = new();
                trans_baidu = new();
                trans_caiyun = new();

                auto_captrue_trans_interval = 150;
                auto_captrue_trans_countdown = 2;
                auto_captrue_trans_open = false;
                auto_captrue_trans_similarity = 0.95f;
            }
        }
        private static Options _options;

        private static string _settingsFilePath;

        public static Options LoadSetting()
        {
            if (_options == null)
            {
                _options = new Options();
                if (_settingsFilePath == null)
                {
                    _settingsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "settings.json");
                }
                try
                {
                    _options = BKMisc.JsonDeserialize<Options>(BKMisc.LoadTextFile(_settingsFilePath));
                }
                catch
                {
                }
            }
            return _options;
        }
        public static void SaveSettings()
        {
            BKMisc.SaveTextFile(_settingsFilePath, BKMisc.JsonSerialize<Options>(_options));
        }

        public Settings()
        {
            InitializeComponent();
            LoadWindow();
        }

        protected void LoadWindow()
        {
            LoadSetting();
            this.textbox_client_id.Text = _options.ocr_baidu.client_id;
            this.textbox_client_secret.Text = _options.ocr_baidu.client_secret;
            this.textbox_baidu_appid.Text = _options.trans_baidu.appid;
            this.textbox_baidu_secretkey.Text = _options.trans_baidu.secretkey;
            this.textbox_caiyun_token.Text = _options.trans_caiyun.token;
        }

        protected void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            _options.ocr_baidu.client_id = this.textbox_client_id.Text;
            _options.ocr_baidu.client_secret = this.textbox_client_secret.Text;
            _options.trans_baidu.appid = this.textbox_baidu_appid.Text;
            _options.trans_baidu.secretkey = this.textbox_baidu_secretkey.Text;
            _options.trans_caiyun.token = this.textbox_caiyun_token.Text;

            SaveSettings();
            this.Close();
        }

        protected void btn_cancle_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
