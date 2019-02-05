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
using Android.Graphics;
using Android.Support.V4.View;
using Android.Util;

namespace InPublishing
{
	public class ZoomableRelativeLayout : RelativeLayout
	{
		private int INVALID_POINTER_ID = -1;
		private float mPosX;
		private float mPosY;
		private float mLastTouchX;
		private float mLastTouchY;
		private float mLastGestureX;
		private float mLastGestureY;
		private int mActivePointerId;
		//private HorizontalPager baseHorScrollView;
		private ScaleGestureDetector mScaleDetector;
		private GestureDetector mgestureDetector;
		public float mScaleFactor = 1f;

		public float MIN_ZOOM = 1f;
		public float MAX_ZOOM = 2;

		public bool OnLeftSide = false, OnRightSide = false;

		private bool _IsZooming = false;

		static float mFocusX = 0, mFocusY = 0;

		public ZoomableRelativeLayout(Context context):base(context)
		{
			mActivePointerId = INVALID_POINTER_ID;

			mScaleDetector = new ScaleGestureDetector(Context, new ScaleListener(this));
			mgestureDetector = new GestureDetector(Context, new GestureListener(this));

			SetWillNotDraw(false);

			this.SetClipChildren(false);
		}
			
		public override bool OnTouchEvent(MotionEvent e)
		{
			mScaleDetector.OnTouchEvent(e);
			mgestureDetector.OnTouchEvent (e);

			var parent = Parent;

			int n = 0;
			bool check = false;
			while (n < 6)
			{
				if(parent.GetType() == typeof(ViewPager))
				{
					check = true;
					break;
				}

				parent = parent.Parent;
				n++;
			}

			if(check)
			{
				parent = parent as ViewPager;
			}
			else
			{
				parent = null;
			}

			//int action = e.Action;
			switch(e.Action & MotionEventActions.Mask)
			{
				case MotionEventActions.Down:
					{
						float x = e.GetX();
						float y = e.GetY();

						mLastTouchX = x;
						mLastTouchY = y;
						mActivePointerId = e.GetPointerId(0);

						break;
					}
					case MotionEventActions.PointerDown:
					{
						float gx = mScaleDetector.FocusX;
						float gy = mScaleDetector.FocusY;

						mLastGestureX = gx;
						mLastGestureY = gy; 

						_IsZooming = true;

						break;
					}
				case MotionEventActions.Move:
					{
						if(mScaleFactor == 1.0f)
							break;

						if(!mScaleDetector.IsInProgress)
						{
							int pointerIdx = e.FindPointerIndex(mActivePointerId);
							float x = e.GetX(pointerIdx);
							float y = e.GetY(pointerIdx);

							float dx = x - mLastTouchX;
							float dy = y - mLastTouchY;

							mPosX += dx;
							mPosY += dy;

							//translateX = dx;
							//translateY = dx;

							this.Invalidate();

							mLastTouchX = x;
							mLastTouchY = y;
					
							//Console.WriteLine("MOVE: x=" + x + "y=" + y + "dx=" + dx + "dy=" + dy);
						}
						else
						{
							float gx = mScaleDetector.FocusX;
							float gy = mScaleDetector.FocusY;

							float gdx = gx - mLastGestureX;
							float gdy = gy - mLastGestureY;

							mPosX += gdx;
							mPosY += gdy;

							this.Invalidate();

							mLastGestureX = gx;
							mLastGestureY = gy;
						}

						break;
					}
				case MotionEventActions.Up:
					mActivePointerId = INVALID_POINTER_ID;
					break;
				case MotionEventActions.Cancel:
					mActivePointerId = INVALID_POINTER_ID;
					break;
				case MotionEventActions.PointerUp:

					int pointerIdx2 = (int)(e.Action & MotionEventActions.PointerIndexMask) >> (int)MotionEventActions.PointerIndexShift;
					int pointerId = e.GetPointerId(pointerIdx2);

					if(pointerId == mActivePointerId)
					{
						int NewPointerIndex = pointerIdx2 == 0 ? 1 : 0;
						mLastTouchX = e.GetX(NewPointerIndex);
						mLastTouchY = e.GetY(NewPointerIndex);
						mActivePointerId = e.GetPointerId(NewPointerIndex);
					}
					else
					{
						int TempPointerIdx = e.FindPointerIndex(mActivePointerId);
						mLastTouchX = e.GetX(TempPointerIdx);
						mLastTouchY = e.GetY(TempPointerIdx);
					}
					_IsZooming = false;
					break;
			}

			if(parent != null)
			{
				if(mScaleFactor > 1 || _IsZooming)
				{
					parent.RequestDisallowInterceptTouchEvent(true);
				}
				else
				{
					parent.RequestDisallowInterceptTouchEvent(false);
				}
			}

			return true;
		}

		protected override void OnLayout(bool changed, int l, int t, int r, int b)
		{
			for(int i=0; i<this.ChildCount; i++)
			{
				View child = this.GetChildAt(i); 
				if(child.Visibility != ViewStates.Gone)
				{
					RelativeLayout.LayoutParams param = (RelativeLayout.LayoutParams)child.LayoutParameters;
					child.Layout(
						(int)(param.LeftMargin), 
						(int)(param.TopMargin), 
						(int)((param.LeftMargin + child.MeasuredWidth)), 
						(int)((param.TopMargin + child.MeasuredHeight)) 
					);

					//child.Invalidate();
					//child.RequestLayout();
				}
			}

			//base.OnLayout(changed, l, t, r, b);
		}

