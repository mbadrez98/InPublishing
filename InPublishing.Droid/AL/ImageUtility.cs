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
using Android.Content.Res;
using System.Drawing;
using System.IO;
using Android.Opengl;
using Java.Nio;

namespace InPublishing
{
	class ImageUtility
	{
		public static int CalculateInSampleSize(BitmapFactory.Options options, int reqWidth, int reqHeight)
		{
			// Raw height and width of image
			float height = (float)options.OutHeight;
			float width = (float)options.OutWidth;
			double inSampleSize = 1D;

			/*if(reqWidth > 2048)
			{
				reqWidth = 2048;

				reqHeight *= (int)(width / reqWidth);
			}

			if(reqHeight > 2048)
			{
				reqHeight = 2048;
				reqWidth *= (int)(height / reqHeight);
			}*/

			if (height > reqHeight || width > reqWidth) 
			{
				inSampleSize = width > height
					? height / reqHeight
						: width / reqWidth;
			}

			return (int)inSampleSize;
		}

		public static Bitmap DecodeSampledBitmapFromResource(Resources res, int resId, int reqWidth, int reqHeight)
		{
			try
			{
				// First decode with inJustDecodeBounds=true to check dimensions
				BitmapFactory.Options options = new BitmapFactory.Options
				{
					InJustDecodeBounds = true,
					InPurgeable = true
				};
				BitmapFactory.DecodeResource(res, resId, options);

				// Calculate inSampleSize
				options.InSampleSize = CalculateInSampleSize(options, reqWidth, reqHeight);

				// Decode bitmap with inSampleSize set
				options.InJustDecodeBounds = false;
				return BitmapFactory.DecodeResource(res, resId, options);
			}
			catch(Exception ex)
			{
				Console.WriteLine("Errore immagine: " + ex.Message);
				return null;
			}
		}

		public static Bitmap DecodeSampledBitmapFromFile(string path, int reqWidth, int reqHeight)
		{
			try
			{
				// First decode with inJustDecodeBounds=true to check dimensions
				BitmapFactory.Options options = new BitmapFactory.Options
				{
					InJustDecodeBounds = true,
					InPurgeable = true
				};
				BitmapFactory.DecodeFile(path, options);

				// Calculate inSampleSize
				options.InSampleSize = CalculateInSampleSize(options, reqWidth, reqHeight);

				// Decode bitmap with inSampleSize set
				options.InJustDecodeBounds = false;
				return BitmapFactory.DecodeFile(path, options);
			}
			catch(Exception ex)
			{
				Console.WriteLine("Errore caricamento immagine: " + ex.Message);
				return null;
			}
		}

		public static Size GetBitmapSize(string path, bool limit = false)
		{
			const int MAX_SIZE = 2048;

			BitmapFactory.Options options = new BitmapFactory.Options
			{
				InJustDecodeBounds = true
			};

			// get the size and mime type of the image
			BitmapFactory.DecodeFile(path, options);
			int imageHeight = options.OutHeight;
			int imageWidth = options.OutWidth;

			/*if(limit)
			{
				if(imageWidth > MAX_SIZE || imageHeight > MAX_SIZE)
				{
					var ratioX = (float)MAX_SIZE / imageWidth;
					var ratioY = (float)MAX_SIZE / imageHeight;
					var ratio = Math.Min(ratioX, ratioY);

					imageHeight = (int)(imageHeight * ratio);
					imageWidth = (int)(imageWidth * ratio);
				}
			}*/

			return new Size(imageWidth, imageHeight);
		}

		public static string GetBitmapPath(string path, Context context = null)
		{
			//string newPath;
			if(File.Exists(path))
				return path;
			
			//System.Drawing.Size imgSize = ImageUtility.GetBitmapSize(path);

			//if(imgSize.Width > 2048 || imgSize.Height > 2048)
			{
				//Canvas a = new Canvas();

				//GL10..GlMaxTextureSize
				//int[] textureLimit = new int[1];
				//GLES10.GlGetIntegerv(GLES10.GlMaxTextureSize, textureLimit, 0); 

				/*if(context != null)
				{
					var dialog = new AlertDialog.Builder(context);
					dialog.SetTitle("Attenzione");
					dialog.SetMessage("In questa pagina sono presenti una o più immagini di dimensioni superiori alle massime consentite. Tali immagini sono state tagliate.");
					dialog.SetCancelable(true);
					dialog.SetPositiveButton("OK", delegate
						{
							return;
						});
					dialog.Create();
					dialog.Show();


				}*/

				FileInfo fi = new FileInfo(path);

				string newPath = System.IO.Path.Combine(fi.DirectoryName, fi.Name.Replace(fi.Extension, "") + "_AND" + fi.Extension);

				if(File.Exists(newPath))
				{
					path = newPath;
				}
			}

			return path;
		}
	}
}

