using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TacticPlanner.Common {
	public static class Lazy {
		public static TType Init<TType>(ref TType that, Func<TType> initialization)
			where TType : class {
			if (that == null)
				that = initialization();
			return that;
		}
	}
}
