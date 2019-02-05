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

namespace InPublishing
{
	class ImageViewZoom : ImageView
	{
		private static int INVALID_POINTER_ID = -1;
		private float mPosX;
		private float mPosY;

		private float mLastTouchX;
		private float mLastTouchY;

		private float mLastGestureX;
		private float mLastGestureY;
		private int mActivePointerId = INVALID_POINTER_ID;

		private ScaleGestureDetector mScaleDetector;
		private GestureDetector mgestureDetector;
		private float mScaleFactor = 1.0f;
		public float MIN_ZOOM = 1f;
		public float MAX_ZOOM = 2;

		public ImageViewZoom(Context context):base(context)
		{
			mScaleDetector = new ScaleGestureDetector(Context,new ScaleListener(this));
			mgestureDetector = new GestureDetector(Context, new GestureListener(this));
		}

		public override bool OnTouchEvent (MotionEvent e)
		{
			mScaleDetector.OnTouchEvent(e);
			mgestureDetector.OnTouchEvent (e);

			//int action = e.Action;
			switch(e.Action & MotionEventActions.Mask)
			{
				case MotionEventActions.Down:
					//if(!mScaleDetector.IsInProgress)
					{
						float x = e.GetX();
						float y = e.GetY();

						mLastTouchX = x;
						mLastTouchY = y;
						mActivePointerId = e.GetPointerId(0);

						//Console.WriteLine("DOWN: x=" + x + "y=" + y);
					}
						//Parent.RequestDisallowInterceptTouchEvent(true);
						break;
				case MotionEventActions.PointerDown:
						//if(mScaleDetector.IsInProgress)
					{
						float gx = mScaleDetector.FocusX;
						float gy = mScaleDetector.FocusY;

						mLastGestureX = gx;
						mLastGestureY = gy; 
					}
					break;
				case MotionEventActions.Move:
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

						Invalidate();

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

						Invalidate();

						mLastGestureX = gx;
						mLastGestureY = gy;
					}
					break;
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

					break;
			}
			return true;
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			int widthSize = MeasureSpec.GetSize(widthMeasureSpec);
			int heightSize = MeasureSpec.GetSize(heightMeasureSpec);
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
			SetMeasuredDimension((int) (widthSize * mScaleFactor), (int)(heightSize * mScaleFactor));
		}

		protected override void OnDraw (Canvas canvas)
		{
			if((mPosX * -1) < 0)
			{
				mPosX = 0;

			}
			else if((mPosX * -1) > (mScaleFactor - 1) * this.Width)
			{
				mPosX = (1 - mScaleFactor) * this.Width;
			}

			if(mPosY * -1 < 0) 
			{
				mPosY = 0;
			}
			else if((mPosY * -1) > (mScaleFactor - 1) * this.Height) 
			{
				mPosY = (1 - mScaleFactor) * this.Height;
			}

			canvas.Translate(mPosX, mPosY);

			canvas.Scale(mScaleFactor, mScaleFactor);

			base.OnDraw(canvas);
		}

		private class ScaleListener : ScaleGestureDetector.SimpleOnScaleGestureListener
		{
			private ImageViewZoom _padre;

			public ScaleListener(ImageViewZoom padre)
			{
				_padre = padre;
			}

			public override bool OnScale (ScaleGestureDetector detector)
			{
				float scale = detector.ScaleFactor;
				_padre.mScaleFactor *= detector.ScaleFactor;

				_padre.mScaleFactor = Math.Max(_padre.MIN_ZOOM, Math.Min(_padre.mScaleFactor, _padre.MAX_ZOOM));

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
				return true;
			}
		}

		private class GestureListener : GestureDetector.SimpleOnGestureListener, GestureDetector.IOnDoubleTapListener
		{
			private ImageViewZoom _padre;

			public GestureListener(ImageViewZoom padre){
				_padre = padre;
			}

			public override bool OnDoubleTap(MotionEvent e)
			{
				_padre.mPosX = e.GetX() * -1;
				_padre.mPosY = e.GetY() * -1;

				if(_padre.mScaleFactor > _padre.MIN_ZOOM)
				{
					_padre.mScaleFactor = _padre.MIN_ZOOM;

				}
				else 
				{
					_padre.mScaleFactor = _padre.MAX_ZOOM;
				}

				_padre.Invalidate ();

				return true;
			}
		}
	}
}