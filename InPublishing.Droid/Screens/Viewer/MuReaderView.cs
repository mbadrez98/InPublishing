
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
using System.Runtime.InteropServices;
using Android.Graphics;

namespace InPublishing
{
    public enum MOVING
    {
        DIAGONALLY = 0,
        LEFT = 1,
        RIGHT = 2,
        UP = 3,
        DOWN = 4
    };

	public class MuReaderView : ReaderView
	{
        private static Func<MOVING, bool> _scrollAction;
        public Func<MOVING, bool> ScrollAction
		{
			get{ return _scrollAction; }
			set{ _scrollAction = value; }
		}

		private bool _scrollEnabled = true;
		public bool ScrollEnabled
		{
			get{ return _scrollEnabled; }
			set{ _scrollEnabled = value; }
		}

		private Action _onSingleTap;
		public Action OnSingleTap
		{
			get{ return _onSingleTap; }
			set{ _onSingleTap = value; }
		}

		private Action _onPageSelected;
		public Action OnPageSelected
		{
			get{ return _onPageSelected; }
			set{ _onPageSelected = value; }
		}

		private bool _zoomEnabled = true;
		public bool ZoomEnabled
		{
			get{ return _zoomEnabled; }
			set{ _zoomEnabled = value; }
		}

		private float _zoomMax = 1;
		public float ZoomMax
		{
			get{ return _zoomMax; }
			set{ _zoomMax = value; }
		}

        private Action<MOVING, MotionEvent, MotionEvent> _onFlingAction;
        public Action<MOVING, MotionEvent, MotionEvent> OnFlingAction 
        {
            get { return _onFlingAction; }
            set { _onFlingAction = value; }
        }

		private float _scale = 1;

        private BaseAdapter mAdapter;
        private bool mScaling;

        public MuReaderView(Context context) : base(context)
		{            
            SetMaxScale(2.0f);
		}

        public void SetAdapter(BaseAdapter adapter)
        {
            if (null != mAdapter && adapter != mAdapter) 
            {
                if (adapter is MuPageAdapter)
                {
                    ((MuPageAdapter) mAdapter).ReleaseBitmaps();
                }
            }

            mAdapter = adapter;

            this.Adapter = mAdapter;

            RequestLayout();
        }
        				
		protected override void OnSettle(View v) 
		{
            ((MuPageView)v).UpdateHq(true);
		}
			
		protected override void OnUnsettle(View v) 
		{
            ((MuPageView)v).RemoveHq();
		}
			
		protected override void OnNotInUse(View v) 
		{
            ((MuPageView)v).ReleaseResources();
		}

		public override bool OnScale(ScaleGestureDetector detector)
		{
			float newScale = _scale * detector.ScaleFactor;//Math.Min(Math.Max(_scale * detector.ScaleFactor, ZoomMin), ZoomMax);

			if(/*newScale < 1 ||*/ newScale > _zoomMax)
			{
				return false;
			}

			_scale = Math.Min(Math.Max(newScale, 1), ZoomMax);

			if(_zoomEnabled)
			{
				return base.OnScale(detector); 
			}
			else
			{
				return false; 
			}
		}

		public override bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
		{
			if(!_scrollEnabled)
			{
				return false;
			}

			/*if(_scrollAction != null)
                _scrollAction(DirectionOfTravel(distanceX, distanceY));*/

            bool ret = _scrollAction != null && _scrollAction(DirectionOfTravel(distanceX, distanceY));

			return ret || base.OnScroll(e1, e2, distanceX, distanceY);
		}

		public override bool OnFling(MotionEvent p0, MotionEvent p1, float p2, float p3)
		{
            if(!_scrollEnabled || mScaling)
			{
				return false;
			}

            if (_onFlingAction != null)
            {
                _onFlingAction(DirectionOfTravel(p2, p3), p0, p1);
            }

			return base.OnFling(p0, p1, p2, p3);
		}

        public override bool OnScaleBegin(ScaleGestureDetector p0)
        {
            mScaling = true;

            return base.OnScaleBegin(p0);
        }

        public override void OnScaleEnd(ScaleGestureDetector p0)
        {
            mScaling = false;

            base.OnScaleEnd(p0);
        }

		public override bool OnSingleTapUp(MotionEvent p0)
		{
			if(_onSingleTap != null)
				_onSingleTap();

			return base.OnSingleTapUp(p0);
		}

		protected override void OnChildSetup(int i, View v)
		{
			base.OnChildSetup(i, v);

			if(i == this.DisplayedViewIndex && this.DisplayedView != null)
			{
				MuPageView pageView = (MuPageView)this.DisplayedView;
				pageView.OnScreen = true;
				//pageView.MoveToScreen();
			}
		}

		protected override void OnMoveToChild(int i)
		{
			base.OnMoveToChild(i);

			if(_onPageSelected != null)
				_onPageSelected();

			MuPageView pageView = (MuPageView)this.DisplayedView;

			if(pageView != null)
			{
				pageView.MoveToScreen();
			}
		}

		protected override void OnMoveOffChild(int i)
		{
			MuPageView pageView = (MuPageView)Adapter.GetItem(i);

			if(pageView != null)
			{
				pageView.MoveOffScreen();
			}

			base.OnMoveOffChild(i);
		}
            
		public override View SelectedView
		{
			get
			{
				return null;
			}
		} 

        private static MOVING DirectionOfTravel(float vx, float vy)
        {
            if(Math.Abs(vx) > 2 * Math.Abs(vy))
                return (vx > 0) ? MOVING.RIGHT : MOVING.LEFT;
            else if(Math.Abs(vy) > 2 * Math.Abs(vx))
                return (vy > 0) ? MOVING.DOWN : MOVING.UP;
            else
                return MOVING.DIAGONALLY;
        }
    }
}