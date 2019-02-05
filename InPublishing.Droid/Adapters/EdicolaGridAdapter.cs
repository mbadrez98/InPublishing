using System;
using System.Collections.Generic;
using Android.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using System.IO;
using Android.Graphics.Drawables;
using Com.Artifex.Mupdfdemo;
using System.Threading;
using System.Linq;

namespace InPublishing
{
	public class EdicolaGridAdapter : BaseAdapter<Object> 
	{
        protected Activity context = null;
		protected IList<Object> _Items = new List<Object>();
		public bool ActionMode = false;
		public SparseBooleanArray CheckedPosition = new SparseBooleanArray();

		private List<Download> _downloads = new List<Download>();

		private Action<PopupMenu.MenuItemClickEventArgs, int> _ItemOptionClick;
		public Action<PopupMenu.MenuItemClickEventArgs, int> ItemOptionClick
		{
			get
			{
				return _ItemOptionClick;
			}
			set
			{
				_ItemOptionClick = value;
			}
		}              

		public EdicolaGridAdapter(Activity context, IList<Object> items) : base()
        {
            this.context = context;
			_Items = items;
        }

		public override Object this[int position]
        {
			get { return _Items[position]; }
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override int Count
        {
			get { return _Items.Count; }
        }

		public void CheckUpdates(List<Download> downloads)
		{
			_downloads = downloads;

			this.NotifyDataSetChanged();
		}

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
			var item = _Items[position];
           
			/*var view = (convertView ??
                    context.LayoutInflater.Inflate(
					Resource.Layout.EdicolaGridItem,
                    parent,
                    false)) as LinearLayout;*/

			View view;

            if(item is Pubblicazione)
			{
				view = this.GetViewThumb(position, convertView, parent);
			}
			else if(item is DirectoryInfo)
			{
				if(((DirectoryInfo)item).Name == "/")
				{
					var ly = context.LayoutInflater.Inflate(Resource.Layout.EdicolaFakeItem, parent, false);

					ly.SetOnClickListener(null);
					ly.SetOnLongClickListener(null);
					ly.Clickable = false;

					return ly;
				}
				else
				{
					view = this.GetViewDir(position, convertView, parent);
				}
			}
			else
			{
				view = null;
			}

            if(view is CheckableLayout)
                return view;

            if(view.Parent != null && view.Parent is CheckableLayout)
                return (CheckableLayout)view.Parent;

			CheckableLayout layout = new CheckableLayout(this.context);
			layout.LayoutParameters = new GridView.LayoutParams(GridView.LayoutParams.MatchParent, GridView.LayoutParams.WrapContent);
			//layout.SetPadding(5, 5, 5, 5);
			layout.AddView(view);

			return layout;
        }

		private View GetViewThumb(int position, View convertView, ViewGroup parent)
		{
            var doc = _Items[position] as Pubblicazione;

			GridView grid = null;

			if(parent.GetType() == typeof(GridView))
			{
				grid = parent as GridView;
			}

			bool list = (grid != null && grid.NumColumns == 1);

			EdicolaItem view = null;

            if(convertView != null && convertView is CheckableLayout)
			{
                var v = convertView as CheckableLayout;

                if(v.MainView != null && v.MainView is EdicolaItem)
                {
                    view = v.MainView as EdicolaItem;

                    if(view.IdDoc == doc.ID)
                    {
                        SetUpdate(view, doc.ID);

                        if(ActionMode)
                        {
                            view.ActionButton.Visibility = ViewStates.Invisible;
                        }
                        else
                        {
                            view.ActionButton.Visibility = ViewStates.Visible;
                        }

                        return v;
                    }
                    else
                        view = null;
                }
			}
			
            if(view == null)
                view = new EdicolaItem(this.context, list);			

			view.Tag = position;
            view.IdDoc = doc.ID;

			//copertina
			if(doc.IsPDF)
			{
				view.ImgCover.SetImageResource(Resource.Drawable.pdf_icon);
				view.ImgCover.SetScaleType(ImageView.ScaleType.FitCenter);
			}
			else
			{
				string imgPath = System.IO.Path.Combine(doc.Path, "copertina.jpg");
				if(System.IO.File.Exists(imgPath))
				{
                    /*using(Bitmap bmp = ImageUtility.DecodeSampledBitmapFromFile(imgPath, 280, 280))
					{
						view.ImgCover.SetImageBitmap(bmp);
					}*/

                    //imgPath += "_" + doc.DataPubblicazione.ToString("yyMMddhhmmss");

                    /*Uri uri = new Uri(imgPath);

                    var options = new DisplayImageOptions.Builder()
                    .CacheInMemory(false)
                    .CacheOnDisk(true)
                    .ConsiderExifParams(true)
                    .BitmapConfig(Bitmap.Config.Rgb565)
                    .Build();

                    var targetSize = new ImageSize(280, 280);

                    ImageLoader.Instance.LoadImage(
                        uri.ToString(),
                        targetSize,
                        options,
                        new ImageLoadingListener(
                            loadingComplete: (imageUri, v, loadedImage) => {
                                view.ImgCover.SetImageBitmap(loadedImage);
                            }));*/

                    //MBImageLoader.DisplayDiskImage(imgPath, view.ImgCover, new PointF(280, 280));

                    Koush.UrlImageViewHelper.SetUrlDrawable(view.ImgCover, new Uri(imgPath).AbsoluteUri);
				}
			}

			//pulsante info
			EventHandler onClick = (sender, e) => 
			{
				//_InfoClick(position);
				PopupMenu popup = new PopupMenu(this.context, view.ActionButton);
				popup.MenuInflater.Inflate(Resource.Menu.EdicolaThumbMenu, popup.Menu);

                var menu = popup.Menu;

                var el = menu.FindItem(Resource.Id.EdicolaItemOption_Delete);
                el.SetTitle(context.GetString(Resource.String.gen_delete));

                el = menu.FindItem(Resource.Id.EdicolaItemOption_Details);
                el.SetTitle(context.GetString(Resource.String.pub_details));

				popup.MenuItemClick += (object send, PopupMenu.MenuItemClickEventArgs ev) => 
				{
					_ItemOptionClick(ev, position);
				};

				popup.Show();
			};

			view.ActionButton.Click -= onClick;
			view.ActionButton.Click += onClick;			

			//titolo            
			view.Title = doc.Titolo;

			//dettagli
			if(view.Details != null)
			{
				string autore = doc.Autore;

				if(autore == "")
				{
					autore = context.GetString(Resource.String.pub_sconosciuto);
				}

				view.Details = autore;
			}

            //update
            SetUpdate(view, doc.ID);

            if(ActionMode)
            {
                view.ActionButton.Visibility = ViewStates.Invisible;
            }
            else
            {
                view.ActionButton.Visibility = ViewStates.Visible;
            }

			return view;
		}

