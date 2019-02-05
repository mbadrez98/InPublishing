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
using Android.Graphics.Drawables;
using Android.Views.Animations;
using Java.Lang;
using Newtonsoft.Json;

namespace InPublishing
{
	public class MultistatoView : RelativeLayout, ViewSwitcher.IViewFactory
	{
		private Multistato _multistato;
		private string _basePath;
		private ViewerScreen _docView;
		private RectF _frame;

		private int _currentIndex = 0;
		private int _partialLoop = 0;
		GestureDetector _gestureDetector;
		ImageSwitcher _imgSwitcher;
		//private NSTimer _loopTimer = null;
		private bool _isRunning = false;
		private Handler _changeHandler = new Handler();
		private IRunnable _changeRunnable;

		public Multistato Multi {get{return _multistato;}}

		public MultistatoView(Context context, Multistato multi, string path, ViewerScreen doc, RectF frame) : base (context)
		{
			_multistato = multi;
			_basePath = path;
			_docView = doc;
			_frame = frame;

			LoadMultistato();
		}

		private void LoadMultistato()
		{
			if (_multistato.Stati.Count() == 0)
			{
				return;
			}

			_imgSwitcher = new ImageSwitcher(this.Context);
			_imgSwitcher.SetFactory(this);

			string imgPath = System.IO.Path.Combine(_basePath, _multistato.Stati[0].Path);

            MBImageLoader.DisplayDiskImage(imgPath, _imgSwitcher, new PointF(_frame.Width(), _frame.Height()));

            this.AddView(_imgSwitcher, new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.MatchParent, RelativeLayout.LayoutParams.MatchParent));

			//transizione
			this.SetAnimation();

			//scorrimento temporizzato
			_changeHandler = new Handler(Looper.MainLooper);
			_changeRunnable = new Runnable(() => {
				try
				{
					this.NextState();
				}
				finally
				{
					_changeHandler.PostDelayed(_changeRunnable, _multistato.Delay);
				}
			});

			//gesture
			if(_multistato.Swipe || _multistato.SwipeContinuo || _multistato.ZoomEnabled)
			{
				_gestureDetector = new GestureDetector(this.Context, new GestureListener(this));
			}

			//loop
			this._partialLoop = _multistato.Loop;

			//autoplay
			/*if(_multistato.Autoplay)
			{
				this.Start();
			}*/

			this.SetOnClick();
		}

		private void SetOnClick()
		{
			//playstopclick
			if(_multistato.PlayStopClick)
			{
				_imgSwitcher.Click += (sender, e) => 
				{
					if (this._isRunning)
					{
						this.Stop();
                        AnalyticsService.SendEvent(_docView.Pubblicazione.Titolo, AnalyticsEventAction.MultistatoStop, _multistato.AnalyticsName);
					}
					else
					{
						this.Start(true);
                        AnalyticsService.SendEvent(_docView.Pubblicazione.Titolo, AnalyticsEventAction.MultistatoPlay, _multistato.AnalyticsName);
					}
				};

			}
		}

		public void Autoplay()
		{
			if(_multistato.Autoplay)
			{
				this.Start();
			}
		}

		private void SetAnimation()
		{
			string inAnim = _multistato.Transition;
			string outAnim = _multistato.Transition;
			string direction = _multistato.TransitionDirection;
			
			switch(inAnim)
			{
				case "push":
				case "moveIn":
					inAnim = outAnim = "slide";
					break;
				case "fade":
					direction = "";
					break;
				/*case "push":
					break;*/
				default:
					direction = "";
					inAnim = outAnim = "fade";
					break;
			}

			inAnim += "_in";
			outAnim += "_out";

			if(direction != "")
			{
				switch(direction)
				{
					case "right":
						inAnim += "_left";
						outAnim += "_right";
						break;
					case "top":
						inAnim += "_bottom";
						outAnim += "_top";
						break;
					case "bottom":
						inAnim += "_top";
						outAnim += "_bottom";
						break;
					case "left":
					default:
						inAnim += "_right";
						outAnim += "_left";
						break;
				}
			}

			int inRes = Resources.GetIdentifier(inAnim, "anim", this.Context.PackageName);
			int outRes = Resources.GetIdentifier(outAnim, "anim", this.Context.PackageName);

            Animation anIn = AnimationUtils.LoadAnimation(_docView, inRes);
            anIn.Duration = _multistato.TransitionDuration;

            Animation anOut = AnimationUtils.LoadAnimation(_docView, outRes);
			anOut.Duration = _multistato.TransitionDuration;

            /*_imgSwitcher.SetInAnimation(this.Context, inRes);
			_imgSwitcher.SetOutAnimation(this.Context, outRes);	*/

            _imgSwitcher.InAnimation = anIn;
            _imgSwitcher.OutAnimation = anOut;
		}