		protected override void DispatchDraw(Canvas canvas)
		{
			OnLeftSide = OnRightSide = false;

			float scaleWidth = (float)Math.Round(this.Width * mScaleFactor);
			float scaleHeight = (float)Math.Round(this.Height * mScaleFactor);

			Console.WriteLine("POSX: " + mPosX + "POSY: " + mPosY + "FOCX: " + mFocusX + "FOCY: " + mFocusY);

			if(mScaleFactor == MIN_ZOOM)
			{
				mPosX = 0.0f;
				mPosY = 0.0f;
				OnLeftSide = OnRightSide = true;
			}
			else
			{
				if((mPosX * -1) < 10.0f)
				{
					mPosX = 0;
					OnLeftSide = true;
				}

				if((scaleWidth >= this.Width && (mPosX + scaleWidth - this.Width) < 10) || (scaleWidth <= this.Width && -mPosX + scaleWidth <= this.Width))
				{
					mPosX = this.Width - scaleWidth;
					OnRightSide = true;
				}

				if(-mPosY < 10.0f)
				{
					mPosY = 0;
				}

				if(mPosY + scaleHeight - this.Height < 10.0f)
				{
					mPosY = this.Height - scaleHeight;
				}
			}

			canvas.Save(SaveFlags.Matrix);
			canvas.Translate(mPosX, mPosY);
			//this.TranslationX = mPosX;
			//this.TranslationY = mPosY;
			canvas.Scale(mScaleFactor, mScaleFactor);
			//this.ScaleX = this.ScaleY = mScaleFactor;
			//.InvalidateChilds();
			base.DispatchDraw(canvas);

			canvas.Restore();
			/*if(!mScaleDetector.IsInProgress)
			{
				this.TranslationX = mPosX;
				this.TranslationY = mPosY;
			}
			else
			{
				this.PivotX = mFocusX;
				this.PivotY = mFocusY;
				this.ScaleX = this.ScaleY = mScaleFactor;
			}*/
		}
			
		public void RestoreScale()
		{
			mScaleFactor = 1;
			mPosX = 0;
			mPosY = 0;
			Invalidate();
		}

		private class ScaleListener: ScaleGestureDetector.SimpleOnScaleGestureListener
		{
			private ZoomableRelativeLayout _padre;

			public ScaleListener(ZoomableRelativeLayout padre)
			{
				_padre = padre;
			}

			public override bool OnScale(ScaleGestureDetector detector)
			{
				float scale = detector.ScaleFactor;
				_padre.mScaleFactor *= detector.ScaleFactor;

				_padre.mScaleFactor = Math.Max(_padre.MIN_ZOOM, Math.Min(_padre.mScaleFactor, _padre.MAX_ZOOM));

				mFocusX = detector.FocusX;
				mFocusY = detector.FocusY;

				if (_padre.mScaleFactor < 2f) 
				{
					// 1 Grabbing
					float centerX = detector.FocusX;
					float centerY = detector.FocusY;

					//	Console.WriteLine("CENTER: x=" + centerX + " y=" + centerY);

					// 2 Calculating difference
					float diffX = centerX - _padre.mPosX;
					float diffY = centerY - _padre.mPosY;
					// 3 Scaling difference
					diffX = diffX * scale - diffX;
					diffY = diffY * scale - diffY;
					// 4 Updating image origin
					_padre.mPosX -= diffX;
					_padre.mPosY -= diffY;
				}

				_padre.Invalidate();
				_padre.RequestLayout();

				return true;
			}

			/*public override bool OnScale(ScaleGestureDetector detector) 
			{
				// getting the scaleFactor from the detector
				_padre.mScaleFactor *= detector.ScaleFactor;
				mFocusX = detector.FocusX;
				mFocusY = detector.FocusY;
				// Limit the scale factor in the MIN and MAX bound
				_padre.mScaleFactor = Math.Max(Math.Min(_padre.mScaleFactor, _padre.MAX_ZOOM), _padre.MIN_ZOOM);
				// Here we are only zooming so invalidate has to be done

				//_padre.Invalidate();
				//_padre.RequestLayout();
				return true;
			}*/
		}

		private class GestureListener : GestureDetector.SimpleOnGestureListener, GestureDetector.IOnDoubleTapListener
		{
			private ZoomableRelativeLayout _padre;

			public GestureListener(ZoomableRelativeLayout padre){
				_padre = padre;
			}

			public override bool OnDoubleTap(MotionEvent e)
			{
				//string a;

				_padre.mPosX = e.GetX() * -1;
				_padre.mPosY = e.GetY() * -1;
				/*mFocusX = e.GetX();
				mFocusY = e.GetY();*/

				if(_padre.mScaleFactor > _padre.MIN_ZOOM)
				{
					_padre.mScaleFactor = _padre.MIN_ZOOM;

				}
				else 
				{
					_padre.mScaleFactor = _padre.MAX_ZOOM;
				}

				_padre.Invalidate ();
				_padre.RequestLayout();

				return true;
			}
		}
	}
}

