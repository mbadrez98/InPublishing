using System;
using System.Collections.Generic;
using Android.Database;
using Android.Content;
using Android.App;
using System.IO;
using System.Threading;
using Android.Util;

namespace InPublishing
{
	public class MBDownloadManager2
	{		
		private static Dictionary<Uri, List<IDownloadUpdated2>> pendingRequests;
		static HashSet<Uri> queuedUpdates;
		static Stack<Uri> requestQueue;
		public static MBDownloadManager2 DefaultManager;
        public static Context Context;

		static MBDownloadManager2()
		{
			pendingRequests = new Dictionary<Uri, List<IDownloadUpdated2>>();
			queuedUpdates = new HashSet<Uri>();
			requestQueue = new Stack<Uri>();
		}

		public static long RequestDownload(Uri uri, string filename, string title, IDownloadUpdated2 notify)
		{
			if (MBDownloadManager2.DefaultManager == null)
			{
				MBDownloadManager2.DefaultManager = new MBDownloadManager2();
			}

			return MBDownloadManager2.DefaultManager.AddDownload(uri, filename, title, notify);
		}

		public long AddDownload(Uri uri, string filename, string title, IDownloadUpdated2 notify)
		{
			long id = 0;
			object obj2 = MBDownloadManager2.requestQueue;
			lock (obj2)
			{
				if (MBDownloadManager2.pendingRequests.ContainsKey(uri))
				{
					return 0;
				}
			}

			if (notify == null)
			{
				throw new ArgumentNullException("notify");
			}
			object obj = MBDownloadManager2.requestQueue;
			lock (obj)
			{
				if (MBDownloadManager2.pendingRequests.ContainsKey(uri))
				{
					MBDownloadManager2.pendingRequests[uri].Add(notify);
				}
				else
				{
					List<IDownloadUpdated2> list = new List<IDownloadUpdated2>(4);
					list.Add(notify);
					MBDownloadManager2.pendingRequests[uri] = list;

					id = MBDownloadManager2.Download(uri, filename, title, notify);
				}
			}
			return id;
		}

		static long Download(Uri uri, string filename, string title, IDownloadUpdated2 notify)
		{
			Android.App.DownloadManager downloadManager = (Android.App.DownloadManager)Application.Context.GetSystemService("download");

			Android.App.DownloadManager.Request request = new Android.App.DownloadManager.Request(Android.Net.Uri.Parse(uri.AbsoluteUri));

			//Restrict the types of networks over which this download may proceed.
			request.SetAllowedNetworkTypes(Android.App.DownloadNetwork.Wifi | Android.App.DownloadNetwork.Mobile);
			//Set whether this download may proceed over a roaming connection.
			request.SetAllowedOverRoaming(false);
			//Set the title of this download, to be displayed in notifications (if enabled).
			request.SetTitle(title);
			request.SetVisibleInDownloadsUi(false);

			//request.SetNotificationVisibility(DownloadVisibility.Hidden);
			#if DEBUG
			request.SetNotificationVisibility(DownloadVisibility.Visible);
			#else
			request.SetNotificationVisibility(DownloadVisibility.Hidden);
			#endif

			request.SetDestinationInExternalFilesDir(Application.Context, Android.OS.Environment.DirectoryDownloads,filename);

			//Enqueue a new download and same the referenceId
			var id = downloadManager.Enqueue(request);

			ThreadPool.QueueUserWorkItem(state =>
			{
				MBDownloadManager2.DownloadMonitor(id);
			});

			/*new Thread(() =>
			{
				MBDownloadManager2.DownloadMonitor(id);

			}).Start();*/

			return id;
		}

