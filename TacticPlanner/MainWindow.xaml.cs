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
		public enum activeWindow {
			staticPanel,
			dynamicPanel,
			playPanelStatic,
            playPanelDynamic
		}

		private TacticsController tactics;
		private BriefingController briefing;

		private BitmapImage stampImg;
		private PenDashStyle lineType = PenDashStyle.Solid;

		private int time = 900;
		private bool move = false, draw = false, itemsMoved = false;
		private bool arrowToolChecked = false, lineToolChecked = false;
		private Point mouseFrom;
		private activeWindow window = activeWindow.staticPanel;

		private List<Point> freeBuffer;

		private DispatcherTimer playTimer;
		private DispatcherTimer pingTimer;

		private byte pingAnimationAlpha;
		private byte pingAnimationPhase;
		private KeyValuePair<byte, byte> pingArea;

		public MainWindow() {
			Splash splash = new Splash();
			splash.ShowDialog();

			try {
				tactics = new TacticsController(this, System.AppDomain.CurrentDomain.BaseDirectory);
				briefing = new BriefingController(tactics, this);
			} catch (Exception) {
				MessageBox.Show("Error: unable to load core files! Please reinstall the application.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				Application.Current.Shutdown();
			}

			freeBuffer = new List<Point>();

			playTimer = new DispatcherTimer();
			playTimer.Interval = new TimeSpan(100000);
			playTimer.IsEnabled = false;
			playTimer.Tick += new EventHandler(playTimer_Tick);

			pingTimer = new DispatcherTimer();
			pingTimer.Interval = new TimeSpan(2000);
			pingTimer.IsEnabled = false;
			pingTimer.Tick += new EventHandler(pingTimer_Tick);

			InitializeComponent();

			stampImage.Source = BitmapSource.Create(100, 100, 96, 96, PixelFormats.BlackWhite, BitmapPalettes.BlackAndWhite, new byte[100 * 20], 20);

			timePanelGrid.IsEnabled = menuBattleType.IsEnabled = staticPanelGrid.IsEnabled = dynamicPanelGrid.IsEnabled = playPanelGrid.IsEnabled = false;

			openServer.IsEnabled = kick.IsEnabled = false;
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

		private void Window_Unloaded(object sender, RoutedEventArgs e) {
			briefing.disconnect();
		}

		public void dropError(string message) {
			MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}

		private void newmapMenu_Click(object sender, EventArgs e) {
			MenuItem senderObj = (MenuItem)sender;

			timePanelGrid.IsEnabled = menuBattleType.IsEnabled = staticPanelGrid.IsEnabled = dynamicPanelGrid.IsEnabled = playPanelGrid.IsEnabled = false;

			try {
				tactics.add((senderObj).Name.Split('_')[1]);
				tactics.setBattleType(getBattleType(), getBattleVariant());
				if (briefing.isServer() || briefing.isClient() && (bool)clientsCanDraw.IsChecked) {
					briefing.sendTactic();
				} else if (briefing.isClient()) {
					briefing.disconnect();
				}
			} catch (Exception) {
				MessageBox.Show("Error: unable to initialize tactic!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

			if (!briefing.hasConnection()) {
				openServer.IsEnabled = true;
			}
		}

		public void initFromTactic() {
			if (!tactics.isLoaded()) {
				return;
			}

			this.Dispatcher.Invoke((Action)(() => {
				mapBox.Source = tactics.getMap().getMapImage();
				setBattletype(tactics.getBattleType(), tactics.getBattleVariation());

				timeBar.Value = 900;

				tactics.setDynamicIconSize((int)iconSize.Value);
				tactics.setDynamicPenColor(dynamicTextColor.SelectedColor);

				tactics.setShowTankName(menuShowTankType.IsChecked);
				tactics.setShowPlayerName(menuShowPlayerName.IsChecked);
				tactics.setTankIcon(menuTankTypeIcons.IsChecked ? DisplayTankIcon.tanktype : DisplayTankIcon.tankicon);

				if (!briefing.hasConnection()) {
					timePanelGrid.IsEnabled = menuBattleType.IsEnabled = staticPanelGrid.IsEnabled = dynamicPanelGrid.IsEnabled = playPanelGrid.IsEnabled = true;
					openServer.IsEnabled = true;
				} else if (briefing.isServer()) {
					timePanelGrid.IsEnabled = menuBattleType.IsEnabled = staticPanelGrid.IsEnabled = dynamicPanelGrid.IsEnabled = playPanelGrid.IsEnabled = true;
				} else {	// isClient
					timePanelGrid.IsEnabled = (bool)clientsCanPing.IsChecked;
					menuBattleType.IsEnabled = staticPanelGrid.IsEnabled = dynamicPanelGrid.IsEnabled = playPanelGrid.IsEnabled = (bool)clientsCanDraw.IsChecked;
				}

				dynamicTankList.Items.Clear();
				DynamicTank[] dynamicTanks = tactics.getTanks();
				foreach (DynamicTank tank in dynamicTanks) {
					dynamicTankList.Items.Add(tank);
				}

				refreshNoTimer();
				refreshTime();
				refreshDynamicPanel();
				refreshMap();
			}));
		}

		public void refreshDynamicTactic() {
			dynamicTankList.Items.Clear();
			DynamicTank[] dynamicTanks = tactics.getTanks();
			foreach (DynamicTank tank in dynamicTanks) {
				dynamicTankList.Items.Add(tank);
			}

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

				string actionId = tactics.getTankActionId((DynamicTank)dynamicTankList.SelectedItem, time);
				if (actionId == "") {
					dynamicEvents.SelectedIndex = 0;
				} else {
					dynamicEvents.SelectedItem = tactics.getDynamicIcon(actionId);
				}

				bool alive = tactics.isAlive((DynamicTank)dynamicTankList.SelectedItem, time);
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
			timeLabel.Content = (time / 60).ToString("D2") + ":" + (time % 60).ToString("D2");
		}

		public void refreshStaticMap() {
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
					drawBox.Source = tactics.getStaticImage(time);
				} else if (window == activeWindow.dynamicPanel) {
					drawBox.Source = tactics.getDynamicImage(time);
					refreshDynamicPanel();
				} else {
					if ((bool)playStatic.IsChecked) {
						drawBox.Source = tactics.getStaticPlayImage(time);
					} else {
						drawBox.Source = tactics.getDynamicPlayImage(time);
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
					if (window == activeWindow.staticPanel) {
						return;
					}
					window = activeWindow.staticPanel;
					briefing.showStatic();
					timeBar.Value = (time / 30) * 30;
					timeBar.IsSnapToTickEnabled = true;
					break;
				case "Dynamic panel":
					if (window == activeWindow.dynamicPanel) {
						return;
					}
					window = activeWindow.dynamicPanel;
					briefing.showDynamic();
					timeBar.Value = (time / 30) * 30;
					timeBar.IsSnapToTickEnabled = true;
					break;
				case "Play panel":
					if (window == activeWindow.playPanelStatic && (bool)playStatic.IsChecked || window == activeWindow.playPanelDynamic && (bool)playDynamic.IsChecked) {
						break;
					}
                    if ((bool)playStatic.IsChecked) {
                        window = activeWindow.playPanelStatic;
                        briefing.showPlayStatic();
                    } else {
                        window = activeWindow.playPanelDynamic;
                        briefing.showPlayDynamic();
                    }
					timeBar.IsSnapToTickEnabled = false;
					break;
			}

			refreshNoTimer();
			refreshTime();
			refreshMap();
		}

		public void showStatic() {
			window = activeWindow.staticPanel;
			timeBar.Value = (time / 30) * 30;
			timeBar.IsSnapToTickEnabled = true;

			refreshNoTimer();
			refreshTime();
			refreshMap();

			staticPanel.IsSelected = true;
		}
		public void showDynamic() {
			window = activeWindow.dynamicPanel;
			timeBar.Value = (time / 30) * 30;
			timeBar.IsSnapToTickEnabled = true;

			refreshNoTimer();
			refreshTime();
			refreshMap();

			dynamicPanel.IsSelected = true;
		}
		public void showPlayStatic() {
			window = activeWindow.playPanelStatic;
            playStatic.IsChecked = true;

			refreshNoTimer();
			refreshTime();
			refreshMap();

			playPanel.IsSelected = true;
		}
        public void showPlayDynamic() {
            window = activeWindow.playPanelDynamic;
            playDynamic.IsChecked = true;

            refreshNoTimer();
            refreshTime();
            refreshMap();

            playPanel.IsSelected = true;
        }

		public activeWindow getActiveWindow() {
			return window;
		}

		private void resetStatic_Click(object sender, RoutedEventArgs e) {
			briefing.resetDrawAt(time);
			tactics.removeDraw(time);
		}

		private void cloneStatic_Click(object sender, RoutedEventArgs e) {
			if (time == 900) {
				briefing.resetDrawAt(time);
				tactics.removeDraw(time);
			} else {
				briefing.cloneDrawAt(time);
				tactics.cloneTactic(time + 30, time);
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

		public void pingCell(byte col, byte row) {
			pingAnimationAlpha = 0;
			pingAnimationPhase = 1;
			pingArea = new KeyValuePair<byte, byte>(col, row);
			pingTimer.IsEnabled = true;
		}

		private void drawBox_MouseDown(object sender, MouseButtonEventArgs e) {
			mouseFrom = e.GetPosition(drawBox);

			if (!tactics.isLoaded()) {
				return;
			}

			if ((briefing.isServer() || (bool)clientsCanPing.IsChecked) && (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))) {
				byte col = (byte)(mouseFrom.X / (drawBox.ActualWidth / 10)), row = (byte)(mouseFrom.Y / (drawBox.ActualHeight / 10));
				briefing.ping(col, row);
				pingCell(col, row);
				return;
			}

			if (briefing.isServer() || (bool)clientsCanDraw.IsChecked || !briefing.hasConnection()) {
				if (window == activeWindow.staticPanel) {
					freeBuffer.Clear();
					draw = true;
				} else if (window == activeWindow.dynamicPanel) {
					itemsMoved = tactics.selectIcon(recalculate(mouseFrom), Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift), time);
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
		}

		private void drawBox_MouseMove(object sender, MouseEventArgs e) {
			if (draw) {
				if ((bool)lineTool.IsChecked) {
					tactics.drawSampleLine(recalculate(mouseFrom), recalculate(e.GetPosition(drawBox)), drawColor.SelectedColor, (int)drawThickness.Value, getDashStyle(), time);
				} else if ((bool)arrowTool.IsChecked) {
					tactics.drawSampleArrow(recalculate(mouseFrom), recalculate(e.GetPosition(drawBox)), drawColor.SelectedColor, (int)drawThickness.Value, getDashStyle(), time);
				} else if ((bool)freeTool.IsChecked) {
					tactics.drawPoint(recalculate(e.GetPosition(drawBox)), drawColor.SelectedColor, (int)drawThickness.Value, time);
					freeBuffer.Add(recalculate(e.GetPosition(drawBox)));
				} else if ((bool)eraseTool.IsChecked) {
					tactics.drawEraserPoint(recalculate(e.GetPosition(drawBox)), (int)drawThickness.Value, time);
					freeBuffer.Add(recalculate(e.GetPosition(drawBox)));
				} else if ((bool)stampTool.IsChecked && stampImg != null) {
					tactics.drawSampleStamp(recalculate(e.GetPosition(drawBox)), stampImg, (int)stampSize.Value, time);
				}
			}

			if (move) {
				itemsMoved = true;
				tactics.moveIcons(recalculate(mouseFrom), recalculate(e.GetPosition(drawBox)), time);
				mouseFrom = e.GetPosition(drawBox);
				#if DEBUG
				System.Console.Write("X: " + (recalculate(e.GetPosition(drawBox)).X / 1024.0).ToString());
				System.Console.WriteLine("  Y: " + (recalculate(e.GetPosition(drawBox)).Y / 1024.0).ToString());
				#endif
			}
		}

		private void drawBox_MouseUp(object sender, MouseButtonEventArgs e) {
			if (window == activeWindow.staticPanel) {
				if (!draw) {
					return;
				}

				draw = false;
				if ((bool)lineTool.IsChecked) {
					briefing.drawLine(recalculate(mouseFrom), recalculate(e.GetPosition(drawBox)), drawColor.SelectedColor, (int)drawThickness.Value, getDashStyle(), time);
					tactics.drawLine(recalculate(mouseFrom), recalculate(e.GetPosition(drawBox)), drawColor.SelectedColor, (int)drawThickness.Value, getDashStyle(), time);
				} else if ((bool)arrowTool.IsChecked) {
					briefing.drawArrow(recalculate(mouseFrom), recalculate(e.GetPosition(drawBox)), drawColor.SelectedColor, (int)drawThickness.Value, getDashStyle(), time);
					tactics.drawArrow(recalculate(mouseFrom), recalculate(e.GetPosition(drawBox)), drawColor.SelectedColor, (int)drawThickness.Value, getDashStyle(), time);
				} else if ((bool)stampTool.IsChecked && stampImg != null) {
					briefing.drawStamp(recalculate(e.GetPosition(drawBox)), stampImg, (int)stampSize.Value, time);
					tactics.drawStamp(recalculate(e.GetPosition(drawBox)), stampImg, (int)stampSize.Value, time);
					draw = true;
				} else if ((bool)freeTool.IsChecked) {
					freeBuffer.Add(recalculate(e.GetPosition(drawBox)));
					briefing.drawPoints(freeBuffer.ToArray(), drawColor.SelectedColor, (int)drawThickness.Value, time);
					tactics.drawPoint(recalculate(e.GetPosition(drawBox)), drawColor.SelectedColor, (int)drawThickness.Value, time);
				} else if ((bool)eraseTool.IsChecked) {
					freeBuffer.Add(recalculate(e.GetPosition(drawBox)));
					briefing.drawEraserPoints(freeBuffer.ToArray(), (int)drawThickness.Value, time);
					tactics.drawEraserPoint(recalculate(e.GetPosition(drawBox)), (int)drawThickness.Value, time);
				}
			} else if (window == activeWindow.dynamicPanel) {
				if (!itemsMoved && (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))) {
					tactics.deselectIcon(recalculate(mouseFrom), time);
				}
				if (itemsMoved) {
					briefing.reloadDynamic();
				}
				move = false;
			}
		}

		private void drawBox_MouseEnter(object sender, MouseEventArgs e) {
			if ((bool)stampTool.IsChecked && stampImg != null && (!briefing.isClient() || (bool)clientsCanDraw.IsChecked)) {
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
				if (tactics.hasStaticTimer() != !(bool)noTimer.IsChecked) {
					briefing.staticTimer(!(bool)noTimer.IsChecked);
					tactics.setStaticTimer(!(bool)noTimer.IsChecked);
				}
			} else if (window == activeWindow.dynamicPanel) {
				if (tactics.hasDynamicTimer() != !(bool)noTimer.IsChecked) {
					briefing.dynamicTimer(!(bool)noTimer.IsChecked);
					tactics.setDynamicTimer(!(bool)noTimer.IsChecked);
				}
			}
		}

		private void timeBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
			if ((int)timeBar.Value != time) {
				time = (int)timeBar.Value;
				briefing.setTimer(time);
				refreshTime();
				refreshMap();
			}
		}

		public void setTime(int time) {
			this.time = time;
			timeBar.Value = time;
			refreshTime();
			refreshMap();
		}

		private void playMode_Changed(object sender, RoutedEventArgs e) {
			if (window != activeWindow.playPanelStatic && window != activeWindow.playPanelDynamic) {
				return;
			}
            if ((bool)playStatic.IsChecked && window != activeWindow.playPanelStatic) {
                window = activeWindow.playPanelStatic;
				briefing.showPlayStatic();
			} else if ((bool)playDynamic.IsChecked && window != activeWindow.playPanelDynamic) {
				window = activeWindow.playPanelDynamic;
				briefing.showPlayDynamic();
            }
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

		private void playTimer_Tick(object sender, EventArgs e) {
			if (timeBar.Value == 0) {
				playTimer.IsEnabled = false;
				return;
			}

			timeBar.Value -= 1;
			refreshTime();
			refreshMap();
		}

		private void pingTimer_Tick(object sender, EventArgs e) {
			if (pingAnimationPhase == 6) {
				pingTimer.IsEnabled = false;
				if (menuShowGrid.IsChecked) {
					gridBox.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/grid.png", UriKind.Absolute));
				} else {
					gridBox.Source = null;
				}
				return;
			}
			if (pingAnimationAlpha == 250 || pingAnimationAlpha == 0) {
				pingAnimationPhase += 1;
			}
			if (pingAnimationPhase % 2 == 0) {
				pingAnimationAlpha += 10;
			} else {
				pingAnimationAlpha -= 10;
			}

			BitmapImage img;
			if (menuShowGrid.IsChecked) {
				img = new BitmapImage(new Uri(@"pack://application:,,,/Resources/grid.png", UriKind.Absolute));
			} else {
				img = new BitmapImage(new Uri(@"pack://application:,,,/Resources/clearTactics.png", UriKind.Absolute));
			}

			Pen pen = new Pen(new SolidColorBrush(Color.FromArgb(pingAnimationAlpha, 255, 102, 0)), 4);
			Point p1 = new Point(pingArea.Key * img.PixelWidth / 10, pingArea.Value * img.PixelWidth / 10);
			Point p2 = new Point((pingArea.Key + 1) * img.PixelWidth / 10 + 1, pingArea.Value * img.PixelWidth / 10);
			Point p3 = new Point((pingArea.Key + 1) * img.PixelWidth / 10 + 1, (pingArea.Value + 1) * img.PixelWidth / 10 + 1);
			Point p4 = new Point(pingArea.Key * img.PixelWidth / 10, (pingArea.Value + 1) * img.PixelWidth / 10 + 1);

			DrawingGroup drawing = new DrawingGroup();
			drawing.Children.Add(new ImageDrawing(img, new Rect(0, 0, img.PixelWidth, img.PixelHeight)));
			drawing.Children.Add(new GeometryDrawing(pen.Brush, pen, new LineGeometry(p1, p2)));
			drawing.Children.Add(new GeometryDrawing(pen.Brush, pen, new LineGeometry(p2, p3)));
			drawing.Children.Add(new GeometryDrawing(pen.Brush, pen, new LineGeometry(p3, p4)));
			drawing.Children.Add(new GeometryDrawing(pen.Brush, pen, new LineGeometry(p4, p1)));
			drawing.ClipGeometry = new RectangleGeometry(new Rect(0, 0, img.PixelWidth, img.PixelHeight));
			drawing.Freeze();

			gridBox.Source = new DrawingImage(drawing);
		}

		private void addStatic_Click(object sender, RoutedEventArgs e) {
			if (tactics.hasStaticIcon((StaticIcon)dynamicStaticList.SelectedItem)) {
				tactics.removeStaticIcon((StaticIcon)dynamicStaticList.SelectedItem);
			} else {
				tactics.addStaticIcon((StaticIcon)dynamicStaticList.SelectedItem);
			}
			briefing.reloadDynamic();
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
			briefing.reloadDynamic();
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
			briefing.reloadDynamic();
		}

		private void editTank_Click(object sender, MouseButtonEventArgs e) {
			if (dynamicTankList.SelectedItem == null) {
				return;
			}

			AddTank addTankWindow = new AddTank(tactics.getTanksObj(), (DynamicTank)dynamicTankList.SelectedItem);
			addTankWindow.ShowDialog();

			if (!addTankWindow.dialogResult) {
				return;
			}

			tactics.editTank(addTankWindow.newtank);
			briefing.reloadDynamic();
		}

		private void removeTank_Click(object sender, RoutedEventArgs e) {
			if (dynamicTankList.SelectedItem == null) {
				return;
			}

			tactics.removeTank((DynamicTank)dynamicTankList.SelectedItem);
			briefing.reloadDynamic();
		}

		private void dynamicTankList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			refreshDynamicPanel();
		}

		private void tankAliveStatus_Click(object sender, RoutedEventArgs e) {
			if (dynamicTankList.SelectedItem == null) {
				return;
			}

			if ((string)tankAliveStatus.Content == "Alive") {
				tactics.setKill((DynamicTank)dynamicTankList.SelectedItem, time);
			} else {
				tactics.setKill((DynamicTank)dynamicTankList.SelectedItem, -1);
			}
			briefing.reloadDynamic();
		}

		private void delTankCurrentPosition_Click(object sender, RoutedEventArgs e) {
			if (dynamicTankList.SelectedItem == null) {
				return;
			}

			tactics.removePosition((DynamicTank)dynamicTankList.SelectedItem, time);
			briefing.reloadDynamic();
		}

		private void dynamicEvents_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			if (dynamicTankList.SelectedItem == null) {
				return;
			}

			tactics.setTankActionId((DynamicTank)dynamicTankList.SelectedItem, time, ((DynamicIcon)dynamicEvents.SelectedItem).id);
			briefing.reloadDynamic();
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
			refreshStaticMap();
		}

		private void menuMappackHD_Checked(object sender, RoutedEventArgs e) {
			if (menuMappackOriginal == null) {
				return;
			}

			menuMappackOriginal.IsChecked = false;
			refreshStaticMap();
		}

		private void menuShowGrid_Checked(object sender, RoutedEventArgs e) {
			gridBox.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/grid.png", UriKind.Absolute));
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
			timePanelGrid.IsEnabled = menuBattleType.IsEnabled = staticPanelGrid.IsEnabled = dynamicPanelGrid.IsEnabled = playPanelGrid.IsEnabled = false;

			Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
			ofd.InitialDirectory = "stamps";
			ofd.Filter = "Tactic planner file (*.tactic)|*.tactic";
			if ((bool)ofd.ShowDialog()) {
				try {
					tactics.load(ofd.FileName);
					if (briefing.isServer() || briefing.isClient() && (bool)clientsCanDraw.IsChecked) {
						briefing.sendTactic();
					} else if (briefing.isClient()) {
						briefing.disconnect();
					}
				} catch (Exception ex) {
					MessageBox.Show("Error: unable to load tactic file. Reason: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					drawBox.Source = null;
					mapBox.Source = null;
					return;
				}
				if (!briefing.hasConnection()) {
					openServer.IsEnabled = true;
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
			if (nick.Text == "") {
				MessageBox.Show("You must enter your nick!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			host.Text = briefing.getMyIp();
			nick.IsEnabled = port.IsEnabled = host.IsEnabled = false;
			kick.IsEnabled = true;
			openServer.Visibility = connect.Visibility = System.Windows.Visibility.Hidden;
			disconnect.Visibility = System.Windows.Visibility.Visible;

			addClient(nick.Text);

			try {
				briefing.open(nick.Text, Convert.ToInt32(port.Text));
			} catch (Exception ex) {
				MessageBox.Show("Error: unable to start server. Reason: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
		}

		private void connect_Click(object sender, RoutedEventArgs e) {
			if (nick.Text == "") {
				MessageBox.Show("You must enter your nick!", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
				return;
			}

			nick.IsEnabled = clientsCanDraw.IsEnabled = clientsCanPing.IsEnabled = password.IsEnabled = port.IsEnabled = host.IsEnabled = false;
			openServer.Visibility = connect.Visibility = System.Windows.Visibility.Hidden;
			disconnect.Visibility = System.Windows.Visibility.Visible;

			try {
				briefing.connect(nick.Text, host.Text, Convert.ToInt32(port.Text), password.Password);
			} catch (Exception ex) {
				MessageBox.Show("Error: unable to connect to server. Reason: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
		}

		private void disconnect_Click(object sender, RoutedEventArgs e) {
			briefing.disconnect();

			disconnected();
		}

		public void disconnected() {
			this.Dispatcher.Invoke((Action)(() => {
				openServer.IsEnabled = true;	// You have already received the tactic
				timePanelGrid.IsEnabled = menuBattleType.IsEnabled = staticPanelGrid.IsEnabled = dynamicPanelGrid.IsEnabled = playPanelGrid.IsEnabled = true;	// ...so you can do this too
				nick.IsEnabled = clientsCanDraw.IsEnabled = clientsCanPing.IsEnabled = password.IsEnabled = port.IsEnabled = host.IsEnabled = true;
				kick.IsEnabled = false;
				openServer.Visibility = connect.Visibility = System.Windows.Visibility.Visible;
				disconnect.Visibility = System.Windows.Visibility.Hidden;
				clientList.Items.Clear();
			}));
		}

		public void addClient(string nick) {
			this.Dispatcher.Invoke((Action)(() => {
				clientList.Items.Add(nick);
			}));
		}

		public void removeClient(string nick) {
			this.Dispatcher.Invoke((Action)(() => {
				clientList.Items.Remove(nick);
			}));
		}

		public void setClients(string[] nicks) {
			clientList.Items.Clear();
			for (int i = 0; i < nicks.Length; i++) {
				clientList.Items.Add(nicks[i]);
			}
		}

		private void password_PasswordChanged(object sender, RoutedEventArgs e) {
			briefing.setPassword(password.Password);
		}

		private void clientsCanPing_Checked(object sender, RoutedEventArgs e) {
			if (briefing.isServer()) {
				briefing.enableClientsPing();
			}
		}

		private void clientsCanPing_Unchecked(object sender, RoutedEventArgs e) {
			if (briefing.isServer()) {
				briefing.disableClientsPing();
			}
		}

		private void clientsCanDraw_Checked(object sender, RoutedEventArgs e) {
			if (briefing.isServer()) {
				briefing.enableClientsDraw();
			}
		}

		private void clientsCanDraw_Unchecked(object sender, RoutedEventArgs e) {
			if (briefing.isServer()) {
				briefing.disableClientsDraw();
			}
		}

		public void setClientsPing(bool enable) {
			clientsCanPing.IsChecked = enable;
			timePanelGrid.IsEnabled = enable;
		}

		public void setClientsDraw(bool enable) {
			clientsCanDraw.IsChecked = enable;
			menuBattleType.IsEnabled = staticPanelGrid.IsEnabled = dynamicPanelGrid.IsEnabled = playPanelGrid.IsEnabled = enable;
		}

		private void kick_Click(object sender, RoutedEventArgs e) {
			if (clientList.SelectedItem == null || nick.Text == (string)clientList.SelectedItem) {
				return;
			}

			briefing.kick((string)clientList.SelectedItem);
		}

		private void menuCopyDynamic_Click(object sender, RoutedEventArgs e) {
			if (tactics.hasSelectedIcon()) {
				tactics.copy();
				menuPasteDynamic.IsEnabled = true;
			}
		}

		private void menuPasteDynamic_Click(object sender, RoutedEventArgs e) {
			tactics.paste();
			briefing.reloadDynamic();
		}

		private void menuBattletypeNone_Checked(object sender, RoutedEventArgs e) {
			if (menuBattletypeNormal == null || menuBattletypeEncounter == null || menuBattletypeAssault == null) {
				return;
			}

			if (tactics.getBattleType() != BattleType.Undefined) {
				tactics.setBattleType(BattleType.Undefined, "");
				briefing.setBattleType(BattleType.Undefined, "");
			}
		}

		private void menuBattletypeNormal_Checked(object sender, RoutedEventArgs e) {
			if (tactics.getBattleType() != BattleType.Normal) {
				tactics.setBattleType(BattleType.Normal, getBattleVariant());
				briefing.setBattleType(BattleType.Normal, getBattleVariant());
			}
		}

		private void menuBattletypeEncounter_Checked(object sender, RoutedEventArgs e) {
			if (tactics.getBattleType() != BattleType.Encounter) {
				tactics.setBattleType(BattleType.Encounter, getBattleVariant());
				briefing.setBattleType(BattleType.Encounter, getBattleVariant());
			}
		}

		private void menuBattletypeAssault_Checked(object sender, RoutedEventArgs e) {
			if (tactics.getBattleType() != BattleType.Assault) {
				tactics.setBattleType(BattleType.Assault, getBattleVariant());
				briefing.setBattleType(BattleType.Assault, getBattleVariant());
			}
		}

		private void menuBattleVariantA_Checked(object sender, RoutedEventArgs e) {
			if (menuBattleVariantB == null) {
				return;
			}

			if (tactics.getBattleVariation() != "A") {
				tactics.setBattleType(getBattleType(), "A");
				briefing.setBattleType(getBattleType(), "A");
			}
		}

		private void menuBattleVariantB_Checked(object sender, RoutedEventArgs e) {
			if (tactics.getBattleVariation() != "B") {
				tactics.setBattleType(getBattleType(), "B");
				briefing.setBattleType(getBattleType(), "B");
			}
		}

		private BattleType getBattleType() {
			BattleType type = BattleType.Undefined;
			if (menuBattletypeNormal.IsChecked) {
				type = BattleType.Normal;
			} else if (menuBattletypeEncounter.IsChecked) {
				type = BattleType.Encounter;
			} else if (menuBattletypeAssault.IsChecked) {
				type = BattleType.Assault;
			}
			return type;
		}

		private string getBattleVariant() {
			string variant = "A";
			if (menuBattleVariantA.IsChecked) {
				variant = "A";
			} else if (menuBattleVariantB.IsChecked) {
				variant = "B";
			}
			return variant;
		}

		public void setBattletype(BattleType type = BattleType.Undefined, string variant = "") {
			switch (type) {
				case BattleType.Undefined:
					menuBattletypeNormal.IsChecked = menuBattletypeEncounter.IsChecked = menuBattletypeAssault.IsChecked = false;
					menuBattletypeNone.IsChecked = true;
					break;
				case BattleType.Normal:
					menuBattletypeNone.IsChecked = menuBattletypeEncounter.IsChecked = menuBattletypeAssault.IsChecked = false;
					menuBattletypeNormal.IsChecked = true;
					break;
				case BattleType.Encounter:
					menuBattletypeNone.IsChecked = menuBattletypeNormal.IsChecked = menuBattletypeAssault.IsChecked = false;
					menuBattletypeEncounter.IsChecked = true;
					break;
				case BattleType.Assault:
					menuBattletypeNone.IsChecked = menuBattletypeNormal.IsChecked = menuBattletypeEncounter.IsChecked = false;
					menuBattletypeAssault.IsChecked = true;
					break;
			}

			switch (variant) {
				case "A":
					menuBattleVariantB.IsChecked = false;
					menuBattleVariantA.IsChecked = true;
					break;
				case "B":
					menuBattleVariantA.IsChecked = false;
					menuBattleVariantB.IsChecked = true;
					break;
			}

			refreshStaticMap();
		}

		private void menuTankTypeIcons_Checked(object sender, RoutedEventArgs e) {
			if (menuTankIcons == null) {
				return;
			}

			menuTankIcons.IsChecked = false;
			tactics.setTankIcon(DisplayTankIcon.tanktype);
		}

		private void menuTankIcons_Checked(object sender, RoutedEventArgs e) {
			menuTankTypeIcons.IsChecked = false;
			tactics.setTankIcon(DisplayTankIcon.tankicon);
		}
	}

	public static class MenuCommands {
		public static RoutedCommand ToggleGrid = new RoutedCommand();
		public static RoutedCommand Export = new RoutedCommand();
		public static RoutedCommand ToggleBriefing = new RoutedCommand();
	}
}
