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
using Android.Content.PM;

namespace InPublishing
{
	public class AboutFragment : Fragment
	{
		public override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			var view = inflater.Inflate(Resource.Layout.AboutScreen, container, false);

			if(Activity.ActionBar != null && Activity.ActionBar.CustomView != null)
			{
				Button btnBack = Activity.ActionBar.CustomView.FindViewById<Button>(Resource.Id.btnBack);
				btnBack.Text = Activity.Title = Activity.ActionBar.Title = GetString(Resource.String.about_title);
				btnBack.SetCompoundDrawables(null, null, null, null);
			}

			TextView lblNomeApp = view.FindViewById<TextView>(Resource.Id.lblNomeApp);
			TextView lblVersione = view.FindViewById<TextView>(Resource.Id.lblVersione);
			/*ImageView imgLogo = view.FindViewById<ImageView>(Resource.Id.imgLogo);
			ImageView imgCrediti = view.FindViewById<ImageView>(Resource.Id.imgCrediti);*/
			TextView lblCrediti = view.FindViewById<TextView>(Resource.Id.lblCrediti);

			//nomeApp
			lblNomeApp.Text = Activity.ApplicationInfo.LoadLabel(Activity.PackageManager);

			//versione
			lblVersione.Text = "InPublishing " + DataManager.Get<ISettingsManager>().Settings.InpubVersion;//Activity.PackageManager.GetPackageInfo(Activity.PackageName, 0).VersionName;

			//logo
			//imgLogo.SetImageResource(Resource.Drawable.icon);

			//logo crediti
			//imgCrediti.SetImageResource(Resource.Drawable.logo_crediti);

			//crediti
			lblCrediti.Text = DataManager.Get<ISettingsManager>().Settings.Credits;

			return view;
		}
	}
}

