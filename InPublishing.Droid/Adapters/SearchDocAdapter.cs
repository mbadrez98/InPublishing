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
using Android.Graphics;

namespace InPublishing
{
	public class SearchDocAdapter : BaseAdapter<Pubblicazione>
	{
		protected Activity _context = null;
		private List<Pubblicazione> _Items;

        public SearchDocAdapter(Activity context, List<Pubblicazione> items)
		{
			_context = context;
			_Items = items;
		}

		public override Pubblicazione this[int position]
		{
			get { return _Items[position]; }
		}

		public override long GetItemId(int position)
		{
			return position;
		}

		public override int Count
		{
			get { return _Items.Count; }
		}

		public override View GetView(int position, View convertView, ViewGroup parent)
		{
			var item = _Items[position];

            View view = null;

            if(convertView != null)
            {
                view = convertView;
            }
            else
                view = _context.LayoutInflater.Inflate(Resource.Layout.EdicolaListItem, parent, false);

			var imgCover = view.FindViewById<ImageView>(Resource.Id.edicolaImgCover);
			var txtTitolo = view.FindViewById<TextView>(Resource.Id.edicolaTxtTitolo);	
			var txtDettagli = view.FindViewById<TextView>(Resource.Id.edicolaTxtDettagli);
			var btnInfo = view.FindViewById<ImageView>(Resource.Id.btnInfo);

			TextView txtLabel = view.FindViewById<TextView>(Resource.Id.txtLabel);

			//copertina
			if(item.IsPDF)
			{
				imgCover.SetImageResource(Resource.Drawable.pdf_icon);
				imgCover.SetScaleType(ImageView.ScaleType.FitCenter);
			}
			else
			{
				string imgPath = System.IO.Path.Combine(item.Path, "copertina.jpg");
				if(System.IO.File.Exists(imgPath))
				{
                    MBImageLoader.DisplayDiskImage(imgPath, imgCover, new PointF(280, 280));
				}
			}

			//titolo            
			txtTitolo.SetText(item.Titolo, TextView.BufferType.Normal);

			//dettagli
			if(txtDettagli != null)
			{
				txtDettagli.SetText(item.Autore, TextView.BufferType.Normal);
			}

			//pulsanti
			if (btnInfo != null)
			{
				btnInfo.Visibility = ViewStates.Gone;
			}

			return view;
		}
	}
}

