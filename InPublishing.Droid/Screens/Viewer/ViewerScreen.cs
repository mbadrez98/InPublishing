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
using Android.Support.V4.App;
using Java.Lang;
using Android.Support.V4.View;
using Android.Graphics;
using Android.Content.PM;
using Newtonsoft.Json;
using Android.Util;
using Android.Graphics.Drawables;
using Com.Artifex.Mupdfdemo;
using Android.Views.Animations;
using Android.Animation;

namespace InPublishing
{
	[Activity (Label = "@string/ApplicationName", Theme = "@style/Blue.ActionOverlay", 
		ConfigurationChanges=Android.Content.PM.ConfigChanges.Orientation |  Android.Content.PM.ConfigChanges.ScreenSize | Android.Content.PM.ConfigChanges.KeyboardHidden,
		LaunchMode = LaunchMode.SingleTop)]

	public class ViewerScreen : FragmentActivity
	{
        private MuReaderView _ReaderView;
        private MuPDFCore _PdfCore;
        Pubblicazione _Pubblicazione;
        private Documento _Documento;
        private BottomBar _BottomBar;
        private int _PageCount;
        private List<KeyValuePair<string, int>> _PageHistory = new List<KeyValuePair<string, int>>();

		private int _pageVisited = 0;

        DateTime _lastDocChange = DateTime.Now;

		public MuReaderView ReaderView
		{
			get
			{
				return _ReaderView;
			}
		}

		public Documento Documento
		{
			get
			{
				return _Documento;
			}
		}

        public Pubblicazione Pubblicazione 
        {
            get { return _Pubblicazione; }
        }

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			//Intent act = NavUtils.GetParentActivityIntent(this);

			//this.Window.AddFlags(WindowManagerFlags.Fullscreen);
			SetContentView(Resource.Layout.DocumentScreen);

			this.Window.AddFlags(WindowManagerFlags.HardwareAccelerated);
            this.Window.AddFlags(WindowManagerFlags.Fullscreen);
            //this.Window.SetSoftInputMode(SoftInput.AdjustPan);
                
            var pubPath = Intent.GetStringExtra("pubPath");

            _Pubblicazione = new Pubblicazione(pubPath);
            _Pubblicazione.LoadDocuments();

            AnalyticsService.SendEvent(_Pubblicazione.Titolo, AnalyticsEventAction.PubOpen);

            OpenDocument("", true);
		}

        private void OpenDocument(string docID = "", bool reset = false)
        {
            if(_Pubblicazione.Documenti.Count == 0)
            {
                ShowAlertMessage(GetString(Resource.String.gen_error), GetString(Resource.String.pub_format), true);
            }

            if(_PdfCore != null)
            {
                _PdfCore.OnDestroy();
            }
            _PdfCore = null;

            GC.Collect();

            if(_Pubblicazione.IsPDF)
            {
                _Documento = _Pubblicazione.Documenti[0];
                _PdfCore = new MuPDFCore(this, _Documento.Path);
                _PageCount = _PdfCore.CountPages();
            }
            else
            {
                Documento doc;

                if(docID != "")
                {
                    doc = _Pubblicazione.Documenti.Where(d => d.ID == docID).FirstOrDefault<Documento>();

                    if(doc == null || doc.ID == null || doc.ID == "")
                    {
                        _Documento = _Pubblicazione.Documenti[0];
                    }
                }
                else
                {
                    doc = _Pubblicazione.Documenti[0];
                }

                if(_Documento != null && doc.Path == _Documento.Path) //il documento trovato è già quello aperto
                    return;

                _Documento = doc;
                doc = null;


                if(_Documento.Path == null || !System.IO.File.Exists(System.IO.Path.Combine(_Documento.Path, "sfondo.pdf")))
                {
                    ShowAlertMessage(GetString(Resource.String.gen_error), GetString(Resource.String.pub_format), true);
                    return;
                }

                if(_Pubblicazione.OS != DocumentOS.All && _Pubblicazione.OS != DocumentOS.Android)
                {
                    if(_Documento.ID == _Pubblicazione.Documenti[0].ID && _ReaderView == null) //è il primo documento ed è stato appena aperto dall'edicola
                    {
                        ShowAlertMessage(GetString(Resource.String.gen_error), GetString(Resource.String.pub_os));
                    }
                }

                _PdfCore = new MuPDFCore(this, System.IO.Path.Combine(_Documento.Path, "sfondo.pdf"));

                if(_PdfCore.NeedsPassword())
                {
                    string password = _Pubblicazione.Script.Replace(".", "") + "_" + _Pubblicazione.ID + "_patti_e_lola_prendono_4_sarucchi_dalla_mamma_di_patti";

                    var bytes = System.Text.Encoding.UTF8.GetBytes(password);
                    password = Convert.ToBase64String(bytes);

                    _PdfCore.AuthenticatePassword(password);

                    if(_PdfCore.CountPages() <= 0)
                    {
                        ShowAlertMessage(GetString(Resource.String.gen_error), GetString(Resource.String.pub_format), true);

                        //this.Finish();
                        return;
                    }
                }

                _PageCount = _Documento.Articoli.Count;
            }

            OutlineActivityData.Set(null);

            if(reset)
            {
                InitUI();

                AnalyticsService.SendEvent(_Pubblicazione.Titolo, AnalyticsEventAction.DocOpen, _Documento.Titolo);
            }
            else
            {
                _ReaderView.SetAdapter(new MuPageAdapter(this, null, _PdfCore, _Documento, this));

                MuPageAdapter adapter = (MuPageAdapter)_ReaderView.Adapter;

                _ReaderView.Refresh(false);

                _ReaderView.DisplayedViewIndex = _ReaderView.Adapter.Count - 1;
                 ReaderView.DisplayedViewIndex = 0;
            }
        }

