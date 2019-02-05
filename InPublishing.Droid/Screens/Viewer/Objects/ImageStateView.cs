using System;
using Android.Widget;
using Android.Content;
using System.IO;
using Android.Graphics;
using Android.App;
using Android.OS;

namespace InPublishing
{
    public class ImageStateView : ImageView
    {
        private Image _immagine;
        private string _path;
        public int State = 0;
        RectF _frame;
        Context _context;

        public ImageStateView(Context context, string basePath, Image image, RectF frame) : base(context)
        {
            _immagine = image;
            _path = basePath;
            _frame = frame;
            _context = context;

            LoadImage();
        }

        private string GetPathForState()
        {
            string path = "";

            switch (this.State)
            {
                case 1:
                    path = System.IO.Path.Combine(_path, _immagine.LinkPressed);
                    break;
                case 0:                    
                default:
                    path = System.IO.Path.Combine(_path, _immagine.Link);
                    break;
            }

            return System.IO.Path.Combine(_path, path);
        }

        public void SetState(int state = 0)
        {
            this.State = state;
            this.LoadImage();
        }

        private void LoadImage()
        {
            string path = GetPathForState();

            this.RecycleBitmap();                

            if (!File.Exists(path))
            {       
                this.SetImageBitmap(null);
            }
            else
            {
                MBImageLoader.DisplayDiskImage(path, this, new PointF(_frame.Width(), _frame.Height()));               
            }        
        }
    }
}

