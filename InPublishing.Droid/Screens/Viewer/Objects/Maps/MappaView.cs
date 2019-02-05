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
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Gms.Common;
using Android.Graphics;
using Android.Support.V4;
using System.Xml.Linq;
using System.Globalization;
using Android.Locations;
using System.Net;
using Newtonsoft.Json;

namespace InPublishing
{
	public class MappaView : RelativeLayout
	{
		private Mappa _mappa;
		private XDocument _xDoc;
		private ProgressDialog _progressDialog;
		private GoogleMap _googleMap;
		//private MapFragment _mapFragment;
		private ViewerScreen _docView;
		private MapView _mapView;

		private List<Dictionary<string, string>> _Placemarks;

		public MappaView(Context context, Mappa map, ViewerScreen docView) : base (context)
		{
			_mappa = map;
			_docView = docView;

			//LoadMap();
			GoogleMapOptions mapOptions = new GoogleMapOptions()
				.InvokeMapType(GoogleMap.MapTypeNormal)
					.InvokeZoomControlsEnabled(false)
					.InvokeCompassEnabled(true);
			_mapView = new MapView(context, mapOptions);

			this.AddView(_mapView);

			_mapView.OnCreate(null);
			_mapView.OnResume();

			_googleMap = _mapView.Map;
			SetupMapIfNeeded();
		}

		protected override void OnDraw(Canvas canvas)
		{
			_mapView.OnCreate(null);
			_mapView.OnResume();
		}

		private void LoadMap()
		{
			/*_mapFragment = FragmentManager.FindFragmentByTag("map") as MapFragment;
			if (_mapFragment == null)
			{
				GoogleMapOptions mapOptions = new GoogleMapOptions()
					.InvokeMapType(GoogleMap.MapTypeNormal)
						.InvokeZoomControlsEnabled(false)
						.InvokeCompassEnabled(true);

				FragmentTransaction fragTx = FragmentManager.BeginTransaction();
				_mapFragment = MapFragment.NewInstance(mapOptions);
				fragTx.Add(_contentView.Id, _mapFragment, "map");
				fragTx.Commit();
			}*/
		}

		private void LoadPlacemarks()
		{
			var nodes = _xDoc.Descendants().Where(x => x.Name.LocalName == "Placemark");

			int count = 0;

			_Placemarks = new List<Dictionary<string, string>>();

			foreach(XElement node in nodes)
			{
				/*if(_IsDisposed)
				{
					return;
				}*/

				string name = "";
				string desc = "";
				string image = "";

				var elements = node.Elements().Where(x => x.Name.LocalName == "name");

				if(elements.Count() > 0)
				{
					name = elements.First<XElement>().Value;
				}

				elements = node.Elements().Where(x => x.Name.LocalName == "description");

				if(elements.Count() > 0)
				{
					desc = elements.First<XElement>().Value;
				}

				elements = node.Elements().Where(x => x.Name.LocalName == "ExtendedData");

				if(elements.Count() > 0)
				{
					var datas = elements.First<XElement>().Elements().Where(x => x.Name.LocalName == "Data");

					if(datas.Count() > 0)
					{
						var data = datas.First<XElement>();

						if(data.Attribute("name").Value == "gx_media_links")
						{
							var img = data.Elements().Where(x => x.Name.LocalName == "value").First<XElement>().Value;

							desc = "<img src='" + img + "' style='dispaly:block; margin: 0 auto; max-width: 100%; margin-bottom: 10px;' />" + desc;
						}
					}
				}

				elements = node.Elements().Where(x => x.Name.LocalName == "Point");

				if(elements.Count() == 0)
				{
					continue;
				}

				var point = elements.First<XElement>();

				string[] coordinates = point.Elements().Where(x => x.Name.LocalName == "coordinates").First<XElement>().Value.Split(',');

				NumberFormatInfo provider = new NumberFormatInfo( );

				provider.NumberDecimalSeparator = ".";
				provider.NumberGroupSeparator = ",";

				double lat = Convert.ToDouble(coordinates[1], provider);
				double lon = Convert.ToDouble(coordinates[0], provider);

				MarkerOptions mapOption = new MarkerOptions()
					.SetPosition(new LatLng(lat, lon))
						//.InvokeIcon(icon)
						//.SetSnippet(String.Format("{0}", desc))
						.SetTitle(String.Format("{0}", name));
				Marker mark = _googleMap.AddMarker(mapOption);

				var placemark = new Dictionary<string, string>();
				placemark.Add("id", mark.Id);
				placemark.Add("name", name);
				placemark.Add("desc", desc);
				placemark.Add("lat", mark.Position.Latitude.ToString());
				placemark.Add("lon", mark.Position.Longitude.ToString());

				_Placemarks.Add(placemark);

				if(count == 0 && !_googleMap.MyLocationEnabled)
				{
					if(_mappa.Raggio > 0)
					{

						_googleMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(lat, lon), zoomLevel(_mappa.Raggio)));
					}
					else
					{
						_googleMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(lat, lon), zoomLevel(10)));
					}

