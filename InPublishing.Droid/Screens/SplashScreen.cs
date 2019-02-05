using System;

//using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System.IO;

//using Android.Content.PM;
using Android.Content.Res;
using Android.Content.PM;
using System.Collections.Generic;
using Android.Gms.Common;
using Android.Util;
using Android.Gms.Gcm;
using Java.Lang;
using Android.Net;
using Android.Net.Wifi;

using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Com.Nostra13.Universalimageloader.Core;
using Com.Nostra13.Universalimageloader.Cache.Disc.Naming;
using Com.Nostra13.Universalimageloader.Core.Assist;
using Android;

namespace InPublishing
{
	[Activity (MainLauncher = true, Theme = "@style/Splash", NoHistory = true)]
	public class SplashScreen : Activity
	{
		public static string EXTRA_MESSAGE = "message";

		public static string PROPERTY_REG_ID = "registration_id";

		private static string PROPERTY_APP_VERSION = "appVersion";

		private static int PLAY_SERVICES_RESOLUTION_REQUEST = 9000;

		string SENDER_ID = "512322243493";

		static string TAG = "GCM MB";

		GoogleCloudMessaging gcm;

		string regid;

        private bool _alreadyRun;

		protected override void OnCreate (Bundle bundle)
        {
			base.OnCreate(bundle);
            SetContentView(Resource.Layout.Splash);

            //this.Window.AddFlags(WindowManagerFlags.Fullscreen);
            //this.Window.ClearFlags(WindowManagerFlags.Fullscreen);
            /*System.Threading.ThreadPool.QueueUserWorkItem(state =>
			{
				//Thread.Sleep(2000); //wait 2 sec
                RunOnUiThread(() => Initialize()); //fade out the view
			});	*/
            
            new Handler().PostDelayed(() =>
			{
				Initialize();
			}, 500);
		}

        private void Initialize()
        {
			//Xamarin.Insights.Initialize(global::InPublishing.XamarinInsights.ApiKey, this);
            AppCenter.Start("cab73ad7-da5e-4ce1-a472-6d48df685f2f", typeof(Analytics), typeof(Crashes));

			//image loader
			var config = new ImageLoaderConfiguration.Builder(ApplicationContext);
			config.ThreadPriority(Java.Lang.Thread.NormPriority - 2);
			config.DenyCacheImageMultipleSizesInMemory();
			config.DiskCacheFileNameGenerator(new Md5FileNameGenerator());
			config.DiskCacheSize(50 * 1024 * 1024); // 50 MiB
			config.TasksProcessingOrder(QueueProcessingType.Lifo);
			config.WriteDebugLogs(); // Remove for release app

			// Initialize ImageLoader with configuration.
			ImageLoader.Instance.Init(config.Build());



			if (!DataManager.AlreadyRegistered<ISettingsManager>())
			{
				DataManager.RegisterReference<ISettingsManager, SettingsManager>();
			}

			DataManager.Get<ISettingsManager>().AndroidGetSettings = p =>
			{
				string content;
				using (StreamReader sr = new StreamReader(Assets.Open("AppSettings.xml")))
				{
					content = sr.ReadToEnd();
					return content;
				}
			};

			DataManager.Get<ISettingsManager>().Load();

			//se è attiva la condivisione setto la cartella nella root
			string sharedPath = "";
			if (DataManager.Get<ISettingsManager>().Settings.ShareDir)
			{
				string appName = ApplicationInfo.LoadLabel(PackageManager);
				sharedPath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, appName);
			}
			else
			{
				sharedPath = GetExternalFilesDir("shared").AbsolutePath;
			}

            if(this.CanAccessExternal())
            {
                if (!Directory.Exists(sharedPath))
                {
                    Directory.CreateDirectory(sharedPath);
                }
            }

			//cartella per le pubblicazioni nascosta
/*#if DEBUG
			string docPath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/.Inpublishing/Publications";
			string notePath = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/.Inpublishing/Notes";*/
//#else
            string docPath = GetExternalFilesDir ("publications").AbsolutePath;
            string notePath = GetExternalFilesDir ("notes").AbsolutePath;
//#endif

			//Se non esiste la creo
			if (!Directory.Exists(docPath))
			{
				Directory.CreateDirectory(docPath);
			}

			DataManager.Get<ISettingsManager>().Settings.Debug = true;
			DataManager.Get<ISettingsManager>().Settings.DocPath = docPath;
			DataManager.Get<ISettingsManager>().Settings.SharedPath = sharedPath;
			DataManager.Get<ISettingsManager>().Settings.NotePath = notePath;

			DataManager.Get<ISettingsManager>().Settings.AndroidContext = this;

			/*WifiManager manager = Application.Context.GetSystemService (Context.WifiService) as WifiManager;
            WifiInfo info = manager.ConnectionInfo;
            string address = info.MacAddress;*///uuid

