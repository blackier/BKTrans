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

            public Options()
            {
                ocr_baidu = new();
                trans_baidu = new();
                trans_caiyun = new();
            }
        }
        private static Options mOptions;

        private static string mSettingsFilePath;

        public static Options LoadSetting()
        {
            if (mOptions == null)
            {
                mOptions = new Options();
                if (mSettingsFilePath == null)
                {
                    mSettingsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "settings.json");
                }
                try
                {
                    mOptions = BKMisc.JsonDeserialize<Options>(BKMisc.LoadTextFile(mSettingsFilePath));
                }
                catch
                {
                }
            }
            return mOptions;
        }
        public static void SaveSettings()
        {
            BKMisc.SaveTextFile(mSettingsFilePath, BKMisc.JsonSerialize<Options>(mOptions));
        }

        public Settings()
        {
            InitializeComponent();
            LoadWindow();
        }

        protected void LoadWindow()
        {
            LoadSetting();
            this.textBox_client_id.Text = mOptions.ocr_baidu.client_id;
            this.textBox_client_secret.Text = mOptions.ocr_baidu.client_secret;
            this.textBox_baidu_appid.Text = mOptions.trans_baidu.appid;
            this.textBox_baidu_secretkey.Text = mOptions.trans_baidu.secretkey;
            this.textBox_caiyun_token.Text = mOptions.trans_caiyun.token;
        }

        protected void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            mOptions.ocr_baidu.client_id = this.textBox_client_id.Text;
            mOptions.ocr_baidu.client_secret = this.textBox_client_secret.Text;
            mOptions.trans_baidu.appid = this.textBox_baidu_appid.Text;
            mOptions.trans_baidu.secretkey = this.textBox_baidu_secretkey.Text;
            mOptions.trans_caiyun.token = this.textBox_caiyun_token.Text;
            this.Close();
        }

        protected void btn_cancle_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
