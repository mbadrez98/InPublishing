
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
	public class SearchDownActivity : Activity
	{		
		private ListView _ListView;
		private SearchView _SearchView;
		private ProgressBar _Loader;
		private List<Download> _Downloads;
		private string _SearchString = "";
		private const double SEARCH_INTERVAL = 1000;

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
			_Loader = (ProgressBar) FindViewById<ProgressBar>(Resource.Id.prgLoader);

			int searchPlateId = _SearchView.Context.Resources.GetIdentifier("android:id/search_plate", null, null);
			View searchPlate = _SearchView.FindViewById(searchPlateId);
            searchPlate.Background.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);

			_SearchView.Focusable = true;
			_SearchView.Iconified = false;
			_SearchView.RequestFocusFromTouch();

			_SearchView.QueryTextChange += this.SearchBarTextChanged;

			_SearchView.QueryTextSubmit += (sender, e) => 
			{
				_SearchView.ClearFocus();
			};

			//_SearchView.HapticFeedbackEnabled = false;
			_SearchView.LongClickable = false;
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
			if (_SearchString.Length == 0)
			{
				_ListView.Adapter = new SearchDownAdapter(this, new List<Download>());
				_Loader.Visibility = ViewStates.Gone;
				return;
			}

			_Loader.Visibility = ViewStates.Visible;

			Thread th = new Thread(() =>{ 				

				_Downloads = DownloadManager.SearchDocuments(_SearchString); 

				RunOnUiThread(() => 
				{
                    var adapter = new SearchDownAdapter(this, _Downloads);                   

                    adapter.OpenAction += (string p) => {
                        Intent i = new Intent();
                        i.SetClass(this, typeof(ViewerScreen));

                        i.SetAction(Intent.ActionView);

                        i.PutExtra("pubPath", p);
                        this.StartActivityForResult(i, 0);
                    };

                    _ListView.Adapter = adapter;

                   _Loader.Visibility = ViewStates.Gone;
				});
			});

			th.Start();
		}

		private void ItemClick (object sender, AdapterView.ItemClickEventArgs e)
		{
			var item = _Downloads[e.Position];

            MBDownloadManager.RequestDownload(item.Uri, new VoidNotify());

			item.Stato = DownloadStato.Downloading;

			//_ListView.Adapter = new SearchDownAdapter(this, _Downloads);

            var adapter = new SearchDownAdapter(this, _Downloads);

            adapter.OpenAction += (string p) => {
                Intent i = new Intent();
                i.SetClass(this, typeof(ViewerScreen));

                i.SetAction(Intent.ActionView);

                i.PutExtra("pubPath", p);
                this.StartActivityForResult(i, 0);
            };

            _ListView.Adapter = adapter;
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