		private static void DownloadMonitor(long downId)
		{
			bool downloading = true;
			Android.App.DownloadManager downloadManager = (Android.App.DownloadManager)Application.Context.GetSystemService("download");

			while (downloading)
			{
				DownloadInfo down = MBDownloadManager2.DownloadInfo(downId);

				if(down == null)
				{
					break;
				}

				Uri uri = new Uri(down.Uri);

				switch(down.Status)
				{
				case DownloadStatus.Running:
				case DownloadStatus.Pending:
					if(MBDownloadManager2.pendingRequests.ContainsKey(uri))
					{
						List<IDownloadUpdated2> list = MBDownloadManager2.pendingRequests[uri];
						var perc = (int)((down.ByteDownloaded * 100L) / down.ByteTotal);
						try
						{								
							foreach (IDownloadUpdated2 current2 in list)
							{
								current2.ProgressChanged(perc);

							}
						}
						catch (Exception value)
						{
							Console.WriteLine(value);
						}
					}
					break;
				case DownloadStatus.Successful:
					try
					{
						MBDownloadManager2.InstallMbPackage(down.Uri, down.LocalUri);
						downloadManager.Remove(down.Id);

						downloading = false;

						MBDownloadManager2.RegisterDownload(uri.ToString(), down.LocalUri);

						MBDownloadManager2.FinishDownload(uri, true);
					}
					catch (Exception value)
					{
						Console.WriteLine(value);
						MBDownloadManager2.FinishDownload(uri, false);
					}	
					break;
				default:
					break;
				}
			}
		}

		private static void RegisterDownload(string uri, string localUri)
		{	
			//string fileName = System.IO.Path.GetFileName(localUri);

			string search = "/pub/";

			uri = uri.Substring(uri.IndexOf(search) + search.Length).Trim('/');

			try
			{				
				//registrazione download
				Uri nHost = new Uri(DataManager.Get<ISettingsManager>().Settings.DownloadUrl);
				if (Reachability.IsHostReachable("http://" + nHost.Host))
				{
					var data = Application.Context.DeviceInfo();
					data.Add("file", uri);

					Notification notif = new Notification();

					if(!notif.RegisterDownload(data))
					{
						Log.Error("MBDownloadManager2", "Errore registrazione download");
					}

					Log.Info("File Downloader", "Download registrato");
				}
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
			}					
		}

		private static void FinishDownload(Uri uri, bool result)
		{
			bool flag2 = false;
			object obj = MBDownloadManager2.requestQueue;
			lock (obj)
			{
				if (result)
				{
					MBDownloadManager2.queuedUpdates.Add(uri);
					if (MBDownloadManager2.queuedUpdates.Count == 1)
					{
						flag2 = true;
					}
				}
				else
				{
					MBDownloadManager2.pendingRequests.Remove(uri);
				}

				if (MBDownloadManager2.requestQueue.Count > 0)
				{
					uri = MBDownloadManager2.requestQueue.Pop();
					if (uri == null)
					{
						Console.Error.WriteLine("Dropping request {0} because url is null", uri);
						MBDownloadManager2.pendingRequests.Remove(uri);
						uri = null;
					}
				}
				else
				{
					uri = null;
				}
			}
			if (flag2)
			{
				NotifyDownloadListeners();
			}
		}

		static void NotifyDownloadListeners()
		{
			lock (requestQueue)
			{
				foreach (var quri in queuedUpdates)
				{
					var list = pendingRequests[quri];
					pendingRequests.Remove(quri);
					foreach (var pr in list)
					{
						try
						{
							pr.DownloadCompleted(quri.AbsoluteUri, ""); // this is the bit that should be on the UiThread
						}
						catch (Exception e)
						{
							LogDebug(e.Message);
						}
					}
				}

				queuedUpdates.Clear();
			}
		}

		public static void LogDebug(string message)
		{
			Console.WriteLine(message);
			Log.Debug("MWC", message);
		}

		public static void UpdateNotify(Uri uri, IDownloadUpdated2 notify)
		{			
			if (pendingRequests.ContainsKey(uri))
			{
				//Uri down = d.First() as Uri;
				object obj = MBDownloadManager2.requestQueue;
				lock(obj)
				{
					List<IDownloadUpdated2> list = new List<IDownloadUpdated2>(4);
					/*list.Add(notify);
				pendingRequests[uri] = list;*/

					list = pendingRequests[uri];
					list.Add(notify);
				}

			}
		}

