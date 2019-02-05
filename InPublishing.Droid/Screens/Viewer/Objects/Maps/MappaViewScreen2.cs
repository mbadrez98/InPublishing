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
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using System.Xml.Linq;
using System.Net;
using System.Globalization;
using Android.Locations;
using Newtonsoft.Json;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;

namespace InPublishing
{
	[Activity (Label = "MappaViewScreen2", Theme = "@style/Blue.NoActionBar")]			
	public class MappaViewScreen2 : BaseModalScreen
	{
		private Mappa _Mappa;
		private XDocument _XDoc;
		//private ProgressDialog _ProgressDialog;
		private GoogleMap _GoogleMap;
		private MapFragment _MapFragment;

        private Dictionary<string, Placemark> _Placemarks;
		private LoadingOverlay _LoadingOverlay;

        private KML _Kml;

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);
			//this.OverridePendingTransition(Resource.Animation.SlideInRight, Resource.Animation.SlideOutLeft);
			this.Window.AddFlags(WindowManagerFlags.Fullscreen);

			/*ActionBar.SetDisplayHomeAsUpEnabled(false);
			ActionBar.SetDisplayShowHomeEnabled(false);*/

			var par = (RelativeLayout.LayoutParams)_btnClose.LayoutParameters;
			par.AddRule(LayoutRules.AlignParentRight, 0);
			par.AddRule(LayoutRules.AlignParentLeft);

			_btnClose.LayoutParameters = par;

			_Mappa = JsonConvert.DeserializeObject<Mappa>(Intent.GetStringExtra("mappa"));//(Mappa)ActivitiesBringe.GetObject();

			_MapFragment = FragmentManager.FindFragmentByTag("map") as MapFragment;
			if (_MapFragment == null)
			{
				GoogleMapOptions mapOptions = new GoogleMapOptions()
					.InvokeMapType(GoogleMap.MapTypeNormal)
						.InvokeZoomControlsEnabled(false)
						.InvokeCompassEnabled(true);

				FragmentTransaction fragTx = FragmentManager.BeginTransaction();
				_MapFragment = MapFragment.NewInstance(mapOptions);
				fragTx.Add(_contentView.Id, _MapFragment, "map");
				fragTx.Commit();
			}

