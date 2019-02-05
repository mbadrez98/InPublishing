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
	[Activity (Label = "BaseModalScreen", Theme = "@style/Blue.NoActionBar")]			
	public class BaseModalScreen : Activity
	{
		protected ImageButton _btnClose;
		protected RelativeLayout _contentView;

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);
			this.Window.AddFlags(WindowManagerFlags.Fullscreen);

			SetContentView(Resource.Layout.BaseModalScreen);

			_contentView = FindViewById<RelativeLayout>(Resource.Id.contentView);
			_btnClose = FindViewById<ImageButton>(Resource.Id.btnClose);

			_btnClose.SetBackgroundColor(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.NavigationBarColor));
			_btnClose.Drawable.Colorize (DataManager.Get<ISettingsManager> ().Settings.TintColor);

			_btnClose.Click += (sender, e) => 
			{
				this.Finish();
			};
		}
	}
}