					HideLoadingOverlay();
				}

				count++;
			} 

			_googleMap.MyLocationChange += MapOnLocationChange;
		}

		private void MapOnLocationChange(object sender, GoogleMap.MyLocationChangeEventArgs e)
		{
            Location loc = e.Location;

			if(_googleMap.MyLocationEnabled)
			{
				if(_mappa.Raggio > 0)
				{
					_googleMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(loc.Latitude, loc.Longitude), zoomLevel(_mappa.Raggio)));
				}
				else
				{
					_googleMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(loc.Latitude, loc.Longitude), zoomLevel(10)));
				}
			}

			HideLoadingOverlay();
			_googleMap.MyLocationChange -= MapOnLocationChange;
		}

		private void MapOnInfoWindowClick(object sender, GoogleMap.InfoWindowClickEventArgs e)
		{
            Marker marker = e.Marker;

			var placemarks = _Placemarks.Where(x => x["id"] == marker.Id);

			if(placemarks != null && placemarks.Count() > 0)
			{
				var placemark = placemarks.First();

				Intent i = new Intent();
				i.SetClass(Application.Context, typeof(MappaDetailsScreen));

				i.PutExtra("placemark", JsonConvert.SerializeObject(placemark));

				//ActivitiesBringe.SetObject(placemark);

				_docView.StartActivity(i);
			}
			/*string lat = marker.Position.Latitude.ToString().Replace(".", "").Replace(",", ".");
			string lon = marker.Position.Longitude.ToString().Replace(".", "").Replace(",", ".");

			Intent intent = new Intent(Intent.ActionView,  Android.Net.Uri.Parse("google.navigation:q=" + lat + "," + lon));

			StartActivity(intent);*/



			//this.OverridePendingTransition(Resource.Animation.SlideInRight, Resource.Animation.SlideOutLeft);
		}

		private void SetupMapIfNeeded()
		{
			if(Reachability.InternetConnectionStatus() == NetworkStatus.NotReachable)
			{
				return;
			}

			if (_googleMap == null)
			{
				_googleMap = _mapView.Map;
				if (_googleMap != null)
				{
					var client = new WebClient();

					client.DownloadStringCompleted += (sender, e) => 
					{
						if (e.Error != null)
							return;

						try
						{
							_xDoc = XDocument.Parse(e.Result);

							_docView.RunOnUiThread(() => LoadPlacemarks());
							//RunOnUiThread(() => HideLoadingOverlay());
						}
						catch(Exception ex)
						{
							Console.WriteLine(ex.Message);
						}
					};

					client.DownloadStringAsync(new Uri(_mappa.Url));
					ShowLoadingOverlay();

					//_GoogleMap.SetInfoWindowAdapter(new CustomInfoWindowAdapter(this));
				}
			}
		}

		/*protected override void OnPause()
		{
			base.OnPause();

			// Pause the GPS - we won't have to worry about showing the 
			// location.
			_googleMap.MyLocationEnabled = false;

			//_map.MarkerClick -= MapOnMarkerClick;
			_googleMap.InfoWindowClick -= MapOnInfoWindowClick;
		}

		protected override void OnResume()
		{
			base.OnResume();
			SetupMapIfNeeded();

			_googleMap.MyLocationEnabled = _mappa.ShowUserLocation;

			// Setup a handler for when the user clicks on a marker.
			//_map.MarkerClick += MapOnMarkerClick;
			_googleMap.InfoWindowClick += MapOnInfoWindowClick;
		}*/

		private void ShowLoadingOverlay()
		{
			if (_progressDialog == null) 
			{
				_progressDialog = new ProgressDialog(this.Context);
				_progressDialog.SetTitle(Context.GetString(Resource.String.gen_loading));
				_progressDialog.SetMessage(Context.GetString(Resource.String.gen_loadingData));
				_progressDialog.Indeterminate = true;
				_progressDialog.SetCanceledOnTouchOutside(false);
				//_ProgressDialog.SetProgressStyle(ProgressDialogStyle.Horizontal);
				_progressDialog.Show();
			}
		}

		private void HideLoadingOverlay()
		{
			if (_progressDialog != null)
			{
				_progressDialog.Hide();
			}
			_progressDialog = null;
		}

		public static float zoomLevel (float distance)
		{
			float zoom=1;
			double E = 40075;
			//Log.i("Astrology", "result: "+ (Math.Log(E/distance)/Math.Log(2)+1));
			zoom = (float) Math.Round(Math.Log(E/distance)/Math.Log(2)+1);
			// to avoid exeptions
			if (zoom>21) zoom=21;
			if (zoom<1) zoom =1;

			return zoom;
		}

	}
}

