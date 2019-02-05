using System.Linq;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Views;
using Android.Widget;

using ExpansionDownloader;
using ExpansionDownloader.Client;
using ExpansionDownloader.Database;
using ExpansionDownloader.Service;
using System.Collections.Generic;
using Android.Content.Res;

namespace InPublishing
{
	[Activity(Label = "DownloaderActivity", Theme = "@style/Splash", NoHistory = true)]			
	public class DownloaderScreen : Activity, IDownloaderClient
	{
		private IDownloaderService downloaderService;
		private IDownloaderServiceConnection downloaderServiceConnection;
		private DownloaderState downloaderState;
		//private bool isPaused;
		private ProgressDialog _progressDialog;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

            //se ci sono documenti integrati controllo se è la prima volta che l'app viene lanciata
            //se è la prima volta importo i documenti altrimenti l'app prosegue normalmente
            //se non ci sono documenti integrati cerco il file di espansione

            AssetManager am = Resources.Assets;
            if (am.List("pub").Length > 0)
            {
                if (!DataManager.Get<IPreferencesManager>().Preferences.DocImported)
                {
                    _progressDialog = new ProgressDialog(this);

                    var importTask = new AnonymousAsyncTask<string, string, bool>((p) =>
                        {
                            RunOnUiThread(() =>
                                {
                                    _progressDialog.Indeterminate = true;
                                    _progressDialog.SetProgressPercentFormat(null);
                                    _progressDialog.SetMessage(GetString(Resource.String.apkDown_loadResources));
                                    _progressDialog.Show();
                                });

                            FileSystemManager.ImportDocuments();

                            return true;

                        }, (bd) =>
                        {
                            StartApp();
                        });

                    importTask.Execute(new Java.Lang.Object[] { });
                }
                else
                {
                    StartApp();
                }
            }
            else
            {
                StartApp();
                _progressDialog = new ProgressDialog(this);
                _progressDialog.SetMessage("");
                _progressDialog.SetTitle(GetString(Resource.String.apkDown_downResources));
                _progressDialog.SetCancelable(false);
                _progressDialog.SetProgressStyle(ProgressDialogStyle.Horizontal);
                _progressDialog.Indeterminate = false;
                _progressDialog.SetProgressNumberFormat(null);

                _progressDialog.Show();

                var delivered = this.AreExpansionFilesDelivered();

                if (delivered)
                {
                    _progressDialog.SetMessage(GetString(Resource.String.apkDown_completed));
                    CheckDownloadedFile();
                }
                else if (!this.GetExpansionFiles())
                {
                    this.downloaderServiceConnection = ClientMarshaller.CreateStub(this, typeof(MyDownloaderService));
                }
            }
		}

		protected override void OnResume()
		{
			if (this.downloaderServiceConnection != null)
			{
				this.downloaderServiceConnection.Connect(this);
			}

			base.OnResume();
		}

		protected override void OnStop()
		{
			if (this.downloaderServiceConnection != null)
			{
				this.downloaderServiceConnection.Disconnect(this);
			}

			base.OnStop();
		}

		public void OnDownloadProgress(DownloadProgressInfo progress)
		{
			_progressDialog.SetMessage(GetString(Resource.String.apkDown_downloading) + " " + Helpers.GetDownloadProgressString(progress.OverallProgress, progress.OverallTotal));
			_progressDialog.Max = (int)(progress.OverallTotal >> 8);
			_progressDialog.Progress = (int)(progress.OverallProgress >> 8);
		}

		public void OnDownloadStateChanged(DownloaderState newState)
		{
			if (this.downloaderState != newState)
			{
				this.downloaderState = newState;

				RunOnUiThread(() => {_progressDialog.SetMessage(GetDownloaderStringFromState(newState));});
			}

			if(newState != DownloaderState.Completed)
			{
				_progressDialog.Indeterminate = newState.IsIndeterminate();
			}
			else
			{
				_progressDialog.SetMessage(GetString(Resource.String.apkDown_completed));
				CheckDownloadedFile();
			}
		}

		public void OnServiceConnected(Messenger m)
		{
			this.downloaderService = ServiceMarshaller.CreateProxy(m);
			this.downloaderService.OnClientUpdated(this.downloaderServiceConnection.GetMessenger());
		}

		private bool AreExpansionFilesDelivered()
		{
			var downloads = DownloadsDatabase.GetDownloads();

			return downloads.Any() && downloads.All(x => Helpers.DoesFileExist(this, x.FileName, x.TotalBytes, false));
		}

		private bool GetExpansionFiles()
		{
			bool result = false;

			// Build the intent that launches this activity.
			Intent launchIntent = this.Intent;
			var intent = new Intent(this, typeof(DownloaderScreen));
			intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
			intent.SetAction(launchIntent.Action);

			if (launchIntent.Categories != null)
			{
				foreach (string category in launchIntent.Categories)
				{
					intent.AddCategory(category);
				}
			}

			// Build PendingIntent used to open this activity when user 
			// taps the notification.
			PendingIntent pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.UpdateCurrent);

			// Request to start the download
			DownloadServiceRequirement startResult = DownloaderService.StartDownloadServiceIfRequired(this, pendingIntent, typeof(MyDownloaderService));