		public View MakeView()
		{
			ImageView imageView = new ImageView(this.Context);
			imageView.SetScaleType(ImageView.ScaleType.FitCenter);
			imageView.LayoutParameters = new ImageSwitcher.LayoutParams(LayoutParams.MatchParent,LayoutParams.MatchParent);
			return imageView;
		}

		public void NextState()
		{
			int target;

			if(_currentIndex == _multistato.Stati.Count() - 1)
			{
				if(_multistato.AutoRewind)
				{
					target = 0;

					_partialLoop--;

					if(_multistato.Loop != -1 && _partialLoop <= 0)
					{
						_changeHandler.RemoveCallbacks(_changeRunnable);

						_partialLoop = 0;
					}
				}
				else
				{
					return;
				}
			}
			else
			{
				target = _currentIndex + 1;
			}

			GoToState(target);			
		}

		public void PrevState()
		{
			if(_currentIndex == 0)
			{
				if(_multistato.AutoRewind)
				{
					GoToState(_multistato.Stati.Count() - 1);
				}
				else
				{
					return;
				}
			}
			else
			{
				GoToState(_currentIndex - 1);
			}
		}

		public void Start(bool immediate = false)
		{
			if(immediate)
			{
				_changeHandler.PostDelayed(_changeRunnable, 0);
			}
			else
			{
				_changeHandler.PostDelayed(_changeRunnable, _multistato.Delay);
			}

			this._isRunning = true;
		}

		public void Stop()
		{
			_changeHandler.RemoveCallbacks(_changeRunnable);

			this._isRunning = false;
		}

		public void GoToState(int index)
		{
			if (_imgSwitcher == null || index == _currentIndex)
			{
				return;
			}
			
			string imgPath = System.IO.Path.Combine(_basePath, _multistato.Stati[index].Path);

            MBImageLoader.DisplayDiskImage(imgPath, _imgSwitcher, new PointF(_frame.Width(), _frame.Height()));


            _currentIndex = index;
		}

		public void GoToState(string key)
		{
			var stati = (from x in _multistato.Stati
				where x.Name == key
				select x);

			if(stati.Count() == 0)
				return;

			var stato = stati.First();

			int index = _multistato.Stati.IndexOf(stato);

			this.GoToState(index);
		}

		public void StopAll()
		{
			this.Stop();
		}

		public void OpenZoom()
		{
			if(!_multistato.PlayStopClick && _multistato.ZoomEnabled) //zoom specifico
			{
				ZoomSpecifico zoom = new ZoomSpecifico();
				zoom.Link = _multistato.Stati[_currentIndex].ZoomPath;
				zoom.BackgroundColor = _multistato.ZoomBackgroundColor;
				zoom.BackgroundAlpha = _multistato.ZoomBackgroundAlpha;
				zoom.Width = _multistato.ZoomWidth;
				zoom.Height = _multistato.ZoomHeight;
				zoom.Zoom = _multistato.ZoomMax;

				Intent i = new Intent();
				i.SetClass(Application.Context, typeof(ZoomViewScreen));

				i.PutExtra("path", _basePath);
				i.PutExtra("zoom", JsonConvert.SerializeObject(zoom));				

				_docView.StartActivity(i);

				_docView.OverridePendingTransition(Resource.Animation.grow_fade_in_center, Resource.Animation.fade_out);

                AnalyticsService.SendEvent(_docView.Pubblicazione.Titolo, AnalyticsEventAction.MultistatoZoom, _multistato.AnalyticsName);
			}
		}

