using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MediaPortal.GUI.Library;
using MediaPortal.Dialogs;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace APODPlugin
{
    public class APODPlugin : GUIWindow, ISetupForm
    {
        [SkinControlAttribute(3)]
        protected GUIImage bgimage = null;
        [SkinControlAttribute(0)]
        protected GUIImage infobgimage = null;
        [SkinControlAttribute(4)]
        protected GUIImage image = null;
        [SkinControlAttribute(5)]
        protected GUITextScrollUpControl info = null;
        [SkinControlAttribute(6)]
        protected GUITextControl title = null;
        int imageStep = 0;
        APODDownloader downloader;
        bool downloading = true;
        System.Timers.Timer oneShotTimer;

        public APODPlugin()
        {
        }

        #region ISetupForm Members

        // Returns the name of the plugin which is shown in the plugin menu
        public string PluginName()
        {
            return "APOD";
        }

        // Returns the description of the plugin is shown in the plugin menu
        public string Description()
        {
            return "Astronomy Picture of the day";
        }

        // Returns the author of the plugin which is shown in the plugin menu
        public string Author()
        {
            return "jmbillings";
        }

        // show the setup dialog
        public void ShowPlugin()
        {
            MessageBox.Show("Nothing to see here.");
        }

        // Indicates whether plugin can be enabled/disabled
        public bool CanEnable()
        {
            return true;
        }

        // Get Windows-ID
        public int GetWindowId()
        {
            // WindowID of windowplugin belonging to this setup
            // enter your own unique code
            return 3355;
        }

        // Indicates if plugin is enabled by default;
        public bool DefaultEnabled()
        {
            return true;
        }

        // indicates if a plugin has it's own setup screen
        public bool HasSetup()
        {
            return true;
        }

        /// <summary>
        /// If the plugin should have it's own button on the main menu of MediaPortal then it
        /// should return true to this method, otherwise if it should not be on home
        /// it should return false
        /// </summary>
        /// <param name="strButtonText">text the button should have</param>
        /// <param name="strButtonImage">image for the button, or empty for default</param>
        /// <param name="strButtonImageFocus">image for the button, or empty for default</param>
        /// <param name="strPictureImage">subpicture for the button or empty for none</param>
        /// <returns>true : plugin needs it's own button on home
        /// false : plugin does not need it's own button on home</returns>
        public bool GetHome(out string strButtonText, out string strButtonImage,
            out string strButtonImageFocus, out string strPictureImage)
        {
            strButtonText = PluginName();
            strButtonImage = String.Empty;
            strButtonImageFocus = String.Empty;
            strPictureImage = String.Empty;
            return true;
        }

        // With GetID it will be an window-plugin / otherwise a process-plugin
        // Enter the id number here again
        public override int GetID
        {
            get
            {
                return 3355;
            }
            set { }
        }

        #endregion

        public override bool Init()
        {
            bool ls = Load(GUIGraphicsContext.Skin + @"\APODPlugin.xml");
            return ls;
        }

        public override void DeInit()
        {
            base.DeInit();
        }

        protected override void OnPageDestroy(int newWindowId)
        {
            downloader.onDownloadError -= downloader_onDownloadError;
            downloader.onDownloadComplete -= downloader_onDownloadComplete;
            downloader.onDownloadNoImageFound -= downloader_onDownloadNoImageFound;
            downloader.Dispose();
            oneShotTimer.Elapsed -= oneShotTimer_Elapsed;
            imageStep = 0;
            GUIWaitCursor.Hide();
            base.OnPageDestroy(newWindowId);
        }

        protected override void OnPageLoad()
        {   
            GUIWaitCursor.Show();
            downloader = new APODDownloader();   
            downloader.onDownloadError += downloader_onDownloadError;
            downloader.onDownloadComplete += downloader_onDownloadComplete;
            downloader.onDownloadNoImageFound += downloader_onDownloadNoImageFound;
            oneShotTimer = new System.Timers.Timer(128);
            oneShotTimer.Elapsed += oneShotTimer_Elapsed;
            oneShotTimer.Start();
        }

        void oneShotTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            oneShotTimer.Stop();
            //System.Threading.Thread.Sleep(5000);
            //downloader.GetImage(imageStep, directions.LEFT);
        }

        public override void OnAction(MediaPortal.GUI.Library.Action action)
        {
            base.OnAction(action);
            if (downloading) return;

            if (action.wID == MediaPortal.GUI.Library.Action.ActionType.ACTION_MOVE_LEFT)
            {
                doDownload(directions.LEFT);
            }
            else if (action.wID == MediaPortal.GUI.Library.Action.ActionType.ACTION_MOVE_RIGHT)
            {
                doDownload(directions.RIGHT);
            }
            else if (action.wID == MediaPortal.GUI.Library.Action.ActionType.ACTION_SHOW_INFO)
            {
                if (infobgimage.Visible)
                {
                    infobgimage.Visible = false;
                    info.Visible = false;
                }
                else
                {
                    infobgimage.Visible = true;
                    info.Visible = true;
                }
            }
        }

        private void doDownload(directions d)
        {
            if (d == directions.LEFT)
            {
                //User pressed Left
                GUIWaitCursor.Show();
                downloading = true;
                imageStep = imageStep + 1;
                if (!downloader.GetImage(imageStep, directions.LEFT))
                {
                    GUIDialogOK dlgOK = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
                    if (dlgOK != null)
                    {
                        dlgOK.SetHeading("Info");
                        dlgOK.SetLine(1, "This is the first image, no more to see!");
                        dlgOK.DoModal(this.GetWindowId());
                    }
                    downloading = false;
                    imageStep = imageStep - 1; //set counter back to where we were
                }
            }
            else if (d == directions.RIGHT)
            {
                //User pressed right
                if (imageStep == 0)
                {
                    GUIDialogOK dlgOK = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
                    if (dlgOK != null)
                    {
                        dlgOK.SetHeading("Info");
                        dlgOK.SetLine(1, "This is the most recent image!");
                        dlgOK.DoModal(this.GetWindowId());
                    }
                    downloading = false;
                    return;
                }
                GUIWaitCursor.Show();
                downloading = true;
                imageStep = imageStep - 1;
                downloader.GetImage(imageStep, directions.RIGHT);
            }
        }

        void downloader_onDownloadNoImageFound(object sender, downloadNoImageEventArgs e)
        {
            //happens when no image found; skip next back/forwards in series.
            GUIWaitCursor.Hide();
            doDownload(e.scrollDirection);
        }

        void downloader_onDownloadComplete(object sender, downloadCompleteEventArgs e)
        {
            //set infotext
            info.Label = e.infostring;

            //set title
            title.Label = e.titlestring;

            //size and set image
            Rectangle screenRect = Screen.PrimaryScreen.Bounds;
            Bitmap resizedBitmap = new Bitmap(screenRect.Width, screenRect.Height);
            Bitmap originalBitmap = (Bitmap)e.bitmap;

            Brush brush = new SolidBrush(Color.Black);
            float scale = Math.Min((float)screenRect.Width / (float)originalBitmap.Width, (float)screenRect.Height / (float)originalBitmap.Height);

            Graphics graph = Graphics.FromImage(resizedBitmap);
            graph.InterpolationMode = InterpolationMode.High;
            graph.CompositingQuality = CompositingQuality.HighQuality;
            graph.SmoothingMode = SmoothingMode.AntiAlias;

            int scaleWidth = (int)(originalBitmap.Width * scale);
            int scaleHeight = (int)(originalBitmap.Height * scale);

            graph.FillRectangle(brush, new RectangleF(0, 0, screenRect.Width, screenRect.Height));
            graph.DrawImage(originalBitmap, new Rectangle(((int)screenRect.Width - scaleWidth) / 2, ((int)screenRect.Height - scaleHeight) / 2, scaleWidth, scaleHeight));
            resizedBitmap.Save(System.IO.Path.GetTempPath() + "\\apod" + imageStep.ToString() + ".jpg");
            image.SetFileName(System.IO.Path.GetTempPath() + "\\apod" + imageStep.ToString() + ".jpg");
            image.Refresh();
            downloading = false;
            GUIWaitCursor.Hide();
        }

        void downloader_onDownloadError(object sender, downloadErrorEventArgs e)
        {
            GUIWaitCursor.Hide();
            GUIDialogOK dlgOK = (GUIDialogOK)GUIWindowManager.GetWindow((int)GUIWindow.Window.WINDOW_DIALOG_OK);
            if (dlgOK != null)
            {
                dlgOK.SetHeading("Error");
                dlgOK.SetLine(1, "An exception occurred during download:");
                dlgOK.SetLine(2, e.downloadException.Message);
                dlgOK.DoModal(this.GetWindowId());
            }
        }
    }
}