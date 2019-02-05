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
using System.IO;
using System.Threading;
using Android.Graphics.Drawables;
using Android.Graphics;
using Android.Support.V4.App;
using Android.Content.Res;
using Android.Text;
using Android.Text.Style;
using Android.Content.PM;
using Android;

namespace InPublishing
{
    public class EdicolaFragment : UpdateManagerLoadingFragment, ListView.IMultiChoiceModeListener
    {
        DirectoryInfo _CurrentDir;
      
        private List<Object> _Items;
        private GridView _EdicolaGridView = null;
        private Thread _Thread;
        ActionMode actionMode;
        Button _BtnBack;
        private int ColumnNum = 1;
        ImageView _backImage = null;

        private List<Download> _downloads = new List<Download>();

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.EdicolaScreen, container, false);

            this.SetHasOptionsMenu(true);

            _EdicolaGridView = view.FindViewById<GridView>(Resource.Id.edicolaGrid);
            _EdicolaGridView.ChoiceMode = (ChoiceMode)AbsListViewChoiceMode.MultipleModal;//ChoiceMode.Multiple;
            _EdicolaGridView.SetMultiChoiceModeListener(this);

            _EdicolaGridView.Selector.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);           

            this.SetGridColumnNumber();

            _EdicolaGridView.ItemClick += OnItemClick;

            //pulsante indietro
            _BtnBack = Activity.ActionBar.CustomView.FindViewById<Button>(Resource.Id.btnBack);
            _BtnBack.Text = Activity.Title = Activity.ActionBar.Title = GetString(Resource.String.menu_Edicola);

            _BtnBack.Click += (sender, e) =>
            {
                if (_CurrentDir.FullName == DataManager.Get<ISettingsManager>().Settings.DocPath)
                {
                    if (Activity != null && Activity.GetType() == typeof(HomeScreen))
                    {
                        var home = Activity as HomeScreen;
                        home.OpenDrawer();
                    }
                }

                this.GoBack();
            };

            _CurrentDir = new DirectoryInfo(DataManager.Get<ISettingsManager>().Settings.DocPath);

            if (this.Arguments != null)
            {
                String path = this.Arguments.GetString("path");

                if (path != null && path != "")
                {
                    _CurrentDir = new DirectoryInfo(System.IO.Path.Combine(_CurrentDir.FullName, path.Trim('/')));
                }
            }

            return view;			
        }

        private void SetupIfNeeded()
        {
            if(_EdicolaGridView.Adapter == null)
            {
                SetTitle();
                PopulateTable();
                SetBackground();
            }
        }

        /*public override void OnResume()
		{
			Console.WriteLine("UPDATELOADING OnResume");
			base.OnResume();

			PopulateTable();
		}*/

        #region actionbar
        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.EdicolaActionBarMenu, menu);

            //aggiorna
            var refresh = menu.FindItem(Resource.Id.EdicolaActionBarMenu_Refresh);
            refresh.SetTitle(GetString(Resource.String.gen_refresh));
            refresh.Icon.Colorize(DataManager.Get<ISettingsManager>().Settings.TintColor);

            //ordina
            var order = menu.FindItem(Resource.Id.EdicolaActionBarMenu_Order);
            order.SetTitle(GetString(Resource.String.edic_orderBy));

            //griglia/lista
            var layout = menu.FindItem(Resource.Id.EdicolaActionBarMenu_Layout);
            layout.SetTitle(GetString(Resource.String.edic_list));

            //cerca
            var search = menu.FindItem(Resource.Id.EdicolaActionBarMenu_Search);
            search.Icon.Colorize(DataManager.Get<ISettingsManager>().Settings.TintColor);

            if (!DataManager.Get<ISettingsManager>().Settings.EdicolaSearch)
            {
                menu.RemoveItem(search.ItemId);

                order.SetShowAsAction(ShowAsAction.Always);
            }

            //more
            var overflow = menu.FindItem(Resource.Id.menu_overflow);
            overflow.Icon.Colorize(DataManager.Get<ISettingsManager>().Settings.TintColor);

            if (Activity != null && IsAdded)
            {
                var titleId = Resources.GetIdentifier("action_bar_title", "id", "android");
                var abTitle = Activity.FindViewById<TextView>(titleId);

                if (abTitle != null)
                    abTitle.SetTextColor(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.TintColor));
            }

            SetupIfNeeded();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            //Toast.MakeText (this, "Selected Item: " + item.TitleFormatted, ToastLength.Short).Show ();

            switch (item.ItemId)
            {
                case Resource.Id.EdicolaActionBarMenu_Refresh:
                    this.PopulateTable();
                    return true;

                case Resource.Id.EdicolaActionBarMenu_Layout:
                    if (_EdicolaGridView.NumColumns == 1)
                    {
                        this.SetGridColumnNumber();
                        _EdicolaGridView.SetVerticalSpacing(10);

                        item.SetTitle(GetString(Resource.String.edic_list));

                        item.SetIcon(Resource.Drawable.ic_action_view_as_list);
                    }
                    else
                    {
                        _EdicolaGridView.SetNumColumns(1);
                        _EdicolaGridView.SetVerticalSpacing(2);

                        ColumnNum = 1;

                        item.SetTitle(GetString(Resource.String.edic_grid));

                        item.SetIcon(Resource.Drawable.ic_action_view_as_grid);
                    }

                    this.PopulateTable();

                    return true;

                case Resource.Id.EdicolaActionBarMenu_Order:
                    ShowOrderDialog();
                    return true;

                case Resource.Id.EdicolaActionBarMenu_Search:
                    var search = new Intent(this.Activity, typeof(SearchDocActivity));
                    //search.PutExtra("currentPage", _readerView.DisplayedViewIndex);
                    //ActivitiesBringe.SetObject("doc", _documento);
                    //ActivitiesBringe.SetObject("pdfCore", _pdfCore);

                    StartActivity(search);
                    return true;
            }

            return false;
        }
        #endregion

        #region caricamento elementi
        protected override void PopulateTable()
        {
            if (IsUpdating)
            {
                return;
            }

            //titolo pulsante back
            //if (!DataManager.Get<IPreferencesManager>().Preferences.AlreadyRun || FileSystemManager.DocumentsToImport > 0 || MBDownloadManager.Context == null)
            {
                //_Thread = new Thread(CaricaDocumentiAsync);
                StartUpdating(true);
                _Thread = new Thread(() =>
                    {
                        if(Activity.CanAccessExternal()) 
                        {
                            if (!Directory.Exists(DataManager.Get<ISettingsManager>().Settings.SharedPath))
                            {
                                Directory.CreateDirectory(DataManager.Get<ISettingsManager>().Settings.SharedPath);
                            }

                            FileSystemManager.ImportDocuments();
                        }

                        if (MBDownloadManager.Context == null)
                        {
                            MBDownloadManager.Context = Activity.BaseContext;
                            MBDownloadManager.InstallFinishedMB();
                        }

                        if (Activity != null)
                        {
                            Activity.RunOnUiThread(() =>
                                {
                                    LoadDocuments();
                                    StopUpdating();
                                });
                        }
                    });

                _Thread.Start();


            }
            /*else
            {
                StartUpdating(true);

                LoadDocuments();

                StopUpdating();
            }*/

            CheckUpdates();
        }

        private void LoadDocuments()
        {
            Utils.WriteLog("edicola PopulateTable");
            this.SetTitle();

            IList<Pubblicazione> documents = PubblicazioniManager.GetPubblicazioni(_CurrentDir.FullName, DataManager.Get<IPreferencesManager>().Preferences.EdicolaOrder);

            IList<DirectoryInfo> _Directories = FileSystemManager.GetFolders(_CurrentDir.FullName);

            int lastLineCol = _Directories.Count - ((int)(_Directories.Count / ColumnNum) * ColumnNum);

            if (ColumnNum > 1 && lastLineCol > 0)
            {
                int n = ColumnNum - lastLineCol;

                for (int i = 0; i<n; i++)
                {
                    DirectoryInfo info = new DirectoryInfo("/");
                    _Directories.Add(info);
                }
            }

            _Items = new List<Object>();
            _Items.AddRange(_Directories);
            _Items.AddRange(documents);

            var adapter = new EdicolaGridAdapter(Activity, _Items);

            adapter.ItemOptionClick += OnItemOptionClick;

            _EdicolaGridView.Adapter = adapter;

            if (!DataManager.Get<ISettingsManager>().Settings.DocAutoplayRun)
            {
                DataManager.Get<ISettingsManager>().Settings.DocAutoplayRun = true;

                var docAuto = (from d in documents where d.Autoplay == true select d).FirstOrDefault();

                if (docAuto != null)
                {
                    if (docAuto.DataScadenza >= DateTime.Now || docAuto.DataScadenza == DateTime.MinValue)
                    {
                        Intent i = new Intent();
                        i.SetClass(Activity, typeof(ViewerScreen));

                        i.SetAction(Intent.ActionView);

                        i.PutExtra("pubPath", docAuto.Path);
                        Activity.StartActivityForResult(i, 0);
                    }
                }
            }
        }

        private void DeleteItems(List<int> positions)
        {
            if (positions.Count == 0)
                return;

            var dialog = new AlertDialog.Builder(Activity);

            dialog.SetTitle("Elimina");

            if (positions.Count == 1)
            {
                dialog.SetMessage(GetString(Resource.String.edic_delete));
            }
            else
            {
                dialog.SetMessage(GetString(Resource.String.edic_deleteMulti));
            }

            dialog.SetNegativeButton(GetString(Resource.String.gen_cancel), delegate
                {
                    return;
                });

            dialog.SetPositiveButton(GetString(Resource.String.gen_delete), delegate (object sender, DialogClickEventArgs e)
                {
                    Console.WriteLine("ELIMINATO");

                    foreach (int pos in positions)
                    {
                        var item = _Items[pos];

                        if (item is Pubblicazione)
                        {
                            Pubblicazione doc = item as Pubblicazione;
                            FileSystemManager.Delete(doc.Path);
                        }
                        else if (item is DirectoryInfo)
                        {
                            DirectoryInfo dir = item as DirectoryInfo;
                            FileSystemManager.Delete(dir.FullName);
                        }
                    }

                    if (actionMode != null)
                    {
                        actionMode.Finish();
                    }

                    PopulateTable();
                });

            dialog.Create();
            dialog.Show().SetDivider();
        }

        private void ShowOrderDialog()
        {
            var dialog = new AlertDialog.Builder(Activity);

            dialog.SetTitle(GetString(Resource.String.edic_orderBy));

            String[] items = {
                " " + GetString(Resource.String.pub_title) + " ",
                " " + GetString(Resource.String.pub_date) + " ",
                " " + GetString(Resource.String.pub_author) + " ",
                " " + GetString(Resource.String.pub_expire) + " "};

            dialog.SetSingleChoiceItems(items, DataManager.Get<IPreferencesManager>().Preferences.EdicolaOrder, OrderChoiceClicked);

            dialog.Create();
            dialog.Show().SetDivider();
        }

        private void OrderChoiceClicked(object sender, DialogClickEventArgs e)
        {
            var dialog = sender as AlertDialog;

            DataManager.Get<IPreferencesManager>().Preferences.EdicolaOrder = e.Which;
            DataManager.Get<IPreferencesManager>().Save();

            dialog.Dismiss();
            PopulateTable();
        }
        #endregion

        #region iterazioni elementi
        public void OnItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            if (actionMode != null)
            {
                return;
            }

            var item = _Items[e.Position];

            if (item is Pubblicazione)
            {
                Console.WriteLine("click thumb" + e.Position);

                Pubblicazione doc = item as Pubblicazione;

                //se sta aggiornando non apro il documento
                var down = _downloads.Where(d => d.ID == doc.ID).FirstOrDefault();

                if (down != null)
                {
                    DownloadInfo downInfo = MBDownloadManager.DownloadInfo(down.Uri.AbsoluteUri);

                    if (downInfo != null && downInfo.Id != 0 && downInfo.Uri != "")
                    {
                        return;
                    }
                }

                if (DateTime.Now > doc.DataScadenza && !doc.IsPDF && doc.DataScadenza != DateTime.MinValue)
                {
                    var alert = new AlertDialog.Builder(Activity);
                    alert.SetTitle(GetString(Resource.String.gen_error));
                    alert.SetMessage(GetString(Resource.String.pub_expired));
                    alert.SetPositiveButton("Ok", (EventHandler<DialogClickEventArgs>)null);
                    alert.Show().SetDivider();
                    return;
                }

                Intent i = new Intent();
                i.SetClass(Activity, typeof(ViewerScreen));

                //i.SetFlags(ActivityFlags.NewTask);

                i.SetAction(Intent.ActionView);

                i.PutExtra("pubPath", doc.Path);
                Activity.StartActivityForResult(i, 0);
                //Activity.StartActivity(i);
            }
            else if (item is DirectoryInfo)
            {
                Console.WriteLine("click dir" + e.Position);

                DirectoryInfo dir = item as DirectoryInfo;

                _CurrentDir = dir;
                PopulateTable();
            }
        }

        public void OnItemOptionClick(PopupMenu.MenuItemClickEventArgs e, int position)
        {
            Console.WriteLine("opzione " + position + " " + e.Item.ItemId);

            switch (e.Item.ItemId)
            {
                case Resource.Id.EdicolaItemOption_Delete:
                    List<int> pos = new List<int>();
                    pos.Add(position);

                    DeleteItems(pos);
                    break;

                case Resource.Id.EdicolaItemOption_Details:

                    var item = _Items[position];

                    if (item is Pubblicazione)
                    {
                        EdicolaDetails dialog = new EdicolaDetails(this.Activity, item as Pubblicazione);

                        dialog.DeleteAction += () =>
                        {
                            List<int> poss = new List<int>();
                            poss.Add(position);

                            DeleteItems(poss);
                            dialog.Dismiss();
                        };

                        dialog.OpenAction += () =>
                        {
                            Intent i = new Intent();
                            i.SetClass(Activity, typeof(ViewerScreen));

                            //i.SetFlags(ActivityFlags.NewTask);

                            i.SetAction(Intent.ActionView);

                            i.PutExtra("pubPath", (item as Pubblicazione).Path);
                            Activity.StartActivityForResult(i, 0);

                            dialog.Dismiss();
                        };

                        dialog.Show();
                    }
                    break;
            }
        }

        #endregion

        #region IMultiChoiceModeListener

        public bool OnActionItemClicked(ActionMode mode, IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.EdicolaActionMode_Delete:
                    DeleteItems(GetSelectedItemPositions());
                    return true;
            }

            return false;
        }

        public bool OnCreateActionMode(ActionMode mode, IMenu menu)
        {
            mode.MenuInflater.Inflate(Resource.Menu.EdicolaActionModeMenu, menu);
            actionMode = mode;

            var delete = menu.FindItem(Resource.Id.EdicolaActionMode_Delete);
            delete.SetTitle(GetString(Resource.String.gen_refresh));
            delete.Icon.Colorize(DataManager.Get<ISettingsManager>().Settings.TintColor);


            //abDone.SetTextColor(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.TintColor));

            var adapter = _EdicolaGridView.Adapter as EdicolaGridAdapter;

            adapter.ActionMode = true;

            adapter.NotifyDataSetChanged();

			return true;
		}

		public void OnDestroyActionMode(ActionMode mode)
		{
			var adapter = _EdicolaGridView.Adapter as EdicolaGridAdapter;

			adapter.ActionMode = false;

			adapter.NotifyDataSetChanged();
			actionMode = null;
		}

		public void OnItemCheckedStateChanged (ActionMode mode, int position, long id, bool check)
		{
			int checkedCount = _EdicolaGridView.CheckedItemCount;

			switch (checkedCount) 
			{
				case 0:
					mode.Title = null;
					break;

				case 1:				
					mode.Title = GetString(Resource.String.edic_itemSelected);
					break;

				default:
				mode.Title = string.Format(GetString(Resource.String.edic_itemsSelected), checkedCount);
					break;
			}

            SpannableString text = new SpannableString(mode.Title != null ? mode.Title : "");
			text.SetSpan(new ForegroundColorSpan(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.TintColor)), 0, text.Length(), SpanTypes.InclusiveInclusive);
			Java.Lang.ICharSequence sequence = text.SubSequenceFormatted(0, text.Length());
			mode.TitleFormatted = sequence;

			var doneId = Resources.GetIdentifier("action_mode_close_button", "id", "android");
			var abDone = Activity.FindViewById<ViewGroup>(doneId);

			if (abDone != null && abDone.ChildCount > 0)
			{
				var img = abDone.GetChildAt (0);

				if (img != null && img.GetType () == typeof(ImageView))
				{
					var img2 = img as ImageView;

					img2.Colorize(DataManager.Get<ISettingsManager>().Settings.TintColor);
				}
			}
		}

		public bool OnPrepareActionMode(ActionMode mode, IMenu menu)
		{

			return false;
		}

		#endregion
		private List<int> GetSelectedItemPositions()
		{
			List<int> positions = new List<int>();

			var checks = _EdicolaGridView.CheckedItemPositions;

			for(int i = 0; i < _EdicolaGridView.Adapter.Count; i++)
			{
				if(checks.Get(i))
				{
					positions.Add(i);
				}
			}

			return positions;
		}

		public void GoBack()
		{
			if(_CurrentDir.FullName != DataManager.Get<ISettingsManager>().Settings.DocPath)
			{
				_CurrentDir = _CurrentDir.Parent;
				PopulateTable();
			}
		}

		private void SetTitle()
		{
			if (!IsAdded)
				return;
			
			var icon = Resources.GetDrawable(Resource.Drawable.ic_ab_back);
			icon.Colorize (DataManager.Get<ISettingsManager> ().Settings.TintColor);
			_BtnBack.SetCompoundDrawablesWithIntrinsicBounds(icon, null, null, null);
			_BtnBack.Text = _CurrentDir.Name;

			if(_CurrentDir.FullName == DataManager.Get<ISettingsManager>().Settings.DocPath)
			{
				_BtnBack.Text = GetString(Resource.String.menu_Edicola);

				_BtnBack.SetCompoundDrawables(null, null, null, null);
			}
		}

		private void SetGridColumnNumber()
		{
			var a = Activity.Resources.DisplayMetrics.Density;

			if(Utility.IsTablet(Activity))
			{
				if(Activity.Resources.Configuration.Orientation == Android.Content.Res.Orientation.Portrait)
				{
					ColumnNum = 3;
				}
				else
				{
					ColumnNum = 5;
				}
			}
			else
			{
				if(Activity.Resources.Configuration.Orientation == Android.Content.Res.Orientation.Portrait)
				{
					ColumnNum = 2;
				}
				else
				{
					ColumnNum = 3;
				}
			}

			_EdicolaGridView.SetNumColumns(ColumnNum);
		}

		public bool OnBackPressed()
		{
			if(_CurrentDir.FullName != DataManager.Get<ISettingsManager>().Settings.DocPath)
			{
				this.GoBack();
				return false;
			}

			return true;
		}

		private void CheckUpdates()
		{
			Uri host = new Uri(DataManager.Get<ISettingsManager>().Settings.DownloadUrl);
			if (!Reachability.IsHostReachable("http://" + host.Host))
			{				
				return;
			}

			var info = new DirectoryInfo(DataManager.Get<ISettingsManager>().Settings.DocPath);
			var dir = _CurrentDir.FullName.Replace(info.FullName, "").Trim('/');

			ThreadPool.QueueUserWorkItem(delegate
			{
				var downloads = DownloadManager.GetDocuments(dir);

				//var updates = downloads.Where(d => d.Stato == DownloadStato.Update);

				_downloads = downloads.ToList();

				if (Activity == null)
					return;

				Activity.RunOnUiThread(() => 
				{
					try
					{
						EdicolaGridAdapter adapter = (EdicolaGridAdapter)_EdicolaGridView.Adapter;

						adapter.CheckUpdates(_downloads);
					}
					catch(Exception ex)
					{
						Log.Error("", ex.Message);
					}
				});
			});
		}

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);

            SetBackground(newConfig.Orientation);
        }

        private void SetBackground(Android.Content.Res.Orientation toOrientation = Android.Content.Res.Orientation.Undefined)
        {
            if (toOrientation == Android.Content.Res.Orientation.Undefined)
                toOrientation = Activity.Resources.Configuration.Orientation;

			try
			{
                string imgPath = "";

                if(toOrientation == Android.Content.Res.Orientation.Portrait)
                {
                    imgPath = "bg-portrait.png";
                }
                else
                {
                    imgPath = "bg-landscape.png";
                }

                if (imgPath == "")
                    return;

                System.IO.Stream stream = Resources.Assets.Open(imgPath);

                //View.Background = Drawable.CreateFromStream(stream, null);

                if (_backImage == null)
                {
                    _backImage = new ImageView(Activity);
                    _backImage.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
                    _backImage.SetScaleType(ImageView.ScaleType.CenterCrop);

                    var view = View.Parent as FrameLayout;

                    view.AddView(_backImage, 0);
                }

                _backImage.SetImageDrawable(Drawable.CreateFromStream(stream, null));

			}
			catch (Exception ex)
			{
                Log.Error("EdicolaFragment - SetBackground", ex.Message);
			}
        }


        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            PopulateTable();
        }

        /*public override bool OnKeyDown(Android.Views.Keycode keyCode, Android.Views.KeyEvent e)
		{
			if (keyCode == Keycode.Back)
			{
				if(_CurrentDir.FullName != DataManager.Get<ISettingsManager>().Settings.DocPath)
				{
					_CurrentDir = _CurrentDir.Parent;
					PopulateTable();
					return false;
				}
			}

			return base.OnKeyDown(keyCode, e);
		}*/
    }
}