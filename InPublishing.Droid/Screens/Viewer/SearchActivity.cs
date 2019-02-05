
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
using Android.Graphics;
using Android.Graphics.Drawables;

namespace InPublishing
{
	[Activity(Label = "Search", Theme = "@style/Blue.DialogActivity")]			
	public class SearchActivity : Activity
	{
        private Pubblicazione _pubblicazione;
        private Documento _documento;
		private int _currentPage;
		private MuPDFCore _pdfCore;
		private ListView _pageList;
		private SearchView _searchView;
		List<Articolo> _articoli = new List<Articolo>();

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			_currentPage = Intent.GetIntExtra("currentPage", 0);
            _pubblicazione = (Pubblicazione)ActivitiesBringe.GetObject("pub");
            _documento = (Documento)ActivitiesBringe.GetObject("doc");
			_pdfCore = (MuPDFCore)ActivitiesBringe.GetObject("pdfCore");

			SetContentView(Resource.Layout.SearchDialog);

			Window.SetSoftInputMode(SoftInput.AdjustNothing);

			if(ActionBar != null)
			{
				this.Title = ActionBar.Title = GetString(Resource.String.search);

				ActionBar.SetDisplayUseLogoEnabled(false);
				ActionBar.SetIcon(new ColorDrawable(Color.Transparent));
				ActionBar.SetHomeButtonEnabled(false);
				ActionBar.SetDisplayHomeAsUpEnabled(true);
				ActionBar.SetDisplayShowHomeEnabled(true);
				ActionBar.SetDisplayShowTitleEnabled(true);
			}

			_pageList = (ListView) FindViewById<ListView>(Resource.Id.pageList);

			_searchView = (SearchView) FindViewById<SearchView>(Resource.Id.searchView);

			int searchPlateId = _searchView.Context.Resources.GetIdentifier("android:id/search_plate", null, null);
			View searchPlate = _searchView.FindViewById(searchPlateId);
            searchPlate.Background.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);

			_searchView.Focusable = true;
			_searchView.Iconified = false;
			_searchView.RequestFocusFromTouch();

			_searchView.QueryTextChange += (sender, e) => 
			{
				SearchPages(e.NewText);
			};

			_searchView.QueryTextSubmit += (sender, e) => 
			{
				_searchView.ClearFocus();
			};

			_pageList.ItemClick += (sender, e) => 
			{
				var art = _articoli[e.Position];

				Intent myIntent = new Intent (this, typeof(ViewerScreen));
                myIntent.PutExtra("doc", art.IdDocumento);
                myIntent.PutExtra("page", (art.Index - 1).ToString());
                myIntent.PutExtra("action", "page");

				SetResult (Result.Ok, myIntent);
				Finish();
			};

			//paramentro ricerca
			var str = ActivitiesBringe.GetObject ("search");
			if (str != null)
			{
				string search = (string)str;

				_searchView.SetQuery (search, true);

				_searchView.RequestFocus ();
			}
		}

		protected override void OnStart()
		{
			base.OnStart();

			if(!Utility.IsTablet(this))
				return;

			Rect frame = new Rect();
			Window.DecorView.GetWindowVisibleDisplayFrame(frame);

			Window.SetGravity(GravityFlags.Center | GravityFlags.Center);

			WindowManagerLayoutParams par = Window.Attributes;  
			//par.Y = Utility.dpToPx(this, 50);
			par.Height = frame.Bottom - Utility.dpToPx(this, 121);  
			par.Width = Utility.dpToPx(this, 530); 
			par.DimAmount = 0;
			this.Window.Attributes = par; 

			Window.AddFlags(WindowManagerFlags.DimBehind);
		}

		private void SearchPages(string searchString)
		{			
			try
			{
				var articoliList = _pubblicazione.CercaArticoli(searchString);

				_articoli = new List<Articolo>();

				foreach(KeyValuePair<Articolo, int[]> pair in articoliList)
				{
					_articoli.Add(pair.Key);
				}

                _pageList.Adapter = new PagesAdapter(this, _articoli, _pubblicazione, _documento, _currentPage);
			}
			catch(Exception ex)
			{
				Utils.WriteLog("SearchActivity: " + ex.Message);	
			}
		}

		public override bool OnOptionsItemSelected(IMenuItem item)
		{
			switch (item.ItemId)
			{
				case Android.Resource.Id.Home:
					this.Finish();
					return true;
					//break;
			}

			return false;
		}
	}
}