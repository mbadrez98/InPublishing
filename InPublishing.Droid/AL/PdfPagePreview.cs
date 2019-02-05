using System;
using Android.Widget;
using Com.Artifex.Mupdfdemo;

using Android.Graphics;
using Android.Graphics.Drawables;
using Android.App;
using Android.Content;
using Android.Util;

namespace InPublishing
{
	public class PdfPagePreview
	{
		/*public static void SetImage(ImageView holder, MuPDFCore pdfCore, int position, Point size)
		{
			Bitmap mLoadingBitmap = BitmapFactory.DecodeResource(Application.Context.Resources, Resource.Drawable.darkdenim3);
			BitmapWorkerTask task = new BitmapWorkerTask(holder, pdfCore, position, size);
			AsyncDrawable asyncDrawable = new AsyncDrawable(Application.Context.Resources, mLoadingBitmap, task);
			holder.SetImageDrawable(asyncDrawable);
			task.Execute();
		}

		public class BitmapWorkerTask : Android.OS.AsyncTask
		{
			private WeakReference<ImageView> viewHolderReference;
			private int position;
			private PDFPreviewPagerAdapter mAdapter;
			private Point pageSize;
			private MuPDFCore pdfCore;

			public BitmapWorkerTask(ImageView holder, MuPDFCore pdfCore, int position, Point size) 
			{
				viewHolderReference = new WeakReference<ImageView>(holder);
				this.position = position;
				this.pageSize = size;
				this.pdfCore = pdfCore;
			}

			protected override Java.Lang.Object DoInBackground(params Java.Lang.Object[] @params) 
			{
				//if (pageSize == null) 
				{
					pageSize = new Point();
					int padding = 180;//mContext.Resources.GetDimensionPixelSize(R.dimen.page_preview_size);
					PointF mPageSize = pdfCore.GetPageSize(0);
					float scale = mPageSize.Y / mPageSize.X;
					pageSize.X = (int) ((float) padding / scale);
					pageSize.Y = padding;
				}

				Bitmap lq = null;

				lq = pdfCore.DrawPage(position, pageSize.X, pageSize.Y, 0, 0, pageSize.X, pageSize.Y);

				return lq;
			}

			protected override void OnPostExecute(Java.Lang.Object result) 
			{
				Bitmap bitmap = result as Bitmap;

				if (IsCancelled) 
				{
					bitmap = null;
				}

				if (viewHolderReference != null && bitmap != null) 
				{
					ImageView holder = null;
					bool res = false;

					res = viewHolderReference.TryGetTarget(out holder);

					if (res && holder != null) 
					{
						BitmapWorkerTask bitmapWorkerTask = GetBitmapWorkerTask(holder);
						if (this == bitmapWorkerTask && holder != null) 
						{
							holder.SetImageBitmap(bitmap);
						}
					}
				}
			}
		}

		private Bitmap GetDrawedBitmap(MuPDFCore core, int position, Point size)
		{
			Bitmap lq = null;

			lq = core.DrawPage(position, size.X, size.Y, 0, 0, size.X, size.Y);

			return lq;
		}

		public class AsyncDrawable : BitmapDrawable 
		{
			private WeakReference<BitmapWorkerTask> bitmapWorkerTaskReference;

			public AsyncDrawable(Android.Content.Res.Resources res, Bitmap bitmap, BitmapWorkerTask bitmapWorkerTask) : base(res, bitmap)
			{
				bitmapWorkerTaskReference = new WeakReference<BitmapWorkerTask>(bitmapWorkerTask);
			}

			public BitmapWorkerTask GetBitmapWorkerTask() 
			{
				BitmapWorkerTask holder = null;
				bool result;

				result = bitmapWorkerTaskReference.TryGetTarget(out holder);

				if(result)
				{
					return holder;
				}
				else
				{
					return null;
				}
			}
		}

		private static BitmapWorkerTask GetBitmapWorkerTask(ImageView imageView) 
		{
			if (imageView != null && imageView.Drawable != null) 
			{
				Drawable drawable = imageView.Drawable;
				if (drawable.GetType() == typeof(AsyncDrawable)) 
				{
					AsyncDrawable asyncDrawable = (AsyncDrawable) drawable;
					return asyncDrawable.GetBitmapWorkerTask();
				}
			}
			return null;
		}*/

		private static string TAG = typeof(PdfPagePreview).Name;
		private Context mContext;
		private MuPDFCore mCore;
		private string mPath;
		private Bitmap mLoadingBitmap = null;
		private Point mPreviewSize;
		private SparseArray<Bitmap> mBitmapCache = new SparseArray<Bitmap>();

		public PdfPagePreview(Context context, MuPDFCore core, string basePath)
		{
			mCore = core;
			mContext = context;

			System.IO.FileInfo file = new System.IO.FileInfo(basePath);

			string dir = System.IO.Path.GetFileNameWithoutExtension(basePath) + "_" + file.LastWriteTime.ToString("yyyyMMddhhmmss");

			mPath = System.IO.Path.Combine(context.ExternalCacheDir.ToString(), dir);

			//mLoadingBitmap = BitmapFactory.DecodeResource(mContext.Resources, Resource.Drawable.darkdenim3);

			//mPath = context.ExternalCacheDir + "/" + System.IO.Path.GetFileNameWithoutExtension(basePath) + "/";
		}

