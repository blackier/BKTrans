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
            // Access Token参数
            public string client_id { get; set; }
            public string client_secret { get; set; }
            // 百度ocr参数
            public string language_type { get; set; }
            // 百度翻译参数
            public string appid { get; set; }
            public string secretkey { get; set; }
            public string from { get; set; }
            public string to { get; set; }
        }
        private static Options mOptions;

        private static string mSettingsFilePath;

        public static Options LoadSetting()
        {
            if (mOptions == null)
            {
                if (mSettingsFilePath == null)
                {
                    mSettingsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "settings.json");
                }
                try
                {
                    mOptions = BKUtility.JsonDeserialize<Options>(BKUtility.LoadTextFile(mSettingsFilePath));
                }
                catch
                {
                    mOptions = new Options();
                }

            }
            return mOptions;
        }
        public static void SaveSettings()
        {
            BKUtility.SaveTextFile(mSettingsFilePath, BKUtility.JsonSerialize<Options>(mOptions));
        }

        public Settings()
        {
            InitializeComponent();
            LoadWindow();
        }


        protected void LoadWindow()
        {
            LoadSetting();
            this.textBox_client_id.Text = mOptions.client_id;
            this.textBox_client_secret.Text = mOptions.client_secret;
            this.textBox_appid.Text = mOptions.appid;
            this.textBox_secretkey.Text = mOptions.secretkey;
        }


        protected void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            mOptions.client_id = this.textBox_client_id.Text;
            mOptions.client_secret = this.textBox_client_secret.Text;
            mOptions.appid = this.textBox_appid.Text;
            mOptions.secretkey = this.textBox_secretkey.Text;
            this.Close();
        }

        protected void btn_cancle_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
