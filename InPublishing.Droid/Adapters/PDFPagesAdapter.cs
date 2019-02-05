using System;
using Android.Widget;
using Com.Artifex.Mupdfdemo;
using Android.Content;
using Android.Graphics;
using Java.IO;
using Android.Util;
using Android.Views;
using Android.OS;
using Android.Graphics.Drawables;

namespace InPublishing
{
	public class PDFPagesAdapter : BaseAdapter 
	{
		//private static string TAG = typeof(PDFPreviewPagerAdapter).Name;
		private Context mContext;
		private MuPDFCore mCore;
		int[] mPages;
		//private Point mPreviewSize;
		//private SparseArray<Bitmap> mBitmapCache = new SparseArray<Bitmap>();
		//private string mPath;

		//private int currentlyViewing;
		//private Bitmap mLoadingBitmap;

		private PdfPagePreview mPdfPreview;
		private int mCurrentIndex;

		public PDFPagesAdapter(Context context, MuPDFCore core, string basePath, int index = -1, int[] pages = null) 
		{
			mContext = context;
			mCore = core;
			mCurrentIndex = index;
			mPages = pages;

			mPdfPreview = new PdfPagePreview(mContext, mCore, basePath);

			/*mPath = context.ExternalCacheDir + "/" + System.IO.Path.GetFileNameWithoutExtension(basePath) + "/";

			File mCacheDirectory = new File(mPath);
			if (!mCacheDirectory.Exists())
				mCacheDirectory.Mkdirs();

			mLoadingBitmap = BitmapFactory.DecodeResource(mContext.Resources, Resource.Drawable.darkdenim3);*/
		}

		public override int Count
		{
			get
			{
				if(mPages != null)
				{
					return mPages.Length;
				}
				else
				{
					return mCore.CountPages();
				}
			}
		}

		public override Java.Lang.Object GetItem(int position)
		{
			return null;
		}

		public override long GetItemId(int position)
		{
			return position;
		}

		public override View GetView(int position, View convertView, ViewGroup parent) 
		{
			/*ViewHolder holder;
			if (convertView == null) 
			{
				LayoutInflater inflater = (LayoutInflater) mContext.GetSystemService(Context.LayoutInflaterService);
				convertView = inflater.Inflate(Resource.Layout.preview_pager_item_layout, parent, false);
				holder = new ViewHolder(convertView);
				convertView.SetTag(1, holder);
			} 
			else 
			{
				holder = (ViewHolder) convertView.Tag;
			}

			LayoutInflater inflater = (LayoutInflater) mContext.GetSystemService(Context.LayoutInflaterService);
			convertView = inflater.Inflate(Resource.Layout.preview_pager_item_layout, parent, false);
			holder = new ViewHolder(convertView);

			if (mPreviewSize != null) 
			{
				holder.PreviewPageImageView.LayoutParameters = new LinearLayout.LayoutParams(mPreviewSize.X, mPreviewSize.Y);
			}

			holder.PreviewPageNumber.SetText((position + 1).ToString(), TextView.BufferType.Normal);
			holder.PreviewPageLinearLayout.SetBackgroundColor(Color.Transparent);
			DrawPageImageView(holder, position);
			return convertView;*/

			int index = -1;

			if(mPages != null)
			{
				index = mPages[position];
			}
			else
			{
				index = position;
			}

			LayoutInflater inflater = (LayoutInflater) mContext.GetSystemService(Context.LayoutInflaterService);
			convertView = inflater.Inflate(Resource.Layout.PagesThumbItem, parent, false);

			TextView txtLabel = convertView.FindViewById<TextView>(Resource.Id.txtLabel);

			txtLabel.Text = string.Format(mContext.GetString(Resource.String.pub_pagina), (index + 1));

			ImageView imgThumb = convertView.FindViewById<ImageView>(Resource.Id.imgThumb);

			mPdfPreview.DrawPageImageView(imgThumb, index);

			//se è la pagina corrente setto il testo in grassetto
			if(mCurrentIndex != -1 && index == mCurrentIndex)
			{
				txtLabel.SetTypeface(txtLabel.Typeface, TypefaceStyle.Bold);
				txtLabel.TextSize = 20;
			}

			return convertView;
		}

