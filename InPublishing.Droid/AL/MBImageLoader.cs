using System;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Views;
using Android.Widget;
using Com.Nostra13.Universalimageloader.Core;
using Com.Nostra13.Universalimageloader.Core.Assist;
using Com.Nostra13.Universalimageloader.Core.Imageaware;
using Com.Nostra13.Universalimageloader.Core.Listener;

namespace InPublishing
{
    public static class MBImageLoader
    {
        public static void DisplayDiskImage(string imgPath, ImageView imgView, PointF size = null, bool cache = true)
        {
            if(!System.IO.File.Exists(imgPath))
            {
                Utils.WriteLog("Immagine non esistente - " + imgPath);
                return;
            }

            Uri uri = new Uri(imgPath);

            if(cache)
            {
                Java.IO.File cacheFile = ImageLoader.Instance.DiskCache.Get(uri.ToString());

                if(cacheFile != null)
                {
                    Java.IO.File file = new Java.IO.File(imgPath);

                    if(file.LastModified() > cacheFile.LastModified())
                    {
                        ImageLoader.Instance.DiskCache.Remove(uri.ToString());
                    }
                }
            }

            var options = new DisplayImageOptions.Builder()
                    .CacheInMemory(false)
                    .CacheOnDisk(cache)
                    .ConsiderExifParams(true)
                    .BitmapConfig(Bitmap.Config.Rgb565)
                    .DelayBeforeLoading(0)
                    .Build();

            var targetSize = (size == null) ? null : new ImageSize((int)Math.Round(size.X), (int)Math.Round(size.Y));

            /*ImageLoader.Instance.LoadImage(
                uri.ToString(),
                targetSize,
                options,
                new ImageLoadingListener(
                    loadingComplete: (imageUri, v, loadedImage) => {
                        using(var h = new Handler(Looper.MainLooper))
                        {
                            h.Post(() => {
                                try
                                {
                                    imgView.SetImageBitmap(loadedImage);
                                }
                                catch(Exception ex)
                                {
                                    Utils.WriteLog("ImageStateView", ex.Message);
                                }
                            });
                        }
                    }));*/


            //ImageLoader.Instance.DisplayImage();
            NonViewAware nonViewAware = new NonViewAware(targetSize, ViewScaleType.FitInside);

            Action<string, View, Bitmap> loadingComplete = (imageUri, v, loadedImage) => { 
            
                using (var h = new Handler(Looper.MainLooper))
                {
                    h.Post(() => {
                        try
                        {
                            imgView.SetImageBitmap(loadedImage);
                        }
                        catch (Exception ex)
                        {
                            Utils.WriteLog("DisplayDiskImage", ex.Message);
                        }
                    });
                }
            };

            LoadingListener listener = new LoadingListener(loadingComplete);

            ImageLoader.Instance.DisplayImage(uri.ToString(), nonViewAware, options, listener);
        }

        public static void DisplayNetworkImage(Uri uri, ImageView imgView, PointF size = null, bool cache = true)
        {
            var options = new DisplayImageOptions.Builder()
                    .CacheInMemory(cache)
                    .CacheOnDisk(cache)
                    .ConsiderExifParams(true)
                    .BitmapConfig(Bitmap.Config.Rgb565)
                    .DelayBeforeLoading(0)
                    .Build();

            var targetSize = (size == null) ? null : new ImageSize((int)Math.Round(size.X), (int)Math.Round(size.Y));

            NonViewAware nonViewAware = new NonViewAware(targetSize, ViewScaleType.FitInside);

            Action<string, View, Bitmap> loadingComplete = (imageUri, v, loadedImage) => {

                using (var h = new Handler(Looper.MainLooper))
                {
                    h.Post(() => {
                        try
                        {
                            imgView.SetImageBitmap(loadedImage);
                        }
                        catch (Exception ex)
                        {
                            Utils.WriteLog("DisplayNetworkImage", ex.Message);
                        }
                    });
                }
            };

            LoadingListener listener = new LoadingListener(loadingComplete);

            ImageLoader.Instance.DisplayImage(uri.ToString(), nonViewAware, options, listener);
        }

        public static void DisplayDiskImage(string imgPath, ImageSwitcher imgSwitch, PointF size = null, bool cache = true)
        {
            if(!System.IO.File.Exists(imgPath))
            {
                Utils.WriteLog("Immagine non esistente - " + imgPath);
                return;
            }

            Uri uri = new Uri(imgPath);

            if(cache)
            {
                Java.IO.File cacheFile = ImageLoader.Instance.DiskCache.Get(uri.ToString());

                if(cacheFile != null)
                {
                    Java.IO.File file = new Java.IO.File(imgPath);

                    if(file.LastModified() > cacheFile.LastModified())
                    {
                        ImageLoader.Instance.DiskCache.Remove(uri.ToString());
                    }
                }
            }

            var options = new DisplayImageOptions.Builder()
                    .CacheInMemory(false)
                    .CacheOnDisk(cache)
                    .ConsiderExifParams(true)
                    .BitmapConfig(Bitmap.Config.Rgb565)
                    .DelayBeforeLoading(0)
                    .Build();

            var targetSize = (size == null) ? null : new ImageSize((int)Math.Round(size.X), (int)Math.Round(size.Y));

            //ImageLoader.Instance.DisplayImage();
            NonViewAware nonViewAware = new NonViewAware(targetSize, ViewScaleType.FitInside);

            Action<string, View, Bitmap> loadingComplete = (imageUri, v, loadedImage) => {

                using (var h = new Handler(Looper.MainLooper))
                {
                    h.Post(() => {
                        try
                        {
                            imgSwitch.SetImageDrawable(new BitmapDrawable(loadedImage));
                        }
                        catch (Exception ex)
                        {
                            Utils.WriteLog("DisplayDiskImage", ex.Message);
                        }
                    });
                }
            };

            LoadingListener listener = new LoadingListener(loadingComplete);

            ImageLoader.Instance.DisplayImage(uri.ToString(), nonViewAware, options, listener);
        }

        public static void RemoveFromCache(string uri)
        {
            ImageLoader.Instance.MemoryCache.Remove(uri);
            ImageLoader.Instance.DiskCache.Remove(uri);
        }

        public class LoadingListener : SimpleImageLoadingListener
        {
            public Action<string, View, Bitmap> LoadingComplete;

            public LoadingListener(Action<string, View, Bitmap> onLoadingComplete) : base()
            {
                LoadingComplete = onLoadingComplete;
            }

            public override void OnLoadingComplete(string imageUri, View view, Bitmap loadedImage)
			{
                if (LoadingComplete != null)
                    LoadingComplete(imageUri, view, loadedImage);

                base.OnLoadingComplete(imageUri, view, loadedImage);
			}
		}
    }
}

