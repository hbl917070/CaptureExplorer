using System;
using System.Text;
using System.Windows.Controls;
using System.Xml;

namespace CaptureExplorer.Services {
    /// <summary>
    /// 讀寫 XML 設定檔
    /// </summary>
    public class SettingsService {
        private const string XmlName = "Data/Setting.xml";
        private readonly MainWindow mainWindow;

        /// <summary> tabs 目前選取的項目 </summary>
        public string SelectedFolder = "";
        /// <summary> tabs 排序 </summary>
        public string FolderOrder = "";

        public SettingsService(MainWindow m) {
            mainWindow = m;
        }

        /// <summary>
        /// 儲存設定
        /// </summary>
        public void SaveSettings() {
            var writeSetting = new Action<XmlTextWriter, string, string>((xmlWriter, key, value) => {
                xmlWriter.WriteStartElement("item");
                xmlWriter.WriteAttributeString("name", key);
                xmlWriter.WriteString(value);
                xmlWriter.WriteEndElement();
            });

            using (XmlTextWriter writer = new XmlTextWriter(XmlName, Encoding.UTF8)) {
                writer.WriteStartDocument();
                writer.Formatting = Formatting.Indented;
                writer.Indentation = 2;
                writer.WriteStartElement("settings");

                writeSetting(writer, "IsAutoCopyToClipboard", mainWindow.ChkAutoCopyToClipboard.IsChecked.Value.ToString());
                writeSetting(writer, "ScreenshotHotkey", mainWindow.ScreenshotHotkey);
                writeSetting(writer, "FullscreenHotkey", mainWindow.FullscreenHotkey);
                writeSetting(writer, "ActiveWindowHotkey", mainWindow.ActiveWindowHotkey);
                writeSetting(writer, "IsCustomSavePath", mainWindow.IsCustomSavePath.ToString());
                writeSetting(writer, "CustomSavePath", mainWindow.CustomSavePath);
                writeSetting(writer, "IsSingleLayerSave", mainWindow.IsSingleLayerSave.ToString());
                writeSetting(writer, "FolderOrder", mainWindow.GetSubfolderOrder());
                writeSetting(writer, "SelectedFolder", mainWindow.TabController.SelectedTab.Text);

                writer.WriteEndElement();
                writer.Flush();
            }
        }

        /// <summary>
        /// 載入設定
        /// </summary>
        public void LoadSettingsOnStartup() {
            try {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(XmlName);

                XmlNodeList nodeLists = xmlDocument.SelectNodes("settings/item");
                foreach (XmlNode item in nodeLists) {

                    if (item.Attributes["name"].Value == "IsAutoCopyToClipboard") {
                        mainWindow.ChkAutoCopyToClipboard.IsChecked = item.InnerText.ToUpper() == "TRUE";
                    }
                    if (item.Attributes["name"].Value == "ScreenshotHotkey") {
                        mainWindow.ScreenshotHotkey = item.InnerText;
                    }
                    if (item.Attributes["name"].Value == "FullscreenHotkey") {
                        mainWindow.FullscreenHotkey = item.InnerText;
                    }
                    if (item.Attributes["name"].Value == "ActiveWindowHotkey") {
                        mainWindow.ActiveWindowHotkey = item.InnerText;
                    }
                    if (item.Attributes["name"].Value == "IsCustomSavePath") {
                        mainWindow.IsCustomSavePath = item.InnerText.ToUpper() == "TRUE";
                    }
                    if (item.Attributes["name"].Value == "CustomSavePath") {
                        mainWindow.CustomSavePath = item.InnerText;
                    }
                    if (item.Attributes["name"].Value == "IsSingleLayerSave") {
                        mainWindow.IsSingleLayerSave = item.InnerText.ToUpper() == "TRUE";
                    }
                    if (item.Attributes["name"].Value == "SelectedFolder") {
                        SelectedFolder = item.InnerText;
                    }
                    if (item.Attributes["name"].Value == "FolderOrder") {
                        FolderOrder = item.InnerText;
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// 將單一設定節點套用到對應控制項。
        /// </summary>
        private void ReadSettingItem(XmlNode item, object obj) {
            if (obj is CheckBox) {
                if (item.Attributes["name"].Value.Equals(((CheckBox)obj).Name)) {
                    ((CheckBox)obj).IsChecked = item.InnerText.Equals(true.ToString());
                }
            }
            else if (obj is TextBox) {
                if (item.Attributes["name"].Value.Equals(((TextBox)obj).Name)) {
                    ((TextBox)obj).Text = item.InnerText;
                }
            }
        }

        /// <summary>
        /// 從輸入框讀取浮點數，並限制在允許範圍內。
        /// </summary>
        public float ReadFloatFromTextBox(TextBox textBox, float min, float max) {
            float value = 0;

            textBox.Text = textBox.Text.Replace(" ", "");
            if (textBox.Text == "") {
                textBox.Text = min.ToString();
            }

            try {
                if (float.TryParse(textBox.Text, out value) == false) {
                    value = max;
                }

                if (value < min) {
                    value = min;
                    textBox.Text = min.ToString();
                }
                else if (value > max) {
                    value = max;
                    textBox.Text = max.ToString();
                }
                else {
                    textBox.Text = value.ToString();
                }
            }
            catch {
                value = min;
                textBox.Text = min.ToString();
            }

            return value;
        }
    }
}
