using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Com.Artifex.Mupdfdemo;
using Android.Graphics;
using Android.Webkit;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Android.Graphics.Drawables;

namespace InPublishing
{
	public class MuPageView : MuPDFPageView
	{
		private const string TAG = "MuPageView";

		private Documento _documento;
		//private Point _parentSize;
		private ViewerScreen _docView;
		private RelativeLayout _articleView;
		//private RelativeLayout _bgView;
		private Dictionary<string, RelativeLayout> _oggetti;
		public bool OnScreen = false; 
		private ProgressBar mBusyIndicator;

        public bool Loaded = false;

        public Dictionary<string, RelativeLayout> Oggetti
        {
            get { return _oggetti; }
        }

        public MuPageView(Context c, FilePicker.IFilePickerSupport filePickerSupport, MuPDFCore muPdfCore, Point parentSize, Bitmap sharedHqBm, Documento doc, ViewerScreen docView) : base(c, filePickerSupport, muPdfCore, parentSize, sharedHqBm)
		{
			_documento = doc;
			//_parentSize = parentSize;
			_docView = docView;

		}

		public override void SetPage(int page, PointF size)
		{
			try
			{
				base.SetPage(page, size);

                mBusyIndicator = GetBusyIndicator();
                mBusyIndicator.SetBackgroundColor(Color.Transparent);

				if(!_documento.IsPDF)
				{
					SetObjects();
				}
			}
			catch(Exception ex)
			{
				Log.Error(TAG, ex.Message);
			}
		}

		public override void Blank(int page)
		{
            base.Blank(page);

            mBusyIndicator = GetBusyIndicator();
            mBusyIndicator.SetBackgroundColor(Color.Transparent);
		}

		private void SetObjects()
		{
			int page = Page;

			var articolo = _documento.Articoli[page];

			string artPath = System.IO.Path.Combine (_documento.Path, articolo.Path);

			float ratio = 1;

			float ratioW = MSize.X / articolo.Width;
			float ratioH = MSize.Y / articolo.Height;

			if (ratioW < ratioH) 
			{
				ratio = ratioW;
			} 
			else 
			{
				ratio = ratioH;
			}

			if(_articleView == null)
			{
				_articleView = new RelativeLayout(MContext);

				this.AddView(_articleView, new RelativeLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent));
				//_articleView.SetClipToPadding(false);
				_articleView.BringToFront(); 
			}

			Task.Factory
				.StartNew(() => {
					if(Looper.MyLooper() == null)
						Looper.Prepare();

                    _oggetti = Objects.CreateObjects(articolo.Oggetti, artPath, _docView, ratio);
					/*if(page % 10 == 0)
						GC.Collect();*/

					Log.Info(TAG, "Creazione oggetti pag " + page);
				})
				.ContinueWith(task => {
					try
					{
						var oggetti = _oggetti.Values.ToArray();

						for (int i=0; i<oggetti.Length; i++) 
						{
							View view = oggetti[i];
							_docView.RunOnUiThread(() => 
							{
								if(_articleView != null)
								{
									_articleView.AddView(view);
									mBusyIndicator.BringToFront();
								}
							});
						}

						oggetti = null;

						Log.Info(TAG, "Aggiunta oggetti pag " + page);
                        
                        Loaded = true;

						if(OnScreen)
						{
							_docView.RunOnUiThread(() => 
							{
								Autoplay();
							});
						}
					}
					catch(Exception ex)
					{
						Utils.WriteLog(TAG + " -> SetObjects", ex.Message);
					}
				});
		}

		private void moveToBack(View myCurrentView) 
		{
			ViewGroup myViewGroup = ((ViewGroup) myCurrentView.Parent);
			int index = myViewGroup.IndexOfChild(myCurrentView);
			for(int i = 0; i<index; i++)
			{
				myViewGroup.BringChildToFront(myViewGroup.GetChildAt(i));
			}
		}

		public void Autoplay()
		{
			if(_oggetti == null)
				return;

			var oggetti = _oggetti.Values.ToArray();

			for (int j=0; j<oggetti.Length; j++) 
			{
				RelativeLayout view = oggetti[j];

				for(int i = 0; i < view.ChildCount; i++)
				{
					var child = view.GetChildAt(i);

					switch(child.GetType().Name)
					{
						case "AudioView":
							(child as AudioView).Autoplay();
							break;
						case "VideoView":
							(child as VideoView).Autoplay();
							break;
						case "MultistatoView":
							(child as MultistatoView).Autoplay();
							break;
						case "ScrollerView":
							(child as ScrollerView).AutoplayObjecs();
							break;
						case "AnimazioneView":
							(child as AnimazioneView).LoadContents();
							break;
                        case "SliderView":
                            (child as SliderView).Autoplay();
                            break;
						default:
							break;
					}
				}
			}

			oggetti = null;
		}

