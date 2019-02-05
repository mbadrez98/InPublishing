using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Android.Webkit;
using System.IO;

namespace InPublishing
{
	class ImgView : WebView
	{
		protected ImgView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{

		}

		public ImgView(Context context, string imgPath):base(context)
		{
			if(!File.Exists(imgPath))
			{
				return;
			}

			/*LoadingWebViewClient webClient = new LoadingWebViewClient();
			this.SetWebViewClient(webClient);

			BitmapFactory.Options options = new BitmapFactory.Options
			{
				InJustDecodeBounds = true
			};

			BitmapFactory.DecodeFile(imgPath, options);
			int imageHeight = options.OutHeight;
			int imageWidth = options.OutWidth;*/

			//this.LayoutParameters = new ViewGroup.LayoutParams(width, height);

			//this.Settings.LoadWithOverviewMode = true;
			//this.Settings.UseWideViewPort = true;
			this.Settings.SetSupportZoom(false);
			this.Settings.BuiltInZoomControls = false;
			this.VerticalScrollBarEnabled = false;
			this.HorizontalScrollBarEnabled = false;

			this.SetBackgroundColor(Android.Graphics.Color.Transparent);

			string pa = System.IO.Path.GetFullPath(imgPath);
			FileInfo fi = new FileInfo(pa);
			//this.LoadUrl("file://" + pa);

			string html = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">";
			html += "<html xmlns=\"http://www.w3.org/1999/xhtml\">";
			html += "<head>";
			html += "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />";
			//html += "<meta name=\"viewport\" content=\"initial-scale=0.5\" />";
			html += "<title>Documento senza titolo</title>";
			html += "<style>";
			html += "html,body {margin:0; padding: 0;}";
			html += "</style>";
			html += "</head>";
			html += "<body>";
			//html += "<div style='text-align: center;-webkit-transform-style: preserve-3d;-moz-transform-style: preserve-3d;transform-style: preserve-3d;'>";
			html += "<img style='width: 100%; height: 100%;' src='" + fi.Name + "'/>";

			//html += "<img style='width: " + imageWidth + "px; height: " + imageHeight + "px;' src='" + fi.Name + "'/>";
			//html += "</div>";
			html += "</body>";
			html += "</html>";

			this.LoadDataWithBaseURL("file:///" + fi.Directory.FullName + "/", html, "text/html", "UTF-8", null);
		}

		public override bool OnTouchEvent (MotionEvent e)
		{
			//_docView.PagingScrollView.ScrollEnabled = false;


			//return e.Action == MotionEventActions.Move; 
			return false;
		}
	}
}