		private void InitUI()
		{
			if(_PdfCore == null || _Documento == null)
			{
				return;
			}

            this.Title = ActionBar.Title = _Pubblicazione.Titolo;

			//in base all'orientamento del documento setto il dispositivo

            if(!_Pubblicazione.AutoOrients)
			{
				if(_Pubblicazione.Orientamento == "O")
				{
					RequestedOrientation = ScreenOrientation.SensorLandscape;
				}
				else
				{
					RequestedOrientation = ScreenOrientation.Portrait;
				}
			}

			//inizializzo il reader
			_ReaderView = new MuReaderView(this);
            _ReaderView.SetHorizontalScrolling(!_Pubblicazione.ScrollVerticale);

            _ReaderView.SetAdapter(new MuPageAdapter(this, null, _PdfCore, _Documento, this));           

			_ReaderView.DisplayedViewIndex = _ReaderView.Adapter.Count - 1;
			_ReaderView.DisplayedViewIndex = 0;

			//zoom abilitato/disabilitato
			_ReaderView.ZoomEnabled = _Pubblicazione.AbilitaZoom;

			_ReaderView.ZoomMax = 2.0f;

			//scroll abilitato/disabilitato
			if(!DataManager.Get<ISettingsManager>().Settings.SwipeEnabled)
			{
				_ReaderView.ScrollEnabled = false;
			}
			else
			{
                _ReaderView.ScrollEnabled = _Pubblicazione.Swipe;
			}

            _ReaderView.ScrollAction += OnReaderScroll;

			_ReaderView.OnPageSelected += delegate
			{
				_pageVisited++;

                _BottomBar.SetPage(_Pubblicazione.RelativeToAbsolutePage(_Documento.ID, _ReaderView.DisplayedViewIndex + 1) - 1);

				this.HideUi();

				InvalidateOptionsMenu();

				if(_PageCount > 200 && _pageVisited % 15 == 0)
				{
					GC.Collect();
					_pageVisited = 0;
				}
			};

            _ReaderView.OnFlingAction += OnReaderFling;

			var readerWrap = FindViewById<FrameLayout>(Resource.Id.viewerWrap);
			SurfaceView sv = new SurfaceView(this);
			readerWrap.AddView(sv);
			readerWrap.AddView(_ReaderView);

			//colore sfondo viewer
			Color col = Color.Transparent.FromHex(_Pubblicazione.coloreFondo.ToString("X"));
			_ReaderView.SetBackgroundColor(col);

			//frame fullscreen
			/*Rect frame = new Rect();
			Window.DecorView.GetWindowVisibleDisplayFrame(frame);
			frame.Bottom = frame.Bottom + frame.Top;
			frame.Top = 0;*/

			SetToolBars();

			//pagina
			var page = Intent.GetStringExtra("page");
			if(page != null && page != "")
			{
				GoToPage(page);
			}

            if(DataManager.Get<ISettingsManager>().Settings.ToolbarVisible && _Pubblicazione.GestioneBarre)
			{
				_ReaderView.OnSingleTap += delegate
				{
					this.ToggleUi();
				};
			}

			if (!_Pubblicazione.VisioneBarre || !DataManager.Get<ISettingsManager>().Settings.ToolbarVisible)
			{
				this.HideUi();
			} 

			//GC.Collect(GC.MaxGeneration);
		}

		public override void OnConfigurationChanged(Android.Content.Res.Configuration newConfig)
		{
			base.OnConfigurationChanged(newConfig);

			var w = WindowManager.DefaultDisplay.Width;
			var h = WindowManager.DefaultDisplay.Height;

			var lp = _ReaderView.LayoutParameters;

			lp.Width = w;
			lp.Height = h;
			_ReaderView.LayoutParameters = lp;

			_ReaderView.Refresh(false);
		}

		public void GoToPage(string pageNum, string docID = "")
		{
			if(_ReaderView == null)
				return;
            
            var current = new KeyValuePair<string, int>(_Documento.ID, _ReaderView.DisplayedViewIndex);

            if(_PageHistory.Count == 0)
            {
                _PageHistory.Add(current);
            }
            else
            {
                var last = _PageHistory.Last();

                if(last.Key != current.Key || last.Value != current.Value)
                {
                    _PageHistory.Add(current);
                }
            }

            if(docID != "" && _Documento.ID != docID)
            {
                OpenDocument(docID, false);
            }

            int index = Convert.ToInt32(pageNum);

            _ReaderView.DisplayedViewIndex = index;

		}

		public void NextPage()
		{
			if(_ReaderView == null)
				return;

            if(_ReaderView.DisplayedViewIndex == _Documento.Articoli.Count - 1 && _Pubblicazione.Documenti.Count > 1) //documento successivo prima pagina
            {
                int curDocIndex = _Pubblicazione.Documenti.IndexOf(_Documento);

                if(curDocIndex < _Pubblicazione.Documenti.Count - 1)
                {
                    GoToPage("0", _Pubblicazione.Documenti[curDocIndex + 1].ID);
                }
                    
            }
            else if(_ReaderView.DisplayedViewIndex < _Documento.Articoli.Count - 1) //stesso documento
            {
                var current = new KeyValuePair<string, int>(_Documento.ID, _ReaderView.DisplayedViewIndex);

                if(_PageHistory.Count == 0)
                {
                    _PageHistory.Add(current);
                }
                else
                {
                    var last = _PageHistory.Last();

                    if(last.Key != current.Key || last.Value != current.Value)
                    {
                        _PageHistory.Add(current);
                    }
                }

                if (_Pubblicazione.Swipe)
                {
                    _ReaderView.MoveToNext();
                }
                else
                {
                    GoToPage((_ReaderView.DisplayedViewIndex + 1).ToString());
                }
            }			
		}

