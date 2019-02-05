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
using System.Collections;
using Android.Support.V4.App;

namespace InPublishing
{
	class TitleFragmentAdapter : FragmentPagerAdapter, TitleProvider
	{
		private ArrayList fragments;

		public TitleFragmentAdapter(Android.Support.V4.App.FragmentManager fm, ArrayList fragments) : base(fm)
		{
			this.fragments = fragments;
		}

		public override Android.Support.V4.App.Fragment GetItem(int position) 
		{
			return this.fragments[position] as Android.Support.V4.App.Fragment;
		}

		public override int Count 
		{
			get { return this.fragments.Count; }
		}

		public string GetTitle (int position)
		{
			var item = this.fragments[position] as BaseTitleFragment;

			return item.Title.ToUpper();

		}
	}
}

