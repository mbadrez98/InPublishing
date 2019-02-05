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
using Android.Graphics;
using Java.Lang;
using System.Drawing;
using Android.Views.Animations;
using System.IO;

namespace InPublishing
{
	public class ScrollerView : TwoDScrollView
	{
		private const string TAG = "ScrollerView";
		private Scroller _Scroller;
		private string _BasePath;
		private ViewerScreen _DocView;
		Dictionary<string, RelativeLayout> _objViews;
		RelativeLayout _contentView;
		ImgView _backgroundView;
		private RectangleF _initFrame;
		//private RectangleF _currentFrame;
		private RectangleF _parentFrame;
		public bool IsPopUp;
		public bool PopUpVisible;
		public bool Esclusivo;
		private Handler _scrollHandler = new Handler();
		private IRunnable _scrollRunnable;
		private bool _Opened;
		private bool _firstRun;
		//private bool _animating = false;
		private float _scale;
		private float _zoom = 1;
		private float _contentX;
		private float _contentY;

        private System.Drawing.SizeF _ContentSize;
        private string _openedDirection = "";

		public bool DropOpened { get { return _Opened; } }

		public ScrollerView(Context context, Scroller scroll, string path, ViewerScreen docView, RectF frame, float scale) : base (context)
		{
			_Scroller = scroll;
			_BasePath = path;
			_DocView = docView;
			_initFrame = new RectangleF(0, 0, frame.Width() + frame.Left, frame.Height() + frame.Top);
			_parentFrame = new RectangleF(frame.Left, frame.Top, frame.Width(), frame.Height());
			_scale = scale;

			this.Esclusivo = !_Scroller.MantieniPopUp;
			this.IsPopUp = _Scroller.PopUp;
			//HorizontalScrollBarEnabled = true;
			//VerticalScrollBarEnabled = true;

			if(!_Scroller.PopUp || (_Scroller.PopUp && _Scroller.Visible))
			{
				LoadContent();
				PopUpVisible = true;
			} 

			this.RequestDisallowInterceptTouchEvent(true);

			this.ScrollEnabled = _Scroller.Scroll;

			//this.SetBackgroundColor(Android.Graphics.Color.Green);
		}

		public ScrollerView(Context context) : base (context)
		{

		}

		protected ScrollerView(IntPtr javaReference, JniHandleOwnership transfer)
			: base(javaReference, transfer)
		{

		}

		public ScrollerView(Context context, IAttributeSet attrs)
			: base(context, attrs)
		{

		}
		public ScrollerView(Context context, IAttributeSet attrs, int defStyle)
			: base(context, attrs, defStyle)
		{

		}      

		private void LoadContent()
		{
			//_DocView.PagingScrollView.ScrollEnabled = false;
			string fileName = "";
            float ratio = _Scroller.Zoom > 0 ? _Scroller.Zoom : 1;

			if(_Scroller.TipoSfondo == SfondoType.PDF)
			{  
				fileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(_Scroller.Sfondo), System.IO.Path.GetFileNameWithoutExtension(_Scroller.Sfondo) + ".png");
			}
			else
			{
				fileName = _Scroller.Sfondo;
			}

			string imgPath = ImageUtility.GetBitmapPath(System.IO.Path.Combine(_BasePath, fileName), _DocView);
			//string imgPath = System.IO.Path.Combine(_BasePath, fileName);

			string name = System.IO.Path.GetFileNameWithoutExtension(imgPath).ToUpper();

			//if(imgPath.ToUpper().Contains("_AND"))
			if(name.EndsWith("_AND"))
			{
                ratio = _Scroller.ZoomAndroid > 0 ? _Scroller.ZoomAndroid : 1;;
			}

			// get the size and mime type of the image
			System.Drawing.Size imgSize = ImageUtility.GetBitmapSize(imgPath);			

            float imageHeight = imgSize.Height;
            float imageWidth = imgSize.Width;

            if (_Scroller.SfondoSize[0] > 0)
            {
                float xScale = _Scroller.SfondoSize[0] / imgSize.Width;
                float yScale = _Scroller.SfondoSize[1] / imgSize.Height;
                float imgScale = System.Math.Min(xScale, yScale);

                imageHeight *= imgScale;
                imageWidth *= imgScale;
            }

