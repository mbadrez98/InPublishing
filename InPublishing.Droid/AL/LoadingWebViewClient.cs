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

namespace InPublishing
{
	class LoadingWebViewClient : WebViewClient
	{
		private Action _onComplete;
		public Action OnComplete
		{
			get
			{
				return _onComplete;
			}
			set
			{
				_onComplete = value;
			}
		} 

		private Action _onStart;
		public Action OnStart
		{
			get
			{
				return _onStart;
			}
			set
			{
				_onStart = value;
			}
		}

        public Func<WebView, string, bool> OverrideUrlLoading;

        private Context _context;

        public LoadingWebViewClient(Context context = null) : base()
		{
            _context = context;
		}

		/*public override bool ShouldOverrideUrlLoading(WebView view, string url)
		{
			//base.OnPageFinished(view, url);

			if(_onComplete != null)
			{
				_onComplete();
			}

			return base.ShouldOverrideUrlLoading(view, url);
		}*/

		public override void OnPageFinished(WebView view, string url)
		{
			base.OnPageFinished(view, url);

			if(_onComplete != null)
			{
				try
				{
					_onComplete();
				}
				catch(Exception ex)
				{
					Utils.WriteLog("webView on complete", ex.Message);
				}
			}
		}

        public override bool ShouldOverrideUrlLoading(WebView view, string url)
        {
            if (_context != null && url.StartsWith("rtsp"))
            {
                Android.Net.Uri uri = Android.Net.Uri.Parse(url);
                Intent intent = new Intent(Intent.ActionView, uri);

                _context.StartActivity(intent);

                return true;
            }

            if (OverrideUrlLoading != null)
                return OverrideUrlLoading(view, url);

            return base.ShouldOverrideUrlLoading(view, url);
        }

		/*public override void OnPageStarted(WebView view, string url, Bitmap favicon)
		{
			//base.OnPageStarted(view, url, favicon);

			if(_onStart != null)
			{
				_onStart();
			}
		}*/
	}
}

