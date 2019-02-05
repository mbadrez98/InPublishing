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
using Android.Util;
using Android.Graphics;
using Com.Artifex.Mupdfdemo;

namespace InPublishing
{
	class PagePreviewOverlay : LinearLayout
	{
		private TextView _lblPage;
		private ImageView _imgPage;

		public bool IsEmpty
		{
			get
			{ 
				return (_imgPage.Drawable == null);
			}
		}

		protected PagePreviewOverlay(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
		{
			InitView();
		}

		public PagePreviewOverlay(Context context) : base(IntPtr.Zero, JniHandleOwnership.DoNotTransfer)
		{
			InitView();
		}

		public PagePreviewOverlay(Context context, IAttributeSet attrs) : base(IntPtr.Zero, JniHandleOwnership.DoNotTransfer)
		{
			InitView();
		}

		private void InitView()
		{
			View.Inflate(this.Context, Resource.Layout.PagePreviewOverlay, this);

			_lblPage = FindViewById<TextView>(Resource.Id.lblPagePreview);
			_imgPage = FindViewById<ImageView>(Resource.Id.imgPagePreview);
		}

		public void SetPage(string imgPath, int page)
		{
			if(System.IO.File.Exists(imgPath))
			{
				MBImageLoader.DisplayDiskImage(imgPath, _imgPage, new PointF(400, 400));
            }

			_lblPage.Text = string.Format(this.Context.GetString(Resource.String.pub_pagina), (page + 1));
		}

		public void SetPagePdf(MuPDFCore pdfCore, int page, string basePath)
		{
			//PdfToImage.SetImage(_imgPage, pdfCore, page, new Point(300,300));

			PdfPagePreview mPdfPreview = new PdfPagePreview(this.Context, pdfCore, basePath);

			_lblPage.Text = string.Format(this.Context.GetString(Resource.String.pub_pagina), (page + 1));

			mPdfPreview.DrawPageImageView(_imgPage, page);
		}
	}
}

