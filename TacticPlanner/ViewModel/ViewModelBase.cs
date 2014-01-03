using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace TacticPlanner.ViewModel {

	/// <summary>
	/// Nézetmodellek absztrakt ősosztálya
	/// </summary>
	public abstract class ViewModelBase : INotifyPropertyChanged {

		/// <summary>
		/// Tulajdonság értékének megváltozása esetén lefutó esemény
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Tulajdonság értékének megváltoztatását jelző metódus
		/// </summary>
		protected ViewModelBase OnPropertyChanged(params Expression<Func<object>>[] expressions) {
			foreach (var exp in expressions)
				OnPropertyChanged<object>(exp);
			return this;
		}

		/// <summary>
		/// Tulajdonság értékének megváltoztatását jelző metódus
		/// </summary>
		protected ViewModelBase OnPropertyChanged<TPropertyType>(Expression<Func<TPropertyType>> expression) {
			var body = expression.Body as MemberExpression;
			if (body == null)
				throw new ArgumentException("Not supported expression!", "expression");

			return OnPropertyChanged(body.Member.Name);
		}

		/// <summary>
		/// Tulajdonság értékének megváltoztatását jelző metódus
		/// </summary>
		protected ViewModelBase OnPropertyChanged(params string[] propertyNames) {
			foreach (var prop in propertyNames)
				OnPropertyChanged(prop);
			return this;
		}

		/// <summary>
		/// Tulajdonság értékének megváltoztatását jelző metódus
		/// </summary>
		protected virtual ViewModelBase OnPropertyChanged(string propertyName) {
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
			return this;
		}

		/// <summary>
		/// Az osztályon belül lévő összes tulajdonság értékének frissítése
		/// </summary>
		public virtual void Refresh() {
			foreach (var propertyName in GetType().GetProperties().Select(prop => prop.Name))
				OnPropertyChanged(propertyName);
		}

	}
}