		public void MoveToScreen()
		{
			//viene chiamato quanto una pagina entra nello schermo
			OnScreen = true;

			Autoplay();
		}

		public void MoveOffScreen()
		{
			//viene chiamato quanto una pagina esce dallo schermo
			OnScreen = false;
            Loaded = true;
			//stoppo l'audio e il video
			if(_oggetti == null)
				return;

			var oggetti = _oggetti.Values.ToArray();

			for(int j=0; j<oggetti.Length; j++)
			{
				RelativeLayout view = oggetti[j];

				for(int i = 0; i < view.ChildCount; i++)
				{
					var child = view.GetChildAt(i);

					switch(child.GetType().Name)
					{
						case "AudioView":
							(child as AudioView).Stop();
							break;
						case "VideoView":
							(child as VideoView).Stop();
							break;
						case "MultistatoView":
							(child as MultistatoView).StopAll();
							break;
						case "ScrollerView":
							(child as ScrollerView).ClosePopUp();
							(child as ScrollerView).PauseObjects();
							break;
						case "AnimazioneView":
							(child as AnimazioneView).StopLoading();
							(child as AnimazioneView).LoadUrl("about:blank");
							break;
                        case "ImageStateView":
                            (child as ImageStateView).SetState(0);
                            break;
                        case "SliderView":
                            (child as SliderView).StopAll();
                            break;
						default:
							break;
					}
				}
			}

			oggetti = null;
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

			if(_oggetti == null || _articleView == null)
				return;

			float scale = (float) Width / (float) MSize.X;

			_articleView.Measure((int)(Width * scale), (int)(Height * scale));
			_articleView.BringToFront();

			/*var w = Resources.DisplayMetrics.WidthPixels;
			var h = Resources.DisplayMetrics.HeightPixels;

			_bgView.Measure((int)(w * scale), (int)(h * scale));
			moveToBack(_bgView);*/
		}

		protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
		{
			base.OnLayout(changed, left, top, right, bottom);

			if(_oggetti == null || _articleView == null)
				return;

			float scale = (float) Width / (float) MSize.X;

			_articleView.Layout(0, 0, right - left, bottom - top);
			_articleView.BringToFront();

			/*_bgView.Layout(-50, -50, right - left, bottom - top);
			moveToBack(_bgView);*/

			if(!changed)
				return;

			var oggetti = _oggetti.Values.ToArray();

			for (int j=0; j<oggetti.Length; j++) 
			{
				ObjView ov = (ObjView)oggetti[j];

				RelativeLayout.LayoutParams lp = new RelativeLayout.LayoutParams((int)((ov.Frame.Right - ov.Frame.Left) * scale), (int)((ov.Frame.Bottom - ov.Frame.Top) * scale));
				lp.LeftMargin = (int)(ov.Frame.Left * scale);
				lp.TopMargin = (int)(ov.Frame.Top * scale);
				/*lp.Width = (int)((ov.Frame.Right - ov.Frame.Left) * scale);
				lp.Height = (int)((ov.Frame.Bottom - ov.Frame.Top) * scale);*/
				ov.LayoutParameters = lp;

				//ov.RequestLayout();
				//ov.Invalidate();

				/*ov.Layout((int)(ov.Frame.Left * scale),
					(int)(ov.Frame.Top * scale),
					(int)(ov.Frame.Right * scale),
					(int)(ov.Frame.Bottom * scale));*/

			}

			oggetti = null;
		}

		/*protected override void OnDraw(Canvas canvas)
		{
			base.OnDraw(canvas);

			if(_oggetti == null || _articleView == null)
				return;

			float scale = (float) Width / (float) MSize.X;

			foreach (View view in _oggetti.Values) 
			{
				ObjView ov = (ObjView)view;

				RelativeLayout.LayoutParams lp = new RelativeLayout.LayoutParams((int)((ov.Frame.Right - ov.Frame.Left) * scale), (int)((ov.Frame.Bottom - ov.Frame.Top) * scale));
				lp.LeftMargin = (int)(ov.Frame.Left * scale);
				lp.TopMargin = (int)(ov.Frame.Top * scale);
				lp.Width = (int)((ov.Frame.Right - ov.Frame.Left) * scale);
				lp.Height = (int)((ov.Frame.Bottom - ov.Frame.Top) * scale);
				ov.LayoutParameters = lp;

			}
		}*/

