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
using System.Timers;

namespace InPublishing
{
	public class SliderView : RelativeLayout
	{
        private Slider _Slider;
        private string _BasePath;
		private ViewerScreen _DocView;
        private float _scale;

        List<RelativeLayout> _StatiView;
        List<Dictionary<string, RelativeLayout>> _StatiViewObj;
        int _CurrentState = 0;
		GestureDetector _gestureDetector;
		
		private bool _isRunning = false;
        private bool _stoppedForContent = false;
		private Handler _changeHandler = new Handler();
		private IRunnable _changeRunnable;

        Timer _AutoplayTimer;

        public Slider Slider {get{return _Slider;}}

        public SliderView(Context context, Slider slider, string path, ViewerScreen doc, RectF frame, float scale) : base (context)
		{
			_Slider = slider;
			_BasePath = path;
			_DocView = doc;
            _scale = scale;

            /*_Slider.PlayStopClick = true;
            _Slider.Autoplay = true;
            _Slider.Delay = 3000;
            _Slider.TransitionDuration = 500;*/
            //_Slider.Swipe = true;
			//autoplay
            if (_Slider.Autoplay)
            {
                _AutoplayTimer = new Timer(_Slider.Delay);
                _AutoplayTimer.Elapsed += (sender, e) =>
                {
                    _DocView.RunOnUiThread(() =>
                    {
                        if(_isRunning)
                            this.NextState();
                    });
                };
            }

			//gesture
            if (_Slider.Swipe || _Slider.PlayStopClick)
			{
				_gestureDetector = new GestureDetector(this.Context, new GestureListener(this));
			}

            //scorrimento temporizzato
            _changeHandler = new Handler(Looper.MainLooper);
            _changeRunnable = new Runnable(() =>
            {
                try
                {
                    this.NextState();
                }
                finally
                {
                    _changeHandler.PostDelayed(_changeRunnable, _Slider.Delay);
                }
            });

            LoadContent();
		}

        private void LoadContent()
        {
            if (_Slider.Stati == null || _Slider.Stati.Count == 0)
                return;

            _StatiView = new List<RelativeLayout>();
            _StatiViewObj = new List<Dictionary<string, RelativeLayout>>();

            int count = 0;

            foreach (StatoSlider stato in _Slider.Stati)
            {
                RelativeLayout statoView = new RelativeLayout(_DocView);
                statoView.LayoutParameters = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.MatchParent, RelativeLayout.LayoutParams.MatchParent);

                string fileName = "";

                if (stato.TipoSfondo == SfondoType.PDF)
                {
                    fileName = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(stato.Sfondo), System.IO.Path.GetFileNameWithoutExtension(stato.Sfondo) + ".png");
                }
                else
                {
                    fileName = stato.Sfondo;
                }

                string imgPath = ImageUtility.GetBitmapPath(System.IO.Path.Combine(_BasePath, fileName), _DocView);

                _DocView.RunOnUiThread(() =>
                {
                    ImgView imgView = new ImgView(Context, imgPath);
                    imgView.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
                    //imgView.LayoutParameters = new ViewGroup.LayoutParams(200, 100);
                    statoView.AddView(imgView, 0);
                });

                Dictionary<string, RelativeLayout> objViews = Objects.CreateObjects(stato.Oggetti, _BasePath, _DocView, _scale);

                foreach (var video in stato.VideoViews)
                {
                    if (objViews.ContainsKey(video))
                    {
                        var view = objViews[video];

                        for (int i = 0; i < view.ChildCount; i++)
                        {
                            if (view.GetChildAt(i).GetType() == typeof(VideoView))
                            {
                                VideoView av = view.GetChildAt(i) as VideoView;

                                if (_Slider.Autoplay)
                                {
                                    av.OnStart += () =>
                                    {
                                        this.Stop();
                                        _stoppedForContent = true;
                                    };

                                    av.OnFinish += () =>
                                    {
                                        this.Start(true);
                                        av.Stop();
                                    };
                                }

                                //impedisco al video di partire subito perché lo faccio partire io quando entra la slide
                                av.Stop();
                                //av.StopAutoplay = false;
                                break;
                            }
                        }

                    }
                }

                _DocView.RunOnUiThread(() =>
                {
                    foreach (View view in objViews.Values)
                    {
                        statoView.AddView(view);
                        view.BringToFront();
                    }

                    this.AddView(statoView);
                });

                _StatiView.Add(statoView);
                _StatiViewObj.Add(objViews);

                count++;
            }

