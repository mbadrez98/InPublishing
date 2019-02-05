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

namespace InPublishing
{
	[Activity (Label = "BrowserViewActivity", Theme = "@style/Blue.NoActionBar")]			
	public class BrowserViewScreen : BaseModalScreen
	{
		private Browser _browser;
		private LoadingOverlay _LoadingOverlay;
		WebView _webView;

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);
			this.Window.AddFlags(WindowManagerFlags.Fullscreen);

			string basePath = Intent.GetStringExtra("basePath");

			_browser = new Browser();
			_browser.Tipo = Intent.GetStringExtra("tipo");
			_browser.PageFit = true;//(bool)Intent.GetStringExtra("pagefit");
			_browser.UrlStream = Intent.GetStringExtra("url");
			_browser.Autostart = true;

			BrowserView webView = new BrowserView(this, _browser, basePath);
			RelativeLayout.LayoutParams param = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.MatchParent, RelativeLayout.LayoutParams.MatchParent);

			_contentView.AddView(webView, param);

			/*_url = Intent.GetStringExtra("url");

			_webView = new WebView(Application.Context);

			RelativeLayout.LayoutParams param = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.MatchParent, RelativeLayout.LayoutParams.MatchParent);

			_contentView.AddView(_webView, param);

			/*_webView.FocusableInTouchMode = true;
			_webView.Settings.JavaScriptEnabled = true;
			_webView.Settings.LoadsImagesAutomatically = true;
			//_webView.Settings.PluginsEnabled = true;
			_webView.Settings.DomStorageEnabled = true;
			_webView.Settings.LoadWithOverviewMode = true;
			_webView.Settings.UseWideViewPort = true;
			_webView.Settings.SetSupportZoom(true);
			_webView.Settings.BuiltInZoomControls = true;
			_webView.Settings.DisplayZoomControls = false;
			_webView.Settings.DefaultZoom = WebSettings.ZoomDensity.Far;

			LoadingWebViewClient webClient = new LoadingWebViewClient();
			webClient.OnComplete = () => {
				this.HideLoadingOverlay();
			};

			_webView.SetWebViewClient(webClient);

			_webView.LoadUrl(_url);

			ShowLoadingOverlay();*/
		}

		private void ShowLoadingOverlay()
		{
			if (_LoadingOverlay == null) 
			{
				_LoadingOverlay = new LoadingOverlay (Application.Context, GetString(Resource.String.gen_loading) + "...");
				_contentView.AddView (_LoadingOverlay);

				//this.BringChildToFront(_LoadingOverlay);
			}

			_LoadingOverlay.Show();
		}

		private void HideLoadingOverlay()
		{
			if (_LoadingOverlay != null)
				_LoadingOverlay.Hide ();

			//_LoadingOverlay = null;
		}

		protected override void OnDestroy()
		{
			if(_webView != null)
			{
				_webView.StopLoading();
				_webView.LoadUrl("about:blank");
			}

			base.OnDestroy();
		}
	}
}

