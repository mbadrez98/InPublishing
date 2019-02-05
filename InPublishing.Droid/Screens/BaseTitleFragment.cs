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

namespace InPublishing
{
	public class BaseTitleFragment : Android.Support.V4.App.Fragment
	{
		private string _title;
		public string Title
		{
			get{ return _title; }
			set{ _title = value; }
		}

		public override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// Create your fragment here
		}

		public BaseTitleFragment(string title)
		{
			_title = title;
		}
	}
}

