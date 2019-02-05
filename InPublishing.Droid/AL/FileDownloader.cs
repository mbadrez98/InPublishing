// Copyright 2010-2011 Miguel de Icaza
//
// Based on the TweetStation specific ImageStore
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

//
// Minor changes (UIImage -> Drawable) required to get this running on Mono-for-Android
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

using System.Security.Cryptography;
using Android.Graphics.Drawables;
//using MWC;
using Android.Util;
using System.Collections.Specialized;
using System.Globalization;
using Android.Net.Wifi;
using Android.App;
using Android.Content;
using Android.OS;

namespace InPublishing
{
    /// <summary>
    ///    This interface needs to be implemented to be notified when an image
    ///    has been downloaded.   The notification will happen on the UI thread.
    ///    Upon notification, the code should call RequestImage again, this time
    ///    the image will be loaded from the on-disk cache or the in-memory cache.
    /// </summary>
	public interface IDownloadUpdated
    {
        /// <summary>
        /// On Android, you MUST do the operations in your implementation on the UiThread.
        /// Be sure to use RunOnUiThread()!!!
        /// </summary>
		void DownloadCompleted(Uri uri);
		void ProgressChanged(int progress);
    }

    /// <summary>
    ///   Network image loader, with local file system cache and in-memory cache
    /// </summary>
    /// <remarks>
    ///   By default, using the static public methods will use an in-memory cache
    ///   for 50 images and 4 megs total.   The behavior of the static methods 
    ///   can be modified by setting the public DefaultLoader property to a value
    ///   that the user configured.
    /// 
    ///   The instance methods can be used to create different imageloader with 
    ///   different properties.
    ///  
    ///   Keep in mind that the phone does not have a lot of memory, and using
    ///   the cache with the unlimited value (0) even with a number of items in
    ///   the cache can consume memory very quickly.
    /// 
    ///   Use the Purge method to release all the memory kept in the caches on
    ///   low memory conditions, or when the application is sent to the background.
    /// </remarks>

