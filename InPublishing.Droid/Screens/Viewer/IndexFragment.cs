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

namespace InPublishing
{
	public class IndexFragment : BaseTitleFragment
	{
        private Pubblicazione _pubblicazione;
        private Documento _documento;
		private int _currentPage;
		private ListView _pagesList;
		private MuPDFCore _pdfCore;

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

        public IndexFragment(string title, Pubblicazione pub, Documento doc, int page, MuPDFCore core = null) : base(title)
		{
			_pubblicazione = pub;
            _documento = doc;
			_currentPage = page;
			_pdfCore = core;
		}

		public override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
		}

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			var view = inflater.Inflate(Resource.Layout.PagesThumbList, container, false);

			_pagesList = view.FindViewById<ListView>(Resource.Id.viewerPageList);

			_pagesList.ChoiceMode = ChoiceMode.Single;

			if(_pubblicazione.IsPDF)
			{
				_pagesList.Adapter = new PDFPagesAdapter(Activity, _pdfCore, _pubblicazione.Path, _currentPage);

                _pagesList.ItemClick += (sender, e) => {
                    _pageItemClick(new string[] { _pubblicazione.ID, e.Position.ToString() });
                };
			}
			else
			{
                var list = GetPages();
                _pagesList.Adapter = new PagesAdapter(Activity, list, _pubblicazione, _documento, _currentPage);

                _pagesList.ItemClick += (sender, e) => {
                    var item = list[e.Position];

                    _pageItemClick(new string[] { item.IdDocumento, (item.Index - 1).ToString() });
                };


			}

            /*_pagesList.ScrollStateChanged += (sender, scrollArgs) => {
                switch(scrollArgs.ScrollState)
                {
                    case ScrollState.Fling:
                        ImageLoader.Instance.Pause(); // all image loading requests will be silently canceled
                        break;
                    case ScrollState.Idle:
                        ImageLoader.Instance.Resume(); // loading requests are allowed again

                        // Here you should have your custom method that forces redrawing visible list items
                        //_pagesList.forc();
                        break;
                }
            };*/


			/*_pagesList.ItemSelected += (sender, e) => 
			{
				var view = _pagesList.Adapter.v
			};*/

			_pagesList.Post(ScrollToPage);

			return view;
		}

        private List<Articolo> GetPages()
        {
            var articoli = new List<Articolo>();

            foreach(Documento doc in _pubblicazione.Documenti)
            {
                articoli.AddRange(doc.Articoli);
            }

            return articoli;
        }

		private void ScrollToPage()
		{
            //_pagesList.SmoothScrollToPosition(_currentPage);
            int abspage = _pubblicazione.RelativeToAbsolutePage(_documento.ID, _currentPage + 1);
            _pagesList.SetSelection(abspage - 1);
		}
	}
}

