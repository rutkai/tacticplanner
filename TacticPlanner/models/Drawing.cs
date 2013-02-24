using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;

namespace TacticPlanner.models {
	public class Drawing {
		private WriteableBitmap bmp;

		public Drawing(BitmapSource bmp) {
			this.bmp = BitmapFactory.ConvertToPbgra32Format(bmp);
		}

		public void drawLine(Point x, Point y, Pen pen, Color penColor) {
			DrawingGroup drawing = new DrawingGroup();
			drawing.Children.Add(new ImageDrawing(bmp, new Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight)));
			drawing.Children.Add(new GeometryDrawing(pen.Brush, pen, new LineGeometry(x, y)));
			drawing.ClipGeometry = new RectangleGeometry(new Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight));
			drawing.Freeze();
			var image = new Image { Source = new DrawingImage(drawing) };
			var bitmap = new RenderTargetBitmap(bmp.PixelWidth, bmp.PixelHeight, 96, 96, PixelFormats.Pbgra32);
			image.Arrange(new Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight));
			bitmap.Render(image);
			bmp = BitmapFactory.ConvertToPbgra32Format(bitmap);
		}

		public void drawArrow(Point x, Point y, Pen pen, Color penColor) {
			DrawingGroup drawing = new DrawingGroup();
			drawing.Children.Add(new ImageDrawing(bmp, new Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight)));
			drawing.Children.Add(new GeometryDrawing(pen.Brush, pen, Drawing.makeArrowGeometry(x, y, pen.Thickness)));
			drawing.ClipGeometry = new RectangleGeometry(new Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight));
			drawing.Freeze();
			var image = new Image { Source = new DrawingImage(drawing) };
			var bitmap = new RenderTargetBitmap(bmp.PixelWidth, bmp.PixelHeight, 96, 96, PixelFormats.Pbgra32);
			image.Arrange(new Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight));
			bitmap.Render(image);
			bmp = BitmapFactory.ConvertToPbgra32Format(bitmap);
		}

		public static Geometry makeArrowGeometry(Point x, Point y, double thickness) {
			double theta = Math.Atan2(x.Y - y.Y, x.X - y.X);
			double sint = Math.Sin(theta);
			double cost = Math.Cos(theta);
			double HeadWidth = thickness * 2;
			double HeadHeight = HeadWidth * 2.5;

			Point pt1 = new Point(
				y.X + (HeadHeight * cost - HeadWidth * sint),
				y.Y + (HeadHeight * sint + HeadWidth * cost));

			Point pt2 = new Point(
				y.X + (HeadHeight * cost + HeadWidth * sint),
				y.Y - (HeadWidth * cost - HeadHeight * sint));

			GeometryGroup group = new GeometryGroup();
			group.Children.Add(new LineGeometry(x, y));
			group.Children.Add(new LineGeometry(y, pt1));
			group.Children.Add(new LineGeometry(y, pt2));

			return group;
		}

		public void drawPoint(Point p, Pen pen, Color penColor) {
			if (bmp.IsFrozen) {
				bmp = bmp.Clone();
			}

			if (p.X + pen.Thickness / 2 > bmp.PixelWidth || p.Y + pen.Thickness / 2 > bmp.PixelHeight ||
				p.X - pen.Thickness / 2 < 0 || p.Y - pen.Thickness / 2 < 0) {
					return;
			}

			for (int i = 0; i < pen.Thickness / 2; ++i) {
				for (int j = 0; j < pen.Thickness / 2; ++j) {
					if (Math.Sqrt(Math.Pow(i, 2) + Math.Pow(j, 2)) < pen.Thickness / 2) {
						bmp.SetPixel((int)p.X + i, (int)p.Y + j, penColor);
						bmp.SetPixel((int)p.X - i, (int)p.Y + j, penColor);
						bmp.SetPixel((int)p.X + i, (int)p.Y - j, penColor);
						bmp.SetPixel((int)p.X - i, (int)p.Y - j, penColor);
					}
				}
			}
		}

		public void drawEraser(Point p, Pen pen) {
			if (bmp.IsFrozen) {
				bmp = bmp.Clone();
			}

			if (p.X + pen.Thickness / 2 > bmp.PixelWidth || p.Y + pen.Thickness / 2 > bmp.PixelHeight ||
				p.X - pen.Thickness / 2 < 0 || p.Y - pen.Thickness / 2 < 0) {
				return;
			}

			for (int i = 0; i < pen.Thickness / 2; ++i) {
				for (int j = 0; j < pen.Thickness / 2; ++j) {
					if (Math.Sqrt(Math.Pow(i, 2) + Math.Pow(j, 2)) < pen.Thickness / 2) {
						bmp.SetPixel((int)p.X + i, (int)p.Y + j, 0);
						bmp.SetPixel((int)p.X - i, (int)p.Y + j, 0);
						bmp.SetPixel((int)p.X + i, (int)p.Y - j, 0);
						bmp.SetPixel((int)p.X - i, (int)p.Y - j, 0);
					}
				}
			}
		}

		public void drawStamp(Point p, BitmapSource stamp, int size) {
			int stampWidth, stampHeight;
			if (stamp.PixelHeight > stamp.PixelWidth) {
				stampHeight = size;
				stampWidth = (int)((double)size * ((double)stamp.PixelWidth / (double)stamp.PixelHeight));
			} else {
				stampHeight = (int)((double)size * ((double)stamp.PixelHeight / (double)stamp.PixelWidth));
				stampWidth = size;
			}
			DrawingGroup drawing = new DrawingGroup();
			drawing.Children.Add(new ImageDrawing(bmp, new Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight)));
			drawing.Children.Add(new ImageDrawing(stamp, new Rect(p.X - stampWidth / 2, p.Y - stampHeight / 2, stampWidth, stampHeight)));
			drawing.ClipGeometry = new RectangleGeometry(new Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight));
			drawing.Freeze();
			var image = new Image { Source = new DrawingImage(drawing) };
			var bitmap = new RenderTargetBitmap(bmp.PixelWidth, bmp.PixelHeight, 96, 96, PixelFormats.Pbgra32);
			image.Arrange(new Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight));
			bitmap.Render(image);
			bmp = BitmapFactory.ConvertToPbgra32Format(bitmap);
		}

		public BitmapSource getImage() {
			return bmp;
		}

		#region ICloneable Members

		public Drawing Clone() {
			Drawing clone = new Drawing(bmp);

			return clone;
		}

		#endregion
	}
}