		public void PreviousPage()
		{
			if(_ReaderView == null)
				return;

            if(_ReaderView.DisplayedViewIndex == 0 && _Pubblicazione.Documenti.Count > 0)
            {
                int curDocIndex = _Pubblicazione.Documenti.IndexOf(_Documento);

                if(curDocIndex - 1 >= 0) //documento precedente
                {
                    GoToPage((_Pubblicazione.Documenti[curDocIndex - 1].Articoli.Count - 1).ToString(), _Pubblicazione.Documenti[curDocIndex - 1].ID);
                }
            }
            else if(_ReaderView.DisplayedViewIndex > 0)
            {
                var current = new KeyValuePair<string, int>(_Documento.ID, _ReaderView.DisplayedViewIndex);

                if(_PageHistory.Count == 0)
                {
                    _PageHistory.Add(current);
                }
                else
                {
                    var last = _PageHistory.Last();

                    if(last.Key != current.Key || last.Value != current.Value)
                    {
                        _PageHistory.Add(current);
                    }
                }

                if (_Pubblicazione.Swipe)
                {
                    _ReaderView.MoveToPrevious();
                }
                else
                {
                    GoToPage((_ReaderView.DisplayedViewIndex - 1).ToString());
                }
            }
		}

		public void BackPage()
		{
			if(_ReaderView == null)
				return;			

            if(_PageHistory == null || _PageHistory.Count == 0)
                return;

            int index = _PageHistory.Last().Value;
            string docID = _PageHistory.Last().Key;

            if(_Documento.ID != docID)
            {
                OpenDocument(docID, true);
            }

            _ReaderView.DisplayedViewIndex = index;

            _PageHistory.RemoveAt(_PageHistory.Count - 1);
		}

        public void FirstPage()
        {
            if(_ReaderView == null)
                return;

            var doc = _Pubblicazione.Documenti.FirstOrDefault();

            if(doc != null)
            {
                GoToPage("0", doc.ID);
            }
        }

        public void LastPage()
        {
            if(_ReaderView == null)
                return;

            var doc = _Pubblicazione.Documenti.LastOrDefault();

            if(doc != null)
            {
                GoToPage((doc.Articoli.Count - 1).ToString(), doc.ID);
            }
        }

		/*public void LoadPub(ThumbFile doc, string page = "0")
		{
			this.Finish();
			Intent i = new Intent();
			i.SetClass(Application.Context, typeof(ViewerScreen));

			i.PutExtra("docPath", doc.Path);
			//i.PutExtra("doc", JsonConvert.SerializeObject(doc));
			i.PutExtra("page", page);

			StartActivity(i);
		}*/

        public void LoadPub(Pubblicazione pub, string docId = "", string pageNum = "0")
        {
            //se il percorso della pubblicazione è diversa da quella corrente carico quella nuova
            if(_Pubblicazione.Path != pub.Path)
            {
                _Pubblicazione = pub;

                this.Title = ActionBar.Title = _Pubblicazione.Titolo;

                _Pubblicazione.LoadDocuments();

                _PageHistory = new List<KeyValuePair<string, int>>();

                //InitUI();
            }

            bool result;
            int page;

            result = Int32.TryParse(pageNum, out page);

            if(!result)
                page = 0;

            //vado alla pagina e se necessario carico il documento
            GoToPage(page.ToString(), docId);

            if(DataManager.Get<ISettingsManager>().Settings.ToolbarVisible && _Pubblicazione.GestioneBarre)
            {
                _ReaderView.OnSingleTap += delegate
                {
                    this.ToggleUi();
                };
            }
            else
            {
                _ReaderView.OnSingleTap -= delegate
                {
                    this.ToggleUi();
                };
            }

            if (!_Pubblicazione.VisioneBarre || !DataManager.Get<ISettingsManager>().Settings.ToolbarVisible)
            {
                this.HideUi();
            } 

            ResetToolBars();
        }

        public void MenuNav(string[] param)
		{
			if(DataManager.Get<ISettingsManager>().Settings.SingolaApp)
				return;

            int index = Convert.ToInt32(param[0]);
            //differenza da ios
            switch (index)
            {
                case 0:
                    index = 2;
                    break;
                case 1:
                    index = 0;
                    break;
                case 2:
                    index = 1;
                    break;
                default:
                    index = 0;
                    break;
            }

            Action action = delegate {
                Intent intent = new Intent(this, typeof(HomeScreen));
                intent.PutExtra("index", index);
                intent.PutExtra("action", "appNav");

                if(index == 0 || index == 1)
                {
                    if(param.Length > 1)
                    {
                        intent.PutExtra("path", param[1]);
                    }
                }

                SetResult(Result.Ok, intent);
                Finish();
            };

            if (_Pubblicazione.PinUscita != null && _Pubblicazione.PinUscita != "")
            {
                ExitPinDialog(action);
            }
            else
            {
                action();
            }
			
		}

		public void OpenPopUp(string[] param)
		{			
			Intent intent = null;

			string par = param [0];

			switch(par)
			{
				case "indice":
					intent = new Intent(this, typeof(ContentsTableActivity));
					intent.PutExtra("currentPage", _ReaderView.DisplayedViewIndex);
					intent.PutExtra("currentItem", 0);

                    ActivitiesBringe.SetObject("pub", _Pubblicazione);
                    ActivitiesBringe.SetObject("doc", _Documento);
					ActivitiesBringe.SetObject("pdfCore", _PdfCore);

					StartActivityForResult(intent, 0);
					break;
				case "segnalibri":
					intent = new Intent(this, typeof(ContentsTableActivity));
					intent.PutExtra("currentPage", _ReaderView.DisplayedViewIndex);
					intent.PutExtra("currentItem", 1);

					ActivitiesBringe.SetObject("pub", _Pubblicazione);
                    ActivitiesBringe.SetObject("doc", _Documento);
					ActivitiesBringe.SetObject("pdfCore", _PdfCore);

					StartActivityForResult(intent, 0);

					break;
				case "note":
					if(_Pubblicazione.NoteUtenti)
					{
						intent = new Intent(this, typeof(ContentsTableActivity));
						intent.PutExtra("currentPage", _ReaderView.DisplayedViewIndex);
						intent.PutExtra("currentItem", 2);

						ActivitiesBringe.SetObject("pub", _Pubblicazione);
                        ActivitiesBringe.SetObject("doc", _Documento);
						ActivitiesBringe.SetObject("pdfCore", _PdfCore);

						StartActivityForResult(intent, 0);
					}
					break;
				case "cerca":
					if(_Pubblicazione.SearchText)
					{
						intent = new Intent(this, typeof(SearchActivity));
						intent.PutExtra("currentPage", _ReaderView.DisplayedViewIndex);
                        ActivitiesBringe.SetObject("pub", _Pubblicazione);
                        ActivitiesBringe.SetObject("doc", _Documento);
						ActivitiesBringe.SetObject("pdfCore", _PdfCore);

						if (param.Length > 1)
						{
							string search = param [1];
							ActivitiesBringe.SetObject("search", search);
						}

						StartActivityForResult(intent, 0);
					}
					break;
				case "crediti":
					intent = new Intent(this, typeof(AboutActivity));
					StartActivity(intent);
					break;
				default:
					break;
			}
		}

