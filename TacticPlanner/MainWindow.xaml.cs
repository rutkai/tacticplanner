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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

using TacticPlanner.gui;
using TacticPlanner.models;

namespace TacticPlanner {
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		private enum activeWindow {
			staticPanel,
			dynamicPanel,
			playPanel
		}

		private Tactic tactic;

		private BitmapImage stampImg;
		private PenDashStyle lineType = PenDashStyle.Solid;

		private bool move = false, draw = false, itemsMoved = false;
		private bool arrowToolChecked = false, lineToolChecked = false;
		private Point mouseFrom;
		private activeWindow window = activeWindow.staticPanel;

		private DispatcherTimer playTimer;

		private Briefing briefing;

		public MainWindow() {
			Splash splash = new Splash();
			splash.ShowDialog();

			try {
				tactic = new Tactic(System.AppDomain.CurrentDomain.BaseDirectory);
			} catch (Exception) {
				MessageBox.Show("Error: unable to load core files! Please reinstall the application.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				Application.Current.Shutdown();
			}

			briefing = new Briefing();

			playTimer = new DispatcherTimer();
			playTimer.Interval = new TimeSpan(100000);
			playTimer.IsEnabled = false;
			playTimer.Tick += new EventHandler(playTimer_Tick);

			InitializeComponent();

			stampImage.Source = BitmapSource.Create(100, 100, 96, 96, PixelFormats.BlackWhite, BitmapPalettes.BlackAndWhite, new byte[100 * 20], 20);

			briefingPanelGrid.IsEnabled = timePanelGrid.IsEnabled = staticPanelGrid.IsEnabled = dynamicPanelGrid.IsEnabled = playPanelGrid.IsEnabled = false;
			briefingPanel.Hide();

			foreach (Map map in tactic.getMaps()) {
				MenuItem newmap = new MenuItem();
				newmap.Header = map.name;
				newmap.Name = "newmapMenuItem_" + map.id;
				newmap.Click += newmapMenu_Click;
				newMenu.Items.Add(newmap);
			}

			List<StaticIcon> staticIcons = tactic.getStaticIcons();
			foreach (StaticIcon icon in staticIcons) {
				dynamicStaticList.Items.Add(icon);
			}
			dynamicStaticList.SelectedIndex = 0;

			List<DynamicIcon> dynamicIcons = tactic.getDynamicIcons();
			dynamicEvents.Items.Add(new DynamicIcon("", "(none)", ""));
			foreach (DynamicIcon icon in dynamicIcons) {
				dynamicEvents.Items.Add(icon);
			}
			dynamicEvents.SelectedIndex = 0;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			if (App.mArgs.Length > 0) {
				try {
					tactic.load(App.mArgs[0]);
					initFromTactic();
				} catch (Exception ex) {
					MessageBox.Show("Error: unable to load tactic file. Reason: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					drawBox.Source = null;
					mapBox.Source = null;
					return;
				}
			}
		}

		private void newmapMenu_Click(object sender, EventArgs e) {
			MenuItem senderObj = (MenuItem)sender;

			briefingPanelGrid.IsEnabled = timePanelGrid.IsEnabled = staticPanelGrid.IsEnabled = dynamicPanelGrid.IsEnabled = playPanelGrid.IsEnabled = false;

			try {
				tactic.newTactic((senderObj).Name.Split('_')[1]);
			} catch (Exception) {
				MessageBox.Show("Error: unable to initialize tactic!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			initFromTactic();
		}

		private void initFromTactic() {
			if (!tactic.isLoaded())
				return;

			mapBox.Source = tactic.getMap().getMapImage();

			timeBar.Value = 900;

			tactic.getStaticTactic().setPenColor(drawColor.SelectedColor);
			tactic.getStaticTactic().setDashStyle(getDashStyle());
			tactic.getStaticTactic().setThickness((int)drawThickness.Value);
			tactic.getDynamicTactic().setPenColor(dynamicTextColor.SelectedColor);

			tactic.getDynamicTactic().ShowTankName = menuShowTankType.IsChecked;
			tactic.getDynamicTactic().ShowPlayerName = menuShowPlayerName.IsChecked;

			briefingPanelGrid.IsEnabled = timePanelGrid.IsEnabled = staticPanelGrid.IsEnabled = dynamicPanelGrid.IsEnabled = playPanelGrid.IsEnabled = true;

			refreshNoTimer();

			dynamicTankList.Items.Clear();
			DynamicTank[] dynamicTanks = tactic.getDynamicTactic().getDynamicTanks();
			foreach (DynamicTank tank in dynamicTanks) {
				dynamicTankList.Items.Add(tank);
			}

			briefing.setTactic(tactic);

			refreshTime();
			refreshAddStatic();
			refreshDynamicAction();
			refreshMap();
		}

		private void refreshNoTimer() {
			if (!tactic.isLoaded()) {
				return;
			}

			if (window == activeWindow.staticPanel) {
				noTimer.IsChecked = !tactic.getStaticTactic().timer;
				timeBar.IsEnabled = tactic.getStaticTactic().timer;
			} else if (window == activeWindow.dynamicPanel) {
				noTimer.IsChecked = !tactic.getDynamicTactic().timer;
				timeBar.IsEnabled = tactic.getDynamicTactic().timer;
			} else {
				if ((bool)playStatic.IsChecked) {
					noTimer.IsChecked = !tactic.getStaticTactic().timer;
					timeBar.IsEnabled = tactic.getStaticTactic().timer;
				} else {
					noTimer.IsChecked = !tactic.getDynamicTactic().timer;
					timeBar.IsEnabled = tactic.getDynamicTactic().timer;
				}
			}
			if ((bool)noTimer.IsChecked) {
				timeBar.Value = 900;
				refreshTime();
			}
			refreshPlayControllers();
		}

		private void refreshPlayControllers() {
			if ((bool)noTimer.IsChecked) {
				playPlay.IsEnabled = playPause.IsEnabled = playStop.IsEnabled = false;
			} else {
				playPlay.IsEnabled = playPause.IsEnabled = playStop.IsEnabled = true;
			}
		}

		private void refreshMap() {
			if (tactic == null || !tactic.isLoaded())
				return;

			try {
				if (window == activeWindow.staticPanel) {
					refreshStatic();
				} else if (window == activeWindow.dynamicPanel) {
					refreshDynamic();
				} else {
					refreshPlay();
				}
			} catch (Exception) {
			    MessageBox.Show("Error: unable to render the map!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			    return;
			}
		}
		private void refreshStatic() {
			drawBox.Source = tactic.getStaticTactic().getTacticAt((int)timeBar.Value);
		}
		private void refreshDynamic() {
			drawBox.Source = tactic.getDynamicTactic().getTacticAt((int)timeBar.Value);

			refreshDynamicAction();
		}
		private void refreshPlay() {
			ImageSource newImg;
			if ((bool)playStatic.IsChecked) {
				newImg = tactic.getStaticTactic().getPlayTacticAt((int)timeBar.Value);
			} else {
				newImg = tactic.getDynamicTactic().getPlayTacticAt((int)timeBar.Value);
			}
			if (newImg != null) {
				drawBox.Source = newImg;
			}
		}

		private void refreshTime() {
			timeLabel.Content = ((int)timeBar.Value / 60).ToString("D2") + ":" + ((int)timeBar.Value % 60).ToString("D2");
		}

		private Point recalculate(Point orig) {
			int offset = Math.Abs((int)drawBox.Width - (int)drawBox.Height) / 2;

			if (drawBox.DesiredSize.Width > drawBox.DesiredSize.Height) {
				return new Point(
					((orig.X - offset) * 1024) / drawBox.DesiredSize.Height,
					(orig.Y * 1024) / drawBox.DesiredSize.Height
				);
			} else {
				return new Point(
					(orig.X * 1024) / drawBox.DesiredSize.Width,
					((orig.Y - offset) * 1024) / drawBox.DesiredSize.Width
				);
			}
		}

		private void exitMenu_Click(object sender, RoutedEventArgs e) {
			Application.Current.Shutdown();
		}

		private void aboutMenu_Click(object sender, RoutedEventArgs e) {
			AboutBox ab = new AboutBox();
			ab.ShowDialog();
		}

		private void drawColor_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e) {
			if (tactic == null || !tactic.isLoaded()) {
				return;
			}

			tactic.getStaticTactic().setPenColor(drawColor.SelectedColor);
		}

		private void dynamicTextColor_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e) {
			if (tactic == null || !tactic.isLoaded()) {
				return;
			}

			tactic.getDynamicTactic().setPenColor(dynamicTextColor.SelectedColor);
			playTextColor.SelectedColor = dynamicTextColor.SelectedColor;
			refreshMap();
		}

		private void playTextColor_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e) {
			if (tactic == null || !tactic.isLoaded()) {
				return;
			}

			tactic.getDynamicTactic().setPenColor(playTextColor.SelectedColor);
			dynamicTextColor.SelectedColor = playTextColor.SelectedColor;
			refreshMap();
		}

		private void LayoutAnchorable_IsActiveChanged(object sender, EventArgs e) {
			AvalonDock.Layout.LayoutAnchorable layout = (AvalonDock.Layout.LayoutAnchorable)sender;
			if (!layout.IsActive) {
				return;
			}

			switch (layout.Title) {
				case "Static panel":
					window = activeWindow.staticPanel;
					timeBar.Value = ((int)timeBar.Value / 30) * 30;
					timeBar.IsSnapToTickEnabled = true;
					break;
				case "Dynamic panel":
					window = activeWindow.dynamicPanel;
					timeBar.Value = ((int)timeBar.Value / 30) * 30;
					timeBar.IsSnapToTickEnabled = true;
					break;
				case "Play panel":
					window = activeWindow.playPanel;
					timeBar.IsSnapToTickEnabled = false;
					break;
			}

			refreshNoTimer();
			refreshTime();
			refreshMap();
		}

		private void resetStatic_Click(object sender, RoutedEventArgs e) {
			tactic.getStaticTactic().removeTactic((int)timeBar.Value);
			refreshMap();
		}

		private void cloneStatic_Click(object sender, RoutedEventArgs e) {
			if ((int)timeBar.Value == 900) {
				tactic.getStaticTactic().removeTactic((int)timeBar.Value);
			} else {
				tactic.getStaticTactic().cloneTactic((int)timeBar.Value + 30, (int)timeBar.Value);
			}
			refreshMap();
		}

		private void stampImage_MouseUp(object sender, MouseButtonEventArgs e) {
			Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
			ofd.InitialDirectory = "stamps";
			ofd.Filter = "Image file (*.jpg, *.jpeg, *.bmp, *.png)|*.jpg;*.jpeg;*.bmp;*.png";
			if ((bool)ofd.ShowDialog()) {
				try {
					stampImg = new BitmapImage(new Uri(ofd.FileName, UriKind.RelativeOrAbsolute));
				} catch (Exception) {
					MessageBox.Show("Cannot open file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

				stampImage.Source = stampImg;
			}
		}

		private void drawBox_MouseDown(object sender, MouseButtonEventArgs e) {
			mouseFrom = e.GetPosition(drawBox);

			if (!tactic.isLoaded()) {
				return;
			}

			if (window == activeWindow.staticPanel) {
				draw = true;
			} else if (window == activeWindow.dynamicPanel) {
				itemsMoved = tactic.getDynamicTactic().selectItem(recalculate(mouseFrom), Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift), (int)timeBar.Value);
				if (tactic.getDynamicTactic().hasSelectedItem()) {
					if (tactic.getDynamicTactic().isSelectedCopyable()) {
						menuCopyDynamic.IsEnabled = true;
					}
					move = true;
				} else {
					menuCopyDynamic.IsEnabled = false;
				}
			}
		}

		private void drawBox_MouseMove(object sender, MouseEventArgs e) {
			if (draw) {
				if ((bool)lineTool.IsChecked) {
					tactic.getStaticTactic().drawSampleLine(recalculate(mouseFrom), recalculate(e.GetPosition(drawBox)), (int)timeBar.Value);
				} else if ((bool)arrowTool.IsChecked) {
					tactic.getStaticTactic().drawSampleArrow(recalculate(mouseFrom), recalculate(e.GetPosition(drawBox)), (int)timeBar.Value);
				} else if ((bool)freeTool.IsChecked) {
					tactic.getStaticTactic().drawPoint(recalculate(e.GetPosition(drawBox)), (int)timeBar.Value);
				} else if ((bool)eraseTool.IsChecked) {
					tactic.getStaticTactic().drawEraserPoint(recalculate(e.GetPosition(drawBox)), (int)timeBar.Value);
				} else if ((bool)stampTool.IsChecked && stampImg != null) {
					tactic.getStaticTactic().drawSampleStamp(recalculate(e.GetPosition(drawBox)), stampImg, (int)stampSize.Value, (int)timeBar.Value);
				}
			}

			if (move) {
				itemsMoved = true;
				tactic.getDynamicTactic().moveItem(recalculate(mouseFrom), recalculate(e.GetPosition(drawBox)), (int)timeBar.Value);
				mouseFrom = e.GetPosition(drawBox);
			}

			if (move || draw) {
				refreshMap();
			}
		}

		private void drawBox_MouseUp(object sender, MouseButtonEventArgs e) {
			if (window == activeWindow.staticPanel) {
				if (!draw)
					return;

				draw = false;
				if ((bool)lineTool.IsChecked) {
					tactic.getStaticTactic().drawLine(recalculate(mouseFrom), recalculate(e.GetPosition(drawBox)), (int)timeBar.Value);
				} else if ((bool)arrowTool.IsChecked) {
					tactic.getStaticTactic().drawArrow(recalculate(mouseFrom), recalculate(e.GetPosition(drawBox)), (int)timeBar.Value);
				} else if ((bool)stampTool.IsChecked && stampImg != null) {
					tactic.getStaticTactic().drawStamp(recalculate(e.GetPosition(drawBox)), stampImg, (int)stampSize.Value, (int)timeBar.Value);
					draw = true;
				} else if ((bool)freeTool.IsChecked) {
					tactic.getStaticTactic().drawPoint(recalculate(e.GetPosition(drawBox)), (int)timeBar.Value);
				} else if ((bool)eraseTool.IsChecked) {
					tactic.getStaticTactic().drawEraserPoint(recalculate(e.GetPosition(drawBox)), (int)timeBar.Value);
				}
			} else if (window == activeWindow.dynamicPanel) {
				if (!itemsMoved && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))) {
					tactic.getDynamicTactic().deselectItem(recalculate(mouseFrom), (int)timeBar.Value);
				}
				move = false;
			}

			refreshMap();
		}

		private void drawBox_MouseEnter(object sender, MouseEventArgs e) {
			if ((bool)stampTool.IsChecked && stampImg != null) {
				draw = true;
			}
		}

		private void drawBox_MouseLeave(object sender, MouseEventArgs e) {
			move = draw = false;
			tactic.getStaticTactic().removeSamples();

			refreshMap();
		}

		private void lineTool_Click(object sender, RoutedEventArgs e) {
			if (lineToolChecked) {
				rotateToolTypes();
			} else {
				lineToolChecked = true;
			}
			arrowToolChecked = false;
		}

		private void arrowTool_Click(object sender, RoutedEventArgs e) {
			if (arrowToolChecked) {
				rotateToolTypes();
			} else {
				arrowToolChecked = true;
			}
			lineToolChecked = false;
		}

		private void rotateToolTypes() {
            switch (lineType) {
                case PenDashStyle.Solid:
					lineType = PenDashStyle.Dotted;
                    arrowTool.Content = "Arrow tool: Dotted";
					lineTool.Content = "Line tool: Dotted";
                    break;
				case PenDashStyle.Dotted:
					lineType = PenDashStyle.Dash;
					arrowTool.Content = "Arrow tool: Dash";
					lineTool.Content = "Line tool: Dash";
                    break;
				case PenDashStyle.Dash:
					lineType = PenDashStyle.Solid;
					arrowTool.Content = "Arrow tool: Solid";
					lineTool.Content = "Line tool: Solid";
                    break;
            }
			tactic.getStaticTactic().setDashStyle(getDashStyle());
        }

		private DashStyle getDashStyle() {
			DashStyle style = null;
			switch (lineType) {
				case PenDashStyle.Solid:
					style = DashStyles.Solid;
					break;
				case PenDashStyle.Dotted:
					style = DashStyles.Dot;
					break;
				case PenDashStyle.Dash:
					style = DashStyles.Dash;
					break;
			}
			return style;
		}

		private void cursorTool_Click(object sender, RoutedEventArgs e) {
			lineToolChecked = arrowToolChecked = false;
		}

		private void freeTool_Click(object sender, RoutedEventArgs e) {
			lineToolChecked = arrowToolChecked = false;
		}

		private void stampTool_Click(object sender, RoutedEventArgs e) {
			lineToolChecked = arrowToolChecked = false;
		}

		private void eraseTool_Click(object sender, RoutedEventArgs e) {
			lineToolChecked = arrowToolChecked = false;
		}

		private void noTimer_Changed(object sender, RoutedEventArgs e) {
			if (window == activeWindow.staticPanel) {
				tactic.getStaticTactic().timer = !(bool)noTimer.IsChecked;
			} else if (window == activeWindow.dynamicPanel) {
				tactic.getDynamicTactic().timer = !(bool)noTimer.IsChecked;
			}
			refreshNoTimer();
		}

		private void timeBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			refreshTime();
			refreshMap();
		}

