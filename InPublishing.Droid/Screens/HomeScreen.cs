using System;

using Android.Content;
using Android.OS;
using Android.Widget;

using Android.Content.Res;
using Android.Content.PM;
//using Android.Support.V4.App;
//using Android.Support.V4.Widget;
using Android.Views;
using Android.Graphics.Drawables;
using Android.Graphics;
using Android.Support.V4.App;
using Android.Support.V4.Widget;
using System.Collections.Specialized;
using Android.App;
using System.Collections.Generic;
using System.Linq;
using Android.Text;
using Android.Text.Style;
using System.Threading.Tasks;
using Android.Util;
using Newtonsoft.Json;
using Xamarin.InAppBilling;
using System.IO;
using Android.Runtime;
using Android;

namespace InPublishing
{
    [Android.App.Activity (Label = "HomeScreen", Theme = "@style/Blue", LaunchMode = LaunchMode.SingleTop)]				
	public class HomeScreen : FragmentActivity
	{
		private DrawerLayout _Drawer;
		private MyActionBarDrawerToggle _DrawerToggle;
		private ListView _DrawerList;
		private string _DrawerTitle;
		private string _Title;

        private LinearLayout _DrawerContent;

		private  List<string> MenuItems = new List<string>();
		private static Dictionary<string, string> MenuLabels;

		private string _CurrentItem;
        public SaneInAppBillingHandler BillingHandler = null;

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