		#region toolBars
		private void SetToolBars()
		{
            //barra in alto
             ActionBar.SetDisplayUseLogoEnabled(false);
            ActionBar.SetIcon(new ColorDrawable(Color.Transparent));
            ActionBar.SetHomeButtonEnabled(false);
            ActionBar.SetDisplayHomeAsUpEnabled(true);
            ActionBar.SetDisplayShowHomeEnabled(true);
            ActionBar.SetDisplayShowTitleEnabled(true);

            ActionBar.SetBackgroundDrawable(new ColorDrawable(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.NavigationBarColor)));

            //titolo
            var titleId = Resources.GetIdentifier("action_bar_title", "id", "android");
            var abTitle = FindViewById<TextView>(titleId);
            abTitle.SetTextColor(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.TintColor));

            //back
            var iconId = Resources.GetIdentifier("up", "id", "android");
            var abBack = FindViewById<ImageView>(iconId);
            abBack.Colorize(DataManager.Get<ISettingsManager>().Settings.TintColor);

            if (DataManager.Get<ISettingsManager>().Settings.SingolaApp)
            {
                ActionBar.SetDisplayHomeAsUpEnabled(false);
                ActionBar.SetDisplayShowHomeEnabled(false);
            }


			//ActionBar.Subtitle = _documento.Autore != "" ? _documento.Autore : GetString(Resource.String.pub_sconosciuto);

			//barra in basso
			_BottomBar = FindViewById<BottomBar>(Resource.Id.bottomBar);

			var pagePreview = FindViewById<PagePreviewOverlay>(Resource.Id.pagePreviewOverlay);
			pagePreview.Visibility = ViewStates.Invisible;

            if(_Pubblicazione.IsPDF)
            {
                _BottomBar.ProgressMax = _PageCount;
            }
            else
            {
                _BottomBar.ProgressMax = _Pubblicazione.PagesCount - 1;
            }

			_BottomBar.StartProgress += p =>
			{
				if(_Pubblicazione.IsPDF)
                {
                    pagePreview.SetPagePdf(_PdfCore, p, _Documento.Path);
                }
                else
                {
                    /*var art = _documento.Articoli[p];
                    string imgPath = System.IO.Path.Combine(_documento.Path, art.Path, "miniatura.jpg");
                    pagePreview.SetPage(imgPath, p);*/
                    var pair = _Pubblicazione.AbsoluteToRelativePage(p);

                    var doc = _Pubblicazione.GetDocumento(pair.Key);

                    if(doc != null && doc.ID != null)
                    {
                        var art = doc.Articoli[pair.Value];
                        string imgPath = System.IO.Path.Combine(doc.Path, art.Path, "miniatura.jpg");
                        pagePreview.SetPage(imgPath, p);
                    }
                }

				pagePreview.Visibility = ViewStates.Visible;
			};

			_BottomBar.StopProgress += p =>
			{
				pagePreview.Visibility = ViewStates.Invisible;

                var pair = _Pubblicazione.AbsoluteToRelativePage(p);

                this.GoToPage(pair.Value.ToString(), pair.Key);            
            };

			_BottomBar.ChangeProgress += p =>
			{
				if(_Pubblicazione.IsPDF)
				{
					pagePreview.SetPagePdf(_PdfCore, p, _Documento.Path);
				}
				else
				{
                    /*var art = _documento.Articoli[p];
					string imgPath = System.IO.Path.Combine(_documento.Path, art.Path, "miniatura.jpg");
					pagePreview.SetPage(imgPath, p);*/
                    var pair = _Pubblicazione.AbsoluteToRelativePage(p);

                    var doc = _Pubblicazione.GetDocumento(pair.Key);

                    if(doc != null && doc.ID != null)
                    {
                        var art = doc.Articoli[pair.Value];
                        string imgPath = System.IO.Path.Combine(doc.Path, art.Path, "miniatura.jpg");
                        pagePreview.SetPage(imgPath, p);
                    }
				}
			};

			//mostra/nascondi barre
			var uiOptions = SystemUiFlags.LowProfile | SystemUiFlags.LayoutFullscreen | SystemUiFlags.Fullscreen | SystemUiFlags.LayoutStable;

			Window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
			Window.DecorView.SetFitsSystemWindows(true);

			Window.DecorView.SystemUiVisibilityChange += delegate (object sender, View.SystemUiVisibilityChangeEventArgs e) 
			{
				if (ActionBar != null) 
				{				

					if ((e.Visibility & (StatusBarVisibility)SystemUiFlags.Fullscreen) == 0) 
					{
						if(DataManager.Get<ISettingsManager>().Settings.ToolbarVisible && _Pubblicazione.GestioneBarre)
						{
							ActionBar.Show ();
							_BottomBar.Show();
						}
					} 
					else 
					{
						ActionBar.Hide ();
						_BottomBar.Hide();
					}
				}
			};
		}

        private void ResetToolBars()
        {
            _BottomBar = FindViewById<BottomBar>(Resource.Id.bottomBar);

            var pagePreview = FindViewById<PagePreviewOverlay>(Resource.Id.pagePreviewOverlay);
            pagePreview.Visibility = ViewStates.Invisible;

            if (_Pubblicazione.IsPDF)
            {
                _BottomBar.ProgressMax = _PageCount;
            }
            else
            {
                _BottomBar.ProgressMax = _Pubblicazione.PagesCount - 1;
            }

        }

		public override bool OnPrepareOptionsMenu(IMenu menu)
		{
			if(_ReaderView == null)
				return false;

			var item = menu.FindItem(Resource.Id.DocViewerActionBarMenu_Bookmark);

            var manager = new BookmarksManager(_Pubblicazione);

            var absPage = _Pubblicazione.RelativeToAbsolutePage(_Documento.ID, _ReaderView.DisplayedViewIndex + 1);

            if(manager.GetBookmarkByPage(absPage) == null)
			{
				item.SetTitle("Aggiungi segnalibro");

				//item.SetIcon(Resource.Drawable.ic_action_view_as_grid);
				item.SetIcon(Resource.Drawable.ic_action_segnalibro_off);
			}
			else
			{
				item.SetTitle("Rimuovi segnalibro");

				item.SetIcon(Resource.Drawable.ic_action_segnalibro_on);
			}

			item.Icon.Colorize (DataManager.Get<ISettingsManager> ().Settings.TintColor);

			return base.OnPrepareOptionsMenu(menu);
		}

		public override bool OnCreateOptionsMenu(IMenu menu)
		{
			MenuInflater.Inflate(Resource.Menu.DocViewerActionBarMenu, menu);

			//pusante cerca
			var search = menu.FindItem(Resource.Id.DocViewerActionBarMenu_Search);
			search.Icon.Colorize (DataManager.Get<ISettingsManager> ().Settings.TintColor);

            if(_Pubblicazione.IsPDF || !_Pubblicazione.SearchText)
			{
				menu.RemoveItem(search.ItemId);
			}

			//indice
			var index = menu.FindItem(Resource.Id.DocViewerActionBarMenu_Page);
			index.Icon.Colorize (DataManager.Get<ISettingsManager> ().Settings.TintColor);

			//more
			var overflow = menu.FindItem(Resource.Id.menu_overflow);
			overflow.Icon.Colorize (DataManager.Get<ISettingsManager> ().Settings.TintColor);			

			if(DataManager.Get<ISettingsManager>().Settings.EdicolaEnabled && !DataManager.Get<ISettingsManager>().Settings.SingolaApp)
			{				
				menu.RemoveItem(overflow.ItemId);
			}

            var titleId = Resources.GetIdentifier("action_bar_title", "id", "android");
            var abTitle = FindViewById<TextView>(titleId);
            abTitle.SetTextColor(Color.Transparent.FromHex(DataManager.Get<ISettingsManager>().Settings.TintColor));

			return base.OnCreateOptionsMenu(menu);
		}

		public override bool OnOptionsItemSelected(IMenuItem item)
		{
			switch (item.ItemId)
			{
				case Resource.Id.DocViewerActionBarMenu_Page:
					var intent = new Intent(this, typeof(ContentsTableActivity));
						intent.PutExtra("currentPage", _ReaderView.DisplayedViewIndex);
						//intent.PutExtra("docPath", _documento.Path);
						//intent.PutExtra("pdfCore", JsonConvert.SerializeObject(_pdfCore));

                        ActivitiesBringe.SetObject("pub", _Pubblicazione);
                        ActivitiesBringe.SetObject("doc", _Documento);
						ActivitiesBringe.SetObject("pdfCore", _PdfCore);

						StartActivityForResult(intent, 0);
					break;				
				case Resource.Id.DocViewerActionBarMenu_Bookmark:
                    BookmarksManager bookMan = new BookmarksManager(_Pubblicazione);

                    var absPage = _Pubblicazione.RelativeToAbsolutePage(_Documento.ID, _ReaderView.DisplayedViewIndex + 1);

                    if(bookMan.GetBookmarkByPage(absPage) == null)
					{
						var book = new Bookmark{
                            Pubblicazione = _Pubblicazione.ID,
                            Documento = _Documento.ID, 
							Pagina = _ReaderView.DisplayedViewIndex + 1, 
                            Titolo = string.Format(GetString(Resource.String.pub_pagina), (absPage)),
                            PaginaAbs = absPage
                                           
						};
						bookMan.AddBookmark(book);
						Toast.MakeText(this, GetString(Resource.String.pub_bookmarksAdded), ToastLength.Short).Show();

						item.SetTitle(GetString(Resource.String.pub_bookmarksRemove));

						item.SetIcon(Resource.Drawable.ic_action_segnalibro_on);
						item.Icon.Colorize (DataManager.Get<ISettingsManager> ().Settings.TintColor);
					}
					else
					{
						bookMan.DeleteBookmark(absPage);
						Toast.MakeText(this, GetString(Resource.String.pub_bookmarksRemoved), ToastLength.Short).Show();

						item.SetTitle(GetString(Resource.String.pub_bookmarksAdd));

						//item.SetIcon(Resource.Drawable.ic_action_view_as_grid);
						item.SetIcon(Resource.Drawable.ic_action_segnalibro_off);
					}

					break;
				case Resource.Id.DocViewerActionBarMenu_Search:
						var search = new Intent(this, typeof(SearchActivity));
						search.PutExtra("currentPage", _ReaderView.DisplayedViewIndex);
						ActivitiesBringe.SetObject("pub", _Pubblicazione);
                        ActivitiesBringe.SetObject("doc", _Documento);
						ActivitiesBringe.SetObject("pdfCore", _PdfCore);

					StartActivityForResult(search, 0);
					break;
				case Resource.Id.DocViewerActionBarMenu_Info:
					var about = new Intent(this, typeof(AboutActivity));
					StartActivity(about);
					break;
				case Android.Resource.Id.Home:
					if (!DataManager.Get<ISettingsManager> ().Settings.SingolaApp)
					{
                        Action action = delegate {
                            this.Finish();
                            StatisticheManager.SendStats();
                        };

                        if(_Pubblicazione.PinUscita != null && _Pubblicazione.PinUscita != "")
                        {
                            ExitPinDialog(action);
                        }
                        else
                        {
                            action(); 
                        }						
					}
					break;
			}

			return false;
		}
		#endregion

		//solo in caso di telefoni
		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);
			if (resultCode == Result.Ok) 
			{
				string action = data.GetStringExtra("action");

				if(action == "page")
				{
					int page = 0;

                    string docId = data.GetStringExtra("doc");

					bool result = int.TryParse(data.GetStringExtra("page"), out page);

					if(result)
					{
                        this.GoToPage(page.ToString(), docId);
					}
				}
				else if(action == "note")
				{
                    NoteManager nManager = new NoteManager(_Pubblicazione);

					NotaUtente nota = nManager.GetNota(data.GetStringExtra("idNota"));
                    var doc = _Pubblicazione.GetDocumento(nota.IdDocumento);

                    if(!nManager.NotaIsInline(nota, doc))
					{
						this.ShowNotaPop(nota);
					}

                    this.GoToPage((nota.Pagina - 1).ToString(), nota.IdDocumento);
				}

				//Console.WriteLine("Selezionata pagina: " + data.GetStringExtra("page"));
			}
		}

		public void ShowNotaPop(NotaUtente nota)
		{
			Console.WriteLine("apertura nota: p: " + nota.Pagina + " t: " + nota.Titolo);

            var dialog = new ViewerNoteEdit(nota, _Pubblicazione);
			dialog.Show(this.FragmentManager, "");
		}

		public void ToggleUi()
		{
			View decorView = Window.DecorView;
			if ((decorView.SystemUiVisibility & (StatusBarVisibility)SystemUiFlags.Fullscreen) == 0) 
			{
				this.HideUi();
			} 
			else 
			{
				this.ShowUi();
			}
		}

		public void ShowUi()
		{
			View decorView = Window.DecorView;
			var uiOptions = SystemUiFlags.LayoutFullscreen | SystemUiFlags.LayoutStable;
			decorView.SystemUiVisibility = (StatusBarVisibility)uiOptions; 
		}

		public void HideUi()
		{
			View decorView = Window.DecorView;
			var uiOptions = SystemUiFlags.LowProfile | SystemUiFlags.LayoutFullscreen | SystemUiFlags.Fullscreen | SystemUiFlags.LayoutStable;
			decorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
		}

		protected override void OnDestroy()
		{
			if (_PdfCore != null) 
			{
				_PdfCore.OnDestroy();
			}
			_PdfCore = null;

			base.OnDestroy();

            AnalyticsService.SendEvent(_Pubblicazione.Titolo, AnalyticsEventAction.PubClose);

			Runtime.GetRuntime().Gc();
            GC.Collect();
		}

		private void ShowAlertMessage(string title, string message, bool exit = false)
		{
			var alert = new AlertDialog.Builder(this);
			alert.SetTitle(title);
			alert.SetMessage(message);
			alert.SetPositiveButton("Ok", delegate
			{
				if(exit)
				{
					this.Finish();
				}
			});

			alert.Show().SetDivider();
		}

        private bool OnReaderScroll(MOVING moving)
        {
            this.HideUi();

            if (!_Pubblicazione.Swipe || _Pubblicazione.Documenti.Count <= 1 || !_Pubblicazione.SwipeDoc)
                return false;

            if (!_Pubblicazione.SwipeDocSeguente)
            {
                
                if(_Pubblicazione.ScrollVerticale)
                {
                    return moving == MOVING.LEFT || moving == MOVING.RIGHT;
                }
                else
                {
                    return moving == MOVING.UP || moving == MOVING.DOWN;
                }
            }
            else
            {
                if (_Pubblicazione.ScrollVerticale)
                {
                    return moving == MOVING.UP || moving == MOVING.DOWN;
                }
                else
                {
                    return moving == MOVING.LEFT || moving == MOVING.RIGHT;
                }
            }

            //return false;
        }

        private void OnReaderFling(MOVING moving, MotionEvent e1, MotionEvent e2)
        {
            if(!_Pubblicazione.Swipe || _Pubblicazione.Documenti.Count <= 1 || !_Pubblicazione.SwipeDoc)
                return;

            MuPageView pageView = (MuPageView)_ReaderView.DisplayedView;

            if(pageView == null || !pageView.OnScreen || !pageView.Loaded)
            {
                return;
            }

            if((DateTime.Now - _lastDocChange).TotalMilliseconds < 800)
            {
                return;
            }

            var index = _Pubblicazione.Documenti.IndexOf(_Documento);

            if(!_Pubblicazione.SwipeDocSeguente)
            {
                if(_Pubblicazione.ScrollVerticale)
                {
                    var dist = System.Math.Abs(e2.RawX - e1.RawX);

                    if (dist < 50)
                        return;

                    switch(moving)
                    {
                        case MOVING.LEFT:
                            index += 1;
                            break;
                        case MOVING.RIGHT:
                            index -= 1;
                            break;
                        default:
                            return;
                    }
                }
                else
                {
                    var dist = System.Math.Abs(e2.RawY - e1.RawY);

                    if (dist < 50)
                        return;
                    
                    switch(moving)
                    {
                        case MOVING.UP:
                            index += 1;
                            break;
                        case MOVING.DOWN:
                            index -= 1;
                            break;
                        default:
                            return;
                    }
                }
            }
            else
            {
                if(_Pubblicazione.ScrollVerticale)
                {
                    if(_ReaderView.DisplayedViewIndex == _PageCount - 1 && moving == MOVING.UP)
                    {
                        index += 1;
                    }
                    else if(_ReaderView.DisplayedViewIndex == 0 && moving == MOVING.DOWN)
                    {
                        index -= 1;
                    }
                    else
                        return;
                }
                else
                {
                    if(_ReaderView.DisplayedViewIndex == _PageCount - 1 && moving == MOVING.LEFT)
                    {
                        index += 1;
                    }
                    else if(_ReaderView.DisplayedViewIndex == 0 && moving == MOVING.RIGHT)
                    {
                        index -= 1;
                    }
                    else
                        return;
                }
            }

            Documento doc = null;

            /*switch(moving)
            {
                case MOVING.LEFT:
                    index += 1;
                    break;
            }*/

            if(index >= 0 && index < _Pubblicazione.Documenti.Count)
            {
                doc = _Pubblicazione.Documenti[index];
                //this.GoToPage("0", doc.ID);
            }

            if(doc == null)
                return;

            int page = 0;

            if(_Pubblicazione.SwipeDocSeguente)
            {
                if(moving == MOVING.RIGHT || moving == MOVING.DOWN)
                {
                    page = doc.Articoli.Count - 1;
                }
            }

            this.GoToPage(page.ToString(), doc.ID);

            _lastDocChange = DateTime.Now;

            ChangeDocTransition(moving, doc);

        }

        private void ChangeDocTransition(MOVING moving, Documento doc, bool prevLastPage = false)
        {
            var readerWrap = FindViewById<FrameLayout>(Resource.Id.viewerWrap);

            RelativeLayout.LayoutParams img1Param;
            RelativeLayout.LayoutParams img2Param;
            int screenW = readerWrap.MeasuredWidth;
            int screenH = readerWrap.MeasuredHeight;
            int animW = readerWrap.MeasuredWidth;
            int animH = readerWrap.MeasuredHeight;
            int translateX = 0;
            int translateY = 0;
            int animType;

            switch (moving)
            {
                default:
                case MOVING.LEFT:
                    animType = Resource.Animation.doc_slide_left;

                    animW *= 2;

                    img1Param = new RelativeLayout.LayoutParams(screenW, screenH);
                    img1Param.LeftMargin = 0;
                    img1Param.TopMargin = 0;

                    img2Param = new RelativeLayout.LayoutParams(screenW, screenH);
                    img2Param.LeftMargin = screenW;
                    img2Param.TopMargin = 0;

                    break;

                case MOVING.RIGHT:
                    animType = Resource.Animation.doc_slide_right;

                    animW *= 2;
                    translateX = -screenW;

                    img1Param = new RelativeLayout.LayoutParams(screenW, screenH);
                    img1Param.LeftMargin = screenW;
                    img1Param.TopMargin = 0;

                    img2Param = new RelativeLayout.LayoutParams(screenW, screenH);
                    img2Param.LeftMargin = 0;
                    img2Param.TopMargin = 0;

                    break;

                case MOVING.UP:
                    animType = Resource.Animation.doc_slide_up;

                    animH *= 2;

                    img1Param = new RelativeLayout.LayoutParams(screenW, screenH);
                    img1Param.LeftMargin = 0;
                    img1Param.TopMargin = 0;

                    img2Param = new RelativeLayout.LayoutParams(screenW, screenH);
                    img2Param.LeftMargin = 0;
                    img2Param.TopMargin = screenH;

                    break;

                case MOVING.DOWN:
                    animType = Resource.Animation.doc_slide_down;

                    animH *= 2;
                    translateY = -screenH;

                    img1Param = new RelativeLayout.LayoutParams(screenW, screenH);
                    img1Param.LeftMargin = 0;
                    img1Param.TopMargin = screenH;

                    img2Param = new RelativeLayout.LayoutParams(screenW, screenH);
                    img2Param.LeftMargin = 0;
                    img2Param.TopMargin = 0;

                    break;
            }

            RelativeLayout animView = new RelativeLayout(this);
            readerWrap.AddView(animView, new ViewGroup.LayoutParams(animW, animH));
            animView.TranslationX = translateX;
            animView.TranslationY = translateY;
            animView.SetBackgroundColor(Color.Transparent.FromHex(_Pubblicazione.coloreFondo.ToString("X")));

            Bitmap bitmap = Bitmap.CreateBitmap(screenW, screenH,
                    Bitmap.Config.Rgb565);
            Canvas canvas = new Canvas(bitmap);
            readerWrap.Layout(0, 0, screenW, screenH);
            readerWrap.Draw(canvas);

            var imgView1 = new ImageView(this);
            imgView1.SetImageBitmap(bitmap);
            imgView1.LayoutParameters = img1Param;
            //imgView1.SetBackgroundColor(Color.Red);
            animView.AddView(imgView1);

            var imgPath2 = System.IO.Path.Combine(doc.Path, "first.jpg");
            var imgView2 = new ImageView(this);
            imgView2.SetScaleType(ImageView.ScaleType.FitCenter);
            imgView2.LayoutParameters = img2Param;

            if (_Pubblicazione.SwipeDocSeguente || prevLastPage)
            {
                if (moving == MOVING.RIGHT || moving == MOVING.DOWN)
                {
                    
                    imgPath2 = System.IO.Path.Combine(doc.Path, "last.jpg");
                }
            }

            Koush.UrlImageViewHelper.SetUrlDrawable(imgView2, new Uri(imgPath2).AbsoluteUri);
            //imgView2.SetBackgroundColor(Color.Blue);         
            animView.AddView(imgView2);

           /* Animation myAnimation = AnimationUtils.LoadAnimation(this, animType);

            //myAnimation.Duration = 2000;

            myAnimation.AnimationStart += delegate {

            };

            myAnimation.AnimationEnd += delegate {

                animView.Alpha = 0;
                animView.RemoveAllViews();
                animView.Dispose();
                animView = null;

                imgView1.Dispose();
                imgView1 = null;

                imgView2.Dispose();
                imgView2 = null;
            };

            animView.StartAnimation(myAnimation);*/

            AnimationSet aniSet = (AnimationSet)AnimationUtils.LoadAnimation(this, animType);

            var tAnimation = aniSet.Animations[0];
            var fAnimation = aniSet.Animations[1];

            tAnimation.Duration = 500;
            fAnimation.StartOffset = 500;
            fAnimation.Duration = 300;

            fAnimation.Interpolator = new AccelerateInterpolator();


            aniSet.AnimationStart += delegate {
            
                _ReaderView.ScrollEnabled = false;
            };

            aniSet.AnimationEnd += delegate {

                animView.Alpha = 0;
                animView.RemoveAllViews();
                animView.Dispose();
                animView = null;

                imgView1.Dispose();
                imgView1 = null;

                imgView2.Dispose();
                imgView2 = null;

                _ReaderView.ScrollEnabled = true;
            };



            animView.StartAnimation(aniSet);
        }

        private void ExitPinDialog(Action succAction = null)
        {
            var builder = new AlertDialog.Builder(this);

            builder.SetTitle("PIN Uscita");
            builder.SetView(this.LayoutInflater.Inflate(Resource.Layout.PinDialog, null));

            builder.SetCancelable(false);

            builder.SetPositiveButton("OK", (EventHandler<DialogClickEventArgs>)null);
            builder.SetNegativeButton(GetString(Resource.String.gen_cancel), (EventHandler<DialogClickEventArgs>)null);

            var dialog = builder.Create();
            dialog.Show();
            dialog.SetDivider();

            EditText txtPin = dialog.FindViewById<EditText>(Resource.Id.txtPin); //new EditText(Activity);
            EditText txtPin2 = dialog.FindViewById<EditText>(Resource.Id.txtPin2); //new EditText(Activity);
            TextView lblSuccess = dialog.FindViewById<TextView>(Resource.Id.lblSuccess);

            txtPin2.Visibility = ViewStates.Gone;

            //pulsante ok
            var btnOK = dialog.GetButton((int)DialogButtonType.Positive);

            if (btnOK == null)
                return;

            btnOK.Click += (sender, e) =>
            {
                if (txtPin.Text == _Pubblicazione.PinUscita)
                {
                    dialog.Dismiss();

                    if (succAction != null)
                        succAction();
                }
                else
                {
                    lblSuccess.Visibility = ViewStates.Visible;
                    lblSuccess.Text = "PIN errato";
                }
            };
        }

        public override void OnBackPressed()
        {
            Action action = delegate {
                base.OnBackPressed();
            };

            if(_Pubblicazione.PinUscita != null && _Pubblicazione.PinUscita != "")
            {
                ExitPinDialog(action);
            }
            else
            {
                action(); 
            } 
        }

        public void NavTo(PubNav pubNav)
        {
            string pubPath = System.IO.Path.Combine(DataManager.Get<ISettingsManager>().Settings.DocPath, pubNav.Cartella, pubNav.Pubblicazione);

            bool pubExixts = System.IO.File.Exists(pubPath) || System.IO.Directory.Exists(pubPath);
            bool isReachable = Reachability.IsHostReachable(DataManager.Get<ISettingsManager>().Settings.DownloadUrl);

            /**
             * se è disponibile la rete cerco se la pubblicazione è da scaricare o aggiornare
             * nel caso la scarico e poi la apro
             * 
             * se non c'è rete o la pubblicazione non è da scaricare guardo se esiste nel disco e la apro
             */
            if (isReachable)
            {
                var down = DownloadManager.GetDocumentById(pubNav.IdPubblicazione, pubNav.Cartella);

                if (down != null && down.IapID == "" && (down.Stato == DownloadStato.Download || down.Stato == DownloadStato.Update))
                {
                    this.DownloadAndGoTo(down, pubPath, pubNav.IdDocumento, pubNav.Pagina.ToString());
                    return;
                }
            }

            if (System.IO.File.Exists(pubPath) || System.IO.Directory.Exists(pubPath))
            {
                Pubblicazione pub = new Pubblicazione(pubPath);
                this.LoadPub(pub, pubNav.IdDocumento != "" ? pubNav.IdDocumento : pub.ID, pubNav.Pagina.ToString());
            }
        }

        public void DownloadAndGoTo(Download down, string pubPath, string idDoc = "", string page = "")
        {
            if (down != null)
            {
                /*UIAlertController alert = UIAlertController.Create("Download".t(), "Do you want to download the publication?".t(), UIAlertControllerStyle.Alert);

                alert.AddAction(UIAlertAction.Create("Download".t(), UIAlertActionStyle.Default, (UIAlertAction obj) => {

                    var downloadOverlay = new DownloadOverlay(new CGRect(0, 0, this.View.Frame.Width, this.View.Frame.Height), "Download pubblicazione in corso".t());
                    this.View.Add(downloadOverlay);
                    this.View.BringSubviewToFront(downloadOverlay);

                    downloadOverlay.DownloadCompleted = () =>
                    {
                        Pubblicazione pub = new Pubblicazione(pubPath);

                        this.LoadPub(pub, idDoc != "" ? idDoc : pub.ID, page);
                    };

                    downloadOverlay.Download(down.Uri);

                }));

                alert.AddAction(UIAlertAction.Create("Cancel".t(), UIAlertActionStyle.Cancel, null));

                PresentViewController(alert, true, null);
                alert.View.TintColor = UIColor.Clear.FromHex(DataManager.Get<ISettingsManager>().Settings.ButtonColor);*/

                var alert = new AlertDialog.Builder(this);
                alert.SetTitle(GetString(Resource.String.pub_download));
                alert.SetMessage(GetString(Resource.String.pub_downloadFile));
                alert.SetNegativeButton(GetString(Resource.String.gen_cancel), delegate
                {
                    return;
                });

                alert.SetPositiveButton("Ok", delegate
                {
                    var wrap = FindViewById<RelativeLayout>(Resource.Id.contentLayout);

                    var downloadOverlay = new DownloadOverlay(this, GetString(Resource.String.pub_downloadProgress) + "...");

                    downloadOverlay.DownloadCompleted = () =>
                    {
                        Pubblicazione pub = new Pubblicazione(pubPath);

                        this.LoadPub(pub, idDoc != "" ? idDoc : pub.ID, page);

                        downloadOverlay.Hide();
                        wrap.RemoveView(downloadOverlay);
                        downloadOverlay = null;
                    };

                   
                    wrap.AddView(downloadOverlay);
                    wrap.BringChildToFront(downloadOverlay);

                    downloadOverlay.Download(down.Uri);

                    downloadOverlay.Show();
                });

                alert.Show().SetDivider();
            }
        }
	}
}