			ISharedPreferences prefs = GetSharedPreferences(this.PackageName, FileCreationMode.Private);

			string deviceId = prefs.GetString("UniqueDeviceIdentifier", "");

			if (deviceId == "")
			{
				//Guid guid = Guid.NewGuid();
				//deviceId = guid.ToString ();
				deviceId = Android.Provider.Settings.Secure.GetString(ApplicationContext.ContentResolver, Android.Provider.Settings.Secure.AndroidId);
				ISharedPreferencesEditor editor = prefs.Edit();
				editor.PutString("UniqueDeviceIdentifier", deviceId);
				editor.Apply();
			}

			DataManager.Get<ISettingsManager>().Settings.DeviceUID = deviceId;
			DataManager.Get<ISettingsManager>().Settings.DeviceOS = DocumentOS.Android;
			DataManager.Get<ISettingsManager>().Settings.DeviceType = Utility.IsTablet(this) ? DocumentDevice.Tablet : DocumentDevice.Phone;

			//statistiche
			DataManager.Get<ISettingsManager>().Settings.StatsPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal);

			FileSystemManager.AndroidCopyFunc = delegate ()
			{
                //se è singola e devo importare i documenti elimino quelli presenti
                if(DataManager.Get<ISettingsManager>().Settings.SingolaApp && Directory.Exists(DataManager.Get<ISettingsManager>().Settings.DocPath))
                {
                    System.IO.DirectoryInfo di = new DirectoryInfo(DataManager.Get<ISettingsManager>().Settings.DocPath);

                    foreach (FileInfo file in di.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                }

				string dPath = DataManager.Get<ISettingsManager>().Settings.SharedPath;
				AssetManager am = Resources.Assets;
				var files = am.List("pub");
				foreach (string file in files)
				{
					using (Stream source = Assets.Open("pub/" + file))
					{
						if (!File.Exists(Path.Combine(dPath, file)))
						{
							using (var dest = System.IO.File.Create(Path.Combine(dPath, file)))
							{
								source.CopyTo(dest);
							}
						}
					}
				}
			};

			FileSystemManager.AndroidCountFunc = p =>
			{
				AssetManager am = Resources.Assets;
				return am.List("pub").Length;
			};

			//preferenze
			if (!DataManager.AlreadyRegistered<IPreferencesManager>())
			{
				DataManager.RegisterReference<IPreferencesManager, PreferencesManager>();
			}

			DataManager.Get<ISettingsManager>().Load();

			//ordinamento
			if (!DataManager.Get<IPreferencesManager>().Preferences.AlreadyRun)
			{
				var order = DataManager.Get<ISettingsManager>().Settings.EdicolaOrder;

				DataManager.Get<IPreferencesManager>().Preferences.EdicolaOrder = order;
				DataManager.Get<IPreferencesManager>().Save();
			}

			//notifiche
			if (CheckPlayServices())
			{
				gcm = GoogleCloudMessaging.GetInstance(this);
				regid = GetRegistrationId(ApplicationContext);
				//regid = "";
				if (regid == "")
				{
					//ConnectivityManager connectivityManager = (ConnectivityManager) GetSystemService(ConnectivityService);
					NetworkStatus internetStatus = Reachability.InternetConnectionStatus();
					if (internetStatus == NetworkStatus.ReachableViaCarrierDataNetwork || internetStatus == NetworkStatus.ReachableViaWiFiNetwork)
					{
						RegisterInBackground();
					}
				}
				else //anche se ho già il token registro comunque il dispositivo sull'edicola, saltando la richiesta del token però
				{
					Thread _Thread = new Thread(() =>
					{
						try
						{
							SendRegistrationIdToBackend();
						}
						catch (Java.IO.IOException ex)
						{
							Log.Info(TAG, ex.Message);
						}
					});
					_Thread.Start();
				}
			}
			else
			{
				Log.Info(TAG, "No valid Google Play Services APK found.");
			}

            //se la versione è diversa setto come se fosse la prima volta che l'app parte
            string version = PackageManager.GetPackageInfo(PackageName, 0).VersionCode.ToString();
            if (DataManager.Get<IPreferencesManager>().Preferences.AppVersion == "" || DataManager.Get<IPreferencesManager>().Preferences.AppVersion != version)
            {
                DataManager.Get<IPreferencesManager>().Preferences.DocImported = false;
            }

            DataManager.Get<IPreferencesManager>().Preferences.AppVersion = version;
            DataManager.Get<IPreferencesManager>().Save();

			if (DataManager.Get<ISettingsManager>().Settings.EdicolaEnabled && !DataManager.Get<ISettingsManager>().Settings.SingolaApp)
			{
				//var intent = new Intent(this, typeof(EdicolaScreen));
				var intent = new Intent(this, typeof(HomeScreen));
				intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
				StartActivity(intent);
			}
			else
			{
				var intent = new Intent(this, typeof(DownloaderScreen));
				intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
				StartActivity(intent);
			}

			StatisticheManager.SendStats();

			MBDownloadManager.RemoveAll();

			//google analytics
			AnalyticsService.Initialize(this);
			AnalyticsService.SendEvent("App", AnalyticsEventAction.AppStart); 

            DataManager.Get<IPreferencesManager>().Preferences.AlreadyRun = true;
            DataManager.Get<IPreferencesManager>().Save();        
        }