			ratio *= _scale;

            _ContentSize = new System.Drawing.SizeF(imageWidth * ratio, imageHeight * ratio);

			_contentView = new RelativeLayout(this.Context);
			_contentView.LayoutParameters = new RelativeLayout.LayoutParams((int)(imageWidth*ratio), (int)(imageHeight*ratio));
			//_contentView.SetBackgroundColor(Android.Graphics.Color.Red);
			_DocView.RunOnUiThread(() => 
			{
				if(_contentView != null)
				{
					_backgroundView = new ImgView(this.Context, imgPath);
					_backgroundView.LayoutParameters = new ViewGroup.LayoutParams((int)(imageWidth*ratio), (int)(imageHeight*ratio));
					_contentView.AddView(_backgroundView, 0);
				}
			});	

			_objViews = Objects.CreateObjects(_Scroller.Oggetti, _BasePath, _DocView, ratio, this);

			foreach (View view in _objViews.Values) 
			{
				_contentView.AddView (view);
				view.BringToFront();
			}

			this.AddView(_contentView);
			_contentView.RequestFocus();

			_contentX = _Scroller.ContentX * ratio;
			_contentY = _Scroller.ContentY * ratio;

            this.ScrollBy((int)_contentX, (int)_contentY);
		}

		public void RemoveContent()
		{
			DisposeObjects();
			this.RemoveView(_contentView);
			_objViews = null;
			//_ContentView.RemoveFromSuperview();
		}

		public void GoAction(ScrollControl control)
		{
			if (control.Action == "scroll")
			{
				StopScroll();
				StartScroll(control.Direction, control.Step);
			}
			else if (control.Action == "margin")
			{
				ScrollToMargin(control.Direction);
				//scrollto
			}
			else if (control.Action == "open")
			{
				ToggleObject(control.Direction);
			}
			else if(control.Action == "switchPopUp")
			{
				TogglePopUp();
			}
		}

		public void ScrollToMargin(string margin)
		{
			System.Drawing.Point offset = new System.Drawing.Point(ComputeHorizontalScrollOffset(), ComputeVerticalScrollOffset());

			switch (margin)
			{
				case "top":
					offset.Y = 0;
					break;
				case "bottom":
                    offset.Y = ComputeVerticalScrollRange() - Height;
					break;
					case "left":
					offset.X = 0;
					break;
				case "right":
                    offset.X = ComputeHorizontalScrollRange() - Width;
					break;
					default:
					break;
			}

			this.SmoothScrollTo(offset.X, offset.Y);
		}

		public void StartScroll(string direction, float step)
		{         
			_scrollHandler = new Handler();
			_scrollRunnable = new Runnable(() => {
				try
				{
					if(direction == "V")
					{
						SmoothScrollBy(0, -(int)step);
					}
					else
					{
						SmoothScrollBy(-(int)step, 0);
					}
				}
				finally
				{
					_scrollHandler.PostDelayed(_scrollRunnable, 550);
				}
			});

			_scrollHandler.PostDelayed(_scrollRunnable, 0);
		}

		public void StopScroll()
		{
			_scrollHandler.RemoveCallbacks(_scrollRunnable);
		}

		public void TogglePopUp()
		{
			if(PopUpVisible)
			{
				ClosePopUp();
			}
			else
			{
				OpenPopUp();
			}
		}

		public void OpenPopUp()
		{
			if(_Scroller.PopUp && !PopUpVisible && this.Parent != null)
			{
				RelativeLayout parent = this.Parent as RelativeLayout;

				//this.Alpha = 0;
				LoadContent();
				AutoplayObjecs();
				parent.Visibility = ViewStates.Visible;
				PopUpVisible = true;

				Animation a = new AlphaAnimation(0.0f, 1.0f);
				a.Duration = 400;
				a.FillAfter = true;

				this.StartAnimation(a);
			}
		}

