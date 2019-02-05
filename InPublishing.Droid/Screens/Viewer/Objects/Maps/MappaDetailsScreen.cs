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
using Android.Gms.Maps.Model;
using Android.Webkit;
using Android.Gms.Maps;
using Newtonsoft.Json;

namespace InPublishing
{
	[Activity (Label = "MappaDetailsScreen", Theme = "@style/Blue.NoActionBar")]			
	public class MappaDetailsScreen : Activity
	{
		private Dictionary<string, string> _Placemark;

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);
			this.Title = "Indietro";
			this.Window.AddFlags(WindowManagerFlags.Fullscreen);
			this.OverridePendingTransition(Resource.Animation.slide_in_right, Resource.Animation.slide_out_left);

			//ActionBar.SetDisplayHomeAsUpEnabled(true);

			_Placemark = JsonConvert.DeserializeObject<Dictionary<string, string>>(Intent.GetStringExtra("placemark"));//(Dictionary<string, string>)ActivitiesBringe.GetObject();

			SetContentView(Resource.Layout.MappaDetailsScreen);

			//titolo
			TextView txtTitle = this.FindViewById<TextView>(Resource.Id.txtTitle);
			txtTitle.Text = _Placemark["name"];

			//pulsante back
			ImageButton btnBack = this.FindViewById<ImageButton>(Resource.Id.btnBack);
			btnBack.Click += (sender, e) => 
			{
				this.Finish(); 
				this.OverridePendingTransition(Resource.Animation.slide_in_left, Resource.Animation.slide_out_right);
			};

			//pulsante close
			ImageButton btnClose= this.FindViewById<ImageButton>(Resource.Id.btnClose);
			btnClose.Click += (sender, e) => 
			{
				var topClass = (new ViewerScreen()).Class;
				if (this.Class != topClass)
				{
					Intent intent = new Intent(this, topClass);
					intent.AddFlags(ActivityFlags.NewTask);
					intent.AddFlags(ActivityFlags.ClearTop);

					StartActivity(intent);
				}
			};

			if(_Placemark["desc"] == "")
			{
				LinearLayout cntDesc = this.FindViewById<LinearLayout>(Resource.Id.cntDesc);

				cntDesc.Visibility = ViewStates.Invisible;
			}
			else
			{
				WebView webView = this.FindViewById<WebView>(Resource.Id.webView);

				string html = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">";
				html += "<html xmlns=\"http://www.w3.org/1999/xhtml\">";
				html += "<head>";
				html += "<meta http-equiv=\"Content-Type\" content=\"text/html; charset=UTF-8\" />";
				html += "<title>Documento senza titolo</title>";
				html += "<style>";
				html += "html,body {margin:0; padding: 0; height: 100%;}";
				html += "</style>";
				html += "</head>";
				html += "<body>";
				html += _Placemark["desc"];
				html += "</body>";
				html += "</html>";

				webView.LoadData(html, "text/html", "UTF-8");
			}

			LoadMap();
		}

		private void LoadMap()
		{
			GoogleMapOptions mapOptions = new GoogleMapOptions()
				.InvokeMapType(GoogleMap.MapTypeNormal)
					.InvokeZoomControlsEnabled(false)
					.InvokeCompassEnabled(false);

			MapView mapView = new MapView(this, mapOptions);

			mapView.OnCreate(null);
			mapView.OnResume();

			GoogleMap googleMap = mapView.Map;

			LatLng latLng = new LatLng(Convert.ToDouble(_Placemark["lat"]), Convert.ToDouble(_Placemark["lon"]));

			MarkerOptions mapOption = new MarkerOptions().SetPosition(latLng);
			googleMap.AddMarker(mapOption);

			googleMap.MoveCamera(CameraUpdateFactory.NewLatLngZoom(latLng, 12));

			googleMap.UiSettings.ScrollGesturesEnabled = false;
			googleMap.UiSettings.ZoomGesturesEnabled = false;

			FrameLayout mapFrame = this.FindViewById<FrameLayout>(Resource.Id.mapContent);

			mapFrame.AddView(mapView, new FrameLayout.LayoutParams(FrameLayout.LayoutParams.MatchParent, FrameLayout.LayoutParams.MatchParent));

			//pulsante mappa
			Button btnMap = this.FindViewById<Button>(Resource.Id.btnMap);
			btnMap.Click += (sender, e) => 
			{
				string lat = _Placemark["lat"].Replace(".", "").Replace(",", ".");
				string lon = _Placemark["lon"].Replace(".", "").Replace(",", ".");

				Intent intent = new Intent(Intent.ActionView,  Android.Net.Uri.Parse("http://maps.google.com/maps?q=" + lat + "," + lon));

				StartActivity(intent);
			};

			//pulsante navigazione
			Button btnNav = this.FindViewById<Button>(Resource.Id.btnNav);
			btnNav.Click += (sender, e) => 
			{
				string lat = _Placemark["lat"].ToString().Replace(".", "").Replace(",", ".");
				string lon = _Placemark["lon"].ToString().Replace(".", "").Replace(",", ".");

				Intent intent = new Intent(Intent.ActionView,  Android.Net.Uri.Parse("google.navigation:q=" + lat + "," + lon));

				StartActivity(intent);
			};
		}

		/*public override bool OnCreateOptionsMenu(IMenu menu)
		{
			//return base.OnCreateOptionsMenu(menu);
			MenuInflater inflater = new Android.Views.MenuInflater(this);
			inflater.Inflate(Resource.Menu.close_menu, menu);
			return true;
		}*/

		/*public override bool OnMenuItemSelected(int featureId, IMenuItem item)
		{
			var close = FindViewById(Resource.Id.menu_close);

			if (close.Id == item.ItemId)
			{
				var topClass = (new DocumentView()).Class;
				if (this.Class != topClass)
				{
					Intent intent = new Intent(this, topClass);
					intent.AddFlags(ActivityFlags.NewTask);
					intent.AddFlags(ActivityFlags.ClearTop);

					StartActivity(intent);
				}
			}

			if (Android.Resource.Id.Home == item.ItemId)
			{
				this.Finish(); 
				this.OverridePendingTransition(Resource.Animation.slide_in_left, Resource.Animation.slide_out_right);
			}

			return base.OnMenuItemSelected(featureId, item);
		}*/

		public override void OnBackPressed()
		{
			base.OnBackPressed();
			this.OverridePendingTransition(Resource.Animation.slide_in_left, Resource.Animation.slide_out_right);
		}
	}
}

