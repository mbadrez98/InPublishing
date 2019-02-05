using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Support.V4.App;
using Android.Graphics;
using Com.Artifex.Mupdfdemo;
using System.IO;

namespace InPublishing
{
	public class BookmarksFragment : BaseTitleFragment
	{
        private Pubblicazione _pubblicazione;
        private Documento _documento;
		private MuPDFCore _pdfCore;
		private int _currentPage;

		private Action<string[]> _pageItemClick;
        public Action<string[]> PageItemClick 
        {
            get
            {
                return _pageItemClick;
            }
            set
            {
                _pageItemClick = value;
            }
        }

        public BookmarksFragment(string title, Pubblicazione pub, Documento doc, int page, MuPDFCore core = null) : base(title)
		{
			_pubblicazione = pub;
            _documento = doc;
			_pdfCore = core;
			_currentPage = page;
		}

		public override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// Create your fragment here
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			var view = inflater.Inflate(Resource.Layout.PagesThumbList, container, false);

			ListView list = view.FindViewById<ListView>(Resource.Id.viewerPageList);

			var manager = new BookmarksManager(_pubblicazione);

			List<Articolo> articles = new List<Articolo>();

			var bookmarks = manager.GetBookMarks();

			if(_pubblicazione.IsPDF)
			{
				int[] pages = { };

				var el = from b in bookmarks
					select (b.Pagina - 1);

				if(el.Count() > 0)
				{
					pages = el.ToArray<int>();
				}

				list.Adapter = new PDFPagesAdapter(Activity, _pdfCore, _pubblicazione.Path, _currentPage, pages);
			}
			else
			{
				foreach(Bookmark book in bookmarks)
				{
					var doc = _pubblicazione.Documenti.Where(d => d.ID == book.Documento).FirstOrDefault<Documento>();

                    if(doc == null)
                    {
                        continue;
                    }

                    if(!_pubblicazione.IsPDF && !File.Exists(System.IO.Path.Combine(doc.Path, doc.Articoli[book.Pagina - 1].Path, "miniatura.jpg")))
                    {
                        continue;
                    }

                    if(book.Pagina - 1 < doc.Articoli.Count)
					{
						var art = doc.Articoli[book.Pagina - 1];

						articles.Add(art);
					}
				}

                list.Adapter = new PagesAdapter(Activity, articles, _pubblicazione, _documento, _currentPage, bookmarks);
			}

			list.ItemClick += (sender, e) => 
			{
				var book = bookmarks[e.Position];
                _pageItemClick(new string[] { book.Documento, (book.Pagina - 1).ToString() });
			};

			return view;
		}
	}
}

