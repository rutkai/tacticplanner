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
using TacticPlanner.controllers;

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

		private TacticsController tactics;

		private BitmapImage stampImg;
		private PenDashStyle lineType = PenDashStyle.Solid;

		private bool move = false, draw = false, itemsMoved = false;
		private bool arrowToolChecked = false, lineToolChecked = false;
		private Point mouseFrom;
		private activeWindow window = activeWindow.staticPanel;

		private DispatcherTimer playTimer;

		public MainWindow() {
			Splash splash = new Splash();
			splash.ShowDialog();

			try {
				tactics = new TacticsController(this, System.AppDomain.CurrentDomain.BaseDirectory);
			} catch (Exception) {
				MessageBox.Show("Error: unable to load core files! Please reinstall the application.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				Application.Current.Shutdown();
			}

			playTimer = new DispatcherTimer();
			playTimer.Interval = new TimeSpan(100000);
			playTimer.IsEnabled = false;
			playTimer.Tick += new EventHandler(playTimer_Tick);

			InitializeComponent();

			stampImage.Source = BitmapSource.Create(100, 100, 96, 96, PixelFormats.BlackWhite, BitmapPalettes.BlackAndWhite, new byte[100 * 20], 20);

			briefingPanelGrid.IsEnabled = timePanelGrid.IsEnabled = staticPanelGrid.IsEnabled = dynamicPanelGrid.IsEnabled = playPanelGrid.IsEnabled = false;
			briefingPanel.Hide();

			foreach (Map map in tactics.getMaps()) {
				MenuItem newmap = new MenuItem();
				newmap.Header = map.name;
				newmap.Name = "newmapMenuItem_" + map.id;
				newmap.Click += newmapMenu_Click;
				newMenu.Items.Add(newmap);
			}

			List<StaticIcon> staticIcons = tactics.getStaticIcons();
			foreach (StaticIcon icon in staticIcons) {
				dynamicStaticList.Items.Add(icon);
			}
			dynamicStaticList.SelectedIndex = 0;

			List<DynamicIcon> dynamicIcons = tactics.getDynamicIcons();
			dynamicEvents.Items.Add(new DynamicIcon("", "(none)", ""));
			foreach (DynamicIcon icon in dynamicIcons) {
				dynamicEvents.Items.Add(icon);
			}
			dynamicEvents.SelectedIndex = 0;
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			if (App.mArgs.Length > 0) {
				try {
					tactics.load(App.mArgs[0]);
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
				tactics.add((senderObj).Name.Split('_')[1]);
			} catch (Exception) {
				MessageBox.Show("Error: unable to initialize tactic!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
		}

		public void initFromTactic() {
			if (!tactics.isLoaded()) {
				return;
			}

			mapBox.Source = tactics.getMap().getMapImage();

			timeBar.Value = 900;

			tactics.setDynamicPenColor(dynamicTextColor.SelectedColor);

			tactics.setShowTankName(menuShowTankType.IsChecked);
			tactics.setShowPlayerName(menuShowPlayerName.IsChecked);

			briefingPanelGrid.IsEnabled = timePanelGrid.IsEnabled = staticPanelGrid.IsEnabled = dynamicPanelGrid.IsEnabled = playPanelGrid.IsEnabled = true;

			dynamicTankList.Items.Clear();
			DynamicTank[] dynamicTanks = tactics.getTanks();
			foreach (DynamicTank tank in dynamicTanks) {
				dynamicTankList.Items.Add(tank);
			}

			refreshNoTimer();
			refreshTime();
			refreshDynamicPanel();
			refreshMap();
		}

		public void refreshNoTimer() {
			if (!tactics.isLoaded()) {
				return;
			}

			if (window == activeWindow.staticPanel) {
				noTimer.IsChecked = !tactics.hasStaticTimer();
				timeBar.IsEnabled = tactics.hasStaticTimer();
			} else if (window == activeWindow.dynamicPanel) {
				noTimer.IsChecked = !tactics.hasDynamicTimer();
				timeBar.IsEnabled = tactics.hasDynamicTimer();
			} else {
				if ((bool)playStatic.IsChecked) {
					noTimer.IsChecked = !tactics.hasStaticTimer();
					timeBar.IsEnabled = tactics.hasStaticTimer();
				} else {
					noTimer.IsChecked = !tactics.hasDynamicTimer();
					timeBar.IsEnabled = tactics.hasDynamicTimer();
				}
			}
			if ((bool)noTimer.IsChecked) {
				timeBar.Value = 900;
				refreshTime();

				playPlay.IsEnabled = playPause.IsEnabled = playStop.IsEnabled = false;
			} else {
				playPlay.IsEnabled = playPause.IsEnabled = playStop.IsEnabled = true;
			}
		}

		public void refreshDynamicPanel() {
			if (dynamicTankList.SelectedItems.Count == 0) {
				editTank.IsEnabled = removeTank.IsEnabled = tankAliveStatus.IsEnabled = delTankCurrentPosition.IsEnabled = dynamicEvents.IsEnabled = false;
			} else {
				editTank.IsEnabled = removeTank.IsEnabled = tankAliveStatus.IsEnabled = delTankCurrentPosition.IsEnabled = dynamicEvents.IsEnabled = true;

				string actionId = tactics.getTankActionId((DynamicTank)dynamicTankList.SelectedItem, (int)timeBar.Value);
				if (actionId == "") {
					dynamicEvents.SelectedIndex = 0;
				} else {
					dynamicEvents.SelectedItem = tactics.getDynamicIcon(actionId);
				}

				bool alive = tactics.isAlive((DynamicTank)dynamicTankList.SelectedItem, (int)timeBar.Value);
				if (alive) {
					tankAliveStatus.Content = "Alive";
				} else {
					tankAliveStatus.Content = "Dead";
				}
			}

			if (tactics.isLoaded()) {
				if (tactics.hasStaticIcon((StaticIcon)dynamicStaticList.SelectedItem)) {
					addStatic.Content = "Remove static element";
				} else {
					addStatic.Content = "Add static element";
				}
			}
		}

		public void refreshTime() {
			timeLabel.Content = ((int)timeBar.Value / 60).ToString("D2") + ":" + ((int)timeBar.Value % 60).ToString("D2");
		}

		private void refreshMapPack() {
			if (menuMappackOriginal.IsChecked) {
				tactics.setMapPack(types.MapPack.Original);
			} else {
				tactics.setMapPack(types.MapPack.HD);
			}

			if (tactics.isLoaded()) {
				mapBox.Source = tactics.getMap().getMapImage();
				refreshMap();
			}
		}

		public void refreshMap() {
			if (!tactics.isLoaded()) {
				return;
			}

			try {
				if (window == activeWindow.staticPanel) {
					drawBox.Source = tactics.getStaticImage((int)timeBar.Value);
				} else if (window == activeWindow.dynamicPanel) {
					drawBox.Source = tactics.getDynamicImage((int)timeBar.Value);
					refreshDynamicPanel();
				} else {
					if ((bool)playStatic.IsChecked) {
						drawBox.Source = tactics.getStaticPlayImage((int)timeBar.Value);
					} else {
						drawBox.Source = tactics.getDynamicPlayImage((int)timeBar.Value);
					}
				}
			} catch (Exception) {
			    MessageBox.Show("Error: unable to render the map!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
			    return;
			}
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

		private void dynamicTextColor_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e) {
			if (!tactics.isLoaded()) {
				return;
			}

			tactics.setDynamicPenColor(dynamicTextColor.SelectedColor);
			playTextColor.SelectedColor = dynamicTextColor.SelectedColor;
		}

		private void playTextColor_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e) {
			if (!tactics.isLoaded()) {
				return;
			}

			tactics.setDynamicPenColor(playTextColor.SelectedColor);
			dynamicTextColor.SelectedColor = playTextColor.SelectedColor;
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
			tactics.removeDraw((int)timeBar.Value);
		}

		private void cloneStatic_Click(object sender, RoutedEventArgs e) {
			if ((int)timeBar.Value == 900) {
				tactics.removeDraw((int)timeBar.Value);
			} else {
				tactics.cloneTactic((int)timeBar.Value + 30, (int)timeBar.Value);
			}
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

			if (!tactics.isLoaded()) {
				return;
			}

			if (window == activeWindow.staticPanel) {
				draw = true;
			} else if (window == activeWindow.dynamicPanel) {
				itemsMoved = tactics.selectIcon(recalculate(mouseFrom), Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift), (int)timeBar.Value);
				if (tactics.hasSelectedIcon()) {
					if (tactics.isSelectedCopyable()) {
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
					tactics.drawSampleLine(recalculate(mouseFrom), recalculate(e.GetPosition(drawBox)), drawColor.SelectedColor, (int)drawThickness.Value, getDashStyle(), (int)timeBar.Value);
				} else if ((bool)arrowTool.IsChecked) {
					tactics.drawSampleArrow(recalculate(mouseFrom), recalculate(e.GetPosition(drawBox)), drawColor.SelectedColor, (int)drawThickness.Value, getDashStyle(), (int)timeBar.Value);
				} else if ((bool)freeTool.IsChecked) {
					tactics.drawPoint(recalculate(e.GetPosition(drawBox)), drawColor.SelectedColor, (int)drawThickness.Value, (int)timeBar.Value);
				} else if ((bool)eraseTool.IsChecked) {
					tactics.drawEraserPoint(recalculate(e.GetPosition(drawBox)), (int)drawThickness.Value, (int)timeBar.Value);
				} else if ((bool)stampTool.IsChecked && stampImg != null) {
					tactics.drawSampleStamp(recalculate(e.GetPosition(drawBox)), stampImg, (int)stampSize.Value, (int)timeBar.Value);
				}
			}

			if (move) {
				itemsMoved = true;
				tactics.moveIcons(recalculate(mouseFrom), recalculate(e.GetPosition(drawBox)), (int)timeBar.Value);
				mouseFrom = e.GetPosition(drawBox);
			}
		}

		private void drawBox_MouseUp(object sender, MouseButtonEventArgs e) {
			if (window == activeWindow.staticPanel) {
				if (!draw) {
					return;
				}

				draw = false;
				if ((bool)lineTool.IsChecked) {
					tactics.drawLine(recalculate(mouseFrom), recalculate(e.GetPosition(drawBox)), drawColor.SelectedColor, (int)drawThickness.Value, getDashStyle(), (int)timeBar.Value);
				} else if ((bool)arrowTool.IsChecked) {
					tactics.drawArrow(recalculate(mouseFrom), recalculate(e.GetPosition(drawBox)), drawColor.SelectedColor, (int)drawThickness.Value, getDashStyle(), (int)timeBar.Value);
				} else if ((bool)stampTool.IsChecked && stampImg != null) {
					tactics.drawStamp(recalculate(e.GetPosition(drawBox)), stampImg, (int)stampSize.Value, (int)timeBar.Value);
					draw = true;
				} else if ((bool)freeTool.IsChecked) {
					tactics.drawPoint(recalculate(e.GetPosition(drawBox)), drawColor.SelectedColor, (int)drawThickness.Value, (int)timeBar.Value);
				} else if ((bool)eraseTool.IsChecked) {
					tactics.drawEraserPoint(recalculate(e.GetPosition(drawBox)), (int)drawThickness.Value, (int)timeBar.Value);
				}
			} else if (window == activeWindow.dynamicPanel) {
				if (!itemsMoved && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))) {
					tactics.deselectIcon(recalculate(mouseFrom), (int)timeBar.Value);
				}
				move = false;
			}
		}

		private void drawBox_MouseEnter(object sender, MouseEventArgs e) {
			if ((bool)stampTool.IsChecked && stampImg != null) {
				draw = true;
			}
		}

		private void drawBox_MouseLeave(object sender, MouseEventArgs e) {
			move = draw = false;
			tactics.removeSamples();
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
				tactics.setStaticTimer(!(bool)noTimer.IsChecked);
			} else if (window == activeWindow.dynamicPanel) {
				tactics.setDynamicTimer(!(bool)noTimer.IsChecked);
			}
		}

		private void timeBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			refreshTime();
			refreshMap();
		}

		public void setTime(int time) {
			timeBar.Value = time;
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
			if (tactics.hasStaticIcon((StaticIcon)dynamicStaticList.SelectedItem)) {
				tactics.removeStaticIcon((StaticIcon)dynamicStaticList.SelectedItem);
			} else {
				tactics.addStaticIcon((StaticIcon)dynamicStaticList.SelectedItem);
			}
		}

		private void dynamicStaticList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			refreshDynamicPanel();
		}

		private void addTank_Click(object sender, RoutedEventArgs e) {
			AddTank addTankWindow = new AddTank(tactics.getTanksObj());
			addTankWindow.ShowDialog();

			if (!addTankWindow.dialogResult) {
				return;
			}

			tactics.addTank(addTankWindow.newtank);
			dynamicTankList.Items.Add(addTankWindow.newtank);
		}

		private void editTank_Click(object sender, RoutedEventArgs e) {
			if (dynamicTankList.SelectedItem == null) {
				return;
			}

			AddTank addTankWindow = new AddTank(tactics.getTanksObj(), (DynamicTank)dynamicTankList.SelectedItem);
			addTankWindow.ShowDialog();

			if (!addTankWindow.dialogResult) {
				return;
			}

			tactics.editTank(addTankWindow.newtank);
			dynamicTankList.Items.Refresh();
		}

		private void removeTank_Click(object sender, RoutedEventArgs e) {
			if (dynamicTankList.SelectedItem == null) {
				return;
			}

			tactics.removeTank((DynamicTank)dynamicTankList.SelectedItem);
			dynamicTankList.Items.Remove(dynamicTankList.SelectedItem);
		}

		private void dynamicTankList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			refreshDynamicPanel();
		}

		private void tankAliveStatus_Click(object sender, RoutedEventArgs e) {
			if (dynamicTankList.SelectedItem == null) {
				return;
			}

			if ((string)tankAliveStatus.Content == "Alive") {
				tactics.setKill((DynamicTank)dynamicTankList.SelectedItem, (int)timeBar.Value);
			} else {
				tactics.setKill((DynamicTank)dynamicTankList.SelectedItem, -1);
			}
		}

		private void delTankCurrentPosition_Click(object sender, RoutedEventArgs e) {
			if (dynamicTankList.SelectedItem == null) {
				return;
			}

			tactics.removePosition((DynamicTank)dynamicTankList.SelectedItem, (int)timeBar.Value);
		}

		private void dynamicEvents_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (dynamicTankList.SelectedItem == null) {
				return;
			}

			tactics.setTankActionId((DynamicTank)dynamicTankList.SelectedItem, (int)timeBar.Value, ((DynamicIcon)dynamicEvents.SelectedItem).id);
		}

		private void iconSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			if (!tactics.isLoaded()) {
				return;
			}

			tactics.setDynamicIconSize((int)iconSize.Value);
			playIconSize.Value = iconSize.Value;
		}

		private void playIconSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			if (!tactics.isLoaded()) {
				return;
			}

			tactics.setDynamicIconSize((int)playIconSize.Value);
			iconSize.Value = playIconSize.Value;
		}

		private void menuShowPlayerName_Changed(object sender, RoutedEventArgs e) {
			if (!tactics.isLoaded()) {
				return;
			}

			tactics.setShowPlayerName(menuShowPlayerName.IsChecked);
		}

		private void menuShowTankType_Changed(object sender, RoutedEventArgs e) {
			tactics.setShowTankName(menuShowTankType.IsChecked);
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
			if (!tactics.isLoaded())
				return;

			Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
			sfd.Filter = "Tactic planner file (*.tactic)|*.tactic";
			if ((bool)sfd.ShowDialog()) {
				try {
					tactics.save(sfd.FileName);
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
					tactics.load(ofd.FileName);
				} catch (Exception ex) {
					MessageBox.Show("Error: unable to load tactic file. Reason: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					drawBox.Source = null;
					mapBox.Source = null;
					return;
				}
			}
		}

		private void menuExport_Click(object sender, RoutedEventArgs e) {
			if (!tactics.isLoaded())
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
				//briefing.openServer(nick.Text, Convert.ToInt32(port.Text));
			} catch (Exception ex) {
				MessageBox.Show("Error: unable to start server. Reason: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			//host.Text = briefing.getMyIp();

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
				//briefing.connect(nick.Text, host.Text, Convert.ToInt32(port.Text), password.Password);
			} catch (Exception ex) {
				MessageBox.Show("Error: unable to connect to server. Reason: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			nick.IsEnabled = clientsCanDraw.IsEnabled = clientsCanPing.IsEnabled = password.IsEnabled = port.IsEnabled = host.IsEnabled = false;
			openServer.Visibility = connect.Visibility = System.Windows.Visibility.Hidden;
			disconnect.Visibility = System.Windows.Visibility.Visible;
		}

		private void disconnect_Click(object sender, RoutedEventArgs e) {
			//briefing.disconnect();

			nick.IsEnabled = clientsCanDraw.IsEnabled = clientsCanPing.IsEnabled = password.IsEnabled = port.IsEnabled = host.IsEnabled = true;
			kick.IsEnabled = false;
			openServer.Visibility = connect.Visibility = System.Windows.Visibility.Visible;
			disconnect.Visibility = System.Windows.Visibility.Hidden;
		}

		private void password_PasswordChanged(object sender, RoutedEventArgs e) {
			//briefing.setPassword(password.Password);
		}

		private void clientsCanPing_Checked(object sender, RoutedEventArgs e) {
			//briefing.enableClientsPing();
		}

		private void clientsCanPing_Unchecked(object sender, RoutedEventArgs e) {
			//briefing.disableClientsPing();
		}

		private void clientsCanDraw_Checked(object sender, RoutedEventArgs e) {
			//briefing.enableClientsDraw();
		}

		private void clientsCanDraw_Unchecked(object sender, RoutedEventArgs e) {
			//briefing.disableClientsDraw();
		}

		private void kick_Click(object sender, RoutedEventArgs e) {
			if (clientList.SelectedItem == null) {
				return;
			}


		}

		private void menuCopyDynamic_Click(object sender, RoutedEventArgs e) {
			if (tactics.hasSelectedIcon()) {
				tactics.copy();
				menuPasteDynamic.IsEnabled = true;
			}
		}

		private void menuPasteDynamic_Click(object sender, RoutedEventArgs e) {
			tactics.paste();
			refreshMap();
		}
	}

	public static class MenuCommands {
		public static RoutedCommand ToggleGrid = new RoutedCommand();
		public static RoutedCommand Export = new RoutedCommand();
		public static RoutedCommand ToggleBriefing = new RoutedCommand();
	}
}
