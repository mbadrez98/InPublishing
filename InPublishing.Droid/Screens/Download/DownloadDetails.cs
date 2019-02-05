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
using Android.Util;
using Android.Graphics.Drawables;

namespace InPublishing
{
	[Activity(Label = "DownloadDetails")]			
	public class DownloadDetails : Dialog
	{
		private static string TAG = "DownloadDetails";
		Download _download;
		public Action OpenAction;
		public Action DownloadAction;
        public Action BuyAction;

		public DownloadDetails(Context context, Download doc) : base(context)
		{
			_download = doc;
		}

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			this.Window.RequestFeature(WindowFeatures.NoTitle);

			SetContentView(Resource.Layout.DownloadDetails);

			this.SetTitle(_download.Titolo);

			TextView txtTitolo = FindViewById<TextView>(Resource.Id.txtTitolo);
			TextView txtAutore = FindViewById<TextView>(Resource.Id.txtAutore);
			TextView txtPubblicato = FindViewById<TextView>(Resource.Id.txtPubblicato);
			TextView txtScadenza = FindViewById<TextView>(Resource.Id.txtScadenza);
			TextView txtDimensione = FindViewById<TextView>(Resource.Id.txtDimensione);
			ImageView imgCover = FindViewById<ImageView>(Resource.Id.imgCover);
			Button btnOpen = FindViewById<Button>(Resource.Id.btnOpen);
			Button btnDownload = FindViewById<Button>(Resource.Id.btnDownload);
            Button btnBuy = FindViewById<Button>(Resource.Id.btnBuy);
            TextView txtPrezzo = FindViewById<TextView>(Resource.Id.txtPrezzo);

			txtTitolo.Text = _download.Titolo;
			txtAutore.Text = _download.Autore.Trim();
            txtPrezzo.Text = "";

			if(_download.Tipo == "pdf")
			{
				imgCover.SetImageResource(Resource.Drawable.pdf_icon);
			}
			else
			{
				string key = _download.ImageKey;
				var uri = new Uri(DataManager.Get<ISettingsManager>().Settings.DownloadUrl + 
					"services/edicola_services.php?action=pubCover&zip=" + _download.RelativePath + 
					"&img=copertina.jpg&key=" + key + 
					"&app=" + DataManager.Get<ISettingsManager>().Settings.AppId);

				imgCover.Tag = uri.ToString();

                //Koush.UrlImageViewHelper.SetUrlDrawable (imgCover, uri.AbsoluteUri);

                MBImageLoader.DisplayNetworkImage(uri, imgCover, new PointF(360, 360));

            }

			if(_download.DataPubblicazione != DateTime.MinValue)
			{
				txtPubblicato.Text = string.Format(Context.GetString(Resource.String.pub_publishedOn), _download.DataPubblicazione.ToString("dd-MM-yyyy"));
					   
			}
			else
			{
				txtPubblicato.Visibility = ViewStates.Gone;
			}

			if(_download.DataScadenza != DateTime.MinValue)
			{
				txtScadenza.Text = string.Format(Context.GetString(Resource.String.pub_expireOn), _download.DataScadenza.ToString("dd-MM-yyyy"));
			}
			else
			{
				txtScadenza.Visibility = ViewStates.Gone;
			}

			txtDimensione.Text = Utils.FormatBytes(_download.Dimensione);

            if (DataManager.Get<ISettingsManager>().Settings.InAppPurchase && _download.IapID != "")
            {
                if (_download.IapAcquistato)
                {
                    txtPrezzo.Text = string.Format(Context.GetString(Resource.String.iap_purchaseDate), _download.IapDataAcquisto.ToString("dd-MM-yyyy"));
                }
            }

			//pulsante apri
			btnOpen.SetTextColor(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.ButtonColor));
			btnOpen.Text = Context.GetString(Resource.String.pub_open);
			btnOpen.Click += delegate
			{
				if(OpenAction != null)
				{
					OpenAction();
				}
			};

