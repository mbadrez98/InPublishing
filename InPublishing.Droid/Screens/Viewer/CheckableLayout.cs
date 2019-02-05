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
using Android.Graphics.Drawables;
using Android.Graphics;
using Android.Util;
using Android.Content.Res;

namespace InPublishing
{
	public class CheckableLayout : FrameLayout, ICheckable
	{
		private bool _checked;
		private FrameLayout _Overlay;

		public View MainView;

		public CheckableLayout(Context context) : base(context)
		{
			AddOverlay();
		}

		public CheckableLayout(Context context, IAttributeSet attrs) : base (context, attrs)
		{
			AddOverlay();  
		}

		public void AddOverlay()
		{
			if(_Overlay == null)
			{
				string c = DataManager.Get<ISettingsManager>().Settings.ToolbarBarColor;
				//Color color = Color.ParseColor(c);

				_Overlay = new FrameLayout(this.Context);
				_Overlay.LayoutParameters = new GridView.LayoutParams(GridView.LayoutParams.MatchParent, GridView.LayoutParams.MatchParent);

				_Overlay.SetBackgroundResource (Resource.Drawable.blue_list_focused_holo);

                _Overlay.Background.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);

				_Overlay.Visibility = ViewStates.Invisible;
				this.AddView(_Overlay);
			}
		}

		public override void AddView(View child)
		{
            /*if(child.Parent != null)
                ((ViewGroup)child.Parent).RemoveView(child);*/

			base.AddView(child);

			MainView = child;

			if(_Overlay != null)
			{
				_Overlay.BringToFront();
			}
		}

		public void Toggle()
		{
			Checked = !_checked;
		}

		#region ICheckable implementation
		public bool Checked {
			get {
				return _checked;
			}
			set {
				_checked = value;

				/**if(_checked)
				{	
					int[] attrs = new int[] { Resource.Attribute.listSelection};

					TypedArray ta = Context.Theme.ObtainStyledAttributes(attrs);

					Drawable drawableFromTheme = ta.GetDrawable(0);

					//ta.Recycle();

					//this.SetBackgroundDrawable(drawableFromTheme);
					this.SetBackgroundResource(Resource.Drawable.list_focused_blue);
				}
				else
				{
					this.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
				}*/


				/*SetBackgroundDrawable(value
					? new ColorDrawable(Color.Blue)
					: new ColorDrawable(Color.White));*/
				/*if(_checked)
				{
					//color = Color.Argb(150, Color.
					SetBackgroundColor(color);
					Background.SetAlpha(50);
				}
				else
				{
					SetBackgroundColor(Color.White);
				}*/
				if(_checked)
				{
					if(_Overlay != null)
					{
						_Overlay.Visibility = ViewStates.Visible;
					}
				}
				else
				{
					if(_Overlay != null)
					{
						_Overlay.Visibility = ViewStates.Invisible;
					}
				}

			}
		}
		#endregion


	}
}