			SetupMapIfNeeded();
		}

		private void SetupMapIfNeeded()
		{
			if(Reachability.InternetConnectionStatus() == NetworkStatus.NotReachable)
            {
                return;
            }

            if (_GoogleMap == null)
            {
                _GoogleMap = _MapFragment.Map;
                if (_GoogleMap != null)
                {
					var cachePath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "..", "cache");

					_Kml = new KML(_Mappa.Url, Path.Combine(cachePath, "maps"));

					_Kml.Loaded += () =>
					{

						RunOnUiThread(() =>
						{
							LoadPlacemarks();
							HideLoadingOverlay();
						});
					};

					_Kml.Load();

					ShowLoadingOverlay();
                }
            }
		}

        private void LoadPlacemarks()
		{
            _Placemarks = new Dictionary<string, Placemark>();

            List<LatLng> points = new List<LatLng>();

            foreach (Placemark place in _Kml.Placemarks)
			{
                if(place.Type == PlacemarkType.Point)
                {
					MarkerOptions mapOption = new MarkerOptions()
                        .SetPosition(new LatLng(place.Coordinates[0][0], place.Coordinates[0][1]))						
						//.SetSnippet(String.Format("{0}", desc))
                        .SetTitle(String.Format("{0}", place.Name));

                    if(!place.Style.IconPath.StartsWith("http"))
                    {
                        var img = BitmapDescriptorFactory.FromPath(System.IO.Path.Combine(_Kml.BasePath, place.Style.IconPath));
                        mapOption.InvokeIcon(img);
                    }
                    
					Marker mark = _GoogleMap.AddMarker(mapOption);

                    _Placemarks.Add(mark.Id, place);

                    points.Add(mark.Position);					
                }
                else if(place.Type == PlacemarkType.Polyline)
                {
                    PolylineOptions options = new PolylineOptions()
                        .Visible(true)
                        .InvokeColor(Android.Graphics.Color.Transparent.FromHexA(place.Style.LineColor))
                        .InvokeWidth(place.Style.LineWidth);

                    foreach(var c in place.Coordinates)
                    {
                        options.Add(new LatLng(c[0], c[1]));
                    } 

                    var line = _GoogleMap.AddPolyline(options);
                    points.AddRange(line.Points);
                }
                else if (place.Type == PlacemarkType.Polygon)
				{
					PolygonOptions options = new PolygonOptions()
						.Visible(true)
						.InvokeStrokeColor(Android.Graphics.Color.Transparent.FromHexA(place.Style.LineColor))
                        .InvokeFillColor(Android.Graphics.Color.Transparent.FromHexA(place.Style.FillColor))
						.InvokeStrokeWidth(place.Style.LineWidth);

					foreach (var c in place.Coordinates)
					{
						options.Add(new LatLng(c[0], c[1]));
					}

                    var poly = _GoogleMap.AddPolygon(options);

                    points.AddRange(poly.Points);
				}
			}

            SetPointsBounds(points);

            if(_Kml.Placemarks.Count == 0)
            {
				_GoogleMap.MyLocationChange += MapOnLocationChange;
			}			
		}		

		private void MapOnLocationChange(object sender, GoogleMap.MyLocationChangeEventArgs e)
		{
            Location loc = e.Location;

			if(_GoogleMap.MyLocationEnabled)
			{
				if(_Mappa.Raggio > 0)
				{
					_GoogleMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(loc.Latitude, loc.Longitude), zoomLevel(_Mappa.Raggio)));
				}
				else
				{
					_GoogleMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(loc.Latitude, loc.Longitude), zoomLevel(10)));
				}
			}

			_GoogleMap.MyLocationChange -= MapOnLocationChange;
		}

		private void MapOnInfoWindowClick(object sender, GoogleMap.InfoWindowClickEventArgs e)
		{
            Marker marker = e.Marker;

            if(_Placemarks.ContainsKey(marker.Id))
			{
				var placemark = _Placemarks[marker.Id];

				var obj = new Dictionary<string, string>();
                obj.Add("name", placemark.Name);
                obj.Add("desc", placemark.Description);
				obj.Add("lat", marker.Position.Latitude.ToString());
				obj.Add("lon", marker.Position.Longitude.ToString());

				Intent i = new Intent();
				i.SetClass(Application.Context, typeof(MappaDetailsScreen));

                i.PutExtra("placemark", JsonConvert.SerializeObject(obj));

				StartActivity(i);
			}			
		}

        private void SetPointsBounds(List<LatLng> points)
		{
			LatLngBounds.Builder builder;
			float scale = ApplicationContext.Resources.DisplayMetrics.Density;
			int padding = (int)(40 * scale + 0.5f);
			builder = new LatLngBounds.Builder();

            //foreach (Marker marker in markers)
            //{
            //    builder.Include(marker.Position);
            //}

            Double minLatitude = 0;
			Double minLongitude = 0;
			Double maxLatitude = 0;
			Double maxLongitude = 0;

            foreach(var point in points)
            {
                if (minLatitude == 0.0f)
				{ // No matter on wich var we check
                    minLatitude = point.Latitude;
                    minLongitude = point.Longitude;
                    maxLatitude = point.Latitude;
                    maxLongitude = point.Longitude;
				}
				else
				{
                    if (point.Latitude < minLatitude)
					{
						minLatitude = point.Latitude;
					}
					if (point.Latitude > maxLatitude)
					{
						maxLatitude = point.Latitude;
					}
                    if (point.Longitude < minLongitude)
					{
						minLongitude = point.Longitude;
					}
					if (point.Longitude > maxLongitude)
					{
						maxLongitude = point.Longitude;
					}
				}
            }

            builder.Include(new LatLng(minLatitude, minLongitude));
            builder.Include(new LatLng(maxLatitude, maxLongitude));

			LatLngBounds bounds = builder.Build();
			CameraUpdate cu = CameraUpdateFactory.NewLatLngBounds(bounds, padding);
			_GoogleMap.AnimateCamera(cu, 800, null);
		}

		protected override void OnPause()
		{
			base.OnPause();

			// Pause the GPS - we won't have to worry about showing the 
			// location.
			_GoogleMap.MyLocationEnabled = false;

			//_map.MarkerClick -= MapOnMarkerClick;
			_GoogleMap.InfoWindowClick -= MapOnInfoWindowClick;
		}

		protected override void OnResume()
		{
			base.OnResume();
			SetupMapIfNeeded();

			_GoogleMap.MyLocationEnabled = _Mappa.ShowUserLocation;

			// Setup a handler for when the user clicks on a marker.
			//_map.MarkerClick += MapOnMarkerClick;
			_GoogleMap.InfoWindowClick += MapOnInfoWindowClick;
		}

		private void ShowLoadingOverlay()
		{
			if (_LoadingOverlay == null) 
			{
				_LoadingOverlay = new LoadingOverlay (Application.Context, GetString(Resource.String.gen_loading) + "...");
				_contentView.AddView (_LoadingOverlay);
			}

			_LoadingOverlay.Show();
		}

		private void HideLoadingOverlay()
		{
			if (_LoadingOverlay != null)
				_LoadingOverlay.Hide ();
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

