using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace InPublishing
{
    class DownloadOverlay : FrameLayout, IDownloadUpdated2
	{
		private TextView _lblMessage;
        private ProgressBar _progressBar;
        private Activity _context;

        public Action DownloadCompleted;

        public DownloadOverlay(Activity context, string label = null, Action complete = null) : base(context)
		{
            _context = context;
            DownloadCompleted = complete;

			this.LayoutParameters = new FrameLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent);

            View.Inflate(this.Context, Resource.Layout.DownloadOverlay, this);

			_lblMessage = FindViewById<TextView>(Resource.Id.lblMessage);
            _progressBar = FindViewById<ProgressBar>(Resource.Id.dlProgress);

			if(label == null)
			{
				_lblMessage.SetText(context.GetString(Resource.String.gen_loading), TextView.BufferType.Normal);
			}
			else
			{
				_lblMessage.SetText(label, TextView.BufferType.Normal);
			}

			this.Visibility = ViewStates.Invisible;

            Drawable bgDrawable = _progressBar.ProgressDrawable;
            bgDrawable.SetColorFilter(Color.White, PorterDuff.Mode.SrcIn);
            _progressBar.ProgressDrawable = bgDrawable;
		}

		public void Hide ()
		{
			//#if DEBUG
			//#else
			this.Visibility = ViewStates.Invisible;

			//#endif

			//this.Alpha = 0;
		}

		public void Show ()
		{
			//#if DEBUG
			//#else
			this.Visibility = ViewStates.Visible;
			//#endif
			//this.Alpha = 1;
		}

        public void Download(Uri uri)
        {
            MBDownloadManager.RequestDownload(uri, this);
        }

        void IDownloadUpdated2.ProgressChanged(int progress)
        {
            if (_context != null && _lblMessage == null)
            {
                return;
            }

            _context.RunOnUiThread(() =>
            {
                _progressBar.Progress = progress;
            });
        }

        void IDownloadUpdated2.DownloadCompleted(string uri, string localUri)
        {
            if (_context != null && _lblMessage == null)
            {
                return;
            }

            //RegisterDownload(uri, localUri);

            _context.RunOnUiThread(() =>
            {
                this.Hide();

                if (DownloadCompleted != null)
                {
                    DownloadCompleted();
                }
            });
        }
	}
}

