using System;
using Android.Widget;
using Android.App;
using Android.Views;
using Android.Util;
using System.IO;
using System.Threading;
using Android.Graphics;
using Android.Graphics.Drawables;

namespace InPublishing
{
	public class DownloadItem : LinearLayout, IDownloadUpdated2
	{
		private const string TAG = "DownloadItem";
		private long _downloadReference;

		public DownloadStato Stato { get; set; }
		public DownloadStato InitStato { get; set; }
		public Uri DownloadUri { get; set; }
		public Action DownloadFinish { get; set; }

        public string Prezzo { get; set; }

		public ImageView ImgCover 
		{ 
			get { return _imgCover; }
		}

		public virtual string Title 
		{ 
			get { return _txtTitolo.Text; }
			set { _txtTitolo.SetText(value, TextView.BufferType.Normal); }
		}

		public virtual string Details 
		{ 
			get { return _txtDettagli.Text; }
			set { _txtDettagli.SetText(value, TextView.BufferType.Normal); }
		}

		public virtual Java.Lang.Object Tag
		{
			get { return _txtTitolo.Tag; }
			set 
			{ 
				_imgCover.Tag = value;
				_txtTitolo.Tag = value;
			}
		}

		public ImageView OpenButton
		{
			get { return _btnOpen; }
		}

        public TextView BuyButton
        {
            get { return _btnBuy; }
        }
			
		private Activity _context;
		private TextView _txtTitolo;
		private TextView _txtDettagli;
		private ImageView _imgCover;
		private ProgressBar _prgDownload;
		private ImageView _btnDownload;
		private ImageView _btnOpen;
		private ImageView _btnStop;
		private RelativeLayout _overlay;
		private ImageView _imgUpdate;
        private TextView _btnBuy;

		public DownloadItem(Activity context, bool list = false) : base(context)
		{
			_context = context;

            try
            {
    			if(!list)
    			{
    				View.Inflate(Context, Resource.Layout.DownloadGridItem, this);
    			}
    			else
    			{
    				View.Inflate(Context, Resource.Layout.DownloadListItem, this);
    			}

    			_txtTitolo = FindViewById<TextView>(Resource.Id.downloadTxtTitolo);
    			_txtDettagli = FindViewById<TextView>(Resource.Id.downloadTxtDettagli);
    			_imgCover = FindViewById<ImageView>(Resource.Id.downloadImgCover);
    			_btnDownload = FindViewById<ImageView>(Resource.Id.btnDownload);
    			_btnOpen = FindViewById<ImageView>(Resource.Id.btnOpen);
    			_btnStop = FindViewById<ImageView>(Resource.Id.btnStop);
    			_prgDownload = FindViewById<ProgressBar>(Resource.Id.downloadProgress);
    			_overlay = FindViewById<RelativeLayout>(Resource.Id.downloadOverlay);
    			_imgUpdate = FindViewById<ImageView>(Resource.Id.downloadImgUpdate);

                _btnBuy = FindViewById<TextView>(Resource.Id.btnBuy);

    			//colore barra avanzamento
    			//_prgDownload.ProgressDrawable.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);
    			_btnDownload.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);
    			_btnOpen.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);
    			_btnStop.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);

