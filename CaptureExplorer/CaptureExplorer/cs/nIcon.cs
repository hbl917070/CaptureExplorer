using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Resources;
using CaptureExplorer;

namespace CaptureExplorer {
    public class C_右下角圖示 {


        private System.Windows.Forms.NotifyIcon nIcon = new System.Windows.Forms.NotifyIcon();
        private MainWindow M;

        /// <summary>
        /// 
        /// </summary>
        public C_右下角圖示(MainWindow m) {

            this.M = m;

            var uri = new Uri("imgs/ICON.ico", UriKind.RelativeOrAbsolute);
            StreamResourceInfo sri = Application.GetResourceStream(uri);
            nIcon.Icon = new System.Drawing.Icon(sri.Stream);

            nIcon.Text = "Capture Explorer";
            nIcon.Visible = true;




            nIcon.DoubleClick += (sender2, e2) => {

                M.func_縮小至右下角(false);

            };





            var cm = new System.Windows.Forms.ContextMenu();

            cm.MenuItems.Add("show", new EventHandler((sender2, e2) => {
                M.func_縮小至右下角(false);
            }));


            cm.MenuItems.Add("Screenshot", new EventHandler((sender2, e2) => {
                M.func_截圖();
            }));


            /*cm.MenuItems.Add("", new EventHandler((sender2, e2) => {

            

            }));*/




            nIcon.ContextMenu = cm;


            /*nIcon.BalloonTipTitle = "ttt";
            nIcon.BalloonTipText = "xxx";
            nIcon.ShowBalloonTip(1);*/


            //nIcon.ShowBalloonTip(3000, "Hi", "This is a BallonTip from Windows Notification", System.Windows.Forms.ToolTipIcon.Warning);

            //new Form1().Show();
        }



        /// <summary>
        /// 
        /// </summary>
        public void func_顯示() {
            nIcon.Visible = true;
        }

        /// <summary>
        /// 
        /// </summary>
        public void func_隱藏() {
            nIcon.Visible = false;
        }



    }

}
