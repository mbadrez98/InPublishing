
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
using Android.Graphics.Drawables;

namespace InPublishing
{
	[Activity(Label = "AboutActivity", Theme = "@style/Blue.DialogActivity")]			
	public class AboutActivity : Activity
	{
		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			SetContentView(Resource.Layout.AboutDialog);

			if(ActionBar != null)
			{
				this.Title = ActionBar.Title = GetString(Resource.String.about_title);

				ActionBar.SetDisplayUseLogoEnabled(false);
				ActionBar.SetIcon(new ColorDrawable(Color.Transparent));
				ActionBar.SetHomeButtonEnabled(false);
				ActionBar.SetDisplayHomeAsUpEnabled(true);
				ActionBar.SetDisplayShowHomeEnabled(true);
				ActionBar.SetDisplayShowTitleEnabled(true);
			}

			var frag = new AboutFragment();

			FragmentManager.BeginTransaction()
				.Replace(Resource.Id.aboutContent, frag)
				.Commit();
		}

		protected override void OnStart()
		{
			base.OnStart();

			if(!Utility.IsTablet(this))
				return;

			Rect frame = new Rect();
			Window.DecorView.GetWindowVisibleDisplayFrame(frame);

			Window.SetGravity(GravityFlags.Center | GravityFlags.Center);

			WindowManagerLayoutParams par = Window.Attributes;  
			//par.Y = Utility.dpToPx(this, 50);
			//par.Height = frame.Bottom - Utility.dpToPx(this, 121);  
			par.Width = Utility.dpToPx(this, 400); 
			par.DimAmount = 0;
			this.Window.Attributes = par; 

			Window.AddFlags(WindowManagerFlags.DimBehind);
		}

		public override bool OnOptionsItemSelected(IMenuItem item)
		{
			switch (item.ItemId)
			{
				case Android.Resource.Id.Home:
					this.Finish();
					return true;
					//break;
			}

			return false;
		}
	}
}