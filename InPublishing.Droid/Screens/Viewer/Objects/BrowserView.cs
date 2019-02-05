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
using System.Collections.Specialized;

namespace InPublishing
{
	class BrowserView : WebView//, ILocationListener
	{
		private Browser _browser;
		private ViewerScreen _docView;

		//private Location mostRecentLocation;
		//private Context _Context ;
		private LoadingOverlay _LoadingOverlay;

		protected BrowserView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{

		}

        public BrowserView(Context context, Browser browser, string path, ViewerScreen docView = null) : base(context)
		{
			_browser = browser;
			_docView = docView;

			//_Context = context;
			//this.FocusableInTouchMode = true;

			//this.Settings.LoadsImagesAutomatically = true;

			//this.Settings.DomStorageEnabled = true;

			/*mWebView.getSettings().setUserAgentString("Mozilla/5.0 (Linux; U; Android 2.0; en-us; Droid Build/ESD20) AppleWebKit/530.17 (KHTML, like Gecko) Version/4.0 Mobile Safari/530.17");
			mWebView.getSettings().setJavaScriptEnabled(true);
			mWebView.getSettings().setPluginsEnabled(true);
			mWebView.getSettings().setPluginState(PluginState.ON_DEMAND);
			mWebView.getSettings().setAllowFileAccess(true);
			mWebView.getSettings().setAppCacheEnabled(true);
			mWebView.getSettings().setJavaScriptCanOpenWindowsAutomatically(true);*/

			//this.Settings.UserAgentString = "Mozilla/5.0 (Linux; U; Android 2.0; en-us; Droid Build/ESD20) AppleWebKit/530.17 (KHTML, like Gecko) Version/4.0 Mobile Safari/530.17";
            //this.SetInitialScale(1);
			this.Settings.JavaScriptEnabled = true;
            this.Settings.DomStorageEnabled = true;
			this.Settings.SetPluginState(WebSettings.PluginState.OnDemand);
			this.Settings.AllowFileAccess = true;
			this.Settings.SetAppCacheEnabled(false);
			
            this.Settings.JavaScriptCanOpenWindowsAutomatically = true;
            this.Settings.DomStorageEnabled = true;
            this.Settings.SetRenderPriority(WebSettings.RenderPriority.High);
			this.SetBackgroundColor(Android.Graphics.Color.Transparent);
			this.ScrollBarStyle = ScrollbarStyles.OutsideOverlay;

            this.Settings.LoadWithOverviewMode = browser.PageFit;
			this.Settings.UseWideViewPort = browser.PageFit;
            this.Settings.BuiltInZoomControls = browser.PageFit;
            this.Settings.DisplayZoomControls = false;
            this.VerticalScrollBarEnabled = !_browser.ScrollDisable;
			this.HorizontalScrollBarEnabled = !_browser.ScrollDisable;

            this.SetWebChromeClient(new WebChromeClient());

            this.Settings.AllowUniversalAccessFromFileURLs = true;

			LoadingWebViewClient webClient = new LoadingWebViewClient(context);

			/*webClient.OnStart = () => {
				this.ShowLoadingOverlay();
			};*/

			webClient.OnComplete = () => 
			{
				this.HideLoadingOverlay();
			};

            webClient.OverrideUrlLoading = HandleUIWebLoaderControl;

            /*webClient.OverrideUrlLoading = (WebView view, string reqUrl) =>
            {
                if (reqUrl.Contains("applink://"))
                {
                    reqUrl = reqUrl.Replace("applink://", "");
                    var reqParts = reqUrl.Split('?');

                    var action = reqParts[0];

                    if(context is ViewerScreen)
                    {
                        var docView = context as ViewerScreen;
                        docView.OpenPopUp(new string[] { "crediti" });
                    }

                    return true;
                }

                return false;
            };*/

			this.SetWebViewClient(webClient);

			string url;		
			bool overlay = true;

			if(this._browser.Tipo == "embed")
			{
				string html = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">";
				html += "<html xmlns=\"http://www.w3.org/1999/xhtml\">";
				html += "<head>";
				html += "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />";
                //html += "<meta name=\"viewport\" content=\"initial-scale=1\">";
				html += "<title>Documento senza titolo</title>";
				html += "<style>";
				html += "html,body {margin:0; padding: 0; height: 100%;}";
				html += "</style>";
				html += "</head>";
				html += "<body>";
				html += this._browser.HTML;
				html += "</body>";
				html += "</html>";

				this.LoadData(html, "text/html", "UTF-8");
			}
			else if(_browser.Tipo == "gif")
			{
				//this.Settings.LoadWithOverviewMode = true;
				//this.Settings.UseWideViewPort = true;

				string pa = Path.GetFullPath(Path.Combine(path, System.Web.HttpUtility.UrlDecode(this._browser.UrlStream)));

				FileInfo fi = new FileInfo(pa);

				string html = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">";
				html += "<html xmlns=\"http://www.w3.org/1999/xhtml\">";
				html += "<head>";
				html += "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />";
				//html += "<meta name=\"viewport\" content=\"initial-scale=0.5\" />";
				html += "<title>Documento senza titolo</title>";
				html += "<style>";
				html += "html,body {margin:0; padding: 0; height: 100%;}";
				html += "</style>";
				html += "</head>";
				html += "<body>";
				html += "<img width='100%' src='" + fi.Name + "'/>";
				html += "</body>";
				html += "</html>";

				this.LoadDataWithBaseURL("file:///" + fi.Directory.FullName + "/", html, "text/html", "UTF-8", null);

				overlay = false;
			}
			else if(_browser.Tipo == "web" && _browser.UrlStream.EndsWith(".mp4"))
			{
				string html = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">";
				html += "<html xmlns=\"http://www.w3.org/1999/xhtml\">";
				html += "<head>";
				html += "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />";
				//html += "<meta name=\"viewport\" content=\"initial-scale=0.5\" />";
				html += "<title>Documento senza titolo</title>";
				html += "<style>";
				html += "html,body {margin:0; padding: 0; height: 100%;}";
				html += "</style>";
				html += "</head>";
				html += "<body>";
				html += "<video controls autoplay width='100%' height='100%' src='" + _browser.UrlStream + "'></video>";
				html += "</body>";
				html += "</html>";

				this.LoadData(html, "text/html", "UTF-8");
			}
			else 
			{
				if (_browser.Tipo == "htm")
				{
					string pa = Path.GetFullPath(Path.Combine(path, System.Web.HttpUtility.UrlDecode(this._browser.UrlStream)));

					url = "file://" + pa;
				}
                else if (this._browser.Tipo == "pdf")
                {

                    string pa = Path.GetFullPath(Path.Combine(path, System.Web.HttpUtility.UrlDecode(this._browser.UrlStream)));

                    if ((int)Android.OS.Build.VERSION.SdkInt < 19)
                    {                       
                        url = "javascript:void(0);";
                    }
                    else
                    {
                        if (_browser.Fullscreen)
                        {
                            url = string.Format("file:///android_asset/pdfjs/web/viewer.html?file={0}#0", pa);
                        }
                        else
                        {
                            this.SetInitialScale(1);
                            url = string.Format("file:///android_asset/pdfjs/web/viewer_nobarre.html?file={0}#0", pa);
                        }
                    }
                }
				else
				{
					if(this._browser.UrlStream.Contains("http://maps"))
					{
					
						//url = "http://maps.google.com/maps?q="+mostRecentLocation.Latitude+","+mostRecentLocation.Longitude;
						//	url = "http://maps.google.com/maps?q=47.404376,8.601478";
						url = this._browser.UrlStream.Replace("&output=embed", "").Replace("&output=svembed", "");
					}
					else
					{
						url = this._browser.UrlStream;
					}

					overlay = true;
				}

				if(this._browser.Autostart)
				{
					this.LoadUrl(url);

					if(overlay)
					{
						this.ShowLoadingOverlay();
					}
				}
			}
		}