		public void DrawPageImageView(ImageView holder, int position) 
		{
			if (CancelPotentialWork(holder, position)) 
			{
				BitmapWorkerTask task = new BitmapWorkerTask(holder, position, this);
				AsyncDrawable asyncDrawable = new AsyncDrawable(mContext.Resources, mLoadingBitmap, task);
				holder.SetImageDrawable(asyncDrawable);
				task.Execute();
			}
		}

		public static bool CancelPotentialWork(ImageView holder, int position) 
		{
			BitmapWorkerTask bitmapWorkerTask = GetBitmapWorkerTask(holder);

			if (bitmapWorkerTask != null) 
			{
				int bitmapPosition = bitmapWorkerTask.position;
				if (bitmapPosition != position) 
				{
					// Cancel previous task
					bitmapWorkerTask.Cancel(true);
				} 
				else 
				{
					// The same work is already in progress
					return false;
				}
			}
			// No task associated with the ImageView, or an existing task was
			// cancelled
			return true;
		}

		public class BitmapWorkerTask : Android.OS.AsyncTask
		{
			public WeakReference<ImageView> viewHolderReference;
			public int position;
			private PdfPagePreview mWrapper;

			public BitmapWorkerTask(ImageView holder, int position, PdfPagePreview wrapper) 
			{
				viewHolderReference = new WeakReference<ImageView>(holder);
				this.position = position;
				this.mWrapper = wrapper;
			}

			protected override Java.Lang.Object DoInBackground(params Java.Lang.Object[] @params) 
			{
				mWrapper.mPreviewSize = new Point();
				int padding = 400;//mContext.Resources.GetDimensionPixelSize(R.dimen.page_preview_size);
				PointF mPageSize = mWrapper.mCore.GetPageSize(0);
				float scale = mPageSize.Y / mPageSize.X;
				mWrapper.mPreviewSize.X = (int) ((float) padding / scale);
				mWrapper.mPreviewSize.Y = padding;

				Bitmap lq = null;
				lq = mWrapper.GetCachedBitmap(position);
				mWrapper.mBitmapCache.Put(position, lq);
				return lq;
			}

			protected override void OnPostExecute(Java.Lang.Object result) 
			{
				Bitmap bitmap = result as Bitmap;

				if (IsCancelled) 
				{
					bitmap = null;
				}

				if (viewHolderReference != null && bitmap != null) 
				{
					ImageView holder = null;
					bool res = false;

					res = viewHolderReference.TryGetTarget(out holder);

					if (res && holder != null) 
					{
						BitmapWorkerTask bitmapWorkerTask = GetBitmapWorkerTask(holder);
						if (this == bitmapWorkerTask && holder != null) 
						{
							holder.SetImageBitmap(bitmap);
						}
					}
				}
			}
		}

		private Bitmap GetCachedBitmap(int position) 
		{
			string mCachedBitmapFilePath = mPath + position + ".jpg";
			Java.IO.File mCachedBitmapFile = new Java.IO.File(mCachedBitmapFilePath);
			Bitmap lq = null;
			try {
				if (mCachedBitmapFile.Exists() && mCachedBitmapFile.CanRead()) {
					Log.Debug(TAG, "page " + position + " found in cache");
					lq = BitmapFactory.DecodeFile(mCachedBitmapFilePath);
					return lq;
				}
			} catch (Exception ex) {
				//e.printStackTrace();
				// some error with cached file,
				// delete the file and get rid of bitmap
				mCachedBitmapFile.Delete();
				lq = null;

				Log.Error(TAG, ex.Message);
			}
			if (lq == null) 
			{
                lq = Bitmap.CreateBitmap(mPreviewSize.X, mPreviewSize.Y, Bitmap.Config.Argb8888);
                mCore.DrawPage(lq, position, mPreviewSize.X, mPreviewSize.Y, 0, 0, mPreviewSize.X, mPreviewSize.Y, new MuPDFCore.Cookie(mCore));

				try 
				{
					lq.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg, 80, new System.IO.FileStream(mCachedBitmapFilePath, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite));
				} 
				catch (Java.IO.FileNotFoundException ex) 
				{
					//e.printStackTrace();
					mCachedBitmapFile.Delete();

					Log.Error(TAG, ex.Message);
				}
			}
			return lq;
		}

		public class AsyncDrawable : BitmapDrawable 
		{
			private WeakReference<BitmapWorkerTask> bitmapWorkerTaskReference;

			public AsyncDrawable(Android.Content.Res.Resources res, Bitmap bitmap, BitmapWorkerTask bitmapWorkerTask) : base(res, bitmap)
			{
				bitmapWorkerTaskReference = new WeakReference<BitmapWorkerTask>(bitmapWorkerTask);
			}

			public BitmapWorkerTask GetBitmapWorkerTask() 
			{
				BitmapWorkerTask holder = null;
				bool result;

				result = bitmapWorkerTaskReference.TryGetTarget(out holder);

				if(result)
				{
					return holder;
				}
				else
				{
					return null;
				}
			}
		}

		private static BitmapWorkerTask GetBitmapWorkerTask(ImageView imageView) 
		{
			if (imageView != null && imageView.Drawable != null) 
			{
				Drawable drawable = imageView.Drawable;
				if (drawable.GetType() == typeof(AsyncDrawable)) 
				{
					AsyncDrawable asyncDrawable = (AsyncDrawable) drawable;
					return asyncDrawable.GetBitmapWorkerTask();
				}
			}
			return null;
		}
	}
}

