using System;
using Android.Graphics;

namespace InPublishing
{
	public class Border : Android.Graphics.Drawables.Drawable  
	{  
		public Paint paint;  
		public Rect bounds_rect;  

		public Border(string colour, float width)  
		{  
			this.paint = new Paint();  
			//this.paint.setColor(colour);

			this.paint.Color = Color.Transparent.FromHex(colour);

			this.paint.StrokeWidth = width;  
			this.paint.SetStyle(Paint.Style.Stroke);  
		}  
			
		protected override void OnBoundsChange(Rect bounds)
		{
			this.bounds_rect = bounds; 
		}

		public override void Draw(Canvas canvas)
		{
			canvas.DrawRect(this.bounds_rect, this.paint);  
		}

		public override void SetAlpha(int alpha)
		{
			// TODO: Implement this method 
			throw new NotImplementedException();
		}

		public override void SetColorFilter(ColorFilter cf)
		{
			// TODO: Implement this method 
			throw new NotImplementedException();
		}

		public override int Opacity
		{
			get
			{
				// TODO: Implement this method  
				return 0;  
			}
		}
	}     
}

