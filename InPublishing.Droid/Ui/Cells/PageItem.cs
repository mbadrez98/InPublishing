using System;
using Android.Widget;
using Android.Views;
using Android.App;
using Android.Graphics;
using Android.OS;

namespace InPublishing
{
	public class PageItem : LinearLayout
	{
		public ImageView ImgCover 
		{ 
			get { return _imgCover; }
		}

		public string Title 
		{ 
			get { return _txtTitolo.Text; }
			set { _txtTitolo.SetText(value, TextView.BufferType.Normal); }
		}

        public int AbsPage;

		private Activity _context;
		private TextView _txtTitolo;
		private ImageView _imgCover;
		

		public PageItem(Activity context) : base(context)
		{
			_context = context;

			View.Inflate(_context, Resource.Layout.PagesThumbItem, this);			

			_txtTitolo = FindViewById<TextView>(Resource.Id.txtLabel);			
			_imgCover = FindViewById<ImageView>(Resource.Id.imgThumb);
		}

        public void SetBold(bool bold = true)
        {
            if(bold)
            {
                _txtTitolo.SetTypeface(null, TypefaceStyle.Bold);
                //_txtTitolo.TextSize = 20;
                //_txtTitolo.SetTextColor(Color.Red);
            }
            else
            {
                _txtTitolo.SetTypeface(null, TypefaceStyle.Normal);
                //_txtTitolo.TextSize = 20;
                //_txtTitolo.SetTextColor(Color.Blue);
            }
        }
	}
}

