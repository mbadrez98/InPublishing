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
using System.IO;
using Android.Locations;

namespace InPublishing
{
	class AnimazioneView : WebView//, ILocationListener
	{
		private string resPath = "";

		protected AnimazioneView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{

		}

		public AnimazioneView(Context context, Animazione animaz, string path) : base(context)
		{
			this.Settings.UserAgentString = "Mozilla/5.0 (Linux; U; Android 2.0; en-us; Droid Build/ESD20) AppleWebKit/530.17 (KHTML, like Gecko) Version/4.0 Mobile Safari/530.17";
			this.Settings.JavaScriptEnabled = true;
			//this.Settings.PluginsEnabled = true;
			this.Settings.SetPluginState(WebSettings.PluginState.OnDemand);
			this.Settings.AllowFileAccess = true;
			this.Settings.SetAppCacheEnabled(true);
			//this.Settings.JavaScriptCanOpenWindowsAutomatically = true;
			this.SetBackgroundColor(Android.Graphics.Color.Transparent);
			this.ScrollBarStyle = ScrollbarStyles.OutsideOverlay;

			this.Settings.LoadWithOverviewMode = true;
			this.Settings.UseWideViewPort = true;

			this.VerticalScrollBarEnabled = false;
			this.HorizontalScrollBarEnabled = false;

			LoadingWebViewClient webClient = new LoadingWebViewClient();

			this.SetWebViewClient(webClient);

			resPath = Path.GetFullPath(Path.Combine(path, System.Web.HttpUtility.UrlDecode(animaz.UrlStream)));
			/*string pa = Path.GetFullPath(Path.Combine(path, System.Web.HttpUtility.UrlDecode(animaz.UrlStream)));
			string url = "file://" + pa;
			this.LoadUrl(url);*/
		}

		public void LoadContents()
		{
			string url = "file://" + resPath;
			this.LoadUrl(url);
		}

		public override bool OnTouchEvent (MotionEvent e)
		{
			//_docView.PagingScrollView.ScrollEnabled = false;


			//return e.Action == MotionEventActions.Move; 
			return false;
		}

		/*public override bool DispatchTouchEvent(MotionEvent e)
		{

			base.DispatchTouchEvent (e);
			return true;
		}*/
	}
}

