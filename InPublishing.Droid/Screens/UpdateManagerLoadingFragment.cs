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
	public class UpdateManagerLoadingFragment : Fragment
	{
		ProgressDialog progress;
		protected bool IsUpdating = false;

		/*public override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// Create your fragment here
		}*/

		public override void OnStart()
		{
			base.OnStart();

			Utils.WriteLog("UPDATELOADING OnStart");
		}

		public override void OnResume()
		{
			Console.WriteLine("UPDATELOADING OnResume");
			base.OnResume();

			if (IsUpdating) 
			{
				Utils.WriteLog("UPDATELOADING OnResume IsUpdating");
				/*				if (progress == null) 
				{
					progress = ProgressDialog.Show(this, "Loading", "Please Wait...", true); 
				}*/
			} 
			else 
			{
				/*				Utils.WriteLog("UPDATELOADING OnResume PopulateTable");
				if (progress != null)
					progress.Hide();*/

				//PopulateTable();
			}
		}

		public override void OnStop()
		{
			Utils.WriteLog("UPDATELOADING OnStop");

			HideLoadingOverlay();
			base.OnStop();
		}

		protected void StartUpdating(bool overlay = true)
		{
			IsUpdating = true;
			if(overlay)
				ShowLoadingOverlay();
		}

		protected void StopUpdating()
		{
			IsUpdating = false;
			HideLoadingOverlay();
		}

		private void ShowLoadingOverlay()
		{
			if (progress == null && Activity != null) 
			{
				progress = ProgressDialog.Show(Activity, GetString(Resource.String.gen_loading), GetString(Resource.String.gen_loadingData), true);
				progress.SetDivider();
			}			 
		}

		private void HideLoadingOverlay()
		{
			Console.WriteLine("Updates finished, going to populate table.");
			if(progress != null)
			{
				//progress.Hide ();
                progress.Dismiss();
				progress.Dispose();
				progress = null;
			}				
		}

		/*		void HandleUpdateStarted(object sender, EventArgs e)
		{
			Utils.WriteLog("UPDATELOADING HandleUpdateStarted");
		}

		void HandleUpdateFinished(object sender, EventArgs e)
		{
			Utils.WriteLog("UPDATELOADING HandleUpdateFinished");
			RunOnUiThread(() => {
				if (progress != null)
					progress.Hide();
				PopulateTable();
			});
		}*/

		protected virtual void PopulateTable()
		{ 
		}
	}
}

