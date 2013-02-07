using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using TacticPlanner.models;

namespace TacticPlanner.gui {
	/// <summary>
	/// Interaction logic for AddTank.xaml
	/// </summary>
	public partial class AddTank : Window {
		private Tanks tanks;
		public bool dialogResult;

		public DynamicTank newtank;

		public AddTank(Tanks _tanks, DynamicTank _tank = null) {
			InitializeComponent();

			tanks = _tanks;

			if (_tank == null) {
				newtank = new DynamicTank();
				newtank.positions.Add(900, new Point(500, 500));
			} else {
				newtank = _tank;
				add.Content = "Modify";
			}

			dialogResult = false;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			string[] nations = tanks.getNations();

			foreach (string _nation in nations) {
				nation.Items.Add(_nation);
			}
			nation.SelectedIndex = 1;

			fillTankCombo();

			if (newtank.tank != null) {
				playername.Text = newtank.name;
				isAlly.IsChecked = newtank.isAlly;
				isEnemy.IsChecked = !newtank.isAlly;
				nation.SelectedItem = newtank.tank.nation;
				fillTankCombo();
				tank.SelectedItem = newtank.tank;
			}
		}

		private void fillTankCombo() {
			Tank[] tankList = tanks.getSortedTanks((string)nation.SelectedItem);
			tank.Items.Clear();
			foreach (Tank _tank in tankList) {
				tank.Items.Add(_tank);
			}
			tank.SelectedIndex = 0;
		}

		private void nation_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			fillTankCombo();
		}

		private void cancel_Click(object sender, RoutedEventArgs e) {
			Close();
		}

		private void add_Click(object sender, RoutedEventArgs e) {
			newtank.name = playername.Text;
			newtank.isAlly = (bool)isAlly.IsChecked;
			newtank.tank = tanks.getTank(((Tank)(tank.SelectedItem)).id);
			dialogResult = true;

			Close();
		}
	}
}
