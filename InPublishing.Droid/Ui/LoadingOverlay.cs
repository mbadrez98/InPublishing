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

namespace InPublishing
{
	class LoadingOverlay : FrameLayout
	{
		private TextView _lblMessage; 

		public LoadingOverlay(Context context, string label = null) : base(context)
		{
			this.LayoutParameters = new FrameLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent);

			View.Inflate(this.Context, Resource.Layout.LoadingOverlay, this);

			_lblMessage = FindViewById<TextView>(Resource.Id.lblMessage);

			if(label == null)
			{
				_lblMessage.SetText(context.GetString(Resource.String.gen_loading), TextView.BufferType.Normal);
			}
			else
			{
				_lblMessage.SetText(label, TextView.BufferType.Normal);
			}

			this.Visibility = ViewStates.Invisible;
		}

		public void Hide ()
		{
			//#if DEBUG
			//#else
			this.Visibility = ViewStates.Invisible;
			//#endif

			//this.Alpha = 0;
		}

		public void Show ()
		{
			//#if DEBUG
			//#else
			this.Visibility = ViewStates.Visible;
			//#endif
			//this.Alpha = 1;
		}
	}
}