		public static DownloadInfo DownloadInfo(long downId)
		{
			Android.App.DownloadManager.Query query = new Android.App.DownloadManager.Query();

			query.SetFilterById(downId);

			Android.App.DownloadManager downloadManager = (Android.App.DownloadManager)Application.Context.GetSystemService("download");

			ICursor cursor = downloadManager.InvokeQuery(query);

			if(cursor.Count > 0 && cursor.MoveToFirst())
			{
				string uri = cursor.GetString(cursor.GetColumnIndex(Android.App.DownloadManager.ColumnUri));

				DownloadInfo info = new DownloadInfo();
				info.Uri = uri;
				info.LocalUri = cursor.GetString(cursor.GetColumnIndex(Android.App.DownloadManager.ColumnLocalUri));
				info.Id = cursor.GetLong(cursor.GetColumnIndex(Android.App.DownloadManager.ColumnId));
				info.title = cursor.GetString(cursor.GetColumnIndex(Android.App.DownloadManager.ColumnTitle));
				info.Status = (DownloadStatus)cursor.GetInt(cursor.GetColumnIndex(Android.App.DownloadManager.ColumnStatus));
				info.ByteDownloaded = cursor.GetLong(cursor.GetColumnIndex(Android.App.DownloadManager.ColumnBytesDownloadedSoFar));
				info.ByteTotal = cursor.GetLong(cursor.GetColumnIndex(Android.App.DownloadManager.ColumnTotalSizeBytes));

				cursor.Close();
				return info;

			}

			cursor.Close();

			return null;
		}

		public static DownloadInfo DownloadInfo(string uri)
		{
			Android.App.DownloadManager.Query query = new Android.App.DownloadManager.Query();

			//query.SetFilterByStatus(DownloadStatus.Pending | DownloadStatus.Running | DownloadStatus.Paused);

			Android.App.DownloadManager downloadManager = (Android.App.DownloadManager)Application.Context.GetSystemService("download");

			ICursor cursor = downloadManager.InvokeQuery(query);

			while (cursor.MoveToNext())
			{
				string url = cursor.GetString(cursor.GetColumnIndex(Android.App.DownloadManager.ColumnUri));

				if(url == uri)
				{
					DownloadInfo info = new DownloadInfo();
					info.Uri = url;
					info.LocalUri = cursor.GetString(cursor.GetColumnIndex(Android.App.DownloadManager.ColumnLocalUri));
					info.Id = cursor.GetLong(cursor.GetColumnIndex(Android.App.DownloadManager.ColumnId));
					info.title = cursor.GetString(cursor.GetColumnIndex(Android.App.DownloadManager.ColumnTitle));
					info.Status = (DownloadStatus)cursor.GetInt(cursor.GetColumnIndex(Android.App.DownloadManager.ColumnStatus));
					info.ByteDownloaded = cursor.GetLong(cursor.GetColumnIndex(Android.App.DownloadManager.ColumnBytesDownloadedSoFar));
					info.ByteTotal = cursor.GetLong(cursor.GetColumnIndex(Android.App.DownloadManager.ColumnTotalSizeBytes));

					cursor.Close();
					return info;
				}
			}

			cursor.Close();

			return null;
		}

		public static void Remove(long id)
		{
			Android.App.DownloadManager downloadManager = (Android.App.DownloadManager)Application.Context.GetSystemService("download");

			var info = MBDownloadManager2.DownloadInfo(id);

			var uri = new Uri(info.Uri);

			downloadManager.Remove(id);

			if(pendingRequests != null && pendingRequests.ContainsKey(uri))
			{
				pendingRequests.Remove(uri);

				if(queuedUpdates.Contains(uri))
					queuedUpdates.Remove(uri);

			}
		}