		/*public class ViewHolder 
		{
			public ImageView PreviewPageImageView = null;
			public TextView PreviewPageNumber = null;
			public LinearLayout PreviewPageLinearLayout = null;

			public ViewHolder(View view) 
			{
				this.PreviewPageImageView = (ImageView) view.FindViewById(Resource.Id.PreviewPageImageView);
				this.PreviewPageNumber = (TextView) view.FindViewById(Resource.Id.PreviewPageNumber);
				this.PreviewPageLinearLayout = (LinearLayout) view.FindViewById(Resource.Id.PreviewPageLinearLayout);
			}
		}

		private void DrawPageImageView(ViewHolder holder, int position) 
		{
			if (CancelPotentialWork(holder, position)) 
			{
				BitmapWorkerTask task = new BitmapWorkerTask(holder, position, this);
				AsyncDrawable asyncDrawable = new AsyncDrawable(mContext.Resources, mLoadingBitmap, task);
				holder.PreviewPageImageView.SetImageDrawable(asyncDrawable);
				task.Execute();
			}
		}

		public static bool CancelPotentialWork(ViewHolder holder, int position) 
		{
			BitmapWorkerTask bitmapWorkerTask = GetBitmapWorkerTask(holder.PreviewPageImageView);

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
			public WeakReference<ViewHolder> viewHolderReference;
			public int position;
			private PDFPreviewPagerAdapter mAdapter;

			public BitmapWorkerTask(ViewHolder holder, int position, PDFPreviewPagerAdapter adapter) 
			{
				viewHolderReference = new WeakReference<ViewHolder>(holder);
				this.position = position;
				this.mAdapter = adapter;
			}

			protected override Java.Lang.Object DoInBackground(params Java.Lang.Object[] @params) 
			{
				if (mAdapter.mPreviewSize == null) 
				{
					mAdapter.mPreviewSize = new Point();
					int padding = 100;//mContext.Resources.GetDimensionPixelSize(R.dimen.page_preview_size);
					PointF mPageSize = mAdapter.mCore.GetPageSize(0);
					float scale = mPageSize.Y / mPageSize.X;
					mAdapter.mPreviewSize.X = (int) ((float) padding / scale);
					mAdapter.mPreviewSize.Y = padding;
				}
				Bitmap lq = null;
				lq = mAdapter.GetCachedBitmap(position);
				mAdapter.mBitmapCache.Put(position, lq);
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
					ViewHolder holder = null;
					bool res = false;

					res = viewHolderReference.TryGetTarget(out holder);

					if (res && holder != null) 
					{
						BitmapWorkerTask bitmapWorkerTask = GetBitmapWorkerTask(holder.PreviewPageImageView);
						if (this == bitmapWorkerTask && holder != null) 
						{
							holder.PreviewPageImageView.SetImageBitmap(bitmap);
							holder.PreviewPageNumber.SetText((position + 1).ToString(), TextView.BufferType.Normal);
							if (mAdapter.GetCurrentlyViewing() == position) 
							{
								holder.PreviewPageLinearLayout
									.SetBackgroundColor(Color.Red);
							} 
							else 
							{
								holder.PreviewPageLinearLayout.SetBackgroundColor(Color.Transparent);
							}
						}
					}
				}
			}
		}

		private Bitmap GetCachedBitmap(int position) 
		{
			string mCachedBitmapFilePath = mPath + position + ".jpg";
			File mCachedBitmapFile = new File(mCachedBitmapFilePath);
			Bitmap lq = null;
			try {
				if (mCachedBitmapFile.Exists() && mCachedBitmapFile.CanRead()) {
					Log.Debug(TAG, "page " + position + " found in cache");
					lq = BitmapFactory.DecodeFile(mCachedBitmapFilePath);
					return lq;
				}
			} catch (Exception e) {
				//e.printStackTrace();
				// some error with cached file,
				// delete the file and get rid of bitmap
				mCachedBitmapFile.Delete();
				lq = null;
			}
			if (lq == null) 
			{
				//lq = Bitmap.CreateBitmap(mPreviewSize.X, mPreviewSize.Y,Bitmap.Config.Argb8888);
				lq = mCore.DrawPage(position, mPreviewSize.X, mPreviewSize.Y, 0, 0, mPreviewSize.X, mPreviewSize.Y);
				try 
				{
					lq.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg, 50, new System.IO.FileStream(mCachedBitmapFilePath, System.IO.FileMode.OpenOrCreate, System.IO.FileAccess.ReadWrite));
				} 
				catch (FileNotFoundException e) 
				{
					//e.printStackTrace();
					mCachedBitmapFile.Delete();
				}
			}
			return lq;
		}

		public int GetCurrentlyViewing() {
			return currentlyViewing;
		}

		public void SetCurrentlyViewing(int currentlyViewing) {
			this.currentlyViewing = currentlyViewing;
			NotifyDataSetChanged();
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
	}
}