        private void SetUpdate(EdicolaItem view, string docID)
        {
            if(_downloads != null)
            {
                var down = _downloads.Where(d => d.ID == docID).FirstOrDefault();

                if(down != null)
                {
                    view.DownloadUri = down.Uri;
                    view.DownloadStato = down.Stato;

                    EventHandler onUpdate = delegate {
                        view.StartDownload();
                    };

                    view.UpButton.Click -= onUpdate;
                    view.UpButton.Click += onUpdate;
                }
                else
                {
                    view.DownloadStato = DownloadStato.NoUpdate;
                }

                view.Initialize();
            }
        }

		private View GetViewDir(int position, View convertView, ViewGroup parent)
		{
			var dir = _Items[position] as DirectoryInfo;

			GridView grid = null;

			if(parent.GetType() == typeof(GridView))
			{
				grid = parent as GridView;
			}

			View view = null;

			if(grid != null && grid.NumColumns == 1)
			{
				view = context.LayoutInflater.Inflate(Resource.Layout.EdicolaListItem, parent, false);	
			}
			else
			{
				view = context.LayoutInflater.Inflate(Resource.Layout.EdicolaDirItem, parent, false);			
			}

			var imgCover = view.FindViewById<ImageView>(Resource.Id.edicolaImgCover);
			var txtTitolo = view.FindViewById<TextView>(Resource.Id.edicolaTxtTitolo);	
			var txtDettagli = view.FindViewById<TextView>(Resource.Id.edicolaTxtDettagli);
			var btnInfo = view.FindViewById<ImageView>(Resource.Id.btnInfo);

			//copertina
			imgCover.SetImageResource(Resource.Drawable.ic_folder);
			imgCover.SetScaleType(ImageView.ScaleType.CenterInside);
			imgCover.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);

			//campo titolo		            
			txtTitolo.SetText(dir.Name, TextView.BufferType.Normal);

			//dettagli
			if(txtDettagli != null)
			{
				//txtDettagli.SetText(context.GetString(Resource.String.pub_cartella), TextView.BufferType.Normal);
				txtDettagli.Visibility = ViewStates.Gone;
			}

			//pulsante info
			btnInfo.Tag = position;
			btnInfo.Drawable.Colorize(DataManager.Get<ISettingsManager>().Settings.ButtonColor);
			btnInfo.Click += (sender, e) => 
			{
				//_InfoClick(position);
				PopupMenu popup = new PopupMenu(this.context, btnInfo);
				popup.MenuInflater.Inflate(Resource.Menu.EdicolaDirMenu, popup.Menu);

                var el = popup.Menu.FindItem(Resource.Id.EdicolaItemOption_Delete);
                el.SetTitle(context.GetString(Resource.String.gen_delete));

				popup.MenuItemClick += (object send, PopupMenu.MenuItemClickEventArgs ev) => 
				{
					_ItemOptionClick(ev, position);
				};

				popup.Show();
			};

			if(ActionMode)
			{
				btnInfo.Visibility = ViewStates.Invisible;
			}
			else
			{
				btnInfo.Visibility = ViewStates.Visible;
			}

			return view;
		}

		/*public int GetPositionForSection(int section)
        {
			if (section >= sections.Length) {
				return Count - 1;
			}

			//return alphaIndexer.get(sections[section]);
            return alphaIndexer[sections[section]];
        }*/

		/*public int GetSectionForPosition(int position)
        {
            return 1;
		}*/

		/*public Java.Lang.Object[] GetSections()
        {
            return sectionsO;
        }*/
    }
}