		public static void RemoveAll()
		{
			Android.App.DownloadManager downloadManager = (Android.App.DownloadManager)Application.Context.GetSystemService("download");

			Android.App.DownloadManager.Query query = new Android.App.DownloadManager.Query();
			//query.SetFilterById(_downloadReference);
			query.SetFilterByStatus(DownloadStatus.Pending | DownloadStatus.Running | DownloadStatus.Paused);

			ICursor cursor = downloadManager.InvokeQuery(query);

			while (cursor.MoveToNext())
			{				
				long id = cursor.GetLong(cursor.GetColumnIndex(Android.App.DownloadManager.ColumnId));
				downloadManager.Remove(id);
			}


		}

		public static bool InstallMbPackage(string uri, string localUri)
		{	

			string fileName = System.IO.Path.GetFileName(localUri);

			string search = "/pub/";
			//string url = uri.AbsoluteUri;
			uri = uri.Substring(uri.IndexOf(search) + search.Length).Trim('/');

			string outPath = System.IO.Path.Combine(DataManager.Get<ISettingsManager>().Settings.DocPath, uri);
			//outPath = Path.Combine(outPath, parts[1]).Trim('/'); 
			outPath = System.Web.HttpUtility.UrlDecode(outPath);
			outPath = System.IO.Path.GetDirectoryName(outPath);

			string filePath = new Uri(localUri).AbsolutePath;

			try
			{
				if(System.IO.Path.GetExtension(localUri) == ".mb")
				{
					//outPath = Path.Combine(outPath, fileName.Replace(fileExt, ".mbp"));
					FileSystemManager.UnzipDocument(filePath, outPath);
					File.Delete(filePath);
				}
				else if(System.IO.Path.GetExtension(localUri) == ".pdf")
				{
					outPath = System.IO.Path.Combine(outPath, fileName); 
					if(File.Exists(outPath))
					{
						File.Delete(outPath);
					}

					//se la cartella non esiste la creo
					string dir = System.IO.Path.GetDirectoryName(outPath);

					if(!Directory.Exists(dir))
					{
						Directory.CreateDirectory(dir);
					}

					File.Move(filePath, outPath);
				}


				return true;
			}
			catch (Exception value)
			{
				Console.WriteLine(value);

				return false;
			}					
		}

        public static void InstallFinishedMB()
        {
            Android.App.DownloadManager.Query query = new Android.App.DownloadManager.Query();

            query.SetFilterByStatus(DownloadStatus.Successful);

            Android.App.DownloadManager downloadManager = (Android.App.DownloadManager)Context.GetSystemService("download");

            ICursor cursor = downloadManager.InvokeQuery(query);

            while (cursor.MoveToNext())
            {
                string localUri = cursor.GetString(cursor.GetColumnIndex(Android.App.DownloadManager.ColumnLocalUri));
                string uri = cursor.GetString(cursor.GetColumnIndex(Android.App.DownloadManager.ColumnUri));
                long id = cursor.GetLong(cursor.GetColumnIndex(Android.App.DownloadManager.ColumnId));
                string filePath = new Uri(localUri).AbsolutePath;

                if(File.Exists(filePath))
                {
                    MBDownloadManager2.InstallMbPackage(uri, localUri);
                    MBDownloadManager2.Remove(id);
                }
            }

            cursor.Close();
        }
	}

	public interface IDownloadUpdated2
	{
		/// <summary>
		/// On Android, you MUST do the operations in your implementation on the UiThread.
		/// Be sure to use RunOnUiThread()!!!
		/// </summary>
		void DownloadCompleted(string uri, string localUri);
		void ProgressChanged(int progress);
        //void DownloadCancelled();
	}

    public class DownloadInfo
    {
        public Android.App.DownloadStatus Status;
        public long Id = 0;
        public string title = "";
        public string Uri = "";
        public string LocalUri = "";
        public long ByteDownloaded = 0;
        public long ByteTotal = 0;

        public DownloadInfo()
        {

        }
    }

    public class VoidNotify : IDownloadUpdated2
    {
        public VoidNotify()
        {
        }

        void IDownloadUpdated2.ProgressChanged(int progress)
        {
        }

        void IDownloadUpdated2.DownloadCompleted(string uri, string localUri) 
        {
        }
    }
}

