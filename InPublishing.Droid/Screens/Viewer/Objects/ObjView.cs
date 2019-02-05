
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

namespace InPublishing
{
	public class ObjView : RelativeLayout
	{
		public Rect Frame;
		public bool Interaction = true;

		public ObjView(Context context, Rect frame) : base(context)
		{
			this.Frame = frame;
		}

		public override bool OnTouchEvent(MotionEvent e)
		{
			if(!Interaction)
				return false;

			return base.OnTouchEvent(e);
		}

		/*protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

			//int width = MeasureSpec.MakeMeasureSpec ((int) ((this.Width)), MeasureSpecMode.Exactly);
			//int height = MeasureSpec.MakeMeasureSpec ((int) ((this.Height)), MeasureSpecMode.Exactly);

			for(int i = 0; i < ChildCount; i++)
			{
				View child = GetChildAt(i);

				//float scale = (float)this.Width / (float)Frame.Width();

				//RelativeLayout.LayoutParams elParam = new RelativeLayout.LayoutParams(10, 10);
				//elParam.LeftMargin = 0;
				//child.LayoutParameters = elParam;
				//child.Measure(width, height);

				child.Measure(10, 10);
			}
		}*/

		protected override void OnLayout(bool changed, int l, int t, int r, int b)
		{
			base.OnLayout(changed, l, t, r, b);

			if (!changed)
				return;

			//var lp = this.LayoutParameters;

			/*RelativeLayout.LayoutParams lp = new RelativeLayout.LayoutParams (this.Width, this.Height);
			for(int i = 0; i < ChildCount; i++)
			{
				var child = GetChildAt(i);
				child.LayoutParameters = lp;
				//child.Layout(0, 0, r - l, b - t);
			}*/
		}
	}
}