            if (DataManager.Get<ISettingsManager>().Settings.InAppPurchase)
            {
                BillingHandler = new SaneInAppBillingHandler(this, DataManager.Get<ISettingsManager>().Settings.PublicKey);

                try
                {
                    Task.Run(
                        async () => { 
                                await BillingHandler.Connect();

                                if (DownloadManager.IsLogged())
                                {
                                    //abbonamenti
                                    var purch = await BillingHandler.GetPurchases(ItemType.Subscription);
                                    var subList = new List<Dictionary<string, string>>();

                                    foreach (var p in purch)
                                    {
                                        //BillingHandler.ConsumePurchase(p);
                                        Dictionary<string, string> data = new Dictionary<string, string>();

                                        string orderID = p.OrderId;
                                        if (orderID == null || orderID == "")
                                        {
                                            orderID = p.PurchaseToken;
                                        }

                                        data.Add("packageName", this.PackageName);
                                        data.Add("orderId", orderID);
                                        data.Add("productId", p.ProductId);
                                        data.Add("developerPayload", p.DeveloperPayload);
                                        data.Add("purchaseTime", p.PurchaseTime.ToString());
                                        data.Add("purchaseToken", p.PurchaseToken);
                                        data.Add("purchaseState", p.PurchaseState.ToString());

                                        subList.Add(data);
                                    }

                                    var encodeData = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(subList)));

                                    Notification.CheckSubscriptions(encodeData);
                                }
                            }

                        );
                    //await _billingHandler.Connect();
                }
                catch (InAppBillingException ex)
                {
                    // Thrown if the commection fails for whatever reason (device doesn't support In-App billing, etc.)
                    // All methods (except for Disconnect()) may throw this exception, 
                    // handling it is omitted for brevity in the rest of the samples
                    Log.Error("HomeScreen", ex.Message);
                }
            }

            //permessi cartella condivisa
            if(!this.CanAccessExternal())
            {
                var permissions = new string[] { Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage };
                this.RequestPermissions(permissions, 1);
            }

			//this.SetTheme(Resource.Style.MB);

			SetContentView(Resource.Layout.HomeScreen);


            MenuLabels = new Dictionary<string, string>()
            {
                {"edicola", GetString(Resource.String.menu_Edicola)},
                {"download", GetString(Resource.String.menu_Download)},
                {"impostazioni", GetString(Resource.String.menu_Settings)},
                {"crediti", GetString(Resource.String.menu_Credits)},
                {"ciccio", "ddddddd"}
			};

			_Title = _DrawerTitle = Title = this.AppName();

			_Drawer = this.FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
			_DrawerList = this.FindViewById<ListView>(Resource.Id.left_drawer);
            _DrawerContent = this.FindViewById<LinearLayout>(Resource.Id.drawer_content);

			_Drawer.SetBackgroundColor (Color.Transparent.FromHex("eeeeee"));

			//voci menu
			List<string> Sections = new List<string>();

			if(DataManager.Get<ISettingsManager>().Settings.EdicolaEnabled)
			{
				MenuItems.Add("edicola");
				Sections.Add(MenuLabels["edicola"]);
			}

			if(DataManager.Get<ISettingsManager>().Settings.DownloadEnabled)
			{
				MenuItems.Add("download");
				Sections.Add(MenuLabels["download"]);
			}

			if(DataManager.Get<ISettingsManager>().Settings.SettingsEnabled)
			{
				MenuItems.Add("impostazioni");
				Sections.Add(MenuLabels["impostazioni"]);
			}

			MenuItems.Add("crediti");
			Sections.Add(MenuLabels["crediti"]);

			//_DrawerList.Adapter = new ArrayAdapter<string>(this, Resource.Layout.DrawerListItem, Sections);
            _DrawerList.Adapter = new DrawerAdapter<string>(this, Resource.Layout.DrawerListItem, Sections);

			_DrawerList.ItemClick += (sender, args) => ListItemClicked(args.Position);

			_Drawer.SetDrawerShadow(Resource.Drawable.drawer_shadow_light, (int)Android.Views.GravityFlags.Start);

            _DrawerContent.SetBackgroundColor(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.MenuFondoColor));
            _DrawerList.SetBackgroundColor(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.MenuFondoColor));

            _DrawerList.Divider = new ColorDrawable(Color.Transparent);
            _DrawerList.DividerHeight = 8;

			_DrawerToggle = new MyActionBarDrawerToggle(this, _Drawer,
				Resource.Drawable.ic_drawer,
				Resource.String.DrawerOpen,
				Resource.String.DrawerClose);


			//Display the current fragments title and update the options menu
			_DrawerToggle.DrawerClosed += (o, args) => 
			{
				this.ActionBar.Title = _Title;
				ActionBar.SetDisplayShowCustomEnabled(true);
				this.InvalidateOptionsMenu();
			};

			//Display the drawer title and update the options menu
			_DrawerToggle.DrawerOpened += (o, args) => 
			{
				this.ActionBar.Title = _DrawerTitle;
				ActionBar.SetDisplayShowCustomEnabled(false);
				this.InvalidateOptionsMenu();
			};

			//Set the drawer lister to be the toggle.
			_Drawer.SetDrawerListener(this._DrawerToggle);


			//if first time you will want to go ahead and click first item.
			if (bundle == null)
			{
				ListItemClicked(0);
			}

			/*string color = "#" + DataManager.Get<ISettingsManager>().Settings.NavigationBarColor;
			ActionBar.SetBackgroundDrawable(new ColorDrawable(Color.ParseColor(color)));

			int amId = this.Resources.GetIdentifier("action_context_bar", "id", "android");
			View view= FindViewById(amId);
			view.SetBackgroundColor(Color.ParseColor(color));
			*/

			ActionBar.SetCustomView(Resource.Layout.CustomActionBar);
			ActionBar.SetDisplayShowCustomEnabled(true);

			ActionBar.SetDisplayHomeAsUpEnabled(true);
			ActionBar.SetHomeButtonEnabled(true);
			//ActionBar.SetDisplayShowHomeEnabled (false);
			ActionBar.SetIcon(Android.Resource.Color.Transparent);

            ActionBar.SetBackgroundDrawable(new ColorDrawable(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.NavigationBarColor)));

            //ActionBar.CustomView.Background.Colorize(DataManager.Get<ISettingsManager>().Settings.NavigationBarColor);

			var btnBack = ActionBar.CustomView.FindViewById<Button>(Resource.Id.btnBack);
			btnBack.SetTextColor(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.TintColor));

			btnBack.Click += (sender, e) => 
			{
				if(_CurrentItem == "crediti" || _CurrentItem == "impostazioni")
				{
                    _Drawer.OpenDrawer(_DrawerContent);
				}
			};

			//colore titolo
			var titleId = Resources.GetIdentifier("action_bar_title", "id", "android");
			var abTitle = FindViewById<TextView>(titleId);
			abTitle.SetTextColor(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.TintColor));

			//se è un'applicazione edicola e non ci sono documenti lo mando direttamente ai downloads
			if(DataManager.Get<ISettingsManager>().Settings.EdicolaEnabled && DataManager.Get<ISettingsManager>().Settings.DownloadEnabled)
			{
				if(FileSystemManager.ElemetsInBaseDir == 0)
				{
                    //if(/*this.CanAccessExternal() &&*/ FileSystemManager.DocumentsToImport == 0)
					GoTo(1);
				}
			}

			//coloro l'icona di navigazione
			ColorizeDrawer ();
		}

		private void ColorizeDrawer()
		{
			ViewGroup parentView = (ViewGroup) FindViewById(Android.Resource.Id.Home).Parent;

			if (parentView != null && parentView.ChildCount > 0)
			{
				var img = (ImageView)parentView.GetChildAt (0);

				if (img != null)
				{
					img.Colorize(DataManager.Get<ISettingsManager>().Settings.TintColor);
				}
			}
		}

        private void ListItemClicked(int position, string[] param = null)
		{
			if(position > MenuItems.Count - 1)
				return;

			string key = MenuItems[position];

			switch(key.ToLower())
			{
				case "edicola":
					var eFragment = new EdicolaFragment();

                    if (param != null && param.Length > 0)
                    {
                        Bundle bundle = new Bundle();
                        bundle.PutString("path", param[0]);

                        eFragment.Arguments = bundle;
                    }

					FragmentManager.BeginTransaction()
						.Replace(Resource.Id.content_frame, eFragment, MenuItems[position].ToLower())
						.Commit();
					break;
				case "download":
					if(!DataManager.Get<IPreferencesManager>().Preferences.DownloadEnabled)
					{
						var alert = new AlertDialog.Builder(this);
						alert.SetTitle(GetString(Resource.String.gen_error));
						alert.SetMessage(GetString(Resource.String.down_disable));
						alert.SetPositiveButton("Ok", (EventHandler<DialogClickEventArgs>)null);
						alert.Show().SetDivider();

						return;
					}

                    var dFragment = new DownloadFragment();

                    if (param != null && param.Length > 0)
                    {
                        Bundle bundle = new Bundle();
                        bundle.PutString("path", param[0]);

                        dFragment.Arguments = bundle;
                    }

					FragmentManager.BeginTransaction()
						.Replace(Resource.Id.content_frame, dFragment, MenuItems[position].ToLower())
						.Commit();
					break;
				case "impostazioni":
					var sFragment = new SettingsFragment();

					FragmentManager.BeginTransaction()
						.Replace(Resource.Id.content_frame, sFragment, MenuItems[position].ToLower())
						.Commit();
					break;
				case "crediti":
					var aFragment = new AboutFragment();
					FragmentManager.BeginTransaction()
						.Replace(Resource.Id.content_frame, aFragment, MenuItems[position].ToLower())
						.Commit();
					break;				
				default:
					break;
			}

			_CurrentItem = key.ToLower ();

			_DrawerList.SetItemChecked(position, true);
			ActionBar.Title = _Title = MenuLabels[key];
			//ActionBar.SetTitle(Html.FromHtml ("<font color='#ff0000'>ActionBartitle </font>"));
            _Drawer.CloseDrawer(_DrawerContent);

			/*String title = ActionBar.Title;
			SpannableString spannablerTitle = new SpannableString(title);
			spannablerTitle.SetSpan(new ForegroundColorSpan(Color.Red), 0, spannablerTitle.Length(), SpanTypes.ExclusiveExclusive);
			ActionBar.Title = spannablerTitle.ToString();*/
		}

		protected override void OnPostCreate(Bundle savedInstanceState)
		{
			base.OnPostCreate(savedInstanceState);
			_DrawerToggle.SyncState();
		}

		public override void OnConfigurationChanged(Configuration newConfig)
		{
			base.OnConfigurationChanged(newConfig);
			_DrawerToggle.OnConfigurationChanged(newConfig);
		}

		// Pass the event to ActionBarDrawerToggle, if it returns
		// true, then it has handled the app icon touch event
		public override bool OnOptionsItemSelected(IMenuItem item)
		{
			/*if (_DrawerToggle.OnOptionsItemSelected(item))
				return true;*/

			if (item.ItemId == Android.Resource.Id.Home) {

                if (_Drawer.IsDrawerOpen(_DrawerContent)) {
					//mDrawerLayout.closeDrawer(mDrawerList);
                    _Drawer.CloseDrawer(_DrawerContent);
				} else {
					//mDrawerLayout.openDrawer(mDrawerList);
                    _Drawer.OpenDrawer(_DrawerContent);
				}
			}

			return base.OnOptionsItemSelected(item);
		}

		public override bool OnPrepareOptionsMenu(IMenu menu)
		{

            var drawerOpen = _Drawer.IsDrawerOpen(_DrawerContent);
			//when open don't show anything
			for (int i = 0; i < menu.Size(); i++)
				menu.GetItem(i).SetVisible(!drawerOpen);


			return base.OnPrepareOptionsMenu(menu);
		}

		/*public override bool OnCreateOptionsMenu (IMenu menu)
		{

			var drawerOpen = _Drawer.IsDrawerOpen(_DrawerList);
			//when open don't show anything
			for (int i = 0; i < menu.Size(); i++)
				menu.GetItem(i).SetVisible(!drawerOpen);


			return base.OnCreateOptionsMenu(menu);
		}*/

		public override bool OnKeyDown(Android.Views.Keycode keyCode, Android.Views.KeyEvent e)
		{
			Android.App.Fragment fragment = null;

			foreach(string sec in MenuItems)
			{
				fragment = FragmentManager.FindFragmentByTag(sec.ToLower());

				if(fragment == null)
				{
					continue;
				}

				if(fragment.GetType() == typeof(EdicolaFragment))
				{
					var f = fragment as EdicolaFragment;

					if(!f.OnBackPressed())
					{
						return false; 
					}
				}
				else if(fragment.GetType() == typeof(DownloadFragment))
				{
					var f = fragment as DownloadFragment;

					if(!f.OnBackPressed())
					{
						return false; 
					}
				}
			}

			/*EdicolaFragment fragment = SupportFragmentManager.FindFragmentByTag("edicola") as EdicolaFragment;

			if(keyCode == Keycode.Back)
			{
				if(!fragment.OnBackPressed())
				{
					return false; 
				}
			}*/

			return base.OnKeyDown(keyCode, e);
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);
			if (resultCode == Result.Ok) 
			{
				string action = data.GetStringExtra("action");

				if(action == "appNav")
				{
					int index = data.GetIntExtra("index", 0);

                    List<string> parList = new List<string>();

                    if (data.HasExtra("path"))
                    {
                        parList.Add(data.GetStringExtra("path"));
                    }

                    GoTo(index, parList.ToArray());
				}
			}

            if(BillingHandler != null)
            {
                BillingHandler.HandleActivityResult(requestCode, resultCode, data);
            }
		}

        public void GoTo(int index, string[] param = null)
		{
			if(index < MenuLabels.Count)
			{
				string key = MenuLabels.ElementAt(index).Key;

				if(MenuItems.Contains(key))
				{
					index = MenuItems.IndexOf(key);
                    ListItemClicked(index,param);

                    /*if (param != null && param.Length > 0)
                    {
                        var fragment = FragmentManager.FindFragmentByTag(MenuItems[index].ToLower());

                        if (fragment is EdicolaFragment && fragment.IsVisible)
                        {
                            var edFrag = fragment as EdicolaFragment;

                            edFrag.GoToFolder(param[0]);
                        }
                    }*/
				}
			}
		}

		public void OpenDrawer()
		{
            _Drawer.OpenDrawer(_DrawerContent);
		}

		protected override void OnResume()
		{
			base.OnResume();

			if (DataManager.Get<IPreferencesManager>().Preferences.PinEnabled)
			{
				CheckPin(this);                
			}
		}

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (BillingHandler != null)
            {
                BillingHandler.Disconnect();
            }
        }

		public static void CheckPin(Activity activity)
		{
			var builder = new AlertDialog.Builder(activity);

			builder.SetTitle("PIN");
			builder.SetView(activity.LayoutInflater.Inflate(Resource.Layout.PinDialog, null));

			builder.SetCancelable(false);

			builder.SetPositiveButton("OK", (EventHandler<DialogClickEventArgs>)null);
			//builder.SetNegativeButton(GetString(Resource.String.gen_cancel), (EventHandler<DialogClickEventArgs>)null);

			var dialog = builder.Create();
			dialog.Show();
			dialog.SetDivider();

			EditText txtPin = dialog.FindViewById<EditText>(Resource.Id.txtPin); //new EditText(Activity);
			EditText txtPin2 = dialog.FindViewById<EditText>(Resource.Id.txtPin2); //new EditText(Activity);
			TextView lblSuccess = dialog.FindViewById<TextView>(Resource.Id.lblSuccess);

			txtPin2.Visibility = ViewStates.Gone;

			//pulsante ok
			var btnOK = dialog.GetButton((int)DialogButtonType.Positive);

			if(btnOK == null)
				return;

			btnOK.Click += (sender, e) =>
			{
				if(Encryptor.MD5Hash(txtPin.Text) == DataManager.Get<IPreferencesManager>().Preferences.PinCode)
				{
					dialog.Dismiss();
				}
				else
				{
					activity.RunOnUiThread(() => 
					{
						lblSuccess.Visibility = ViewStates.Visible;
						lblSuccess.Text = "PIN errato";
					});

				}
			};
		}

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if(grantResults.Length == 2 && grantResults[0] == Permission.Granted && grantResults[1] == Permission.Granted)
            {
                if (!Directory.Exists(DataManager.Get<ISettingsManager>().Settings.SharedPath))
                {
                    Directory.CreateDirectory(DataManager.Get<ISettingsManager>().Settings.SharedPath);
                }
            }
        }
	}
}

