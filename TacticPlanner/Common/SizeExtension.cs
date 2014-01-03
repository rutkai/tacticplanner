using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace TacticPlanner.Common {
	public static class SizeExtension {
		public static Size Add(this Size size, Size value) {
			return new Size(size.Width + value.Width, size.Height + value.Height);
		}

		public static Size Add(this Size size, double scalar) {
			return size.Add(scalar, scalar);
		}

		public static Size Add(this Size size, double width, double height) {
			return new Size(size.Width + width, size.Height + height);
		}

		public static Size Multiply(this Size size, Size value) {
			return new Size(size.Width * value.Width, size.Height * value.Height);
		}

		public static Size Multiply(this Size size, double scalar) {
			return size.Multiply(scalar, scalar);
		}

		public static Size Multiply(this Size size, double width, double height) {
			return new Size(size.Width * width, size.Height * height);
		}
	}
}