            //se è in autoplay la funzione viene chiamata quando entro nella pagina, altrimenti la chiamo subito
            if (!_Slider.Autoplay)
                this.Initialize();
        }

        private void Initialize()
        {
           _DocView.RunOnUiThread(() =>
            {
                int count = 0;
                foreach (View statoView in _StatiView)
                {
                    if (count > 0)
                        statoView.Visibility = ViewStates.Invisible;
                    else
                        statoView.Visibility = ViewStates.Visible;

                    count++;
                }

                OnEnterState(0);
            }); 
        }

		public void PlayStop()
		{
			if (this._isRunning)
            {
                this.Stop();

                AnalyticsService.SendEvent(_DocView.Pubblicazione.Titolo, AnalyticsEventAction.SliderStop, _Slider.AnalyticsName);
            }
            else if(!_stoppedForContent)
            {
                this.Start(true);

                AnalyticsService.SendEvent(_DocView.Pubblicazione.Titolo, AnalyticsEventAction.SliderPlay, _Slider.AnalyticsName);
            }
		}

		public void Autoplay()
		{
			if(_Slider.Autoplay)
			{
				this.Start();
			}

            this.Initialize();
		}
		
		public void NextState()
		{
			int target = _CurrentState + 1;

            if (target >= _Slider.Stati.Count)
            {
                if (_Slider.Loop)
                    target = 0;
                else
                    return;
            }

            GoToState(target, "n");		
		}

		public void PrevState()
		{
			int target = _CurrentState - 1;

            if (target < 0)
            {
                if (_Slider.Loop)
                    target = _Slider.Stati.Count - 1;
                else
                    return;
            }

            GoToState(target, "p");
		}		

		public void GoToState(int index, string direction = "")
		{
            try
            {
                if (index == _CurrentState || index < 0 || index > _StatiView.Count - 1)
                {
                    return;
                }

                var curStato = _StatiView[_CurrentState];
                var stato = _StatiView[index];

                string inAnim = _Slider.Transition;
                string outAnim = _Slider.Transition;

                bool directionDef = true;

                switch (inAnim)
                {
                    case "push":
                    case "moveIn":
                        inAnim = outAnim = "slide";
                        break;
                    case "fade":
                        direction = "";
                        directionDef = false;
                        break;

                    default:
                        direction = "";
                        inAnim = outAnim = "fade";
                        directionDef = false;
                        break;
                }

                inAnim += "_in";
                outAnim += "_out";

                if (directionDef)
                {
                    if (direction.ToLower() == "n")
                    {
                        if (_Slider.TransitionDirection.ToUpper() == "O")
                        {
                            //transition.Subtype = "fromRight";
                            inAnim += "_right";
                            outAnim += "_left";
                        }
                        else if (_Slider.TransitionDirection.ToUpper() == "V")
                        {
                            //transition.Subtype = "fromTop";
                            inAnim += "_top";
                            outAnim += "_bottom";
                        }
                    }
                    else if (direction.ToLower() == "p")
                    {
                        if (_Slider.TransitionDirection.ToUpper() == "O")
                        {
                            inAnim += "_left";
                            outAnim += "_right";
                        }
                        else if (_Slider.TransitionDirection.ToUpper() == "V")
                        {
                            inAnim += "_bottom";
                            outAnim += "_top";
                        }
                    }
                    else
                    {
                        if (index > _CurrentState) //avanti
                        {
                            if (_Slider.TransitionDirection.ToUpper() == "O")
                            {
                                //transition.Subtype = "fromRight";
                                inAnim += "_right";
                                outAnim += "_left";
                            }
                            else if (_Slider.TransitionDirection.ToUpper() == "V")
                            {
                                //transition.Subtype = "fromTop";
                                inAnim += "_top";
                                outAnim += "_bottom";
                            }
                        }
                        else //indietro
                        {
                            if (_Slider.TransitionDirection.ToUpper() == "O")
                            {
                                //transition.Subtype = "fromLeft";
                                inAnim += "_left";
                                outAnim += "_right";
                            }
                            else if (_Slider.TransitionDirection.ToUpper() == "V")
                            {
                                //transition.Subtype = "fromBottom";

                                inAnim += "_bottom";
                                outAnim += "_top";
                            }
                        }
                    }
                }

                int inRes = Resources.GetIdentifier(inAnim, "anim", this.Context.PackageName);
                int outRes = Resources.GetIdentifier(outAnim, "anim", this.Context.PackageName);

                var inAnimation = AnimationUtils.LoadAnimation(this.Context, inRes);
                inAnimation.Duration = _Slider.TransitionDuration;
                inAnimation.AnimationStart += (sender, e) =>
                {

                };

                inAnimation.AnimationEnd += (sender, e) =>
                {
                    stato.Visibility = ViewStates.Visible;
                };

                var outAnimation = AnimationUtils.LoadAnimation(this.Context, outRes);
                outAnimation.Duration = _Slider.TransitionDuration;
                outAnimation.AnimationEnd += (sender, e) =>
                {
                    curStato.Visibility = ViewStates.Invisible;
                };

                curStato.StartAnimation(outAnimation);
                stato.StartAnimation(inAnimation);

                this.OnExitState(_CurrentState);
                this.OnEnterState(index);

                _CurrentState = index;
            }
            catch(System.Exception ex)
            {
                Log.Error("SliderView", ex.Message);
                return;
            }
		}

		public void GoToState(string key)
		{
			var stati = (from x in _Slider.Stati
				where x.Name == key
				select x);

			if(stati.Count() == 0)
				return;

			var stato = stati.First();

			int index = _Slider.Stati.IndexOf(stato);

			this.GoToState(index);
		}

        public void Start(bool immediate = false)
        {
            if (_AutoplayTimer == null)
                return;

            this._isRunning = true;
            _stoppedForContent = false;

            _AutoplayTimer.Enabled = true;
            _AutoplayTimer.Start();

            if (immediate)
                this.NextState();
        }

        public void Stop()
        {
            if (_AutoplayTimer == null)
                return;

            this._isRunning = false;

            _AutoplayTimer.Stop();
            _AutoplayTimer.Enabled = false;
        }

        private void OnEnterState(int index)
        {
            if (index >= _Slider.Stati.Count)
                return;

            var stato = _Slider.Stati[index];
            var statoObj = _StatiViewObj[index];

            foreach (var video in stato.VideoViews)
            {
                if (statoObj.ContainsKey(video))
                {
                    var view = statoObj[video];

                    for (int i = 0; i < view.ChildCount; i++)
                    {
                        if (view.GetChildAt(i).GetType() == typeof(VideoView))
                        {
                            VideoView av = view.GetChildAt(i) as VideoView;

                            av.Play();

                            break;
                        }
                    }
                }
            }
        }

        private void OnExitState(int index)
        {
            if (index >= _Slider.Stati.Count)
                return;

            var stato = _Slider.Stati[index];
            var statoObj = _StatiViewObj[index];

            foreach (var video in stato.VideoViews)
            {
                if (statoObj.ContainsKey(video))
                {
                    var view = statoObj[video];

                    for (int i = 0; i < view.ChildCount; i++)
                    {
                        if (view.GetChildAt(i).GetType() == typeof(VideoView))
                        {
                            VideoView av = view.GetChildAt(i) as VideoView;

                            av.Stop();
                            _stoppedForContent = false;

                            break;
                        }
                    }
                }
            }
        }

		public void StopAll()
		{
			this.Stop();

            for (int i = 0; i < _StatiView.Count; i++)
            {
                OnExitState(i);
            }
		}		

        float StartX, StartY;
		public override bool OnTouchEvent(MotionEvent e)
		{
            if (_gestureDetector == null)
                return false;

			_DocView.ReaderView.RequestDisallowInterceptTouchEvent(false);

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
                    if (_Slider.Swipe)
                    {
                        var deltaX = System.Math.Abs(StartX - e.RawX);
	                    var deltaY = System.Math.Abs(StartY - e.RawY);

	                    if (deltaX < deltaY)
	                    {
	                        _DocView.ReaderView.OnTouchEvent(e);
	                    }
	                }
                    else
                    {
                        _DocView.ReaderView.OnTouchEvent(e);
                    }
					break;
				case MotionEventActions.Up:
					_DocView.ReaderView.OnTouchEvent(e);
					break;
			}

			return _gestureDetector.OnTouchEvent(e);
		}

		private class GestureListener : GestureDetector.SimpleOnGestureListener, GestureDetector.IOnDoubleTapListener
		{
            private SliderView _SliderView;
			private float _startX = 0;

            public GestureListener(SliderView view)
			{
				_SliderView = view;
			}

			public override bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
			{
				//float _startX = 0;
				float ev1X = e1.GetX();
				float ev2X = e2.GetX();

				float xdistance = System.Math.Abs(ev1X - ev2X);
				float xvelocity = System.Math.Abs(velocityX);

                if(!_SliderView.Slider.Autoplay && _SliderView.Slider.Swipe)
				{
					if((xvelocity > 200) && (xdistance > 120))
					{
						if(ev1X < ev2X) //Switch Left
						{
							_SliderView.PrevState();

                            AnalyticsService.SendEvent(_SliderView._DocView.Pubblicazione.Titolo, AnalyticsEventAction.SliderPrev, _SliderView._Slider.AnalyticsName);
						}
						else //Switch Right
						{
							_SliderView.NextState();

                            AnalyticsService.SendEvent(_SliderView._DocView.Pubblicazione.Titolo, AnalyticsEventAction.SliderNext, _SliderView._Slider.AnalyticsName);
						}
					}
				}

				return false;
			}

			public override bool OnScroll(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
			{
				
                return base.OnScroll(e1, e2, velocityX, velocityY);
			}

			public override void OnLongPress(MotionEvent e)
			{

			}

			public override bool OnSingleTapUp(MotionEvent e)
			{
                if (_SliderView.Slider.Autoplay && _SliderView.Slider.PlayStopClick)
                {
                    _SliderView.PlayStop();
                }

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