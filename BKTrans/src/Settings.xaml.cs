using BKTrans.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Windows;

namespace BKTrans
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        [Serializable]
        public class OCRReplace
        {
            public string replace_src { get; set; }
            public string replace_dst { get; set; }
            public OCRReplace()
            {
                replace_src = "";
                replace_dst = "";
            }
        }

        [Serializable]
        public class Options
        {
            // 翻译API
            public string trans_type { get; set; }
            // ocr参数
            public BKOCRBaidu.SettingBaiduOCR ocr_baidu { get; set; }
            public bool ocr_microsoft_open { get; set; }
            // 翻译参数
            public BKTransBaidu.SettingBaiduTrans trans_baidu { get; set; }
            public BKTransCaiyun.SettingCaiyunTrans trans_caiyun { get; set; }
            public bool trans_both { get; set; }

            // 截图翻译事件间隔
            public int auto_captrue_trans_interval { get; set; }
            public int auto_captrue_trans_countdown { get; set; }
            public bool auto_captrue_trans_open { get; set; }
            public float auto_captrue_trans_similarity { get; set; }

            // ocr翻译替换
            public string ocr_replace_select { get; set; }
            public Dictionary<string, List<OCRReplace>> ocr_replace { get; set; }

            public Options()
            {
                trans_type = "baidu";
                ocr_baidu = new();
                trans_baidu = new();
                trans_caiyun = new();
                ocr_microsoft_open = false;
                trans_both = false;

                auto_captrue_trans_interval = 150;
                auto_captrue_trans_countdown = 5;
                auto_captrue_trans_open = false;
                auto_captrue_trans_similarity = 0.9f;

                ocr_replace_select = "";
                ocr_replace = new() { { "", new() } };
            }
        }
        private static Options _options;

        private static string _settingsFilePath;

        private bool _combox_ocr_replace_updating = false;

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
            BKMisc.SaveTextFile(_settingsFilePath, BKMisc.JsonSerialize<Options>(_options, true, true, JavaScriptEncoder.Create(UnicodeRanges.All)));
        }

        public Settings()
        {
            InitializeComponent();
            LoadWindow();
        }

        protected void LoadWindow()
        {
            LoadSetting();

            // api
            textbox_baiduocr_client_id.Text = _options.ocr_baidu.client_id;
            textbox_baiduocr_client_secret.Text = _options.ocr_baidu.client_secret;
            textbox_baiduapi_appid.Text = _options.trans_baidu.appid;
            textbox_baiduapi_secretkey.Text = _options.trans_baidu.secretkey;
            textbox_baiduapi_salt.Text = _options.trans_baidu.salt;
            textbox_caiyunapi_token.Text = _options.trans_caiyun.token;
            textbox_caiyunapi_request_id.Text = _options.trans_caiyun.request_id;

            // ocr替换
            _combox_ocr_replace_updating = true;
            combox_ocr_replace.ItemsSource = _options.ocr_replace.Keys.ToList();
            combox_ocr_replace.SelectedItem = _options.ocr_replace_select;
            _combox_ocr_replace_updating = false;

            datagrid_ocr_replace.ItemsSource = _options.ocr_replace[_options.ocr_replace_select];

            textbox_ocr_replace_new.IsEnabled = false;

            // 自动翻译
            textbox_auto_captrue_trans_interval.Text = _options.auto_captrue_trans_interval.ToString();
            textbox_auto_captrue_trans_countdown.Text = _options.auto_captrue_trans_countdown.ToString();
            textbox_auto_captrue_trans_similarity.Text = _options.auto_captrue_trans_similarity.ToString();
        }

        protected void btn_ok_Click(object sender, RoutedEventArgs e)
        {
            _options.ocr_baidu.client_id = textbox_baiduocr_client_id.Text;
            _options.ocr_baidu.client_secret = textbox_baiduocr_client_secret.Text;
            _options.trans_baidu.appid = textbox_baiduapi_appid.Text;
            _options.trans_baidu.secretkey = textbox_baiduapi_secretkey.Text;
            _options.trans_baidu.salt = textbox_baiduapi_salt.Text;
            _options.trans_caiyun.token = textbox_caiyunapi_token.Text;
            _options.trans_caiyun.request_id = textbox_caiyunapi_request_id.Text;

            if (int.TryParse(textbox_auto_captrue_trans_interval.Text, out int auto_captrue_trans_interval))
                _options.auto_captrue_trans_interval = auto_captrue_trans_interval;
            if (int.TryParse(textbox_auto_captrue_trans_countdown.Text, out int auto_captrue_trans_countdown))
                _options.auto_captrue_trans_countdown = auto_captrue_trans_countdown;
            if (float.TryParse(textbox_auto_captrue_trans_similarity.Text, out float auto_captrue_trans_similarity))
                _options.auto_captrue_trans_similarity = auto_captrue_trans_similarity;

            SaveSettings();
            Close();
        }

        #region 事件处理
        protected void btn_cancle_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btn_ocr_replace_new_Click(object sender, RoutedEventArgs e)
        {
            bool ctrl_enable = textbox_ocr_replace_new.IsEnabled;

            combox_ocr_replace.IsEnabled = ctrl_enable;
            btn_ocr_replace_delete.IsEnabled = ctrl_enable;
            datagrid_ocr_replace.IsEnabled = ctrl_enable;

            if (ctrl_enable)
            {
                if (!string.IsNullOrEmpty(textbox_ocr_replace_new.Text))
                {
                    _options.ocr_replace[textbox_ocr_replace_new.Text] = new();
                    _options.ocr_replace_select = textbox_ocr_replace_new.Text;
                }
                textbox_ocr_replace_new.Text = "";

                _combox_ocr_replace_updating = true;
                combox_ocr_replace.ItemsSource = _options.ocr_replace.Keys.ToList();
                combox_ocr_replace.SelectedItem = _options.ocr_replace_select;
                _combox_ocr_replace_updating = false;

                datagrid_ocr_replace.ItemsSource = _options.ocr_replace[_options.ocr_replace_select];
            }

            textbox_ocr_replace_new.IsEnabled = !ctrl_enable;
        }

        private void btn_ocr_replace_delete_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("确定删除？", "警告", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.No)
                return;

            if (!_options.ocr_replace.ContainsKey((string)combox_ocr_replace.SelectedItem))
                return;
            // 删除旧的
            _options.ocr_replace.Remove((string)combox_ocr_replace.SelectedItem);
            // 选择第一个
            if (_options.ocr_replace.Count == 0)
            {
                _options.ocr_replace_select = "";
                _options.ocr_replace = new() { { "", new() } };
            }
            else
            {
                _options.ocr_replace_select = _options.ocr_replace.ElementAt(0).Key;
            }

            _combox_ocr_replace_updating = true;
            combox_ocr_replace.ItemsSource = _options.ocr_replace.Keys.ToList();
            combox_ocr_replace.SelectedItem = _options.ocr_replace_select;
            _combox_ocr_replace_updating = false;

            datagrid_ocr_replace.ItemsSource = _options.ocr_replace[_options.ocr_replace_select];
        }

        private void combox_ocr_replace_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_combox_ocr_replace_updating)
                return;
            _options.ocr_replace_select = (string)combox_ocr_replace.SelectedItem;
            datagrid_ocr_replace.ItemsSource = _options.ocr_replace[_options.ocr_replace_select];
        }

        private void textbox_auto_captrue_trans_interval_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (e.Delta == 0)
                return;
            int unit = 10;
            if (e.Delta < 0)
                unit = -unit;
            int interval = int.Parse(textbox_auto_captrue_trans_interval.Text);
            if (interval + unit < 100)
                return;
            textbox_auto_captrue_trans_interval.Text = (interval + unit).ToString();
        }

        private void textbox_auto_captrue_trans_countdown_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (e.Delta == 0)
                return;
            int unit = 1;
            if (e.Delta < 0)
                unit = -unit;
            int interval = int.Parse(textbox_auto_captrue_trans_countdown.Text);
            if (interval + unit < 0)
                return;
            textbox_auto_captrue_trans_countdown.Text = (interval + unit).ToString();
        }

        private void textbox_auto_captrue_trans_similarity_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (e.Delta == 0)
                return;
            float unit = 0.01f;
            if (e.Delta < 0)
                unit = -unit;
            float interval = float.Parse(textbox_auto_captrue_trans_similarity.Text);
            if (interval + unit > 1)
                return;
            if (interval + unit < 0.5f)
                return;
            textbox_auto_captrue_trans_similarity.Text = (interval + unit).ToString("0.00");
        }
        #endregion 事件处理
    }
}