		private void ShowLoadingOverlay()
		{
			if (_LoadingOverlay == null) 
			{
				_LoadingOverlay = new LoadingOverlay (this.Context, Context.GetString(Resource.String.gen_loading) + "...");

				this.AddView (_LoadingOverlay);

				this.BringChildToFront(_LoadingOverlay);
			}

			_LoadingOverlay.Show();
		}

		private void HideLoadingOverlay()
		{
			if(_LoadingOverlay != null)
			{
				_LoadingOverlay.Hide();
			}
		}

		public override bool OnTouchEvent (MotionEvent e)
		{
			//_docView.PagingScrollView.ScrollEnabled = false;

			if(_browser.ScrollDisable)
			{
				return e.Action == MotionEventActions.Move;
			}

			return base.OnTouchEvent (e);			 
		}

		public override bool DispatchTouchEvent(MotionEvent e)
		{
			base.DispatchTouchEvent (e);
			return true;
		}

		public void getLocation(){
			/*LocationManager locationManager = (LocationManager) Context.GetSystemService(Context.LocationService);
			Criteria criteria = new Criteria();
			criteria.Accuracy = Accuracy.Fine;
			String provider = locationManager.GetBestProvider(criteria, true);
			// In order to make sure the device is getting the location, request
			// updates.
			locationManager.RequestLocationUpdates(provider, 1, 0, this);
			mostRecentLocation = locationManager.GetLastKnownLocation(provider);*/
		}