		public void ClosePopUp()
		{
			if(_Scroller.PopUp && PopUpVisible && this.Parent != null)
			{
				RelativeLayout parent = this.Parent as RelativeLayout;

				Animation a = new AlphaAnimation(1.0f, 0.0f);
				a.Duration = 400;
				a.FillAfter = true;
				a.AnimationEnd += (sender, e) => 
				{
					RemoveContent();
					parent.Visibility = ViewStates.Invisible;
					PopUpVisible = false;
				};

				this.StartAnimation(a);
			}
		}

		public void ToggleObject(string direction)
		{
			ObjView parent = this.Parent as ObjView;
			//parent.SetBackgroundColor(Android.Graphics.Color.Red);
			RectangleF frame = new RectangleF(this.GetX(), this.GetY(), this.Width, this.Height);//this._initFrame;
			RectangleF sFrame = new RectangleF(parent.GetX(), parent.GetY(), parent.Width, parent.Height);
			System.Drawing.Point offset = new System.Drawing.Point(ComputeHorizontalScrollOffset(), ComputeVerticalScrollOffset());

            if (!_Opened)
                _openedDirection = direction;
            else
                direction = _openedDirection;

			switch (direction)
			{
				case "top":
					if(_Opened)
					{
						frame.Height = _initFrame.Height * _zoom;
						sFrame.Height = _initFrame.Height * _zoom;

						sFrame.Y = _parentFrame.Y * _zoom;//_parentFrame.Y;
						_Opened = false;
					}
					else
					{
                        float contentH = ComputeVerticalScrollRange() > 0 ? ComputeVerticalScrollRange() : _ContentSize.Height;

                        frame.Height = sFrame.Height = contentH;	
						sFrame.Y -= contentH - _initFrame.Height;

						_Opened = true;
					}
					break;
					case "bottom":
					if(_Opened)
					{
						frame.Height = _initFrame.Height * _zoom;
						sFrame.Height = _initFrame.Height * _zoom;
						offset.Y = (int)_contentY;
						_Opened = false;
					}
					else
					{
                        float contentH = ComputeVerticalScrollRange() > 0 ? ComputeVerticalScrollRange() : _ContentSize.Height;

						frame.Height = sFrame.Height = contentH;
						offset.Y = (int)_Scroller.ContentY;

						/*RelativeLayout.LayoutParams lp = new RelativeLayout.LayoutParams((int)frame.Width, (int)frame.Height);
						lp.LeftMargin = (int)frame.X;
						lp.TopMargin = (int)frame.Y;
						this.LayoutParameters = lp;*/

						//this.ContentOffset = offset;
						ScrollTo(offset.X, offset.Y);
						offset.Y = 0;
						_Opened = true;
					}
					break;
					case "left":
					if(_Opened)
					{
						frame.Width = _initFrame.Width * _zoom;
						sFrame.Width = _initFrame.Width * _zoom;
						sFrame.X = _parentFrame.X * _zoom;
						_Opened = false;
					}
					else
					{
                        float contentW = ComputeHorizontalScrollRange() > 0 ? ComputeHorizontalScrollRange() : _ContentSize.Width;

						frame.Width = sFrame.Width = contentW;
						sFrame.X -= (contentW - _initFrame.Width);						

						_Opened = true;
					}                   
					break;
					case "right":
					if(_Opened)
					{
						frame.Width = _initFrame.Width * _zoom;
						sFrame.Width = _initFrame.Width * _zoom;
						offset.X = (int)(_contentX * _zoom);

						_Opened = false;
					}
					else
					{
                        float contentW = ComputeHorizontalScrollRange() > 0 ? ComputeHorizontalScrollRange() : _ContentSize.Width;

						frame.Width = sFrame.Width = contentW;
						offset.X = (int)(_Scroller.ContentX * _zoom);						
						offset.X = 0;

						_Opened = true;
					}
					break;
					default:
					break;
			}

			//TransformAnimation animation = new TransformAnimation(parent, sFrame);
			/*TransformAnimation animation = new TransformAnimation(parent, sFrame, p => 
			{
				float deltaOffsetX = offset.X - ComputeHorizontalScrollOffset();
				float deltaOffsetY = offset.Y - ComputeVerticalScrollOffset();

				int offX = (int)(ComputeHorizontalScrollOffset() + deltaOffsetX * (float)p);
				int offY = (int)(ComputeVerticalScrollOffset() + deltaOffsetY * (float)p);

				this.ScrollTo(offX, offY);

				float deltaW = sFrame.Width - parent.Width;
				float deltaH = sFrame.Height - parent.Height;
				float deltaX = sFrame.X - parent.GetX();
				float deltaY = sFrame.Y - parent.GetY();

				int w = (int)(parent.Width + deltaW * ((float)p));
				int h = (int)(parent.Height + deltaH * ((float)p));
				int x = (int)(parent.GetX() + deltaX * ((float)p));
				int y = (int)(parent.GetY() + deltaY * ((float)p));

				RelativeLayout.LayoutParams lp = new RelativeLayout.LayoutParams(w, h);
				lp.LeftMargin = x;
				lp.TopMargin = y;

				parent.LayoutParameters = lp;
			});

			animation.FillAfter = true;
			animation.Duration = 500;
			animation.Interpolator = new AccelerateInterpolator(1);
			parent.Animation = animation;

			_animating = true;
			animation.AnimationEnd += delegate
			{
				parent.Frame = new Rect((int)(sFrame.Left/_zoom), (int)(sFrame.Top/_zoom), (int)(sFrame.Right/_zoom), (int)(sFrame.Bottom/_zoom));
			};

			animation.StartNow ();*/

			RelativeLayout.LayoutParams lp = new RelativeLayout.LayoutParams((int)sFrame.Width, (int)sFrame.Height);
			lp.LeftMargin = (int)sFrame.Left;
			lp.TopMargin = (int)sFrame.Top;

			parent.LayoutParameters = lp;
			parent.Frame = new Rect((int)(sFrame.Left/_zoom), (int)(sFrame.Top/_zoom), (int)(sFrame.Right/_zoom), (int)(sFrame.Bottom/_zoom));

			PostDelayed(() =>
			{
				this.ScrollTo(offset.X, offset.Y);
			}, 1);

		}