		private void playMode_Changed(object sender, RoutedEventArgs e) {
			try {
				refreshNoTimer();
			} catch (NullReferenceException) { }
			refreshMap();
		}

		private void playPlay_Click(object sender, RoutedEventArgs e) {
			if (timeBar.Value == 0) {
				return;
			}

			playTimer.IsEnabled = true;
		}

		private void playPause_Click(object sender, RoutedEventArgs e) {
			playTimer.IsEnabled = false;
		}

		private void playStop_Click(object sender, RoutedEventArgs e) {
			playTimer.IsEnabled = false;
			timeBar.Value = 900;
			refreshTime();
			refreshMap();
		}

		private void playSpeed_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			playTimer.Interval = new TimeSpan(1000000 / (int)playSpeed.Value);
		}

		void playTimer_Tick(object sender, EventArgs e) {
			if (timeBar.Value == 0) {
				playTimer.IsEnabled = false;
				return;
			}

			timeBar.Value -= 1;
			refreshTime();
			refreshMap();
		}

		private void addStatic_Click(object sender, RoutedEventArgs e) {
			if (tactic.getDynamicTactic().hasStaticElement((StaticIcon)dynamicStaticList.SelectedItem)) {
				tactic.getDynamicTactic().removeStaticElement((StaticIcon)dynamicStaticList.SelectedItem);
			} else {
				tactic.getDynamicTactic().addStaticElement((StaticIcon)dynamicStaticList.SelectedItem);
			}

			refreshAddStatic();
			refreshMap();
		}

		private void dynamicStaticList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			refreshAddStatic();
		}

		private void refreshAddStatic() {
			if (tactic != null && tactic.isLoaded()) {
				if (tactic.getDynamicTactic().hasStaticElement((StaticIcon)dynamicStaticList.SelectedItem)) {
					addStatic.Content = "Remove static element";
				} else {
					addStatic.Content = "Add static element";
				}
			}
		}

		private void addTank_Click(object sender, RoutedEventArgs e) {
			AddTank addTankWindow = new AddTank(tactic.getTanks());
			addTankWindow.ShowDialog();

			if (!addTankWindow.dialogResult) {
				return;
			}

			tactic.getDynamicTactic().addDynamicTank(addTankWindow.newtank);
			dynamicTankList.Items.Add(addTankWindow.newtank);

			refreshMap();
		}

		private void editTank_Click(object sender, RoutedEventArgs e) {
			if (dynamicTankList.SelectedItem == null) {
				return;
			}

			AddTank addTankWindow = new AddTank(tactic.getTanks(), (DynamicTank)dynamicTankList.SelectedItem);
			addTankWindow.ShowDialog();

			if (!addTankWindow.dialogResult) {
				return;
			}

			dynamicTankList.Items.Refresh();

			refreshMap();
		}

		private void removeTank_Click(object sender, RoutedEventArgs e) {
			if (dynamicTankList.SelectedItem == null) {
				return;
			}

			tactic.getDynamicTactic().removeDynamicTank((DynamicTank)dynamicTankList.SelectedItem);
			dynamicTankList.Items.Remove(dynamicTankList.SelectedItem);

			refreshMap();
		}

		private void dynamicTankList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			refreshDynamicAction();
		}

		private void refreshDynamicAction() {
			if (dynamicTankList.SelectedItems.Count == 0) {
				editTank.IsEnabled = removeTank.IsEnabled = tankAliveStatus.IsEnabled = delTankCurrentPosition.IsEnabled = dynamicEvents.IsEnabled = false;
			} else {
				editTank.IsEnabled = removeTank.IsEnabled = tankAliveStatus.IsEnabled = delTankCurrentPosition.IsEnabled = dynamicEvents.IsEnabled = true;

				string actionId = tactic.getDynamicTactic().getDynamicTankActionId((DynamicTank)dynamicTankList.SelectedItem, (int)timeBar.Value);
				if (actionId == "") {
					dynamicEvents.SelectedIndex = 0;
				} else {
					dynamicEvents.SelectedItem = tactic.getIcons().getDynamicIcon(actionId);
				}

				bool alive = tactic.getDynamicTactic().isAlive((DynamicTank)dynamicTankList.SelectedItem, (int)timeBar.Value);
				if (alive) {
					tankAliveStatus.Content = "Alive";
				} else {
					tankAliveStatus.Content = "Dead";
				}
			}
		}

		private void tankAliveStatus_Click(object sender, RoutedEventArgs e) {
			if (dynamicTankList.SelectedItem == null) {
				return;
			}

			if ((string)tankAliveStatus.Content == "Alive") {
				tactic.getDynamicTactic().setKill((DynamicTank)dynamicTankList.SelectedItem, (int)timeBar.Value);
			} else {
				tactic.getDynamicTactic().setKill((DynamicTank)dynamicTankList.SelectedItem, -1);
			}

			refreshDynamicAction();
			refreshMap();
		}

		private void delTankCurrentPosition_Click(object sender, RoutedEventArgs e) {
			if (dynamicTankList.SelectedItem == null) {
				return;
			}

			tactic.getDynamicTactic().removeDynamicPosition((DynamicTank)dynamicTankList.SelectedItem, (int)timeBar.Value);

			refreshMap();
		}

		private void dynamicEvents_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (dynamicTankList.SelectedItem == null) {
				return;
			}

			tactic.getDynamicTactic().setDynamicTankAction((DynamicTank)dynamicTankList.SelectedItem, (int)timeBar.Value, ((DynamicIcon)dynamicEvents.SelectedItem).id);

			refreshMap();
		}

		private void iconSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			if (tactic == null || !tactic.isLoaded()) {
				return;
			}

			tactic.getDynamicTactic().setIconsSize((int)iconSize.Value);
			playIconSize.Value = iconSize.Value;

			refreshMap();
		}

		private void playIconSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			if (tactic == null || !tactic.isLoaded()) {
				return;
			}

			tactic.getDynamicTactic().setIconsSize((int)playIconSize.Value);
			iconSize.Value = playIconSize.Value;

			//refreshMap();  <-- a másik frissít
		}

		private void menuShowPlayerName_Changed(object sender, RoutedEventArgs e) {
			if (tactic == null || !tactic.isLoaded()) {
				return;
			}

			tactic.getDynamicTactic().ShowPlayerName = menuShowPlayerName.IsChecked;
			refreshMap();
		}

		private void menuShowTankType_Changed(object sender, RoutedEventArgs e) {
			tactic.getDynamicTactic().ShowTankName = menuShowTankType.IsChecked;
			refreshMap();
		}

		private void drawThickness_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			if (tactic == null || !tactic.isLoaded()) {
				return;
			}

			tactic.getStaticTactic().setThickness((int)drawThickness.Value);
		}

		private void menuMappackOriginal_Checked(object sender, RoutedEventArgs e) {
			if (menuMappackHD == null) {
				return;
			}

			menuMappackHD.IsChecked = false;
			refreshMapPack();
		}

		private void menuMappackHD_Checked(object sender, RoutedEventArgs e) {
			if (menuMappackOriginal == null) {
				return;
			}

			menuMappackOriginal.IsChecked = false;
			refreshMapPack();
		}

		private void refreshMapPack() {
			if (menuMappackOriginal.IsChecked) {
				tactic.setMapPack(types.MapPack.Original);
			} else {
				tactic.setMapPack(types.MapPack.HD);
			}

			if (tactic.isLoaded()) {
				mapBox.Source = tactic.getMap().getMapImage();
				refreshMap();
			}
		}

		private void menuShowGrid_Checked(object sender, RoutedEventArgs e) {
			var source = new Uri(@"pack://application:,,,/Resources/grid.png", UriKind.Absolute);
			gridBox.Source = new BitmapImage(source);  
		}

		private void menuShowGrid_Unchecked(object sender, RoutedEventArgs e) {
			gridBox.Source = null;
		}

		private void menuShowGrid_Toggle(object sender, RoutedEventArgs e) {
			menuShowGrid.IsChecked = !menuShowGrid.IsChecked;
		}

		private void menuSave_Click(object sender, RoutedEventArgs e) {
			if (tactic == null || !tactic.isLoaded())
				return;

			Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
			sfd.Filter = "Tactic planner file (*.tactic)|*.tactic";
			if ((bool)sfd.ShowDialog()) {
				try {
					tactic.save(sfd.FileName);
				} catch (Exception ex) {
					MessageBox.Show("Error: unable to save tactic file. Reason: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}
			}
		}

		private void menuLoad_Click(object sender, RoutedEventArgs e) {
			briefingPanelGrid.IsEnabled = timePanelGrid.IsEnabled = staticPanelGrid.IsEnabled = dynamicPanelGrid.IsEnabled = playPanelGrid.IsEnabled = false;

			Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
			ofd.InitialDirectory = "stamps";
			ofd.Filter = "Tactic planner file (*.tactic)|*.tactic";
			if ((bool)ofd.ShowDialog()) {
				try {
					tactic.load(ofd.FileName);
				} catch (Exception ex) {
					MessageBox.Show("Error: unable to load tactic file. Reason: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					drawBox.Source = null;
					mapBox.Source = null;
					return;
				}
			}

			initFromTactic();
		}

		private void menuExport_Click(object sender, RoutedEventArgs e) {
			if (tactic == null || !tactic.isLoaded())
				return;

			Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
			sfd.Filter = "Portable Network Graphic file (*.png)|*.png|Joint Photographic Experts Group file (*.jpg)|*.jpg|Bitmap (*.bmp)|*.bmp";
			if ((bool)sfd.ShowDialog()) {
				try {
					DrawingVisual dv = new DrawingVisual();
					DrawingContext dc = dv.RenderOpen();
					dc.DrawImage(mapBox.Source, new Rect(0, 0, 1024, 1024));
					dc.DrawImage(drawBox.Source, new Rect(0, 0, 1024, 1024));
					dc.Close();
					RenderTargetBitmap renderer = new RenderTargetBitmap(1024, 1024, 96, 96, PixelFormats.Default);
					renderer.Render(dv);
					System.IO.Stream imageStreamSource = new System.IO.FileStream(sfd.FileName, System.IO.FileMode.Create);
					BitmapEncoder encoder;
					switch (System.IO.Path.GetExtension(sfd.SafeFileName).ToLowerInvariant()) {
						case ".png":
							encoder = new PngBitmapEncoder();
							break;
						case ".jpg":
							encoder = new JpegBitmapEncoder();
							break;
						case ".bmp":
							encoder = new BmpBitmapEncoder();
							break;
						default:
							MessageBox.Show("Error: unknown file type.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
							return;
					}
					encoder.Frames.Add(BitmapFrame.Create(renderer));
					encoder.Save(imageStreamSource);
					imageStreamSource.Close();
				} catch (Exception ex) {
					MessageBox.Show("Error: unable to save image file. Reason: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}
			}
		}

		private void menuShowBriefing_Checked(object sender, RoutedEventArgs e) {
			briefingPanel.Show();
		}

		private void menuShowBriefing_Unchecked(object sender, RoutedEventArgs e) {
			briefingPanel.Hide();
		}

		private void menuShowBriefing_Toggle(object sender, RoutedEventArgs e) {
			menuShowBriefing.IsChecked = !menuShowBriefing.IsChecked;
		}

		private void openServer_Click(object sender, RoutedEventArgs e) {
			try {
				briefing.openServer(nick.Text, Convert.ToInt32(port.Text));
			} catch (Exception ex) {
				MessageBox.Show("Error: unable to start server. Reason: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			host.Text = briefing.getMyIp();

			nick.IsEnabled = port.IsEnabled = host.IsEnabled = false;
			kick.IsEnabled = true;
			openServer.Visibility = connect.Visibility = System.Windows.Visibility.Hidden;
			disconnect.Visibility = System.Windows.Visibility.Visible;
		}

		private void connect_Click(object sender, RoutedEventArgs e) {
			if (nick.Text == "") {
				MessageBox.Show("You must enter your nick!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			try {
				briefing.connect(nick.Text, host.Text, Convert.ToInt32(port.Text), password.Password);
			} catch (Exception ex) {
				MessageBox.Show("Error: unable to connect to server. Reason: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			nick.IsEnabled = clientsCanDraw.IsEnabled = clientsCanPing.IsEnabled = password.IsEnabled = port.IsEnabled = host.IsEnabled = false;
			openServer.Visibility = connect.Visibility = System.Windows.Visibility.Hidden;
			disconnect.Visibility = System.Windows.Visibility.Visible;
		}

		private void disconnect_Click(object sender, RoutedEventArgs e) {
			briefing.disconnect();

			nick.IsEnabled = clientsCanDraw.IsEnabled = clientsCanPing.IsEnabled = password.IsEnabled = port.IsEnabled = host.IsEnabled = true;
			kick.IsEnabled = false;
			openServer.Visibility = connect.Visibility = System.Windows.Visibility.Visible;
			disconnect.Visibility = System.Windows.Visibility.Hidden;
		}

		private void password_PasswordChanged(object sender, RoutedEventArgs e) {
			briefing.setPassword(password.Password);
		}

		private void clientsCanPing_Checked(object sender, RoutedEventArgs e) {
			briefing.enableClientsPing();
		}

		private void clientsCanPing_Unchecked(object sender, RoutedEventArgs e) {
			briefing.disableClientsPing();
		}

		private void clientsCanDraw_Checked(object sender, RoutedEventArgs e) {
			briefing.enableClientsDraw();
		}

		private void clientsCanDraw_Unchecked(object sender, RoutedEventArgs e) {
			briefing.disableClientsDraw();
		}

		private void kick_Click(object sender, RoutedEventArgs e) {
			if (clientList.SelectedItem == null) {
				return;
			}


		}

		private void menuCopyDynamic_Click(object sender, RoutedEventArgs e) {
			if (tactic.getDynamicTactic().hasSelectedItem()) {
				tactic.getDynamicTactic().copySelected();
				menuPasteDynamic.IsEnabled = true;
			}
		}

		private void menuPasteDynamic_Click(object sender, RoutedEventArgs e) {
			tactic.getDynamicTactic().paste();
			refreshMap();
		}
	}

	public static class MenuCommands {
		public static RoutedCommand ToggleGrid = new RoutedCommand();
		public static RoutedCommand Export = new RoutedCommand();
		public static RoutedCommand ToggleBriefing = new RoutedCommand();
	}
}
