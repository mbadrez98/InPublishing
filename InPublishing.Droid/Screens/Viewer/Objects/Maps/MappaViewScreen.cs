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
	[Activity (Label = "MappaViewScreen", Theme = "@style/Blue.NoActionBar")]			
	public class MappaViewScreen : BaseModalScreen
	{
		private Mappa _Mappa;
		private XDocument _XDoc;
		//private ProgressDialog _ProgressDialog;
		private GoogleMap _GoogleMap;
		private MapFragment _MapFragment;

		private List<Dictionary<string, string>> _Placemarks;
		private LoadingOverlay _LoadingOverlay;

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

			LoadMap();

			//SetupMapIfNeeded();
			DownloadKML();
		}

		private void LoadMap()
		{
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
		}

		private void LoadPlacemarks()
		{
			var nodes = _XDoc.Descendants().Where(x => x.Name.LocalName == "Placemark");

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

							desc = "<img src='" + img + "' style='display:block; margin: 0 auto; max-width: 100%; margin-bottom: 10px;' />" + desc;
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
				Marker mark = _GoogleMap.AddMarker(mapOption);

				var placemark = new Dictionary<string, string>();
				placemark.Add("id", mark.Id);
				placemark.Add("name", name);
				placemark.Add("desc", desc);
				placemark.Add("lat", mark.Position.Latitude.ToString());
				placemark.Add("lon", mark.Position.Longitude.ToString());

				_Placemarks.Add(placemark);

				if(count == 0 && !_GoogleMap.MyLocationEnabled)
				{
					if(_Mappa.Raggio > 0)
					{

						_GoogleMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(lat, lon), zoomLevel(_Mappa.Raggio)));
					}
					else
					{
						_GoogleMap.AnimateCamera(CameraUpdateFactory.NewLatLngZoom(new LatLng(lat, lon), zoomLevel(10)));
					}

					HideLoadingOverlay();
				}

				count++;
			} 

			_GoogleMap.MyLocationChange += MapOnLocationChange;
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

			HideLoadingOverlay();
			_GoogleMap.MyLocationChange -= MapOnLocationChange;
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

				StartActivity(i);
			}

			/*string lat = marker.Position.Latitude.ToString().Replace(".", "").Replace(",", ".");
			string lon = marker.Position.Longitude.ToString().Replace(".", "").Replace(",", ".");

			Intent intent = new Intent(Intent.ActionView,  Android.Net.Uri.Parse("google.navigation:q=" + lat + "," + lon));

			StartActivity(intent);*/


			//this.OverridePendingTransition(Resource.Animation.SlideInRight, Resource.Animation.SlideOutLeft);
		}

		private void DownloadKML()
		{
			if(Reachability.InternetConnectionStatus() == NetworkStatus.NotReachable)
			{
				return;
			}

			var client = new WebClient();

			client.DownloadDataCompleted += (sender, e) => 
			{
				if (e.Error != null)
					return;

				try
				{
					var localKMZ = Utils.md5(_Mappa.Url) + ".zip";
					var localKML = Utils.md5(_Mappa.Url) + ".kml";
					var bytes = e.Result;
					var tmpPath = System.Environment.GetFolderPath (System.Environment.SpecialFolder.Personal);

					File.WriteAllBytes (Path.Combine (tmpPath, localKMZ), bytes);

					FileStream fs = File.OpenRead(Path.Combine (tmpPath, localKMZ));
					ZipFile zf = new ZipFile(fs);

					foreach (ZipEntry zipEntry in zf) 
					{
						if(zipEntry.IsFile && zipEntry.Name.Contains(".kml"))
						{
							using (FileStream streamWriter = File.Create(Path.Combine (tmpPath, localKML))) 
							{
								byte[] buffer = new byte[8192];
								Stream zipStream = zf.GetInputStream(zipEntry);
								StreamUtils.Copy(zipStream, streamWriter, buffer);
							}

							break;
						}
					}

					if(File.Exists(Path.Combine (tmpPath, localKML)))
					{
						_XDoc = XDocument.Load(Path.Combine (tmpPath, localKML));

						RunOnUiThread(() => {
							LoadPlacemarks();

							File.Delete(Path.Combine (tmpPath, localKMZ));
							File.Delete(Path.Combine (tmpPath, localKML));
						});
					}

					RunOnUiThread(() => HideLoadingOverlay());

				}
				catch(Exception ex)
				{
					Console.WriteLine(ex.Message);

					RunOnUiThread(() => HideLoadingOverlay());
				}
			};

			client.DownloadDataAsync(new Uri(_Mappa.Url));
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
					var client = new WebClient();

					client.DownloadStringCompleted += (sender, e) => 
					{
						if (e.Error != null)
							return;

						try
						{
							_XDoc = XDocument.Parse(e.Result);

							RunOnUiThread(() => LoadPlacemarks());
							//RunOnUiThread(() => HideLoadingOverlay());
						}
						catch(Exception ex)
						{
							Console.WriteLine(ex.Message);
						}
					};

					client.DownloadStringAsync(new Uri(_Mappa.Url));
					ShowLoadingOverlay();

					//_GoogleMap.SetInfoWindowAdapter(new CustomInfoWindowAdapter(this));
				}
			}
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

		/*private void ShowLoadingOverlay()
		{
			if (_ProgressDialog == null) 
			{
				_ProgressDialog = new ProgressDialog(this);
				_ProgressDialog.SetTitle("Caricamento...");
				_ProgressDialog.SetMessage("Caricamento dati...");
				_ProgressDialog.Indeterminate = true;
				_ProgressDialog.SetCanceledOnTouchOutside(false);
				//_ProgressDialog.SetProgressStyle(ProgressDialogStyle.Horizontal);
				_ProgressDialog.Show();
			}
		}

		private void HideLoadingOverlay()
		{
			if (_ProgressDialog != null)
			{
				_ProgressDialog.Hide();
			}
			_ProgressDialog = null;
		}*/

		private void ShowLoadingOverlay()
		{
			if (_LoadingOverlay == null) 
			{
				_LoadingOverlay = new LoadingOverlay (Application.Context, GetString(Resource.String.gen_loading) + "...");
				_contentView.AddView (_LoadingOverlay);

				//this.BringChildToFront(_LoadingOverlay);
			}

			_LoadingOverlay.Show();
		}

		private void HideLoadingOverlay()
		{
			if (_LoadingOverlay != null)
				_LoadingOverlay.Hide ();
			//_LoadingOverlay = null;
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

		/*class CustomInfoWindowAdapter : Java.Lang.Object, GoogleMap.IInfoWindowAdapter 
		{
			MappaViewScreen parent;

			// These a both viewgroups containing an ImageView with id "badge" and two TextViews with id
			// "title" and "snippet".
			private readonly View _Window;

			internal CustomInfoWindowAdapter (MappaViewScreen parent) 
			{
				_Window = parent.LayoutInflater.Inflate (Resource.Layout.CustomInfoWindow, null);
				//mContents = parent.LayoutInflater.Inflate (Resource.Layout.custom_info_contents, null);
				//mOptions = (RadioGroup) parent.FindViewById (Resource.Id.custom_info_window_options);
			}

			public View GetInfoWindow (Marker marker) 
			{

				TextView txtTitle = _Window.FindViewById<TextView>(Resource.Id.txtTitle);
				txtTitle.Text = marker.Title;

				TextView txtDesc = _Window.FindViewById<TextView>(Resource.Id.txtSnippet);
				txtDesc.Text = marker.Snippet;

				Button btnNav = _Window.FindViewById<Button>(Resource.Id.btnNav);
				btnNav.Click += (sender, e) => 
				{
					Toast.MakeText(parent, "naviga", ToastLength.Short).Show();
				};

				return _Window;
			}

			public View GetInfoContents (Marker marker) 
			{

				return _Window;
			}
		}*/
	}
}

