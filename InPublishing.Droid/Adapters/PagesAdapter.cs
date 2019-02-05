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
	public class PagesAdapter : BaseAdapter<Articolo>
	{
		protected Activity _context = null;
		private List<Articolo> _Items;
		private int _currentIndex;
		private List<Bookmark> _bookmarks;
		private Pubblicazione _pubblicazione;
        private Documento _documento;

        public PagesAdapter(Activity context, List<Articolo> pages, Pubblicazione pub, Documento doc, int index = -1, List<Bookmark> bookmarks = null)
		{
			_Items = pages;
			_context = context;
			_currentIndex = index;
			_bookmarks = bookmarks;
			_pubblicazione = pub;
            _documento = doc;
		}

		public override Articolo this[int position]
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

            int absPage = _pubblicazione.RelativeToAbsolutePage(item.IdDocumento, item.Index);

            PageItem view = null;

            if(convertView == null)
            {
                view = new PageItem(_context);
            }
            else
            {
                view = convertView as PageItem;  
            }

            //se è la pagina corrente setto il testo in grassetto
            view.SetBold(false);
            if(item.IdDocumento == _documento.ID && item.Index == _currentIndex + 1)
            {
                view.SetBold();
            }

            if(view.AbsPage == absPage)
                return view;

			if(_bookmarks != null && position < _bookmarks.Count && _bookmarks[position].Titolo != "")
			{
                view.Title = _bookmarks[position].Titolo;
			}
			else
			{           
				if(Utils.CompareVersion(_pubblicazione.Script, "4.6.0") >= 0)
				{
					view.Title = item.Titolo;

                    if(item.Titolo == "")
                    {
                        view.Title = absPage.ToString();

                    }
                    else if(item.Titolo != absPage.ToString())
					{
                        view.Title += " (" + absPage.ToString() + ")";
					}
				}
				else
				{
                    view.Title = string.Format(_context.GetString(Resource.String.pub_pagina), absPage.ToString());
				}
			}

            var doc = _pubblicazione.GetDocumento(item.IdDocumento);

            if(doc != null)
            {
                string imgPath = System.IO.Path.Combine(doc.Path, item.Path, "miniatura.jpg");
                if(System.IO.File.Exists(imgPath))
                {
                    MBImageLoader.DisplayDiskImage(imgPath, view.ImgCover, new PointF(100, 100));
                    //Koush.UrlImageViewHelper.SetUrlDrawable(imgThumb, new Uri(imgPath).AbsoluteUri);
			    }
            }

            view.AbsPage = absPage;

			return view;
		}
	}
}

