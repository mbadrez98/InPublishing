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
using Android.Support.V4.View;
using System.Collections;
using Android.Graphics.Drawables;
using Android.Graphics;
using Com.Artifex.Mupdfdemo;
using Newtonsoft.Json;

namespace InPublishing
{
	[Activity(Label = "ContentsTableActivity", Theme = "@style/Blue.DialogActivity")]			
	public class ContentsTableActivity : Android.Support.V4.App.FragmentActivity
	{
        private Pubblicazione _pubblicazione;
        private Documento _documento;
		private int _currentPage;
		private MuPDFCore _pdfCore;

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			this.SetContentView(Resource.Layout.ContentsTable);

			if(ActionBar != null)
			{
				ActionBar.SetDisplayUseLogoEnabled(false);
				ActionBar.SetIcon(new ColorDrawable(Color.Transparent));
				ActionBar.SetHomeButtonEnabled(false);
				ActionBar.SetDisplayHomeAsUpEnabled(true);
				ActionBar.SetDisplayShowHomeEnabled(true);
				ActionBar.SetDisplayShowTitleEnabled(true);
			}

			this.Title = "Contenuti";

			_currentPage = Intent.GetIntExtra("currentPage", 0);
            _pubblicazione = (Pubblicazione)ActivitiesBringe.GetObject("pub");
            _documento = (Documento)ActivitiesBringe.GetObject("doc");
			_pdfCore = (MuPDFCore)ActivitiesBringe.GetObject("pdfCore"); 

			ViewPager viewPager = (ViewPager) this.FindViewById<ViewPager>(Resource.Id.pagesViewPager);
			ArrayList fragments = getFragments();

			TitleFragmentAdapter ama = new TitleFragmentAdapter(this.SupportFragmentManager, fragments);
			viewPager.Adapter = ama;

			TabPageIndicator indicator = this.FindViewById<TabPageIndicator> (Resource.Id.pagesIndicator);
			indicator.SetViewPager (viewPager);



            /*StateListDrawable bgColorList = new StateListDrawable();

            bgColorList.AddState(new int[] { Android.Resource.Attribute.StateActivated }, new ColorDrawable(Color.Transparent.FromHex("ff0000")));
            bgColorList.AddState(new int[] { }, new ColorDrawable(Color.Transparent));

            indicator.Background = bgColorList;*/

			viewPager.CurrentItem = Intent.GetIntExtra("currentItem", 0);
		}

		private ArrayList getFragments()
		{
			ArrayList fList = new ArrayList();

            var item = new IndexFragment(GetString(Resource.String.pub_pagine), _pubblicazione, _documento, _currentPage, _pdfCore);
			item.PageItemClick += prop =>
			{
                var resp = (string[])prop;

				Intent myIntent = new Intent (this, typeof(ViewerScreen));
                myIntent.PutExtra ("doc", resp[0]);
				myIntent.PutExtra ("page", resp[1]);
				myIntent.PutExtra ("action", "page");
				SetResult (Result.Ok, myIntent);
				Finish();
			};
			fList.Add(item);

			var item2 = new BookmarksFragment(GetString(Resource.String.pub_segnalibri), _pubblicazione, _documento, _currentPage, _pdfCore);
			item2.PageItemClick += prop =>
			{
                var resp = (string[])prop;

				Intent myIntent = new Intent (this, typeof(ViewerScreen));
                myIntent.PutExtra("doc", resp[0]);
                myIntent.PutExtra("page", resp[1]);
				myIntent.PutExtra ("action", "page");
				SetResult (Result.Ok, myIntent);
				Finish();
			};
			fList.Add(item2);

			if(!_pubblicazione.IsPDF && _pubblicazione.NoteUtenti)
			{
				var item3 = new NoteFragment(GetString(Resource.String.pub_note), _pubblicazione);
				item3.NoteItemClick += nota =>
				{
					Intent myIntent = new Intent(this, typeof(ViewerScreen));
					myIntent.PutExtra("idNota", nota.ID);
					myIntent.PutExtra("action", "note");
					SetResult(Result.Ok, myIntent);
					Finish();
				};
				fList.Add(item3);
			}

			//fList.Add(new ViewerBookmarksFragment("Segnalibri"));
			//fList.Add(new tempFragment("Note"));
			return fList;
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
	}
}

