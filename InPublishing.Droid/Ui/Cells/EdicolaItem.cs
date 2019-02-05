using System;
using Android.Widget;
using Android.Views;
using Android.App;

namespace InPublishing
{
	public class EdicolaItem : LinearLayout, IDownloadUpdated2
	{
		public virtual ImageView ImgCover 
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
				ActionButton.Tag = value;
				ImgCover.Tag = value;
				_txtTitolo.Tag = value;
				_txtDettagli.Tag = value;
			}
		}

		public virtual ImageView ActionButton 
		{ 
			get { return _btnAction; }
		}

		public virtual ImageView UpButton
		{ 
			get { return _btnUpdate; }
		}
		//public virtual UIActivityIndicatorView Loader { get; set; }

		public DownloadStato DownloadStato;
		public Uri DownloadUri;
        public string IdDoc;

		private Activity _context;
		private TextView _txtTitolo;
		private TextView _txtDettagli;
		private ImageView _btnAction;
		private ImageView _imgCover;
		private ImageView _imgUpdate;
		private ImageView _btnUpdate;
		private ProgressBar _progress;

		public EdicolaItem(Activity context, bool list = false) : base(context)
		{
			_context = context;

			if(!list)
			{
				View.Inflate(Context, Resource.Layout.EdicolaGridItem, this);
			}
			else
			{
				View.Inflate(Context, Resource.Layout.EdicolaListItem, this);
			}

			_txtTitolo = FindViewById<TextView>(Resource.Id.edicolaTxtTitolo);
			_txtDettagli = FindViewById<TextView>(Resource.Id.edicolaTxtDettagli);
			_imgCover = FindViewById<ImageView>(Resource.Id.edicolaImgCover);
			_btnAction = FindViewById<ImageView>(Resource.Id.btnInfo);
			_imgUpdate = FindViewById<ImageView>(Resource.Id.edicolaImgUpdate);
			_btnUpdate = FindViewById<ImageView>(Resource.Id.edBtnDownload);
			_progress = FindViewById<ProgressBar>(Resource.Id.edProgress);

			_btnAction.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);
			_btnUpdate.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);
			//_progress.IndeterminateDrawable.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);
		}

		public void Initialize()
		{

			if(DownloadUri != null && DownloadUri.AbsolutePath != "")
			{
				DownloadInfo down = MBDownloadManager.DownloadInfo(DownloadUri.AbsoluteUri);

				if(down != null && down.Id != 0 && down.Uri != "")
				{
					MBDownloadManager.UpdateNotify(DownloadUri, this);

					this.DownloadStato = DownloadStato.Downloading;
				}
			}
			else
			{
				this.DownloadStato = DownloadStato.NoUpdate;
			}

			SetUIState();
		}

		private void SetUIState()
		{
			//state 0 scarica, 1 attendi, 2 aggiorna, 3 installato, 4 installazione, 5 scaduto

			switch (this.DownloadStato)
			{
				case DownloadStato.Update:
					_progress.Visibility = ViewStates.Gone;

					_btnAction.Visibility = ViewStates.Visible;
					_btnUpdate.Visibility = ViewStates.Visible;
					_imgUpdate.Visibility = ViewStates.Visible;
					break;
				case DownloadStato.Downloading:
					_progress.Visibility = ViewStates.Visible;

					_btnAction.Visibility = ViewStates.Gone;
					_btnUpdate.Visibility = ViewStates.Gone;
					_imgUpdate.Visibility = ViewStates.Visible;
					break;
				default:
					_progress.Visibility = ViewStates.Gone;

					_btnAction.Visibility = ViewStates.Visible;
					_btnUpdate.Visibility = ViewStates.Gone;
					_imgUpdate.Visibility = ViewStates.Gone;
					break;
			}
		}

		public void StartDownload()
		{	
			MBDownloadManager.RequestDownload(DownloadUri, this);

			this.DownloadStato = DownloadStato.Downloading; //in download
			SetUIState();
		}

		void IDownloadUpdated2.ProgressChanged(int progress)
		{

		}

		void IDownloadUpdated2.DownloadCompleted(string uri, string localUri)  
		{			
			this.DownloadStato = DownloadStato.NoUpdate;

			_context.RunOnUiThread(() =>
			{
				SetUIState();
			});
		}
	}
}

