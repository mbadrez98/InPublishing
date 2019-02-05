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
using Android.Content.PM;
using Android.Graphics;

namespace InPublishing
{
	class VideoEmbedView : WebView//, ILocationListener
	{
        public VideoEmbedView(ViewerScreen docView, VideoEmbed video, string path) : base(docView)
		{
			this.Settings.JavaScriptEnabled = true;
            this.Settings.JavaScriptEnabled = true;
            this.Settings.UseWideViewPort = true;
            this.Settings.LoadWithOverviewMode = true;
            this.Settings.JavaScriptCanOpenWindowsAutomatically = true;
            this.Settings.DomStorageEnabled = true;
            this.Settings.SetRenderPriority(WebSettings.RenderPriority.High);
            this.Settings.BuiltInZoomControls = false;
            this.Settings.AllowFileAccess = true;
            this.Settings.SetPluginState(WebSettings.PluginState.On);
            this.SetInitialScale(1);

            this.SetWebChromeClient(new MyWebClient(docView));

			/*string html = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">";
            html += "<html xmlns=\"http://www.w3.org/1999/xhtml\">";
            html += "<head>";
            html += "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />";
            html += "<title>Documento senza titolo</title>";
            html += "<style>";
            html += "html,body {margin:0; padding: 0; height: 100%;}";
            html += "</style>";
            html += "</head>";
            html += "<body>";
            html += "<iframe width='" + "100%" + "' height='" + "100%" + "' src='https://www.youtube.com/embed/" + video.YoutubeID + "' frameborder='0' allowfullscreen></iframe>";
            html += "</body>";
            html += "</html>";

            //this.LoadData(html, "text/html", "UTF-8");*/

            if (video.YoutubeID != "")
            {
                this.LoadUrl("https://www.youtube.com/embed/" + video.YoutubeID + "?rel=0");
            }
            else if (video.VimeoID != "")
            {
                this.LoadUrl("https://player.vimeo.com/video/" + video.VimeoID);
            }


            //this.LoadUrl("https://www.youtube.com/embed/" + video.YoutubeID);
            //this.LoadUrl("https://player.vimeo.com/video/122786580");
        }

		

		public override bool OnTouchEvent (MotionEvent e)
		{
			//_docView.PagingScrollView.ScrollEnabled = false;

			/*if(_browser.ScrollDisable)
			{
				return e.Action == MotionEventActions.Move;
			}*/

			return base.OnTouchEvent (e);			 
		}

        public class MyWebClient : WebChromeClient
        {
            private View _customView;
            private WebChromeClient.ICustomViewCallback _customViewCallback;
            private ScreenOrientation _originalOrientation;
            private StatusBarVisibility _originalSystemUiVisibility;
            private ViewerScreen _docView;

            public MyWebClient(ViewerScreen docView)
            {
                _docView = docView;
            }

            public override void OnHideCustomView()
            {
                base.OnHideCustomView();

                (((FrameLayout)_docView.Window.DecorView)).RemoveView(_customView);
                _customView = null;
                _docView.Window.DecorView.SystemUiVisibility = _originalSystemUiVisibility;
                _docView.RequestedOrientation = _originalOrientation;
                _customViewCallback.OnCustomViewHidden();
                _customViewCallback = null;
            }

            public override void OnShowCustomView(View view, WebChromeClient.ICustomViewCallback callback)
            {
                base.OnShowCustomView(view, callback);

                /*if (this.mCustomView != null)
                {
                    onHideCustomView();
                    return;
                }
                this.mCustomView = paramView;
                this.mOriginalSystemUiVisibility = MainActivity.this.getWindow().getDecorView().getSystemUiVisibility();
                this.mOriginalOrientation = MainActivity.this.getRequestedOrientation();
                this.mCustomViewCallback = paramCustomViewCallback;
                ((FrameLayout)MainActivity.this.getWindow().getDecorView()).addView(this.mCustomView, new FrameLayout.LayoutParams(-1, -1));
                MainActivity.this.getWindow().getDecorView().setSystemUiVisibility(3846);*/



                if(_customView != null)
                {
                    OnHideCustomView();
                    return;
                }

                _customView = view;

                _customView.SetBackgroundColor(Color.Black);

                _originalSystemUiVisibility = _docView.Window.DecorView.SystemUiVisibility;
                _originalOrientation = _docView.RequestedOrientation;
                _customViewCallback = callback;
                ((FrameLayout)_docView.Window.DecorView).AddView(_customView, new FrameLayout.LayoutParams(-1, -1));
                _docView.Window.DecorView.Visibility = ViewStates.Visible;
            }
        }

		
	}
}

