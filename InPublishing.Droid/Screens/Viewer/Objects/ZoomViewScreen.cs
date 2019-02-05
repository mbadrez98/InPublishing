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
using Android.Webkit;
using Android.Graphics;
using Newtonsoft.Json;
using Android.Content.PM;
using System.IO;

namespace InPublishing
{
	[Activity (Label = "ZoomViewScreen", Theme = "@style/Blue.Transparent")]			
	public class ZoomViewScreen : BaseModalScreen
	{
		private ZoomSpecifico _zoomSpecifico;
		private string _basePath;

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			this.Window.AddFlags(WindowManagerFlags.Fullscreen);

			_zoomSpecifico = JsonConvert.DeserializeObject<ZoomSpecifico>(Intent.GetStringExtra("zoom")); //(ZoomSpecifico)ActivitiesBringe.GetObject();
			_basePath = Intent.GetStringExtra("path");

			Color col = Color.Transparent.FromHex(_zoomSpecifico.BackgroundColor);
			int alpha = (int)(_zoomSpecifico.BackgroundAlpha * 255);
			this.Window.DecorView.SetBackgroundColor(Color.Argb(alpha, col.R, col.G, col.B));

			string imgPath = System.IO.Path.Combine(_basePath, this._zoomSpecifico.Link);

			BitmapFactory.Options options = new BitmapFactory.Options
			{
				InJustDecodeBounds = true
			};

			// get the size and mime type of the image
			/*BitmapFactory.DecodeFile(imgPath, options);
			int imageHeight = options.OutHeight;
			int imageWidth = options.OutWidth;

			//ImageViewZoom imgView = new ImageViewZoom(Application.Context);
			ScaleImageView imgView = new ScaleImageView(Application.Context);
			imgView.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
			//imgView.SetBackgroundColor(Color.Aqua);

			using (Bitmap bmp = ImageUtility.DecodeSampledBitmapFromFile (imgPath, imageWidth, imageHeight))
			{
				imgView.SetImageBitmap(bmp);
			}

			_contentView.AddView(imgView);*/

			WebView webView = new WebView(this.BaseContext);

			webView.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);

			webView.Settings.LoadWithOverviewMode = true;
			webView.Settings.UseWideViewPort = true;
			webView.Settings.DefaultZoom = WebSettings.ZoomDensity.Far;
			webView.Settings.SetSupportZoom(true);
			webView.Settings.BuiltInZoomControls = true;
			webView.Settings.DisplayZoomControls = false;
			webView.SetBackgroundColor(Android.Graphics.Color.Transparent);
			_contentView.AddView(webView);

			string pa = System.IO.Path.GetFullPath(imgPath);

			//webView.LoadUrl("file://" + pa);

			FileInfo fi = new FileInfo(pa);

			string html = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">";
			html += "<html xmlns=\"http://www.w3.org/1999/xhtml\">";
			html += "<head>";
			html += "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />";
			//html += "<meta name=\"viewport\" content=\"initial-scale=0.5\" />";
			html += "<title>Documento senza titolo</title>";
			html += "<style>";
			html += "html,body {margin:0; padding: 0; height: 100%}";
			html += "</style>";
			html += "</head>";
			html += "<body>";
			//html += "<div style='text-align: center;-webkit-transform-style: preserve-3d;-moz-transform-style: preserve-3d;transform-style: preserve-3d;'>";
            html += "<img style='max-width: 100%; max-height: 100%; position: relative;" +
					"top: 50%; " +
					"-webkit-transform: translateY(-50%); -ms-transform: translateY(-50%);transform: translateY(-50%);" +
					" display: block; margin: 0 auto;" +
					"' src='" + fi.Name + "'/>";

			//html += "<img style='width: " + imageWidth + "px; height: " + imageHeight + "px;' src='" + fi.Name + "'/>";
			//html += "</div>";
			html += "</body>";
			html += "</html>";

			webView.LoadDataWithBaseURL("file:///" + fi.Directory.FullName + "/", html, "text/html", "UTF-8", null);
		}

		public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
		{
			base.OnConfigurationChanged(newConfig);
		}
	}
}