			//pulsante download
			btnDownload.SetTextColor(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.ButtonColor));
			btnDownload.Text = Context.GetString(Resource.String.pub_download);
			btnDownload.Click += delegate
			{
				if(DownloadAction != null)
				{
					DownloadAction();
				}
			};

            //pulsante buy
            btnBuy.SetTextColor(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.ButtonColor));

            btnBuy.Text = _download.IapPrezzo;
            btnBuy.Click += delegate
            {
                if (BuyAction != null)
                {
                    BuyAction();
                }
            };

            GradientDrawable gd = new GradientDrawable();
            gd.SetColor(Color.Transparent.FromHex("ffffff"));
            gd.SetCornerRadius(5);
            gd.SetStroke(1, Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.ButtonColor));
            btnBuy.Background = gd;

			/*if(FileDownloader.IsWaiting(_download.Uri))
			{
				_download.Stato = DownloadStato.Downloading;
			}*/
			DownloadInfo down = MBDownloadManager.DownloadInfo(_download.Uri.AbsoluteUri);

			if(down != null && (down.Status == DownloadStatus.Running || down.Status == DownloadStatus.Pending))
			{
				_download.Stato = DownloadStato.Downloading;
			}

			//in base allo stato del download configuro i pulsanti
			switch (_download.Stato)
			{
				case DownloadStato.Download:
					btnDownload.Text = Context.GetString(Resource.String.pub_download);
					btnOpen.Visibility = ViewStates.Invisible;
                    btnBuy.Visibility = ViewStates.Gone;
                    btnBuy.Enabled = false;
					break;
				case DownloadStato.NoUpdate:
					btnDownload.Visibility = ViewStates.Invisible;
					btnOpen.Visibility = ViewStates.Visible;
                    btnBuy.Visibility = ViewStates.Gone;
                    btnBuy.Enabled = false;
					break;
				case DownloadStato.Update:
					btnDownload.Text = Context.GetString(Resource.String.down_update);
					btnOpen.Visibility = ViewStates.Visible;
                    btnBuy.Visibility = ViewStates.Gone;
                    btnBuy.Enabled = false;
					break;
				case DownloadStato.Downloading:
					btnDownload.Text = Context.GetString(Resource.String.down_wait);
					btnDownload.Enabled = false;
					btnOpen.Visibility = ViewStates.Invisible;
                    btnBuy.Visibility = ViewStates.Gone;
                    btnBuy.Enabled = false;
					break;
				case DownloadStato.Expired:
					btnDownload.Text = Context.GetString(Resource.String.pub_expired);
					btnDownload.Enabled = false;
					btnOpen.Visibility = ViewStates.Invisible;
                    btnBuy.Visibility = ViewStates.Gone;
                    btnBuy.Enabled = false;
					break;
                case DownloadStato.Buy:
                    btnDownload.Visibility = ViewStates.Gone;
                    btnDownload.Enabled = false;
                    btnOpen.Visibility = ViewStates.Invisible;
                    btnBuy.Visibility = ViewStates.Visible;
                    btnBuy.Enabled = true;
                    break;
				default:
					break;
			}
		}

		/*public override void OnStart()
		{
			base.OnStart();

			Rect frame = new Rect();
			Activity.Window.DecorView.GetWindowVisibleDisplayFrame(frame);

			WindowManagerLayoutParams lp = Dialog.Window.Attributes;

			Dialog.Window.SetGravity(GravityFlags.Top | GravityFlags.Right);

			lp.DimAmount = 0;
			//lp.Width = Utility.dpToPx(Context, 300); // 600;
			//lp.Height = frame.Bottom - frame.Top*4;
			//lp.X = 50;
			lp.Y = frame.Top*2;

			Dialog.Window.Attributes = lp;

			Dialog.Window.AddFlags(WindowManagerFlags.DimBehind);

			//Dialog.Window.SetLayout(240, 300);
			//Dialog.Window.SetBackgroundDrawable(new ColorDrawable(Color.Transparent));
		}*/
	}
}