        float StartX, StartY;
		public override bool OnTouchEvent(MotionEvent e)
		{
            if (_gestureDetector == null)
                return false;

			_docView.ReaderView.RequestDisallowInterceptTouchEvent(true);

			var parent = Parent;

			int n = 0;
			bool check = false;
			while (n < 6)
			{
				if(parent.GetType() == typeof(ScrollerView))
				{
					check = true;
					break;
				}

				parent = parent.Parent;
				n++;
			}

			if(check)
			{
				parent = parent as ScrollerView;
			}
			else
			{
				parent = null;
			}

			if(parent != null)
			{
				parent.RequestDisallowInterceptTouchEvent(true);
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
					var deltaY = System.Math.Abs(StartY - e.RawY);

					if (deltaX < deltaY)
					{
						_docView.ReaderView.OnTouchEvent(e);
					}
					break;
				case MotionEventActions.Up:
					    _docView.ReaderView.OnTouchEvent(e);
					break;
			}

			return _gestureDetector.OnTouchEvent(e);
		}

		private class GestureListener : GestureDetector.SimpleOnGestureListener, GestureDetector.IOnDoubleTapListener
		{
			private MultistatoView _multistatoView;
			private float _startX = 0;

			public GestureListener(MultistatoView view)
			{
				_multistatoView = view;
			}

			public override bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
			{
				//float _startX = 0;
				float ev1X = e1.GetX();
				float ev2X = e2.GetX();

				float xdistance = System.Math.Abs(ev1X - ev2X);
				float xvelocity = System.Math.Abs(velocityX);

				if(_multistatoView.Multi.Swipe)
				{
					if((xvelocity > 200) && (xdistance > 120))
					{
						if(ev1X < ev2X) //Switch Left
						{
							_multistatoView.PrevState();

                            AnalyticsService.SendEvent(_multistatoView._docView.Pubblicazione.Titolo, AnalyticsEventAction.MultistatoPrev, _multistatoView._multistato.AnalyticsName);
						}
						else //Switch Right
						{
							_multistatoView.NextState();

                            AnalyticsService.SendEvent(_multistatoView._docView.Pubblicazione.Titolo, AnalyticsEventAction.MultistatoNext, _multistatoView._multistato.AnalyticsName);
						}
					}
				}

				return false;
			}

			public override bool OnScroll(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
			{
				//float ev1X = e1.GetX();
				float ev2X = e2.GetX();

				//Console.WriteLine("SCROLL: _startX=" + _startX + ", x2=" + ev2X);
                //_multistatoView.Multi.GapSwipe = 80;
				if(_multistatoView.Multi.SwipeContinuo)
				{
					if(ev2X > 0 && ev2X < _multistatoView._frame.Width())
					{
						if(_startX > ev2X + _multistatoView.Multi.GapSwipe)
						{
							_multistatoView.PrevState();
							_startX = ev2X;

                            AnalyticsService.SendEvent(_multistatoView._docView.Pubblicazione.Titolo, AnalyticsEventAction.MultistatoPrev, _multistatoView._multistato.AnalyticsName);
						}
						else if(_startX < ev2X - _multistatoView.Multi.GapSwipe)
						{
							_multistatoView.NextState();
							_startX = ev2X;

                            AnalyticsService.SendEvent(_multistatoView._docView.Pubblicazione.Titolo, AnalyticsEventAction.MultistatoNext, _multistatoView._multistato.AnalyticsName);
						}
					}
				}
				return false;
			}

			public override void OnLongPress(MotionEvent e)
			{

			}

			public override bool OnSingleTapUp(MotionEvent e)
			{
				_multistatoView.OpenZoom();

				return base.OnSingleTapUp(e);
			}

			public override bool OnDown(MotionEvent e)
			{
				_startX = e.GetX();

				return true;

			}
		}
	}
}

