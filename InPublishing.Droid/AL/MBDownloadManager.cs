using System;
using Android.Content;
using Android.Database;
using Android.App;
using System.IO;
using Android.Util;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace InPublishing
{
	public class MBDownloadManager
	{
        private static string TAG = "MBDownloadManager";
        static Dictionary<Uri, List<IDownloadUpdated2>> pendingRequests;
        static HashSet<Uri> queuedUpdates;
        static Stack<Uri> requestQueue;
        public static MBDownloadManager DefaultManager;
        public static Context Context;
        const int MaxRequests = 1;
        static long fileUriers;

        static MBDownloadManager()
        {
            pendingRequests = new Dictionary<Uri, List<IDownloadUpdated2>>();
            queuedUpdates = new HashSet<Uri>();
            requestQueue = new Stack<Uri>();
        }

        /*public static long CurrentDownloading
        {
            get
            { 
                Android.App.DownloadManager downloadManager = (Android.App.DownloadManager)Application.Context.GetSystemService("download");

                Android.App.DownloadManager.Query query = new Android.App.DownloadManager.Query();
                //query.SetFilterById(_downloadReference);
                //query.SetFilterByStatus(DownloadStatus.Pending | DownloadStatus.Running | DownloadStatus.Paused | DownloadStatus.Successful);

                ICursor cursor = downloadManager.InvokeQuery(query);

                long num = 0;

                while (cursor.MoveToNext())
                {               
                    num++;  
                }

                return num;
            }
        }*/

        public static void RequestDownload(Uri uri, IDownloadUpdated2 notify)
        {
            if (MBDownloadManager.DefaultManager == null)
            {
                MBDownloadManager.DefaultManager = new MBDownloadManager();
            }

            MBDownloadManager.DefaultManager.AddDownload(uri, notify);
        }

        public void AddDownload(Uri uri, IDownloadUpdated2 notify)
        {
            object obj2 = MBDownloadManager.requestQueue;
            lock (obj2)
            {
                if (MBDownloadManager.pendingRequests.ContainsKey(uri))
                {
                    return;
                }
            }

            MBDownloadManager.QueueRequest(uri, notify);
            return;
        }

        static void QueueRequest(Uri uri, IDownloadUpdated2 notify)
        {
            if (notify == null)
            {
                throw new ArgumentNullException("notify");
            }
            object obj = MBDownloadManager.requestQueue;
            lock (obj)
            {
                if (MBDownloadManager.pendingRequests.ContainsKey(uri))
                {
                    MBDownloadManager.pendingRequests[uri].Add(notify);
                }
                else
                {
                    List<IDownloadUpdated2> list = new List<IDownloadUpdated2>(4);
                    list.Add(notify);
                    MBDownloadManager.pendingRequests[uri] = list;
                    if (MBDownloadManager.fileUriers >= MaxRequests)
                    {
                        MBDownloadManager.requestQueue.Push(uri);
                    }
                    else
                    {
                        Interlocked.Increment(ref MBDownloadManager.fileUriers);
                        ThreadPool.QueueUserWorkItem(delegate
                            {
                                try
                                {
                                    MBDownloadManager.StartFileDownload(uri);
                                }
                                catch (Exception value)
                                {
                                    Console.WriteLine(value);
                                }
                            });
                    }
                }
            }
        }

        private static void StartFileDownload(Uri uri)
        {
            //Interlocked.Increment(ref MBDownloadManager.fileUriers);
            try
            {
                //FileLoader._StartFileUri(uri);
                MBDownloadManager.Download(uri);
            }
            catch (Exception arg)
            {
                Console.Error.WriteLine("CRITICAL: should have never happened {0}", arg);
            }
        }

        static void Download(Uri uri)
        {
            string fileName = Path.GetFileName(uri.LocalPath);

            Android.App.DownloadManager downloadManager = (Android.App.DownloadManager)Application.Context.GetSystemService("download");

            Android.App.DownloadManager.Request request = new Android.App.DownloadManager.Request(Android.Net.Uri.Parse(uri.AbsoluteUri));

            //Restrict the types of networks over which this download may proceed.
            request.SetAllowedNetworkTypes(Android.App.DownloadNetwork.Wifi | Android.App.DownloadNetwork.Mobile);
            //Set whether this download may proceed over a roaming connection.
            request.SetAllowedOverRoaming(false);
            //Set the title of this download, to be displayed in notifications (if enabled).
            //request.SetTitle(title);
            request.SetVisibleInDownloadsUi(false);

            //request.SetNotificationVisibility(DownloadVisibility.Hidden);
            #if DEBUG
            request.SetNotificationVisibility(DownloadVisibility.Visible);
            #else
            request.SetNotificationVisibility(DownloadVisibility.Hidden);
            #endif

            request.SetDestinationInExternalFilesDir(Application.Context, Android.OS.Environment.DirectoryDownloads,fileName);

            //Enqueue a new download and same the referenceId
            var id = downloadManager.Enqueue(request);

            MBDownloadManager.DownloadMonitor(id);
        }

        private static void DownloadMonitor(long downId)
        {
            //bool downloading = true;
            //Android.App.DownloadManager downloadManager = (Android.App.DownloadManager)Application.Context.GetSystemService("download");

            DownloadInfo down = MBDownloadManager.DownloadInfo(downId);

            while (down != null)
            {
                down = MBDownloadManager.DownloadInfo(downId);

                if(down == null)
                {
                    break;
                }

                Uri uri = new Uri(down.Uri);

                if (!MBDownloadManager.pendingRequests.ContainsKey(uri))
                {
                    break;
                }

                switch(down.Status)
                {
                    case DownloadStatus.Running:
                    case DownloadStatus.Pending:
                        if(MBDownloadManager.pendingRequests.ContainsKey(uri))
                        {
                            List<IDownloadUpdated2> list = MBDownloadManager.pendingRequests[uri];
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
                            MBDownloadManager.InstallMbPackage(down.Uri, down.LocalUri);
                            //downloadManager.Remove(down.Id);

                            //downloading = false;

                            //MBDownloadManager.RegisterDownload(uri.ToString(), down.LocalUri);

                            //MBDownloadManager.FinishDownload(uri, true);
                        }
                        catch (Exception value)
                        {
                            Console.WriteLine(value);
                            MBDownloadManager.FinishDownload(uri, false);
                        }   
                        break;
                    default:
                        break;
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

        private static void InstallMbPackage(string uri, string localUri)
        { 
            Uri qUri = new Uri(uri);

            localUri = Uri.UnescapeDataString(localUri);
            uri = Uri.UnescapeDataString(uri);

            string fileName = System.IO.Path.GetFileName(localUri);

            string search = "/pub/";
            //string url = uri.AbsoluteUri;
            uri = uri.Substring(uri.IndexOf(search) + search.Length).Trim('/');

            string outPath = System.IO.Path.Combine(DataManager.Get<ISettingsManager>().Settings.DocPath, uri);
            //outPath = Path.Combine(outPath, parts[1]).Trim('/'); 
            outPath = System.Web.HttpUtility.UrlDecode(outPath);
            outPath = System.IO.Path.GetDirectoryName(outPath);

            string filePath = new Uri(localUri).LocalPath;

            try
            {
                if(System.IO.Path.GetExtension(localUri) == ".mb")
                {
                    //outPath = Path.Combine(outPath, fileName.Replace(fileExt, ".mbp"));
                    FileSystemManager.UnzipDocument(filePath, outPath);
                    File.Delete(filePath);
                    //ImageLoader.Instance.ClearDiskCache();
                    //ImageLoader.Instance.ClearMemoryCache();
                    MBImageLoader.RemoveFromCache(qUri.ToString());

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

                MBDownloadManager.FinishDownload(qUri, true);
            }
            catch (Exception value)
            {
                Console.WriteLine(value);
                MBDownloadManager.FinishDownload(qUri, false);
            }                   
        }

        private static void FinishDownload(Uri uri, bool result)
        {
            Interlocked.Decrement(ref MBDownloadManager.fileUriers);
            Console.WriteLine("DECREMENT " + MBDownloadManager.fileUriers);

            bool flag2 = false;
            object obj = MBDownloadManager.requestQueue;
            lock (obj)
            {
                if (result)
                {
                    MBDownloadManager.queuedUpdates.Add(uri);
                    if (MBDownloadManager.queuedUpdates.Count == 1)
                    {
                        flag2 = true;
                    }
                }
                else
                {
                    MBDownloadManager.pendingRequests.Remove(uri);

                    DownloadInfo down = MBDownloadManager.DownloadInfo(uri.AbsoluteUri);

                    if (down != null)
                    {
                        Android.App.DownloadManager downloadManager = (Android.App.DownloadManager)Application.Context.GetSystemService("download");
                        downloadManager.Remove(down.Id);
                    }
                }

                if (MBDownloadManager.requestQueue.Count > 0)
                {
                    uri = MBDownloadManager.requestQueue.Pop();
                    if (uri == null)
                    {
                        Console.Error.WriteLine("Dropping request {0} because url is null", uri);
                        MBDownloadManager.pendingRequests.Remove(uri);
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
                //FileDownloader.nsDispatcher.BeginInvokeOnMainThread(new NSAction(FileDownloader.NotifyDownloadListeners));
                NotifyDownloadListeners();
            }

            if (uri != null)
            {
                Interlocked.Increment(ref MBDownloadManager.fileUriers);
                MBDownloadManager.StartFileDownload(uri);
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

                    DownloadInfo down = MBDownloadManager.DownloadInfo(quri.AbsoluteUri);

                    if (down != null)
                    {
                        Android.App.DownloadManager downloadManager = (Android.App.DownloadManager)Application.Context.GetSystemService("download");
                        downloadManager.Remove(down.Id);

                        MBDownloadManager.RegisterDownload(quri.ToString(), down.LocalUri);
                    }

                    foreach (var pr in list)
                    {
                        try
                        {
                            pr.DownloadCompleted(quri.AbsoluteUri, ""); // this is the bit that should be on the UiThread
                        }
                        catch (Exception e)
                        {
                            Log.Error("MWC", e.Message);
                        }
                    }
                }

                queuedUpdates.Clear();
            }
        }

        public static void StopDownload(Uri uri)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                if(pendingRequests.ContainsKey(uri))
                    pendingRequests.Remove(uri);

                if(queuedUpdates.Contains(uri))
                    queuedUpdates.Remove(uri);
                    
                requestQueue = new Stack<Uri>( requestQueue.Where(i => i != uri).Reverse() ); 
                
                DownloadInfo down = MBDownloadManager.DownloadInfo(uri.AbsoluteUri);

                if (down != null)
                {
                    MBDownloadManager.FinishDownload(uri, false);
                }
                else
                {

                }               
            });
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
                        Log.Error("MBDownloadManager", "Eerrore registrazione download");
                    }

                    Log.Info("File Downloader", "Download registrato");
                }
            }
            catch (Exception value)
            {
                Console.WriteLine(value);
            }                   
        }

        public static void UpdateNotify(Uri uri, IDownloadUpdated2 notify)
        {           
            if (pendingRequests.ContainsKey(uri))
            {
                //Uri down = d.First() as Uri;
                object obj = MBDownloadManager.requestQueue;
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

        public static bool IsWaiting(Uri uri)
        {
            if (MBDownloadManager.pendingRequests.ContainsKey(uri))
            {
                return true;
            }
            else
            {
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
                    MBDownloadManager.InstallMbPackage(uri, localUri);
                    downloadManager.Remove(id);
                    //MBDownloadManager.FinishDownload(new Uri(uri), true);
                }
            }

            cursor.Close();
        }

        public static void RemoveAll()
        {
            try
            {
                Android.App.DownloadManager downloadManager = (Android.App.DownloadManager)Application.Context.GetSystemService("download");

                Android.App.DownloadManager.Query query = new Android.App.DownloadManager.Query();
                //query.SetFilterById(_downloadReference);
                //query.SetFilterByStatus(DownloadStatus.Pending | DownloadStatus.Running | DownloadStatus.Paused | DownloadStatus.Successful);

                ICursor cursor = downloadManager.InvokeQuery(query);

                while (cursor.MoveToNext())
                {               
                    long id = cursor.GetLong(cursor.GetColumnIndex(Android.App.DownloadManager.ColumnId));
                    downloadManager.Remove(id);
                }

                MBDownloadManager.fileUriers = 0;
            }
            catch(Exception ex)
            {
                string tag = TAG + " - RemoveAll";
                Log.Error(tag, ex.Message);
            }
        }
	}	
}

