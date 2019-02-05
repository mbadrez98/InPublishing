
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
using System.Threading;
using System.Threading.Tasks;

namespace InPublishing
{
	[Activity(Label = "Search", Theme = "@style/Blue.DialogActivity")]			
	public class SearchDocActivity : Activity
	{		
		private ListView _ListView;
		private SearchView _SearchView;
        private List<Pubblicazione> _Pubblicazioni;
		private string _SearchString = "";
		private const double SEARCH_INTERVAL = 300;

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

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

			_ListView = (ListView) FindViewById<ListView>(Resource.Id.pageList);

			_SearchView = (SearchView) FindViewById<SearchView>(Resource.Id.searchView);

			//_SearchView.Background = Resources.GetDrawable(Resource.Drawable.ic_action_refresh);

			int searchPlateId = _SearchView.Context.Resources.GetIdentifier("android:id/search_plate", null, null);
			View searchPlate = _SearchView.FindViewById(searchPlateId);
            searchPlate.Background.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);

			_SearchView.Focusable = true;
			_SearchView.Iconified = false;
			_SearchView.RequestFocusFromTouch();

			_SearchView.QueryTextChange += this.SearchBarTextChanged;/*(sender, e) => 
			{
				SearchPerform(e.NewText);
			};*/

			_SearchView.QueryTextSubmit += (sender, e) => 
			{
				_SearchView.ClearFocus();
			};

			_ListView.ItemClick += (sender, e) => 
			{
				var item = _Pubblicazioni[e.Position];

				Intent i = new Intent();
				i.SetClass(Application.Context, typeof(ViewerScreen));

				i.PutExtra("pubPath", item.Path);
				StartActivity(i);
				Finish();
			};
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

		private void SearchPerform()
		{
			Thread th = new Thread(() =>{ 
                _Pubblicazioni = PubblicazioniManager.SearchPubblicazioni(_SearchString);

				RunOnUiThread(() => 
				{
					_ListView.Adapter = new SearchDocAdapter(this, _Pubblicazioni);
				});
			});

			th.Start();

			/*var records = _documento.CercaArticoli(searchString);

			_articoli = new List<Articolo>();

			foreach(KeyValuePair<Articolo, int> pair in records)
			{
				_articoli.Add(pair.Key);
			}

			ListView.Adapter = new PagesAdapter(this, _articoli, _documento, _currentPage);*/
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

		private CancellationTokenSource throttleCts = new CancellationTokenSource();
		private void SearchBarTextChanged(object sender, SearchView.QueryTextChangeEventArgs args)
		{
			Interlocked.Exchange(ref this.throttleCts, new CancellationTokenSource()).Cancel();
			Task.Delay(TimeSpan.FromMilliseconds(SEARCH_INTERVAL), this.throttleCts.Token) // throttle time
				.ContinueWith(
					delegate { 
					_SearchString = args.NewText;
					SearchPerform();         
				},
					CancellationToken.None,
					TaskContinuationOptions.OnlyOnRanToCompletion,
					TaskScheduler.FromCurrentSynchronizationContext());
		}
	}
}