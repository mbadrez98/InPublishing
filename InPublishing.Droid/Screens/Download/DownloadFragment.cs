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
using Android.Database;
using Xamarin.InAppBilling;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace InPublishing
{
	public class DownloadFragment : UpdateManagerLoadingFragment
	{
		string _CurrentDir;
		private List<Object> _Items;
		private GridView _GridView = null;
		private Thread _Thread;
		ActionMode actionMode = null;
		Button _BtnBack;
		private int ColumnNum = 1;
		ImageView _backImage = null;

        private SaneInAppBillingHandler _billingHandler; 
         private List<Subscription> _Abbonamenti = new List<Subscription>();

		public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
		{
			var view = inflater.Inflate(Resource.Layout.DownloadScreen, container, false);

			Activity.Title = Activity.ActionBar.Title = GetString(Resource.String.menu_Download);

            if(Activity.GetType() == typeof(HomeScreen))
            {
                var home = Activity as HomeScreen;

                _billingHandler = home.BillingHandler;
            }

			this.SetHasOptionsMenu(true);
			//this.RetainInstance = true;
			//this.HasOptionsMenu = true;
			/*ActionBar.Title = "Pubblicazioni";
			ActionBar.SetHomeButtonEnabled(true);*/

			string color = DataManager.Get<ISettingsManager>().Settings.ToolbarBarColor;
			//ActionBar.SetBackgroundDrawable(new ColorDrawable(Color.ParseColor(color)));

			_GridView = view.FindViewById<GridView>(Resource.Id.downloadGrid);
			_GridView.ChoiceMode = (ChoiceMode)AbsListViewChoiceMode.None;

			this.SetGridColumnNumber();

			//selettore
			/*StateListDrawable states = new StateListDrawable();
			states.AddState(new int[] {Android.Resource.Attribute.StatePressed}, new ColorDrawable(Color.ParseColor(color).SetAlpha(0.2f)));
			states.AddState(new int[] {}, new ColorDrawable(Color.Transparent));

			_EdicolaGridView.Selector = states; //new ColorDrawable(Color.ParseColor(color).SetAlpha(0.5f));
			_EdicolaGridView.SetDrawSelectorOnTop(true);*/
			_GridView.ItemClick += OnItemClick;

			//pulsante indietro
			_BtnBack = Activity.ActionBar.CustomView.FindViewById<Button>(Resource.Id.btnBack);
			_BtnBack.Text = Activity.Title = Activity.ActionBar.Title = GetString(Resource.String.down_title);
			_BtnBack.SetCompoundDrawables(null, null, null, null);
			_BtnBack.Click += (sender, e) => 
			{
				if(_CurrentDir == "")
				{
					if(Activity != null && Activity.GetType() == typeof(HomeScreen))
					{
						var home = Activity as HomeScreen;
						home.OpenDrawer();
					}
				}

				this.GoBack();
			};

			_CurrentDir = "";

            if (this.Arguments != null)
            {
                String path = this.Arguments.GetString("path");

                if (path != null && path != "")
                {
                    _CurrentDir = path.Trim('/');
                }
            }

			/*IntentFilter filter = new IntentFilter(Android.App.DownloadManager.ActionDownloadComplete);
			Activity.RegisterReceiver(new DownloadReceiver(), filter);*/

			//MBDownloadManager.Context = Activity.BaseContext;			

			return view;
		}

		private void SetupIfNeeded()
		{
            if (_GridView.Adapter == null)
			{
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
			inflater.Inflate(Resource.Menu.DownloadActionBarMenu, menu);

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

			var downAll = menu.FindItem(Resource.Id.EdicolaActionBarMenu_Download);
			downAll.SetTitle(GetString(Resource.String.edic_downloadAll));

			//cerca
			var search = menu.FindItem(Resource.Id.EdicolaActionBarMenu_Search);
			search.Icon.Colorize (DataManager.Get<ISettingsManager> ().Settings.TintColor);

			if (!DataManager.Get<ISettingsManager> ().Settings.EdicolaSearch)
			{
				menu.RemoveItem (search.ItemId);

				order.SetShowAsAction (ShowAsAction.Always);
			}

            var overflow = menu.FindItem(Resource.Id.menu_overflow);
            overflow.Icon.Colorize(DataManager.Get<ISettingsManager>().Settings.TintColor);

            //abbonamenti
            var subscribe = menu.FindItem(Resource.Id.EdicolaActionBarMenu_Subscribe);
            subscribe.SetTitle(GetString(Resource.String.iap_subscribe));

            if (!DataManager.Get<ISettingsManager>().Settings.InAppPurchase && overflow.HasSubMenu)
            {
                overflow.SubMenu.RemoveItem(subscribe.ItemId);
            }

            SetupIfNeeded();
		}

		public override bool OnOptionsItemSelected (IMenuItem item)
		{
			//Toast.MakeText (this, "Selected Item: " + item.TitleFormatted, ToastLength.Short).Show ();

			switch (item.ItemId)
			{
				case Resource.Id.EdicolaActionBarMenu_Refresh:
					this.PopulateTable();
					return true;

				case Resource.Id.EdicolaActionBarMenu_Layout:
					if(_GridView.NumColumns == 1)
					{
						this.SetGridColumnNumber();
						_GridView.SetVerticalSpacing(10);

						item.SetTitle(GetString(Resource.String.edic_list));

						item.SetIcon(Resource.Drawable.ic_action_view_as_list);
					}
					else
					{
						_GridView.SetNumColumns(1);
						_GridView.SetVerticalSpacing(2);

						ColumnNum = 1;

						item.SetTitle(GetString(Resource.String.edic_grid));

						item.SetIcon(Resource.Drawable.ic_action_view_as_grid);
					}

					PopulateTable();

					return true;
				
				case Resource.Id.EdicolaActionBarMenu_Order:
					ShowOrderDialog();
					return true;
				
				case Resource.Id.EdicolaActionBarMenu_Download:
					ShowDownloadDialog();
					return true;

				case Resource.Id.EdicolaActionBarMenu_Search:
					var search = new Intent (this.Activity, typeof(SearchDownActivity));
					StartActivity (search);
					return true;

                case Resource.Id.EdicolaActionBarMenu_Subscribe:
                    ShowSubscribedDialog();
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

			_Thread = new Thread(() => 
			{
				Task loadTask = LoadDocuments();
			});

			_Thread.Start();

			StartUpdating(true);
		}

        private async Task LoadDocuments()
		{
			Utils.WriteLog("download PopulateTable");

            if(!CheckBeforeConnect())
                return;

			List<Download> docs = DownloadManager.GetDocuments(_CurrentDir, DataManager.Get<IPreferencesManager>().Preferences.EdicolaOrder);			

			List<DownDir> _Directories = DownloadManager.GetDirectory(_CurrentDir);

            //aggiungo celle vuote in modo da tenere separati cartelle e documenti
			int lastLineCol = _Directories.Count - ((int)(_Directories.Count / ColumnNum) * ColumnNum);

			if(ColumnNum > 1 && lastLineCol > 0)
			{
				int n = ColumnNum - lastLineCol;

				for(int i = 0; i < n; i++)
				{
					DownDir info = new DownDir();
					_Directories.Add(info);
				}
			}

            //in-app billing
            // prendo i prodotti che non sono stati acquistati e che non hanno permessi dalla lista che mi arriva dal server
            Action<string> buyAction = null;
            _Abbonamenti = new List<Subscription>();

            if (DataManager.Get<ISettingsManager>().Settings.InAppPurchase && _billingHandler != null)
            {
                var products = docs.Where(p => p.IapID != "" && !p.IapAutorizzato && !p.IapAcquistato && p.Stato == DownloadStato.Download);

                if (products.Count() > 0)
                {
                    //lista degli id dei prodotti per richiedere le info a google
                    List<string> productsIds = new List<string>();

                    foreach (var prod in products)
                    {
                        productsIds.Add(prod.IapID);
                    }

                    //richiesta delle informazioni a google
                    IReadOnlyList<Product> billProducts = await _billingHandler.QueryInventory(productsIds, ItemType.Product);

                    if (billProducts != null)
                    {
                        //per ogni prodotto setto i valori corretti nel download
                        foreach (var billProd in billProducts)
                        {
                            var down = docs.Where(d => d.IapID == billProd.ProductId).FirstOrDefault();
                            if (down != null && down.ID != "")
                            {
                                if (!down.IapAcquistato && !down.IapAutorizzato && down.Stato == DownloadStato.Download)
                                {
                                    down.IapPrezzo = billProd.Price;
                                    down.Stato = DownloadStato.Buy;
                                }
                            }

                            //tolgo i prodotti corretti dalla lista degli id
                            productsIds.RemoveAll(x => x == billProd.ProductId);
                        }   

                        //gli id rimasti li tolgo dai download perché vuol dire che google non li ha riconosciuti
                        foreach (var id in productsIds)
                        {
                            docs.RemoveAll(x => x.IapID == id);
                        }

                        buyAction += (string p) =>
                        {
                            if (!DownloadManager.IsLogged())
                            {
                                Action action = delegate {

                                    PopulateTable();
                                };

                                ActivitiesBringe.SetObject("loginSuccess", action);

                                var logActivity = new Intent (Activity, typeof(LoginActivity));
                                StartActivity (logActivity);
                            }
                            else
                            {
                                var prod = billProducts.Where(x => x.ProductId == p).FirstOrDefault();

                                if (prod == null)
                                    return;

                                Task billTask = BillProcess(prod);
                            }
                        };
                    }
                }

                //abbonamenti
                List<string> subIds = new List<string>();
                _Abbonamenti = DownloadManager.GetSubscriptions();

                foreach (var s in _Abbonamenti)
                {
                    subIds.Add(s.IapID);
                }

                if (subIds.Count > 0)
                {
                    IReadOnlyList<Product> billSubscription = await _billingHandler.QueryInventory(subIds, ItemType.Subscription);

                    if (billSubscription != null)
                    {
                        foreach (var billSub in billSubscription)
                        {
                            var sub = _Abbonamenti.Where(d => d.IapID == billSub.ProductId).FirstOrDefault();

                            if (sub != null && sub.IapID != "")
                            {
                                sub.Nome = billSub.Title.Replace("(" + Activity.ApplicationInfo.LoadLabel(Activity.PackageManager) + ")", "").Trim();
                                sub.Prezzo = billSub.Price;
                            }

                            //tolgo i prodotti corretti dalla lista degli id
                            subIds.RemoveAll(x => x == billSub.ProductId);
                        }

                        //gli id rimasti li tolgo dai download perché vuol dire che google non li ha riconosciuti
                        foreach (var id in subIds)
                        {
                            _Abbonamenti.RemoveAll(x => x.IapID == id);
                        }
                    }
                }
            }

			_Items = new List<Object>();
			_Items.AddRange(_Directories);
			_Items.AddRange(docs);

			var adapter = new DownloadGridAdapter(Activity, _Items);

			adapter.OpenAction += (string p) =>
			{
				Intent i = new Intent();
				i.SetClass(Activity, typeof(ViewerScreen));

				i.SetAction(Intent.ActionView);

				i.PutExtra("pubPath", p);
				Activity.StartActivityForResult(i, 0);
			};

			if(Activity != null)
			{
				Activity.RunOnUiThread(() =>
				{
					this.SetTitle();
					StopUpdating();
					_GridView.Adapter = adapter;
				});	
			}

            if (buyAction != null)
            {
                adapter.BuyAction += buyAction;

            }
		}

        private async Task BillProcess(Product prod)
        {
            var result = await _billingHandler.BuyProduct(prod);
            var returnData = new Dictionary<string, string>();

            if(result.Result == BillingResult.OK)
            {
                if (result.Purchase != null && result.Data != null)
                {
                    string orderID = "";
                    if (result.Purchase.OrderId == null || result.Purchase.OrderId == "")
                        orderID = result.Purchase.PurchaseToken;

                    var data = new Dictionary<string, string>();
                    data.Add("packageName", Activity.PackageName);
                    data.Add("orderId", orderID);
                    data.Add("productId", result.Purchase.ProductId);
                    data.Add("developerPayload", result.Purchase.DeveloperPayload);
                    data.Add("purchaseTime", result.Purchase.PurchaseTime.ToString());
                    data.Add("purchaseToken", result.Purchase.PurchaseToken);
                    data.Add("purchaseState", result.Purchase.PurchaseState.ToString());

                    returnData = Notification.CheckRegisterPurchase(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data))));
                }
            }
            else if(result.Result == BillingResult.ItemAlreadyOwned)
            {
               var purch = await _billingHandler.GetPurchases(ItemType.Product);

                foreach (var p in purch)
                {
                    if (p.ProductId == prod.ProductId)
                    {
                        string orderID = "";
                        if (p.OrderId == null || p.OrderId == "")
                        {
                            orderID = p.PurchaseToken;
                        }

                        var data = new Dictionary<string, string>();
                        data.Add("packageName", Activity.PackageName);
                        data.Add("orderId", orderID);
                        data.Add("productId", p.ProductId);
                        data.Add("developerPayload", p.DeveloperPayload);
                        data.Add("purchaseTime", p.PurchaseTime.ToString());
                        data.Add("purchaseToken", p.PurchaseToken);
                        data.Add("purchaseState", p.PurchaseState.ToString());

                        returnData = Notification.CheckRegisterPurchase(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data))));

                        break;
                    }
                }
            }
            else if(result.Result == BillingResult.UserCancelled)
            {
                
            }
            else
            {
                returnData["success"] = "false";
                returnData["errorCode"] = result.Result.ToString();
            }

            if (returnData["success"].ToLower() == "true")
            {
                var sub = _Abbonamenti.Where(d => d.IapID == prod.ProductId).FirstOrDefault();

                if (sub != null && sub.IapID != "") //abbonamento
                {
                    this.PopulateTable();
                }
                else //prodotto
                {
                    Activity.RunOnUiThread(() =>
                    {
                        var uri = new Uri(DataManager.Get<ISettingsManager>().Settings.DownloadUrl + "data/accounts/" + returnData["account"] + "/pub/" + returnData["path"]);
                        MBDownloadManager.RequestDownload(uri, new VoidNotify());

                        this.PopulateTable();
                    });
                }
            }
            else
            {
                string msgError = "";
                switch (returnData["errorCode"])
                {
                    case "401":
                        msgError = Activity.GetString(Resource.String.iap_authenticationError);
                        break;
                    case "403":
                        msgError = Activity.GetString(Resource.String.iap_unauthorizedUser);
                        break;
                    case "402":
                        msgError = Activity.GetString(Resource.String.iap_transactionError);
                        break;
                    case "503":
                        msgError = Activity.GetString(Resource.String.gen_tryLater);
                        break;
					case "505":
						msgError = Activity.GetString(Resource.String.gen_tryLater);
						break;
                }

                Activity.RunOnUiThread(() =>
                {
                    var alert = new AlertDialog.Builder(Activity);
                    alert.SetTitle(GetString(Resource.String.gen_error));

                    if(msgError != "")
                        alert.SetMessage(msgError + " [" + returnData["errorCode"] + "]");
                    else
                        alert.SetMessage("code: " + returnData["errorCode"]);
                    
                    alert.SetPositiveButton("Ok", (EventHandler<DialogClickEventArgs>)null);
                    alert.Show().SetDivider();
                });
            }
        }

        private bool CheckBeforeConnect()
        {
            Uri host = new Uri(DataManager.Get<ISettingsManager>().Settings.DownloadUrl);
            if (!Reachability.IsHostReachable("http://" + host.Host))
            {
                if (Activity == null)
                    return false;

                Activity.RunOnUiThread(() =>
                {
                    StopUpdating();

                    var alert = new AlertDialog.Builder(Activity);
                    alert.SetTitle(GetString(Resource.String.gen_error));
                    alert.SetMessage(GetString(Resource.String.gen_serverNotReachable));
                    alert.SetPositiveButton("Ok", (EventHandler<DialogClickEventArgs>)null);
                    alert.Show().SetDivider();
                });
                return false;
            }

            //se siamo nella cartella principale controllo la compatibilità con la versione dell'app
            var verResult = DownloadManager.CheckAppVersion(2);

            if (!verResult.Success)
            {
                Activity.RunOnUiThread(() =>
                {
                    StopUpdating();
                    var alert = new AlertDialog.Builder(Activity);
                    alert.SetCancelable(false);
                    alert.SetTitle(GetString(Resource.String.gen_error));
                    alert.SetMessage(GetString(Resource.String.gen_appVersion));
                    alert.SetPositiveButton(GetString(Resource.String.gen_downApp), delegate
                    {
                        var callIntent = new Intent(Intent.ActionView);

                        Android.Net.Uri uri = Android.Net.Uri.Parse(verResult.Link);
                        callIntent.SetData(uri);
                        this.StartActivity(callIntent);

                        //va all'edicola
                        var home = Activity as HomeScreen;
                        home.GoTo(0);
                    });

                    alert.SetNegativeButton(GetString(Resource.String.gen_cancel), delegate
                    {
                        //va all'edicola
                        var home = Activity as HomeScreen;
                        home.GoTo(0);
                    });

                    alert.Show().SetDivider();
                });

                this.IsUpdating = false;
                return false;

            }

            //controllo utente disattivato
            var result = DownloadManager.CheckUser();

            if (!result.Success)
            {
                if (result.ErrBloccante)
                {
                    Activity.RunOnUiThread(() =>
                    {
                        StopUpdating();
                        var alert = new AlertDialog.Builder(Activity);
                        alert.SetTitle(GetString(Resource.String.gen_error));
                        alert.SetMessage(result.Message);
                        alert.SetPositiveButton("Ok", (EventHandler<DialogClickEventArgs>)null);
                        alert.Show().SetDivider();

                        var adapter = new DownloadGridAdapter(Activity, new List<Object>());
                        _GridView.Adapter = adapter;
                        StopUpdating();

                    });

                    this.IsUpdating = false;
                    return false;

                }
            }

            return true;
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

		private void OrderChoiceClicked (object sender, DialogClickEventArgs e)
		{
			var dialog = sender as AlertDialog;
			
            DataManager.Get<IPreferencesManager>().Preferences.EdicolaOrder = e.Which;
            DataManager.Get<IPreferencesManager>().Save();

			dialog.Dismiss();
			PopulateTable();
		}

		private void ShowDownloadDialog()
		{
			var dialog = new AlertDialog.Builder(Activity);

			dialog.SetTitle(GetString(Resource.String.down_title));
			dialog.SetMessage (GetString (Resource.String.down_downAll));
			dialog.SetNegativeButton(GetString(Resource.String.gen_cancel), delegate { return; });
			dialog.SetPositiveButton(GetString(Resource.String.gen_download), delegate(object sender, DialogClickEventArgs e)
			{	
				if(_Items == null)
					return;

				foreach(var item in _Items)				
				{
					if(item == null)
						continue;
					
					if(item.GetType() == typeof(Download))
					{
						Download down = (item as Download);

						if(down.Stato == DownloadStato.Download || down.Stato == DownloadStato.Update)
						{
                            MBDownloadManager.RequestDownload(down.Uri, new VoidNotify());
						}
					}
				}

				PopulateTable();
			});

			dialog.Create();
			dialog.Show().SetDivider();
		}

        private void ShowSubscribedDialog()
        {
            if (DownloadManager.IsLogged())
            {
                var dialog = new AlertDialog.Builder(Activity);
                dialog.SetTitle(String.Format(GetString(Resource.String.iap_subscribeTo), Activity.ApplicationInfo.LoadLabel(Activity.PackageManager)));

                string[] items = (from a in _Abbonamenti where a.Prezzo != "" select a.Nome + " " + a.Prezzo).ToArray();

                dialog.SetItems(items, SubscribedDialogClicked);
                dialog.SetNegativeButton(GetString(Resource.String.gen_cancel), delegate { return; });

                dialog.Create();
                dialog.Show().SetDivider();
            }
            else
            {
                Action action = delegate {

                    ShowSubscribedDialog();
                };

                ActivitiesBringe.SetObject("loginSuccess", action);

                var logActivity = new Intent(Activity, typeof(LoginActivity));

                StartActivity(logActivity);
            }


        }

        private void SubscribedDialogClicked(object sender, DialogClickEventArgs e)
        {
            if(e.Which >= 0 && e.Which < _Abbonamenti.Count)
            {
                var sub = _Abbonamenti[e.Which];

                Task.Run(
                    async () =>
                    {
                        List<string> id = new List<string>() { sub.IapID };
                        IReadOnlyList<Product> billProducts = await _billingHandler.QueryInventory(id, ItemType.Subscription);

                        if (billProducts != null)
                        {
                            var prod = billProducts.FirstOrDefault();
                            if (prod != null && prod.ProductId != null)
                            {
                                Task billTask = BillProcess(prod);
                            }
                        }
                    }
                );
            }
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

			if(item.GetType() == typeof(Download))
			{
				DownloadDetails dialog = new DownloadDetails(this.Activity, item as Download);

				dialog.OpenAction += () =>
				{
					Intent i = new Intent();
					i.SetClass(Activity, typeof(ViewerScreen));

					i.SetAction(Intent.ActionView);

					i.PutExtra("pubPath", (item as Download).GetLocalPath());
					Activity.StartActivityForResult(i, 0);

					dialog.Dismiss();
				};

				dialog.DownloadAction += () =>
				{
					//var cell = ;
					//var cell = _GridView.Adapter.get(e.Position);

					//FileDownloader.DefaultRequestDownload((item as Download).Uri, new DownloadGridItem(Activity, (item as Download)));

					Download down = (item as Download);
					//MBDownloadManager.Download(down.Uri.AbsoluteUri, System.IO.Path.GetFileName(down.Uri.LocalPath), down.Titolo);
					MBDownloadManager.RequestDownload(down.Uri, new VoidNotify());

					this.PopulateTable();
					dialog.Dismiss();
				};

                dialog.BuyAction += () => 
                {
                    Task.Run(
                        async () =>
                        {
                            if (!DownloadManager.IsLogged())
                            {
                                Action action = delegate {

                                    PopulateTable();
                                };

                                ActivitiesBringe.SetObject("loginSuccess", action);

                                var logActivity = new Intent(Activity, typeof(LoginActivity));
                                StartActivity(logActivity);
                            }
                            else
                            {
                                Download down = (item as Download);
                                List<string> id = new List<string>() { down.IapID };
                                IReadOnlyList<Product> billProducts = await _billingHandler.QueryInventory(id, ItemType.Product);

                                if (billProducts != null)
                                {
                                    var prod = billProducts.FirstOrDefault();
                                    if (prod != null && prod.ProductId != null)
                                    {
                                        Task billTask = BillProcess(prod);
                                    }
                                }
                            }
                        }
                    );

                    dialog.Dismiss();
                };

				dialog.Show();
			}
			else if(item.GetType() == typeof(DownDir))
			{
				Console.WriteLine("click dir" + e.Position);

				DownDir dir = item as DownDir;

				_CurrentDir = dir.Path;
				PopulateTable();
			}
		}

		public void OnItemOptionClick(PopupMenu.MenuItemClickEventArgs e, int position)
		{
			Console.WriteLine("opzione " + position + " " + e.Item.ItemId);

			switch(e.Item.ItemId)
			{
				case Resource.Id.EdicolaItemOption_Delete:
					List<int> pos = new List<int>();
					pos.Add(position);
					break;
			}
		}

		#endregion

		public void GoBack()
		{
			if(_CurrentDir == "")
				return;

			var parts = _CurrentDir.Split('/');

			if(parts.Length > 0)
			{
				var items = parts.Take(parts.Length - 1);

				_CurrentDir = String.Join("/", items);
				PopulateTable();
			}

			/*if(_CurrentDir.FullName != DataManager.Get<ISettingsManager>().Settings.DocPath)
			{
				_CurrentDir = _CurrentDir.Parent;
				PopulateTable();
			}*/
		}

		private void SetTitle()
		{
			if (!IsAdded)
				return;
			
			var icon = Resources.GetDrawable(Resource.Drawable.ic_ab_back);
			icon.Colorize (DataManager.Get<ISettingsManager> ().Settings.TintColor);
			_BtnBack.SetCompoundDrawablesWithIntrinsicBounds(icon, null, null, null);
			_BtnBack.Text = _CurrentDir;

			if(_CurrentDir == "")
			{				
				_BtnBack.Text = GetString(Resource.String.menu_Download);

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

			_GridView.SetNumColumns(ColumnNum);
		} 

		public bool OnBackPressed()
		{
			if(_CurrentDir != "")
			{
				this.GoBack();
				return false;
			}

			return true;
		}

        public override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (resultCode == Result.Ok) 
            {
                string action = data.GetStringExtra("action");

                if(action == "refresh")
                {
                    this.PopulateTable();
                }
            }
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

				if (toOrientation == Android.Content.Res.Orientation.Portrait)
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
                    View.SetBackgroundColor(Color.Transparent);
				}

				_backImage.SetImageDrawable(Drawable.CreateFromStream(stream, null));

			}
			catch (Exception ex)
			{
				Log.Error("EdicolaFragment - SetBackground", ex.Message);
			}
		}

	}

	class DownloadReceiver : BroadcastReceiver
	{
		public override void OnReceive (Context context, Android.Content.Intent intent)
		{
			if(intent.Action.Equals(Android.App.DownloadManager.ActionDownloadComplete))
			{
				Bundle data = intent.Extras;
				long download_id = data.GetLong(Android.App.DownloadManager.ExtraDownloadId );

				Android.App.DownloadManager.Query query = new Android.App.DownloadManager.Query();
				query.SetFilterById(download_id);

				Android.App.DownloadManager downloadManager = (Android.App.DownloadManager)context.GetSystemService("download");

				ICursor c = downloadManager.InvokeQuery(query);

				if(c.MoveToFirst())
				{
					string title = c.GetString(c.GetColumnIndex(Android.App.DownloadManager.ColumnTitle));
					string uri = c.GetString(c.GetColumnIndex(Android.App.DownloadManager.ColumnUri));
					string localUri = c.GetString(c.GetColumnIndex(Android.App.DownloadManager.ColumnLocalUri));

					string fileName = System.IO.Path.GetFileName(localUri);

					string search = "/pub/";
					//string url = uri.AbsoluteUri;
					uri = uri.Substring(uri.IndexOf(search) + search.Length).Trim('/');

					string outPath = System.IO.Path.Combine(DataManager.Get<ISettingsManager>().Settings.DocPath, uri);
					//outPath = Path.Combine(outPath, parts[1]).Trim('/'); 
					outPath = System.Web.HttpUtility.UrlDecode(outPath);
					outPath = System.IO.Path.GetDirectoryName(outPath);

					string filePath = new Uri(localUri).AbsolutePath;
					new Thread(() => 
					{
						try
						{
							if(System.IO.Path.GetExtension(localUri) == ".mb")
							{
								//outPath = Path.Combine(outPath, fileName.Replace(fileExt, ".mbp"));
								FileSystemManager.UnzipDocument(filePath, outPath);
								File.Delete(filePath);
							}
							else if(System.IO.Path.GetExtension(localUri) == ".pdf")
							{
								outPath = System.IO.Path.Combine(outPath, fileName); 
								if(File.Exists(outPath))
								{
									File.Delete(outPath);
								}

								//se la cartella non esiste la creo
								string dir = System.IO.Path.GetDirectoryName(outPath);

								if(!Directory.Exists(dir))
								{
									Directory.CreateDirectory(dir);
								}

								File.Move(filePath, outPath);
							}
						}
						catch (Exception value)
						{
							Console.WriteLine(value);
						}		



					}).Start();
							


				}
			}
		}
	}
}

