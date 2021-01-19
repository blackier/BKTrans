using BKAssembly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BKTrans
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        #region 成员变量
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
        protected static Options options_;

        protected static string settings_file_path_;

        #endregion 成员变量

        #region 静态函数
        public static Options LoadSetting()
        {
            if (options_ == null)
            {
                if (settings_file_path_ == null)
                {
                    settings_file_path_ = Path.Combine(Directory.GetCurrentDirectory(), "settings.json");
                }
                options_ = BKUtility.JsonDeserialize<Options>(BKUtility.LoadTextFile(settings_file_path_));
            }
            return options_;
        }
        public static void SaveSettings()
        {
            BKUtility.SaveTextFile(settings_file_path_, BKUtility.JsonSerialize<Options>(options_));
        }
        #endregion

        #region 公有函数
        public Settings()
        {
            InitializeComponent();
            LoadWindow();
        }

        #endregion 公有函数

        #region 保护函数
        protected void LoadWindow()
        {
            LoadSetting();
            this.textBox_client_id.Text = options_.client_id;
            this.textBox_client_secret.Text = options_.client_secret;
            this.textBox_appid.Text = options_.appid;
            this.textBox_secretkey.Text = options_.secretkey;
        }

        #region 事件处理
        protected void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            options_.client_id = this.textBox_client_id.Text;
            options_.client_secret = this.textBox_client_secret.Text;
            options_.appid = this.textBox_appid.Text;
            options_.secretkey = this.textBox_secretkey.Text;
            this.Close();
        }

        protected void btn_cancle_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion 事件处理

        #endregion 保护函数
    }
}