                GradientDrawable gd = new GradientDrawable();
                gd.SetColor(Color.Transparent.FromHex("ffffff"));
                gd.SetCornerRadius(5);
                gd.SetStroke(1, Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.ButtonColor));

                _btnBuy.Background = gd;
                _btnBuy.SetTextColor(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.ButtonColor));

                Drawable bgDrawable = _prgDownload.ProgressDrawable;
                bgDrawable.SetColorFilter(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.ButtonColor), PorterDuff.Mode.SrcIn);
                _prgDownload.ProgressDrawable = bgDrawable;
            }
            catch(Java.Lang.RuntimeException ex)
            {
                Log.Error(TAG, ex.Message);
            }
		}

		public void ConfigureForDownload()
		{
			//pulsante download
			EventHandler onDownClick = (object sender, EventArgs e) => 
			{
				this.Stato = DownloadStato.Downloading;

				//_downloadReference = MBDownloadManager2.Download(_download.Uri, System.IO.Path.GetFileName(_download.Uri.LocalPath), _download.Titolo, this);
				//_downloadReference = MBDownloadManager2.RequestDownload(DownloadUri, System.IO.Path.GetFileName(DownloadUri.LocalPath), this.Title, this);

                MBDownloadManager.RequestDownload(DownloadUri, this);
				SetUIState();

				Log.Info(this.Title, "Inizio download");
			};

			_btnDownload.Click -= onDownClick;
			_btnDownload.Click += onDownClick;

			//pulsante ferma
			EventHandler onStopClick = (object sender, EventArgs e) => 
			{
				//if(_downloadReference != 0)
				{
                    MBDownloadManager.StopDownload(DownloadUri);
					//_downloadReference = 0;

                    this.Stato = InitStato;
                    SetUIState();
				}
			};

			_btnStop.Click -= onStopClick; 
			_btnStop.Click += onStopClick; 

			DownloadInfo down = MBDownloadManager.DownloadInfo(DownloadUri.AbsoluteUri);

			if(down != null && down.Id != 0 && down.Uri != "")
			{
				//_downloadReference = down.Id;
				MBDownloadManager.UpdateNotify(DownloadUri, this);

				if(down.Status == DownloadStatus.Running || down.Status == DownloadStatus.Pending)
				{					
					_prgDownload.Progress = 0;
				}
				else if(down.Status == DownloadStatus.Successful)
				{
					string filePath = new Uri(down.LocalUri).AbsolutePath;
					if(File.Exists(filePath))
					{						
						_prgDownload.Progress = 100;
					}
				}

				this.Stato = DownloadStato.Downloading;
			}
            else if(MBDownloadManager.IsWaiting(DownloadUri))
            {
                _prgDownload.Progress = 0;
                this.Stato = DownloadStato.Downloading; 

                MBDownloadManager.UpdateNotify(DownloadUri, this);
            }

            _btnBuy.SetText(this.Prezzo, TextView.BufferType.Normal);

			SetUIState();
		}
			
		private void RegisterDownload(string uri, string localUri)
		{	
			//string fileName = System.IO.Path.GetFileName(localUri);

			string search = "/pub/";

			uri = uri.Substring(uri.IndexOf(search) + search.Length).Trim('/');

			try
			{				
				//registrazione download
				Uri nHost = new Uri(DataManager.Get<ISettingsManager>().Settings.DownloadUrl);
				if (Reachability.IsHostReachable("http://" + nHost.Host))
				{
					var data = _context.DeviceInfo();
					data.Add("file", uri);

					Notification notif = new Notification();

					if(!notif.RegisterDownload(data))
					{
						Log.Error("Registrazione download", "Eerrore registrazione download");
					}

					Log.Info(this.Title, "Fine download");
				}
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
			}					
		}

		private void SetUIState()
		{
			//state 0 scarica, 1 attendi, 2 aggiorna, 3 installato, 4 installazione, 5 scaduto

			switch (this.Stato)
			{
				case DownloadStato.Download:

					_btnDownload.Enabled = true;
					_btnDownload.Visibility = ViewStates.Visible;

					_btnOpen.Enabled = false;
					_btnOpen.Visibility = ViewStates.Gone;

					_btnStop.Visibility = ViewStates.Gone;
					_btnStop.Enabled = false;

					_prgDownload.Visibility = ViewStates.Invisible;
					_overlay.Visibility = ViewStates.Invisible;
					_prgDownload.Progress = 0;

					_imgUpdate.Visibility = ViewStates.Gone;

                    _btnBuy.Enabled = false;
                    _btnBuy.Visibility = ViewStates.Gone;
					break;
				case DownloadStato.NoUpdate:

					_btnDownload.Enabled = false;
					_btnDownload.Visibility = ViewStates.Gone;

					_btnOpen.Enabled = true;
					_btnOpen.Visibility = ViewStates.Visible;

					_btnStop.Visibility = ViewStates.Gone;
					_btnStop.Enabled = false;

					_prgDownload.Visibility = ViewStates.Invisible;
					_overlay.Visibility = ViewStates.Invisible;
					_prgDownload.Progress = 0;

					_imgUpdate.Visibility = ViewStates.Gone;

                    _btnBuy.Enabled = false;
                    _btnBuy.Visibility = ViewStates.Gone;
					break;
				case DownloadStato.Update:

					_btnDownload.Enabled = true;
					_btnDownload.Visibility = ViewStates.Visible;

					_btnOpen.Enabled = false;
					_btnOpen.Visibility = ViewStates.Gone;

					_btnStop.Visibility = ViewStates.Gone;
					_btnStop.Enabled = false;

					_prgDownload.Visibility = ViewStates.Invisible;
					_overlay.Visibility = ViewStates.Invisible;
					_prgDownload.Progress = 0;

					_imgUpdate.Visibility = ViewStates.Visible;

                    _btnBuy.Enabled = false;
                    _btnBuy.Visibility = ViewStates.Gone;
					break;
				case DownloadStato.Downloading:

					_btnDownload.Enabled = false;
					_btnDownload.Visibility = ViewStates.Invisible;

					_btnOpen.Enabled = false;
					_btnOpen.Visibility = ViewStates.Invisible;

					_btnStop.Visibility = ViewStates.Visible;
					_btnStop.Enabled = true;

					_prgDownload.Visibility = ViewStates.Visible;
					_overlay.Visibility = ViewStates.Visible;

					_imgUpdate.Visibility = ViewStates.Gone;

                    _btnBuy.Enabled = false;
                    _btnBuy.Visibility = ViewStates.Gone;
					break;
				case DownloadStato.Expired:

					_btnDownload.Enabled = false;
					_btnDownload.Visibility = ViewStates.Gone;

					_btnOpen.Enabled = false;
					_btnOpen.Visibility = ViewStates.Gone;

					_btnStop.Visibility = ViewStates.Gone;
					_btnStop.Enabled = false;

					_prgDownload.Visibility = ViewStates.Invisible;
					_overlay.Visibility = ViewStates.Invisible;
					_prgDownload.Progress = 0;

					_imgUpdate.Visibility = ViewStates.Gone;

                    _btnBuy.Enabled = false;
                    _btnBuy.Visibility = ViewStates.Gone;
					break;
                case DownloadStato.Buy:
                    _btnDownload.Enabled = false;
                    _btnDownload.Visibility = ViewStates.Gone;

                    _btnOpen.Enabled = false;
                    _btnOpen.Visibility = ViewStates.Gone;

                    _btnStop.Visibility = ViewStates.Gone;
                    _btnStop.Enabled = false;

                    _prgDownload.Visibility = ViewStates.Invisible;
                    _overlay.Visibility = ViewStates.Invisible;
                    _prgDownload.Progress = 0;

                    _imgUpdate.Visibility = ViewStates.Gone;

                    _btnBuy.Enabled = true;
                    _btnBuy.Visibility = ViewStates.Visible;
                    break;
				default:
					break;
			}
		}

		void IDownloadUpdated2.ProgressChanged(int progress)
		{
			if(_context != null && _txtTitolo == null)
			{
				return;
			}

			_context.RunOnUiThread(() =>
			{
				//_prgDownload.Visibility = ViewStates.Visible;
				//_overlay.Visibility = ViewStates.Visible;

				//_btnDownload.Enabled = false;
				//SetDownloadButtonTitle(cell.BtnDownload, 1);
				//_prgDownload.Progress = progress;

				this.Stato = DownloadStato.Downloading;
				_prgDownload.Progress = progress;
				SetUIState();

				/*if(_downloadReference == 0)
				{
					this.Stato = InitStato;
					SetUIState();
				}*/

				//Log.Info(this.Title, progress + "%");
			});
		}

		void IDownloadUpdated2.DownloadCompleted(string uri, string localUri) 
		{
			if(_context != null && _txtTitolo == null)
			{
				return;
			}

			//RegisterDownload(uri, localUri);

			_context.RunOnUiThread(() =>
			{
				this.Stato = DownloadStato.NoUpdate;

				SetUIState();

				if(DownloadFinish != null)
				{
					DownloadFinish();
				}
			});
		}

        /*void IDownloadUpdated2.DownloadCancelled() 
        {          
            _context.RunOnUiThread(() =>
            {
                this.Stato = InitStato;
                SetUIState();
            });
        }*/
	}
}