		public void AutoplayObjecs()
		{
			if(_objViews == null)
				return;

			foreach(RelativeLayout view in _objViews.Values)
			{
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
						default:
							break;
					}
				}
			}
		}

		public void PauseObjects()
		{
			//stoppo l'audio e il video
			if(_objViews == null)
				return;

			foreach(RelativeLayout view in _objViews.Values)
			{
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
						default:
							break;
					}
				}
			}
		}

		public void DisposeObjects()
		{
			RemoveSubViews(this);
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
							(child as ImageView).SetImageDrawable (null);						
							break;
							case "MultistatoView":
							(child as MultistatoView).StopAll();
							break;
							/*case "VideoView":
							VideoView a = (child as VideoView);
							a.Stop();
							break;*/
							case "AudioView":
							//(subView as AudioView).Stop();
							(child as AudioView).DisposeMedia();
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

					view.RemoveViewInLayout (child);
					child.Dispose ();

					child = view.GetChildAt (0);
				}
				catch(System.Exception e)
				{
					string message = "Errore rimozione oggetto";

					Utils.WriteLog(message, e.Message);
				}
			}
		}

		protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
		{
			base.OnSizeChanged(w, h, oldw, oldh);

			ScrollTo((int)(mScroller.CurrX * _zoom), (int)(mScroller.CurrX * _zoom));
		}

		protected override void OnLayout(bool changed, int l, int t, int r, int b)
		{
			base.OnLayout(changed, l, t, r, b);

			if(!_firstRun)
			{
				this.ScrollTo((int)_Scroller.ContentX, (int)_Scroller.ContentY);
				_firstRun = true;
			}

			if(_objViews == null || _contentView == null || !changed)
				return;

			float scaleX = (float)Width / (float)_initFrame.Width;
			float scaleY = (float) Height / (float) _initFrame.Height;
			//float scale = System.Math.Min(scaleX, scaleY);
			_zoom = System.Math.Min(scaleX, scaleY);
			//_contentView.SetBackgroundColor(Android.Graphics.Color.Red);

			//this.ScrollTo((int)(ScrollX * _zoom), (int)(ScrollY * _zoom));
			var lp = new ViewGroup.LayoutParams(0, 0);
			if(_contentView != null)
			{
				lp = _contentView.LayoutParameters;
				lp.Width = (int)System.Math.Round(_ContentSize.Width * _zoom);
				lp.Height = (int)System.Math.Round(_ContentSize.Height * _zoom);
				_contentView.LayoutParameters = lp;
			}

			if(_backgroundView != null)
			{
				lp = _backgroundView.LayoutParameters;
				lp.Width = (int)System.Math.Round(_ContentSize.Width * _zoom);
				lp.Height = (int)System.Math.Round(_ContentSize.Height * _zoom);
				_backgroundView.LayoutParameters = lp;
			}

			foreach (View view in _objViews.Values) 
			{
				if(view == null)
					continue;

				ObjView ov = (ObjView)view;

				RelativeLayout.LayoutParams rlp = new RelativeLayout.LayoutParams((int)System.Math.Round((ov.Frame.Right - ov.Frame.Left) * _zoom), (int)System.Math.Round((ov.Frame.Bottom - ov.Frame.Top) * _zoom));
				rlp.LeftMargin = (int)System.Math.Round(ov.Frame.Left * _zoom);
				rlp.TopMargin = (int)System.Math.Round(ov.Frame.Top * _zoom);
				rlp.Width = (int)System.Math.Round((ov.Frame.Right - ov.Frame.Left) * _zoom);
				rlp.Height = (int)System.Math.Round((ov.Frame.Bottom - ov.Frame.Top) * _zoom);
				ov.LayoutParameters = rlp;
			}

			//int a = mScroller.CurrX;
			//int scrollDeltaX = ComputeScrollDeltaToGetChildRectOnScreen(new Rect((int)(_contentX * _zoom), (int)(_contentX * _zoom), lp.Width, lp.Height));

			Log.Info(TAG, "Scroll x=" + mScroller.CurrX + " y=" + mScroller.CurrY);

			//SmoothScrollBy(scrollDeltaX, scrollDeltaX);
		}

		float StartX, StartY;		

		public override bool OnTouchEvent (MotionEvent e)
		{
			//_DocView.PagingScrollView.ScrollEnabled = false;

			var view = this.Parent;

			while (view != null)
			{
				if(view.GetType() == typeof(ScrollerView))
				{
					view.RequestDisallowInterceptTouchEvent(true);
					//Console.WriteLine("PRESA");
					break;
				}

				view = view.Parent;
			}

            if ((CanScrollV || CanScrollH) && !(CanScrollV && CanScrollH))
            {
                int IsHorizontal = 1;

                if(CanScrollV)
                {
                    IsHorizontal = -1;
                }

                switch (e.Action)
                {
                    case MotionEventActions.Down:
                        StartX = e.RawX;
                        StartY = e.RawY;
						//this.Parent.RequestDisallowInterceptTouchEvent(true);
						/*if (mScroller.IsFinished)
							_DocView.ReaderView.OnTouchEvent(e);*/
                        break;
                    case MotionEventActions.Move:
                        var deltaX = System.Math.Abs(StartX - e.RawX);
                        var  deltaY = System.Math.Abs(StartY - e.RawY);

                        if (IsHorizontal * deltaX < IsHorizontal * deltaY)
                        {
                            if(mScroller.IsFinished)
                                _DocView.ReaderView.OnTouchEvent(e);
                        }
                        break;
                    case MotionEventActions.Up:
						if (mScroller.IsFinished)
							_DocView.ReaderView.OnTouchEvent(e);
                        break;
                }
            }

            //_DocView.ReaderView.OnTouchEvent(e);

            //se è un popup ed è aperto non faccio passare l'evento agli oggetti sotto
            /*if(IsPopUp && PopUpVisible)
                return true;
            else*/
			    return base.OnTouchEvent (e);
		}

		public override void GetHitRect(Rect outRect)
		{
			base.GetHitRect(outRect);
		}
	}
}