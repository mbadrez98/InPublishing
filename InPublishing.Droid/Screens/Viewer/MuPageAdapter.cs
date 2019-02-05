
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
using Com.Artifex.Mupdfdemo;
using Android.Util;
using Android.Graphics;

namespace InPublishing
{
	class MuPageAdapter : MuPDFPageAdapter
	{
		private Documento _doc;
		private static String TAG = "MuPDFPageAdapter";

		private Context mContext;
		private MuPDFCore mCore;
		public static SparseArray<PointF> mPageSizes = new SparseArray<PointF>();
		private ViewerScreen _docView;
		private SparseArray<MuPageView> _pages;
        private Bitmap mSharedHqBm;
        private FilePicker.IFilePickerSupport mFilePickerSupport;

        public MuPageAdapter(Context context, FilePicker.IFilePickerSupport filePickerSupport, MuPDFCore core, Documento doc, ViewerScreen docView) : base(context, filePickerSupport, core)
		{
			_doc = doc;
			mCore = core;
			mContext = context;
			_docView = docView;

			_pages = new SparseArray<MuPageView> ();// new MuPageView[core.CountPages()];

			mPageSizes = new SparseArray<PointF>();
		}

		public override Java.Lang.Object GetItem(int i)
		{
			if(i < _pages.Size() && _pages.Get(i) != null)
			{
				return _pages.Get(i);
			}
			else
			{
				return null;
			}
		}

        public void ReleaseBitmaps()
        {
            //  recycle and release the shared bitmap.
            if (mSharedHqBm!=null)
                mSharedHqBm.Recycle();
            mSharedHqBm = null;
        }

		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			Log.Debug(TAG,"getView");
			MuPageView pageView;
			if (convertView == null) 
			{
                try
                {
                if (mSharedHqBm == null || mSharedHqBm.Width != parent.Width || mSharedHqBm.Height != parent.Height)
                    mSharedHqBm = Bitmap.CreateBitmap(parent.Width, parent.Height, Bitmap.Config.Argb8888);
                }
                catch(Java.Lang.Error er)
                {
                    mSharedHqBm = null;

                    string tag = TAG + " - GetView";

                    if(er.Message != null)
                        Log.Error(tag, er.Message);
                }

                try
                {
                    pageView = new MuPageView(mContext, mFilePickerSupport, mCore, new Point(parent.Width, parent.Height), mSharedHqBm, _doc, _docView);
                }
                catch(Java.Lang.OutOfMemoryError ex)
                {
                    Log.Error(TAG, ex.Message);
                    GC.Collect();

                    pageView = new MuPageView(mContext, mFilePickerSupport, mCore, new Point(parent.Width, parent.Height), mSharedHqBm, _doc, _docView);
                }
			} 
			else 
			{
				pageView = (MuPageView)convertView;
			}

			PointF pageSize = mPageSizes.Get(position);

			if (pageSize != null) 
			{
				// We already know the page size. Set it up
				// immediately
				pageView.SetPage(position, pageSize);
			} 
			else 
			{
				// Page size as yet unknown. Blank it for now, and
				// start a background task to find the size
				pageView.Blank(position);

				/*Android.OS.AsyncTask<Void,Void,PointF> sizingTask = new SafeAsyncTask<Void,Void,PointF>() {

					protected override PointF doInBackground(Void... arg0) {
						return core.getPageSize(position);
					}

					@Override
					protected void onPostExecute(PointF result) {
						if (isCancelled()) {
							return;
						}
						// We now know the page size
						mPageSizes.put(position, result);
						// Check that this view hasn't been reused for
						// another page since we started
						if (pageView.getPage() == position)
							pageView.setPage(position, result);
					}
				};*/

				SizingTask task = new SizingTask(mCore, position, pageView);

				task.Execute();
			}

			_pages.Put(position, pageView);

			return pageView;
		}

		public class SizingTask : Android.OS.AsyncTask
		{
			private MuPDFCore Core;
			private int index;
			private MuPageView PageView;

			public SizingTask(MuPDFCore core, int index, MuPageView pageView) : base()
			{
				this.Core = core;
				this.index = index;
				this.PageView = pageView;
			}

			protected override void OnPreExecute()
			{
			}

			protected override Java.Lang.Object DoInBackground(params Java.Lang.Object[] @params)
			{
				return Core.GetPageSize(index);
			}

			protected override void OnPostExecute(Java.Lang.Object result)
			{
				if(IsCancelled)
				{
					return;
				}

				mPageSizes.Put(index, result);

				if (PageView.Page == index)
					PageView.SetPage(index, result as PointF);
			}


		}

	}
}