	public class FileDownloader
    {
        public readonly static string BaseDir = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal), "..");
		const int MaxRequests = 1;
		static string TmpDir;       

        // A list of requests that have been issues, with a list of objects to notify.
		static Dictionary<Uri, List<IDownloadUpdated>> pendingRequests;

        // A list of updates that have completed, we must notify the main thread about them.
        static HashSet<Uri> queuedUpdates;

        // A queue used to avoid flooding the network stack with HTTP requests
        static Stack<Uri> requestQueue;

        /*static NSString nsDispatcher = "x"; */

        static MD5CryptoServiceProvider checksum = new MD5CryptoServiceProvider();

		static long picUriers;

        /// <summary>
        ///    This contains the default loader which is configured to be 50 images
        ///    up to 4 megs of memory.   Assigning to this property a new value will
        ///    change the behavior.   This property is lazyly computed, the first time
        ///    an image is requested.
        /// </summary>
		public static FileDownloader DefaultLoader;

		static FileDownloader()
        {
			TmpDir = Path.Combine(BaseDir, "Library/Caches/Pub");

            if (!Directory.Exists(TmpDir))
                Directory.CreateDirectory(TmpDir);

			pendingRequests = new Dictionary<Uri, List<IDownloadUpdated>>();
            queuedUpdates = new HashSet<Uri>();
            requestQueue = new Stack<Uri>();
        }

        /// <summary>
        ///   Creates a new instance of the image loader
        /// </summary>
        /// <param name="cacheSize">
        /// The maximum number of entries in the LRU cache
        /// </param>
        /// <param name="memoryLimit">
        /// The maximum number of bytes to consume by the image loader cache.
        /// </param>
		public FileDownloader()
        {
			//cache = new LRUCache<Uri, Drawable /*UIImage*/>(cacheSize, memoryLimit, sizer);
        }

        /// <summary>
        ///    Purges the contents of the DefaultLoader
        /// </summary>
        public static void Purge()
        {
            if (DefaultLoader != null)
                DefaultLoader.PurgeCache();
        }

        /// <summary>
        ///    Purges the cache of this instance of the ImageLoader, releasing 
        ///    all the memory used by the images in the caches.
        /// </summary>
        public void PurgeCache()
        {
			//cache.Purge();
        }

        static int hex(int v)
        {
            if (v < 10)
                return '0' + v;
            return 'a' + v - 10;
        }

        static string md5(string input)
        {
            var bytes = checksum.ComputeHash(Encoding.UTF8.GetBytes(input));
            var ret = new char[32];
            for (int i = 0; i < 16; i++)
            {
                ret[i * 2] = (char)hex(bytes[i] >> 4);
                ret[i * 2 + 1] = (char)hex(bytes[i] & 0xf);
            }
            return new string(ret);
        }

		public static void DefaultRequestDownload(Uri uri, IDownloadUpdated notify)
		{
			if (FileDownloader.DefaultLoader == null)
			{
				FileDownloader.DefaultLoader = new FileDownloader();
			}

			FileDownloader.DefaultLoader.AddDownload(uri, notify);
		}

		public void AddDownload(Uri uri, IDownloadUpdated notify)
		{
			object obj2 = FileDownloader.requestQueue;
			lock (obj2)
			{
				if (FileDownloader.pendingRequests.ContainsKey(uri))
				{
					return;
				}
			}

			FileDownloader.QueueRequest(uri, notify);
			return;
		}
			
        static void QueueRequest(Uri uri, IDownloadUpdated notify)
        {
			if (notify == null)
			{
				throw new ArgumentNullException("notify");
			}
			object obj = FileDownloader.requestQueue;
			lock (obj)
			{
				if (FileDownloader.pendingRequests.ContainsKey(uri))
				{
					FileDownloader.pendingRequests[uri].Add(notify);
				}
				else
				{
					List<IDownloadUpdated> list = new List<IDownloadUpdated>(4);
					list.Add(notify);
					FileDownloader.pendingRequests[uri] = list;
					if (FileDownloader.picUriers >= MaxRequests)
					{
						FileDownloader.requestQueue.Push(uri);
					}
					else
					{
						ThreadPool.QueueUserWorkItem(delegate
						{
							try
							{
								FileDownloader.StartFileDownload(uri);
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
			Interlocked.Increment(ref FileDownloader.picUriers);
			try
			{
				//FileLoader._StartFileUri(uri);
				FileDownloader.Download(uri);
			}
			catch (Exception arg)
			{
				Console.Error.WriteLine("CRITICAL: should have never happened {0}", arg);
			}
		}
       
		static void Download(Uri uri)
        {
			//bool result;
			try
			{
				/*NSUrlRequest request = new NSUrlRequest(new NSUrl(uri.ToString()), NSUrlRequestCachePolicy.UseProtocolCachePolicy, 120.0);
                NSUrlResponse nSUrlResponse;
                NSError nSError;
                NSData nSData = NSUrlConnection.SendSynchronousRequest(request, out nSUrlResponse, out nSError);
                result = nSData.Save(target, true, out nSError);*/
				//taskID = UIApplication.SharedApplication.BeginBackgroundTask ( () => { FileDownloader.BackgroundTaskExpiring (); });

				/*PowerManager powerManager = (PowerManager)Application.Context.GetSystemService(Context.PowerService);
				PowerManager.WakeLock wakeLock = powerManager.NewWakeLock(WakeLockFlags.Partial, "myWake");
				wakeLock.Acquire();*/

				WebClient client = new WebClient();

				Console.WriteLine("Download file " + uri.AbsolutePath);

				string localFilename = Path.GetFileName(uri.LocalPath);
				string localPath = Path.Combine (FileDownloader.TmpDir, localFilename);

				//result = false;
				client.DownloadFileCompleted += (s, e) => 
				{
					//var bytes = e.Result;

					if(e.Error == null)
					{              
						
						//File.WriteAllBytes (localPath, bytes); // writes to local storage  

						//FileDownloader._FinishDownload(uri, true);
						FileDownloader.InstallDownload(uri, localPath);

						//wakeLock.Release();
					}
					else
					{
						FileDownloader._FinishDownload(uri, false);
						Utils.WriteLog("Errore download", e.Error.Message);
					}
				};

				client.DownloadProgressChanged += (sender, e) => 
				{
					if(FileDownloader.pendingRequests.ContainsKey(uri))
					{
						List<IDownloadUpdated> list = FileDownloader.pendingRequests[uri];
						foreach (IDownloadUpdated current2 in list)
						{
							try
							{
								current2.ProgressChanged(e.ProgressPercentage);
							}
							catch (Exception value)
							{
								Console.WriteLine(value);
							}
						}
					}
				};

				//client.DownloadDataAsync(uri);
				client.DownloadFileAsync(uri, localPath);

				//FileDownloader.DownloadIcon(uri);

			}
			catch (Exception arg)
			{
				Console.WriteLine("Problem with {0} {1}", uri, arg);
				//result = false;
			}
			//return result;
        }

		private static void InstallDownload(Uri uri, string filePath, bool notify = true)
		{           
			string fileName = Path.GetFileName(filePath);

			string search = "/pub/";
			string url = uri.AbsoluteUri;
			url = url.Substring(url.IndexOf(search) + search.Length).Trim('/');

			string outPath = Path.Combine(DataManager.Get<ISettingsManager>().Settings.DocPath, url);
			//outPath = Path.Combine(outPath, parts[1]).Trim('/'); 
			outPath = System.Web.HttpUtility.UrlDecode(outPath);
			outPath = Path.GetDirectoryName(outPath);

			try
			{
				if(Path.GetExtension(uri.LocalPath) == ".mb")
				{
					//outPath = Path.Combine(outPath, fileName.Replace(fileExt, ".mbp"));
					FileSystemManager.UnzipDocument(filePath, outPath);
					File.Delete(filePath);
				}
				else if(Path.GetExtension(uri.LocalPath) == ".pdf")
				{
					outPath = Path.Combine(outPath, fileName); 
					if(File.Exists(outPath))
					{
						File.Delete(outPath);
					}

					//se la cartella non esiste la creo
					string dir = Path.GetDirectoryName(outPath);

					if(!Directory.Exists(dir))
					{
						Directory.CreateDirectory(dir);
					}

					File.Move(filePath, outPath);
				}
				else if(Path.GetFileName(uri.LocalPath) == "folder.png")
				{
					outPath = Path.Combine(outPath, fileName); 
					if(File.Exists(outPath))
					{
						File.Delete(outPath);
					}

					//se la cartella non esiste la creo
					string dir = Path.GetDirectoryName(outPath);

					if(!Directory.Exists(dir))
					{
						Directory.CreateDirectory(dir);
					}

					//versione normale
					File.Copy(filePath, Path.Combine(dir, "folder.png"), true);

					//versione retina
					File.Copy(filePath, Path.Combine(dir, "folder@2x.png"), true);

					File.Delete(filePath);
				}

				if(notify)
				{
					FileDownloader._FinishDownload(uri, true);
				}
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
				if(notify)
				{
					FileDownloader._FinishDownload(uri, false);
				}
			}				
		}

		private static void _FinishDownload(Uri uri, bool result)
		{
			Interlocked.Decrement(ref FileDownloader.picUriers);

			bool flag2 = false;
			object obj = FileDownloader.requestQueue;
			lock (obj)
			{
				if (result)
				{
					FileDownloader.queuedUpdates.Add(uri);
					if (FileDownloader.queuedUpdates.Count == 1)
					{
						flag2 = true;
					}
				}
				else
				{
					FileDownloader.pendingRequests.Remove(uri);
				}

				if (FileDownloader.requestQueue.Count > 0)
				{
					uri = FileDownloader.requestQueue.Pop();
					if (uri == null)
					{
						Console.Error.WriteLine("Dropping request {0} because url is null", uri);
						FileDownloader.pendingRequests.Remove(uri);
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
				NotifyImageListeners();
			}

			if (uri != null)
			{
				FileDownloader.StartFileDownload(uri);
			}
		}

		public static void UpdateNotify(Uri uri, IDownloadUpdated notify)
		{			
			if (FileDownloader.pendingRequests.ContainsKey(uri))
			{
				//Uri down = d.First() as Uri;
				//down.Autore = "ciccio";
				List<IDownloadUpdated> list = new List<IDownloadUpdated>(4);
				list.Add(notify);
				FileDownloader.pendingRequests[uri] = list;
			}
		}

		public static bool IsWaiting(Uri uri)
		{
			if (FileDownloader.pendingRequests.ContainsKey(uri))
			{
				return true;
			}
			else
			{
				return false;
			}
		}

		/*static void StartPicDownload(Uri uri, string target)
        {
            LogDebug("________star " + picUriers);
            Interlocked.Increment(ref picUriers);
            try
            {
                _StartPicDownload(uri, target);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("CRITICAL: should have never happened {0}", e);
            }
            //Util.Log ("Leaving StartPicDownload {0}", picDownloaders);
            LogDebug("________end  " + picUriers);
            Interlocked.Decrement(ref picUriers);
        }

        static void _StartPicDownload(Uri uri, string target)
        {
            do
            {
                bool downloaded = false;

                //System.Threading.Thread.Sleep (5000);
                downloaded = Download(uri);
                if (!downloaded)
                    LogDebug((String.Format("Error fetching picture for {0} to {1}", uri, target)));

                // Cluster all updates together
                bool doInvoke = false;

                lock (requestQueue)
                {
                    if (downloaded)
                    {
                        queuedUpdates.Add(uri);

                        // If this is the first queued update, must notify
                        if (queuedUpdates.Count == 1)
                            doInvoke = true;
                    }
                    else
                        pendingRequests.Remove(uri);

                    // Try to get more jobs.
                    if (requestQueue.Count > 0)
                    {
                        uri = requestQueue.Pop();
                        if (uri == null)
                        {
                            Console.Error.WriteLine("Dropping request {0} because url is null", uri);
                            pendingRequests.Remove(uri);
                            uri = null;
                        }
                    }
                    else
                    {
                        //Util.Log ("Leaving because requestQueue.Count = {0} NOTE: {1}", requestQueue.Count, pendingRequests.Count);
                        uri = null;
                    }
                }
                if (doInvoke)
                {
					//nsDispatcher.BeginInvokeOnMainThread(NotifyImageListeners);
                    // HACK: need a context to do RunOnUiThread on...
                    //RunOnUiThread(() =>
                    //{
                        NotifyImageListeners();
                    //});
                }
            } while (uri != null);
        }*/

        /// <summary>
        /// NEEDS TO run on the main thread. The iOS version does, but in Android
        /// we need access to a Context to get to the main thread, and I haven't
        /// figured out a non-hacky way to do that yet.
        /// </summary>
        static void NotifyImageListeners()
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
							pr.DownloadCompleted(quri); // this is the bit that should be on the UiThread
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

        /*
Use this to help with ADB watching in CMD 
"c:\Program Files (x86)\Android\android-sdk\platform-tools\adb" logcat -s MonoDroid:* mono:* MWC:* ActivityManager:*
*/
        public static void LogDebug(string message)
        {
            Console.WriteLine(message);
            Log.Debug("MWC", message);
        }
    }
}