			// The DownloaderService has started downloading the files, 
			// show progress otherwise, the download is not needed so  we 
			// fall through to starting the actual app.
			if (startResult != DownloadServiceRequirement.NoDownloadRequired)
			{
				this.downloaderServiceConnection = ClientMarshaller.CreateStub(this, typeof(MyDownloaderService));

				result = true;
			}

			return result;
		}

		private string GetDownloaderStringFromState(DownloaderState state)
		{
			switch(state)
			{
				case DownloaderState.Idle:
					return GetString(Resource.String.apkDown_idle);
				case DownloaderState.FetchingUrl:
					return GetString(Resource.String.apkDown_fetchingUrl);
				case DownloaderState.Connecting:
					return GetString(Resource.String.apkDown_connecting);
				case DownloaderState.Downloading:
					return GetString(Resource.String.apkDown_downloading);
				case DownloaderState.Completed:
					return GetString(Resource.String.apkDown_completed);
				case DownloaderState.PausedNetworkUnavailable:
					return GetString(Resource.String.apkDown_pausedNetworkUnavailable);
				case DownloaderState.PausedWifiDisabledNeedCellularPermission:
				case DownloaderState.PausedWifiDisabled:
					return GetString(Resource.String.apkDown_pausedWifiDisabled);
				case DownloaderState.PausedNeedCellularPermission:
				case DownloaderState.PausedNeedWifi:
					return GetString(Resource.String.apkDown_pausedNeedWifi);
				case DownloaderState.PausedRoaming:
					return GetString(Resource.String.apkDown_pausedRoaming);
				case DownloaderState.PausedNetworkSetupFailure:
					return GetString(Resource.String.apkDown_pausedNetworkSetupFailure);
				case DownloaderState.PausedSdCardUnavailable:
					return GetString(Resource.String.apkDown_pausedSdCardUnavailable);
				case DownloaderState.FailedUnlicensed:
					return GetString(Resource.String.apkDown_failedUnlicensed);
				case DownloaderState.FailedFetchingUrl:
					return GetString(Resource.String.apkDown_failedFetchingUrl);
				case DownloaderState.FailedSdCardFull:
					return GetString(Resource.String.apkDown_failedSdCardFull);
				case DownloaderState.FailedCanceled:
					return GetString(Resource.String.apkDown_failedCanceled);
				case DownloaderState.Failed:
					return GetString(Resource.String.apkDown_failed);
				default:
					return "...";
			}
		}

		private void CheckDownloadedFile()
		{
			var downloads = DownloadsDatabase.GetDownloads().OrderByDescending(d => d.LastModified);

			if(downloads.Count() == 0)
				return;

			var down = downloads.First();

			if(down.ExpansionFileType != LicenseVerificationLibrary.Policy.ApkExpansionPolicy.ExpansionFileType.MainFile)
				return;

			string fileName = down.FileName;


			//int versionCode = PackageManager.GetPackageInfo(this.PackageName, 0).VersionCode;

			//string fileName = Helpers.GetExpansionApkFileName(this, true, versionCode);

			string path = Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/Android/obb/" + this.PackageName + "/" + fileName;

			if(System.IO.File.Exists(path))
			{
				if(fileName != DataManager.Get<IPreferencesManager>().Preferences.ApkDownloaded)
				{
					CopyDownloadedFile(path);
					//Toast.MakeText(this, "Copia", ToastLength.Long).Show();
				}
				else
				{
					//Toast.MakeText(this, "C'è Già", ToastLength.Long).Show();

					StartApp();
				}
			}
		}

		private void CopyDownloadedFile(string path)
		{
			if(System.IO.File.Exists(path))
			{
				var importTask = new AnonymousAsyncTask<string, string, bool>((p) =>
				{
					RunOnUiThread(() => {
						_progressDialog.Indeterminate = true;
						_progressDialog.SetProgressPercentFormat(null);
						_progressDialog.SetMessage(GetString(Resource.String.apkDown_loadResources));
					});

					System.IO.FileInfo fi = new System.IO.FileInfo(path);

					string destFile = DataManager.Get<ISettingsManager>().Settings.SharedPath + "/" + fi.Name.Replace(fi.Extension, "") + ".mb";

					System.IO.File.Copy(path, destFile);

					FileSystemManager.ImportDocuments();

					DataManager.Get<IPreferencesManager>().Preferences.ApkDownloaded = fi.Name;
					DataManager.Get<IPreferencesManager>().Save();

					return true;

				}, (bd) =>
				{
					StartApp();
				});

				importTask.Execute(new Java.Lang.Object[]{});
			}
		}

		private void StartApp()
		{
			if(DataManager.Get<ISettingsManager>().Settings.SingolaApp)
			{			

                List<Pubblicazione> list = PubblicazioniManager.GetPubblicazioni();

				if (list.Count > 0)
				{
					var pub = list.First();
					//var doc = list.Where(x => x.NomeFile == "201493115111.mbp").First();
					Intent i = new Intent();
					i.SetClass(Application.Context, typeof(ViewerScreen));

                    i.PutExtra("pubPath", pub.Path);
					StartActivity(i);
				}
			}
			else
			{
				var intent = new Intent(this, typeof(HomeScreen));
				intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);

				StartActivity(intent);
			}
		}
	}
}