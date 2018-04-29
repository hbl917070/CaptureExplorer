using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Xml;

namespace 截圖 {
  public  class C_setting {


        private String XML_NAME = "data/setting.xml";//儲存的檔名
        private String XML_NAME_預設 = "data/setting-preset.xml";//預設路徑

        public MainWindow M;


        //
        public C_setting (MainWindow m) {
            this.M = m;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public float fun_讀取數字欄位_float (TextBox t, float min, float max) {

            float i = 0;

            t.Text = t.Text.Replace(" ", "");//取代空白（因為textBox有BUG）

            if (t.Text == "")
                t.Text = min + "";

            try {

                bool result = float.TryParse(t.Text.ToString(), out i); //判斷是否符合int，並且把值丟給 i 

                if (result == false) {
                    i = max;
                }

                if (i < min) {//太小
                    i = min;
                    t.Text = min + "";
                } else if (i > max) {//太大
                    i = max;
                    t.Text = max + "";
                } else {//一般
                    t.Text = i + "";
                }

            } catch {
                i = min;
                t.Text = min + "";
            }

            return i;
        }




        /// <summary>
        /// 
        /// </summary>
        public void fun_儲存設定 () {

            var fun_儲存 = new Action<XmlTextWriter, String, String>((XmlTextWriter XTW, String key, String value) => {
                XTW.WriteStartElement("item");
                XTW.WriteAttributeString("name", key);
                XTW.WriteString(value);
                XTW.WriteEndElement();
            });

            XmlTextWriter X = new XmlTextWriter(@XML_NAME, Encoding.UTF8);


            X.WriteStartDocument();//使用1.0版本
            X.Formatting = Formatting.Indented;//自動縮排
            X.Indentation = 2;//縮排距離

            X.WriteStartElement("settings");

            //


            fun_儲存(X, M.checkBox_自動存入剪貼簿.Name, M.checkBox_自動存入剪貼簿.IsChecked.Value.ToString());
            fun_儲存(X, M.checkBox_視窗置頂.Name, M.checkBox_視窗置頂.IsChecked.Value.ToString());

            fun_儲存(X, "s_快速鍵", M.s_快速鍵);




            //
            /*if (m.radio_左.IsChecked.Value) {
                fun_儲存(X, "radio_text_alignment", "l");
            } else if (m.radio_中.IsChecked.Value) {
                fun_儲存(X, "radio_text_alignment", "c");
            } else if (m.radio_右.IsChecked.Value) {
                fun_儲存(X, "radio_text_alignment", "r");
            }*/
            //

            X.WriteEndElement();

            X.Flush();     //寫這行才會寫入檔案
            X.Close();

        }







        /// <summary>
        /// 開啟程式時呼叫(0=讀取設定檔、1=恢復所有設定)
        /// </summary>
        public void fun_開啟程式時讀取上次設定 (int ss) {


            try {

                XmlDocument XmlDoc = new XmlDocument();

                if (ss == 0)
                    XmlDoc.Load(@XML_NAME);
                else if (ss == 1)
                    XmlDoc.Load(@XML_NAME_預設);

                XmlNodeList NodeLists = XmlDoc.SelectNodes("settings/item");

                foreach (XmlNode item in NodeLists) {


                    fun_讀取項目(item, M.checkBox_自動存入剪貼簿);
                    fun_讀取項目(item, M.checkBox_視窗置頂);


                    if (item.Attributes["name"].Value.Equals("s_快速鍵"))
                        M.s_快速鍵 = item.InnerText;


                }//for

            } catch { }


        }


        /// <summary>
        /// 讀取某一個項目呼叫的事件
        /// </summary>
        private void fun_讀取項目 (XmlNode item, Object obj) {

            if (obj is CheckBox) {
                if (item.Attributes["name"].Value.Equals(((CheckBox)obj).Name))
                    ((CheckBox)obj).IsChecked = item.InnerText.Equals((true).ToString());
            } else if (obj is TextBox) {
                if (item.Attributes["name"].Value.Equals(((TextBox)obj).Name))
                    ((TextBox)obj).Text = item.InnerText;
            }

        }


    }
}
