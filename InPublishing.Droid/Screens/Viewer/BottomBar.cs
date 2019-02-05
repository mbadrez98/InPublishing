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
using Android.Util;
using Android.Views.Animations;
using Android.Graphics.Drawables;
using Android.Graphics;

namespace InPublishing
{
	class BottomBar : RelativeLayout
	{
		SeekBar _barPages;
		TextView _lblPages;

		public int ProgressMax
		{
			get { return _barPages.Max; }
			set { _barPages.Max = value; }
		}

		private Action<int> _startProgress;
		public Action<int> StartProgress
		{
			get{ return _startProgress; }
			set
			{ 
				_startProgress = value; 
				_barPages.StartTrackingTouch += (sender, e) => 
				{
					_startProgress(e.SeekBar.Progress);
				};
			}
		}

		private Action<int> _stopProgress;
		public Action<int> StopProgress
		{
			get{ return _stopProgress; }
			set
			{ 
				_stopProgress = value; 
				_barPages.StopTrackingTouch += (sender, e) => 
				{
					_stopProgress(e.SeekBar.Progress);
				};
			}
		}

		private Action<int> _changeProgress;
		public Action<int> ChangeProgress
		{
			get{ return _changeProgress; }
			set
			{ 
				_changeProgress = value; 
				_barPages.ProgressChanged += (sender, e) => 
				{
					_changeProgress(e.Progress);
				};
			}
		}

		protected BottomBar(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
			InitView();
		}

		public BottomBar(Context context) : base(IntPtr.Zero, JniHandleOwnership.DoNotTransfer)
		{
			InitView();
		}

		public BottomBar(Context context, IAttributeSet attrs) : base(IntPtr.Zero, JniHandleOwnership.DoNotTransfer)
		{
			InitView();
		}

		private void InitView()
		{
			View.Inflate(this.Context, Resource.Layout.BottomBar, this);

            //this.SetBackgroundColor(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.NavigationBarColor));

			_barPages = FindViewById<SeekBar>(Resource.Id.barPages);
			_lblPages = FindViewById<TextView>(Resource.Id.lblPages);

			_barPages.ProgressChanged += (sender, e) => 
			{
				_lblPages.Text = (e.Progress + 1) + " / " + (_barPages.Max + 1);
			};

            _lblPages.SetTextColor(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.TintColor));

            /*Drawable bgDrawable = _barPages.ProgressDrawable;
            bgDrawable.SetColorFilter(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.TintColor), PorterDuff.Mode.SrcIn);
            _barPages.ProgressDrawable = bgDrawable;

            Drawable thDrawable = _barPages.Thumb;
            thDrawable.SetColorFilter(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.TintColor), PorterDuff.Mode.SrcIn);
            _barPages.SetThumb(thDrawable);*/

            _barPages.ProgressDrawable.Colorize(DataManager.Get<ISettingsManager>().Settings.TintColor);
            _barPages.Thumb.Colorize(DataManager.Get<ISettingsManager>().Settings.TintColor);

            var barContainer = FindViewById<LinearLayout>(Resource.Id.barContainer);

            /*Drawable bgDrawable = barContainer.Background;
            bgDrawable.SetColorFilter(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.NavigationBarColor), PorterDuff.Mode.SrcOut);
            barContainer.Background = bgDrawable;*/

            barContainer.SetBackgroundColor(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.NavigationBarColor));

            /*View bottomBorder = new View(this.Context);
            bottomBorder.SetBackgroundResource(Resource.Drawable.ShadowTop);
            RelativeLayout.LayoutParams param = new RelativeLayout.LayoutParams(RelativeLayout.LayoutParams.MatchParent, 2);
            param.AddRule(LayoutRules.AlignParentTop);
            barWrapper.AddView(bottomBorder, param);*/
		}

		public void SetPage(int page)
		{
			_barPages.Progress = page;
		}

		public void Hide()
		{
			Animation a = new AlphaAnimation(1.0f, 0.0f);
			a.Duration = 500;
			a.FillAfter = true;
			a.AnimationEnd += (sender, e) => 
			{
				this.Visibility = ViewStates.Gone;
				_barPages.Enabled = false;
			};

			this.StartAnimation(a);
		}

		public void Show()
		{
			Animation a = new AlphaAnimation(0.0f, 1.0f);
			a.Duration = 500;
			a.FillAfter = true;

			a.AnimationStart += (sender, e) => 
			{
				_barPages.Enabled = true;
				this.Visibility = ViewStates.Visible;
			};

			this.StartAnimation(a);
		}
	}
}