		protected override void OnResume ()
		{
			base.OnResume ();
			// OPTIONAL! The following block of code removes all notifications from the status bar.
			NotificationManager notificationManager = (NotificationManager)GetSystemService (Context.NotificationService);
			notificationManager.CancelAll ();
		}

		private bool CheckPlayServices ()
		{
			int resultCode = GooglePlayServicesUtil.IsGooglePlayServicesAvailable (this);
			if (resultCode != ConnectionResult.Success) {
				if (GooglePlayServicesUtil.IsUserRecoverableError (resultCode)) {
					GooglePlayServicesUtil.GetErrorDialog (resultCode, this, PLAY_SERVICES_RESOLUTION_REQUEST).Show ();
				} else {
					Log.Info (TAG, "This device is not supported.");
					Finish ();
				}
				return false;
			}
			return true;
		}

		private void StoreRegistrationId (Context context, string regId)
		{
			ISharedPreferences prefs = GetGcmPreferences (context);
			int appVersion = GetAppVersion (context);
			Log.Info (TAG, "Saving regId on app version " + appVersion);
			ISharedPreferencesEditor editor = prefs.Edit ();
			editor.PutString (PROPERTY_REG_ID, regId);
			editor.PutInt (PROPERTY_APP_VERSION, appVersion);
			editor.Commit ();
		}

		private string GetRegistrationId (Context context)
		{
			ISharedPreferences prefs = GetGcmPreferences (context);
			string registrationId = prefs.GetString (PROPERTY_REG_ID, "");
			if (registrationId == "") {
				Log.Info (TAG, "Registration not found.");
				return "";
			}
			// Check if app was updated; if so, it must clear the registration ID
			// since the existing regID is not guaranteed to work with the new
			// app version.
			int registeredVersion = prefs.GetInt (PROPERTY_APP_VERSION, int.MinValue);
			int currentVersion = GetAppVersion (context);
			if (registeredVersion != currentVersion) {
				Log.Info (TAG, "App version changed.");
				return "";
			}
			return registrationId;
		}

		/**
	     * Registers the application with GCM servers asynchronously.
	     * <p>
	     * Stores the registration ID and the app versionCode in the application's
	     * shared preferences.
	     */
        private void RegisterInBackground ()
		{
			Thread _Thread = new Thread (() => {
				try {
					if (gcm == null) {
						gcm = GoogleCloudMessaging.GetInstance (ApplicationContext);
					}
					regid = gcm.Register (SENDER_ID);
					SendRegistrationIdToBackend ();
					StoreRegistrationId (ApplicationContext, regid);
				} catch (Java.IO.IOException ex) {
					Log.Info (TAG, ex.Message);
				}
				//return msg;					
			});
			_Thread.Start ();
		}

		private static int GetAppVersion (Context context)
		{
			try {
				PackageInfo packageInfo = context.PackageManager.GetPackageInfo (context.PackageName, 0);
				return packageInfo.VersionCode;
			} catch (Android.Content.PM.PackageManager.NameNotFoundException e) {
				// should never happen
				throw new RuntimeException ("Could not get package name: " + e);
			}
		}

		/**
	     * @return Application's {@code SharedPreferences}.
	    */private ISharedPreferences GetGcmPreferences (Context context)
		{
			// This sample app persists the registration ID in shared preferences, but
			// how you store the regID in your app is up to you.
			return GetSharedPreferences (this.PackageName, FileCreationMode.Private);
		}

		/**
	     * Sends the registration ID to your server over HTTP, so it can use GCM/HTTP or CCS to send
	     * messages to your app. Not needed for this demo since the device sends upstream messages
	     * to a server that echoes back the message using the 'from' address in the message.
	     */private void SendRegistrationIdToBackend ()
		{
			if (!DataManager.Get<ISettingsManager> ().Settings.NotificationsEnabled || !DataManager.Get<ISettingsManager> ().Settings.EdicolaEnabled || DataManager.Get<ISettingsManager> ().Settings.DownloadUrl == "") {
				return;
			}

            var data = this.DeviceInfo ();
			data.Add ("deviceToken", regid);

			//data.
			Notification notif = new Notification ();
			if (notif.RegisterDevice (data)) {
				Console.WriteLine ("Registrazione device: " + regid);
			}
		}
    }
}