		/*protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
		{
			base.OnSizeChanged(w, h, oldw, oldh);

			if(_oggetti == null || _articleView == null)
				return;

			float scale = (float) w / (float) MSize.X;

			foreach (View view in _oggetti.Values) 
			{
				ObjView ov = (ObjView)view;

				//RelativeLayout.LayoutParams lp = new RelativeLayout.LayoutParams((int)((ov.Frame.Right - ov.Frame.Left) * scale), (int)((ov.Frame.Bottom - ov.Frame.Top) * scale));
				//lp.LeftMargin = (int)(ov.Frame.Left * scale);
				//lp.TopMargin = (int)(ov.Frame.Top * scale);

				//ov.LayoutParameters = lp;
				ov.BringToFront();
				RelativeLayout.LayoutParams lp = new RelativeLayout.LayoutParams((int)((ov.Frame.Right - ov.Frame.Left) * scale), (int)((ov.Frame.Bottom - ov.Frame.Top) * scale));
				lp.LeftMargin = (int)(ov.Frame.Left * scale);
				lp.TopMargin = (int)(ov.Frame.Top * scale);

				ov.LayoutParameters = lp;

				ov.BringToFront();

				ov.RequestLayout();
				ov.Invalidate();
			}
		}*/

		public override void ReleaseResources()
		{
			//if(Page % 20 == 0)
				//GC.Collect(0);
			
			base.ReleaseResources();

			this.RemoveSubViews(_articleView);

			this.RemoveView(_articleView);

			if(_articleView != null)
			{
				_articleView.Dispose();
			}

			_articleView = null;
			_oggetti = null;
		}

        /*public override void ReleaseBitmaps()
        {
            base.ReleaseBitmaps();


        }*/

		private ProgressBar GetBusyIndicator()
		{
			for(int i = 0; i < ChildCount; i++)
			{
				View v = this.GetChildAt(i);

				if(v.GetType() == typeof(ProgressBar))
				{
					return (ProgressBar)v;
				}
			}

			return null;
		}
			
		private void RemoveSubViews (ViewGroup view)
		{
			if(view == null)
				return;

			var child = view.GetChildAt (0);
			while (null != child) 
			{
				try
				{					 
					switch (child.GetType().Name) 
					{
						case "ImageView":
                        //(subView as UIImageView).RemoveFromSuperview();

                            var imgView = child as ImageView;
    							//(child as ImageView).SetImageDrawable (null);

                            imgView.RecycleBitmap();
							break;
						case "MultistatoView":
							(child as MultistatoView).StopAll();
							break;
							/*case "VideoView":
							VideoView a = (child as VideoView);
							a.Stop();
							break;*/
                        case "VideoView":
                            if(child.GetType() == typeof(VideoView))
                            {
                                //(subView as AudioView).Stop();
                                (child as VideoView).DisposeMedia();
                            }
                            break;
						case "AudioView":
							//(subView as AudioView).Stop();
							(child as AudioView).DisposeMedia();
							break;
						case "BrowserView":
							//(subView as AudioView).Stop();
							/*(child as WebView).StopLoading();
							(child as WebView).LoadUrl("about:blank");*/
							break;
						default:
							break;
					}

					var subChild = child as ViewGroup;
					if (subChild != null) 
					{
						if (subChild.ChildCount > 0) 
						{
							RemoveSubViews (subChild);				
						}
					} 

					view.RemoveView (child);
					child.Dispose ();
					child = null;

					child = view.GetChildAt (0);
				}
				catch(Exception e)
				{
					string message = "Errore rimozione oggetto";

					Utils.WriteLog(message, e.Message);
				}
			}
		}

		/*public class CreateOjbTask : Android.OS.AsyncTask
		{
			private MuPageView _pageview;
			private int _page;
			private ReaderView _readerView;

			public CreateOjbTask(MuPageView pageview, int page, ReaderView readerView) : base()
			{
				this._pageview = pageview;
				this._page = page;
				this._readerView = readerView;
			}

			protected override void OnPreExecute()
			{
			}

			protected override Java.Lang.Object DoInBackground(params Java.Lang.Object[] @params)
			{
				return Core.GetPageSize(index);
			}

			protected override void OnPostExecute(Java.Lang.Object result)
			{
				if(IsCancelled)
				{
					return;
				}

				if (PageView.Page == index)
					PageView.SetPage(index, result as PointF);
			}
		}*/
	}
}