        bool HandleUIWebLoaderControl(WebView view, string reqUrl)
        {
            //if (navigationType == UIWebViewNavigationType.LinkClicked)
            {
                Uri uri = new Uri(reqUrl);
                var param = System.Web.HttpUtility.ParseQueryString(uri.Query);

                if (uri.Scheme == "http" || uri.Scheme == "https")
                {
                    if (param["inpPopUp"] != null && (param["inpPopUp"] == "true" || param["inpPopUp"] == "1"))
                    {
                        Browser browser = new Browser();

                        browser.UrlStream = uri.AbsoluteUri;
                        browser.Tipo = "http";
                        browser.PageFit = true;

                        Intent i = new Intent();
                        i.SetClass(Application.Context, typeof(BrowserViewScreen));
                        i.PutExtra("url", uri.OriginalString);
                        i.PutExtra("tipo", "http");
                        i.PutExtra("pageFit", true);
                        i.PutExtra("basePath", "");
                        _docView.StartActivity(i);

                        return true;
                    }
                    if (param["inpExternal"] != null && (param["inpExternal"] == "true" || param["inpExternal"] == "1"))
                    {
                        var intent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(uri.OriginalString));
                        _docView.StartActivity(intent);

                        return true;
                    }
                }
                else if (uri.Scheme == "inpdo")
                {
                    string action = uri.OriginalString.Split(new[] { '?' })[0];

                    action = action.Replace(uri.Scheme + "://", "");

                    doLinkAction(action, param);

                    return true;
                }
            }

            return false;
        }

        private void doLinkAction(string action, NameValueCollection param)
        {
            action = action.ToLower();

            if (action == "gotopub")
            {
                if (param["pub"] == null || param["doc"] == null || param["dir"] == null || param["page"] == null)
                    return;

                PubNav pubNav = new PubNav(param["pub"], param["doc"], param["dir"], param["page"]);

                _docView.NavTo(pubNav);
            }
            else if (action == "pagenav")
            {
                if (param["page"] == null)
                    return;

                switch (param["page"])
                {
                    case "next":
                        _docView.NextPage();
                        break;
                    case "prev":
                        _docView.PreviousPage();
                        break;
                    case "first":
                        _docView.FirstPage();
                        break;
                    case "last":
                        _docView.LastPage();
                        break;
                    case "back":
                        _docView.BackPage();
                        break;
                    default:
                        int page;
                        if (int.TryParse(param["page"], out page))
                        {
                            page -= 1;

                            string docId = "";

                            if (param["doc"] != null)
                                docId = param["doc"];

                            _docView.GoToPage(page.ToString(), docId);
                        }
                        break;
                }
            }
            else if (action == "appnav")
            {
                if (param["to"] != null)
                {
                    string index = "";
                    switch (param["to"].ToLower())
                    {
                        case "edicola":
                            index = "1";
                            break;
                        case "download":
                            index = "2";
                            break;
                        case "impostazioni":
                            index = "0";
                            break;
                        case "ordini":
                            index = "4";
                            break;
                        default:
                            break;
                    }

                    if (index != "")
                    {
                        string dir = "";
                        if (param["dir"] != null)
                        {
                            dir = param["dir"];
                        }

                        _docView.MenuNav(new string[] { index, dir });
                    }
                }
                else if (param["open"] != null)
                {
                    string[] par;
                    if (param["text"] != null)
                    {
                        par = new string[] { param["open"], param["text"] };
                    }
                    else
                    {
                        par = new string[] { param["open"] };
                    }

                    _docView.OpenPopUp(par);
                }
            }
        }
    }
}