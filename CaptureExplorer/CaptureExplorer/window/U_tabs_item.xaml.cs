using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using CaptureExplorer;

namespace CaptureExplorer.window {
    /// <summary>
    /// U_分頁_item.xaml 的互動邏輯
    /// </summary>
    public partial class U_分頁_item : UserControl {



        U_menu u_menu_用外部程式開啟;
        MainWindow M;

        public U_分頁_item() {
            InitializeComponent();
        }


        public void func_初始化(MainWindow m) {
            this.M = m;
            u_menu_用外部程式開啟 = new U_menu(m);

            this.MouseRightButtonUp += (sender, e) => {
                u_menu_用外部程式開啟.func_open_滑鼠旁邊();
            };


            u_menu_用外部程式開啟.func_add_menu("重新整理", null, () => {
                M.func_重新整理資料夾();
            });
            u_menu_用外部程式開啟.func_add_menu("開啟資料夾位置", null, () => {
                M.func_開啟資料夾();
            });
            u_menu_用外部程式開啟.func_add_menu("複製路徑", null, () => {
                String s = M.func_取得儲存資料夾();           
                try {
                    Clipboard.SetData(DataFormats.UnicodeText, s);
                } catch  {}   
            });

            u_menu_用外部程式開啟.func_add_menu("重新命名", null, () => {
                W_頁籤新增 w = new W_頁籤新增(M);
                w.fun_修改();
                w.Show();
                w.Owner = M;
            });
            u_menu_用外部程式開啟.func_add_menu("刪除資料夾", null, () => {
                M.func_刪除目前資料夾();
            });
       
        }





        public String Text {
            get {
                return tex.Text + "";
            }
            set {
                tex.Text = value;
            }
        }






    




    }
}
