using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

using System.Runtime.InteropServices;

using TacticPlanner.models;
using TacticPlanner.controllers;

namespace TacticPlanner.gui {
    public partial class Tactics : Form {
		private Tactic tactic;

        private Bitmap stampImg;
        private DashStyle lineType;

        private bool arrowToolChecked;
        private bool lineToolChecked;
        private int time;

        private Point mouseFrom;
        private bool draw;
		private bool move;

        public Tactics() {
            InitializeComponent();
            Splash splash = new Splash();
            splash.ShowDialog();

            try {
				tactic = new Tactic(Application.StartupPath);
            } catch (Exception) {
                MessageBox.Show("Error: unable to load core files! Please reinstall the application.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                Application.Exit();
            }

            drawBox.Parent = mapBox;
            drawBox.Top = 0;
            
            lblColorSample.BackColor = Color.Red;
            lblDynamicTextColor.BackColor = lblPlayTextColor.BackColor = Color.White;
            move = draw = false;
            lineType = DashStyle.Solid;
            arrowToolChecked = lineToolChecked = false;
        }

        private void Tactics_Load(object sender, EventArgs e) {
            foreach (Map map in tactic.getMaps()) {
                ToolStripItem newmap = new ToolStripMenuItem(map.name);
                newmap.Name = "newmapToolStripItem_" + map.id;
                newmap.Click += newmap_Click;
                newMapToolStripMenuItem.DropDownItems.Add(newmap);
            }

            staticPanel.Visible = dynamicPanel.Visible = playPanel.Visible = false;
            staticPanel.Visible = true;

            cmbDynamicStaticList.ValueMember = "id";
            cmbDynamicStaticList.DisplayMember = "name";
            List<StaticIcon> staticIcons = tactic.getStaticIcons();
            foreach (StaticIcon icon in staticIcons) {
                cmbDynamicStaticList.Items.Add(icon);
            }
            cmbDynamicStaticList.SelectedIndex = 0;

            cmbDynamicEvents.ValueMember = "id";
            cmbDynamicEvents.DisplayMember = "name";
            List<DynamicIcon> dynamicIcons = tactic.getDynamicIcons();
            cmbDynamicEvents.Items.Add(new DynamicIcon("", "(none)", ""));
            foreach (DynamicIcon icon in dynamicIcons) {
                cmbDynamicEvents.Items.Add(icon);
            }
            cmbDynamicEvents.SelectedIndex = 0;

            dynamicTankList.ValueMember = "listName";

            
        }

        private void newmap_Click(object sender, EventArgs e) {
            ToolStripItem senderObj = (ToolStripItem)sender;

            staticPanel.Enabled = dynamicPanel.Enabled = playPanel.Enabled = false;

            try {
				tactic.newTactic(((ToolStripItem)sender).Name.Split('_')[1]);
            } catch (Exception) {
                MessageBox.Show("Error: unable to initialize tactic!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            initFromTactic();
        }

        private void initFromTactic() {
			if (!tactic.isLoaded())
				return;

            mapBox.Image = tactic.getMap().getMapImage();

            staticTimeBar.Value = 30;
            time = 900;

			tactic.getStaticTactic().setPenColor(lblColorSample.BackColor);
			tactic.getStaticTactic().setDashStyle(lineType);
			tactic.getStaticTactic().setThickness((int)thickness.Value);
			tactic.getDynamicTactic().setPenColor(lblDynamicTextColor.BackColor);

			tactic.getDynamicTactic().ShowTankName = showTankTypeToolStripMenuItem.Checked;
			tactic.getDynamicTactic().ShowPlayerName = showPlayernameToolStripMenuItem.Checked;

            staticPanel.Enabled = dynamicPanel.Enabled = playPanel.Enabled = true;
            notime.Checked = !tactic.getStaticTactic().timer;
			staticTimeBar.Enabled = tactic.getStaticTactic().timer;
			dynamicnotimer.Checked = !tactic.getDynamicTactic().timer;
			dynamicTimeBar.Enabled = tactic.getDynamicTactic().timer;

            dynamicTankList.Items.Clear();
            DynamicTank[] dynamicTanks = tactic.getDynamicTactic().getDynamicTanks();
            foreach (DynamicTank tank in dynamicTanks) {
                dynamicTankList.Items.Add(tank);
            }

            refreshTime();
            refreshMap();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) {
            Application.Exit();
        }

        private void lblColorSample_Click(object sender, EventArgs e) {
            ColorDialog cd = new ColorDialog();
            DialogResult result = cd.ShowDialog();
            if (result == DialogResult.OK) {
                if (cd.Color.Equals(((Bitmap)TacticPlanner.Properties.Resources.clearTactics).GetPixel(1, 1))) {
                    cd.Color = Color.FromArgb(cd.Color.R + 1, cd.Color.G + 1, cd.Color.B + 1);
                }
				lblColorSample.BackColor = cd.Color;
				tactic.getStaticTactic().setPenColor(cd.Color);
            }
        }

        private void aboutToolStripMenuItem1_Click(object sender, EventArgs e) {
            AboutBox ab = new AboutBox();
            ab.ShowDialog();
        }

        private void refreshMap() {
            if (!tactic.isLoaded())
                return;

            try {
                if (staticView.Checked) {
                    refreshStatic();
                } else if (dynamicView.Checked) {
                    refreshDynamic();
                } else {
                    refreshPlay();
                }
            } catch (Exception) {
                MessageBox.Show("Error: unable to render the map!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }
        private void refreshStatic() {
            drawBox.Image = tactic.getStaticTactic().getTacticAt(staticTimeBar.Value * 30);
        }
        private void refreshDynamic() {
            drawBox.Image = tactic.getDynamicTactic().getTacticAt(dynamicTimeBar.Value * 30);

            refreshDynamicAction();
        }
        private void refreshPlay() {
            Bitmap newImg;
            if (playStatic.Checked) {
				newImg = tactic.getStaticTactic().getPlayTacticAt(playTimeBar.Value);
            } else {
				newImg = tactic.getDynamicTactic().getPlayTacticAt(playTimeBar.Value);
            }
            if (newImg != null) {
                drawBox.Image = newImg;
            }
        }

        private void refreshTime() {
            lbldynamictime.Text = lblplaytime.Text = lblTime.Text = (time / 60).ToString("D2") + ":" + (time % 60).ToString("D2");
        }

        private Point recalculate(Point orig) {
			int offset = Math.Abs(drawBox.Width - drawBox.Height) / 2;

			if (drawBox.Width > drawBox.Height) {
				return new Point(
					((orig.X - offset) * 1024) / drawBox.Height,
					(orig.Y * 1024) / drawBox.Height
				);
			} else {
				return new Point(
					(orig.X * 1024) / drawBox.Width,
					((orig.Y - offset) * 1024) / drawBox.Width
				);
			}
        }

        private void reset_Click(object sender, EventArgs e) {
            tactic.getStaticTactic().removeTactic(staticTimeBar.Value * 30);
            refreshMap();
        }

        private void clonePrev_Click(object sender, EventArgs e) {
            if (staticTimeBar.Value == 30) {
				tactic.getStaticTactic().removeTactic(staticTimeBar.Value * 30);
            } else {
				tactic.getStaticTactic().cloneTactic((staticTimeBar.Value + 1) * 30, staticTimeBar.Value * 30);
            }
            refreshMap();
        }

        private void stampImage_Click(object sender, EventArgs e) {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = "stamps";
            ofd.Filter = "Image file (*.jpg, *.jpeg, *.bmp, *.png)|*.jpg;*.jpeg;*.bmp;*.png";
            DialogResult result = ofd.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK) {
                try {
                    stampImg = (Bitmap)Bitmap.FromFile(ofd.FileName);
                } catch (Exception) {
                    MessageBox.Show("Cannot open file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                stampImage.Image = stampImg;
            }
        }

        private void drawBox_MouseDown(object sender, MouseEventArgs e) {
            mouseFrom = e.Location;

			if (!tactic.isLoaded()) {
                return;
            }

			if (staticView.Checked) {
				draw = true;
			} else if (dynamicView.Checked) {
				tactic.getDynamicTactic().selectItem(recalculate(mouseFrom), dynamicTimeBar.Value * 30);
				move = true;
			}
        }

        private void drawBox_MouseMove(object sender, MouseEventArgs e) {
			if (draw) {
				if (lineTool.Checked) {
					tactic.getStaticTactic().drawSampleLine(recalculate(mouseFrom), recalculate(e.Location), staticTimeBar.Value * 30);
				} else if (arrowTool.Checked) {
					tactic.getStaticTactic().drawSampleArrow(recalculate(mouseFrom), recalculate(e.Location), staticTimeBar.Value * 30);
				} else if (freeTool.Checked) {
					tactic.getStaticTactic().drawPoint(recalculate(e.Location), staticTimeBar.Value * 30);
				} else if (eraseTool.Checked) {
					tactic.getStaticTactic().drawEraserPoint(recalculate(e.Location), staticTimeBar.Value * 30);
				}
			}

			if (move) {
				tactic.getDynamicTactic().moveItem(recalculate(e.Location), dynamicTimeBar.Value * 30);
			}

			if (move || draw) {
				refreshMap();
			}
        }

        private void drawBox_MouseUp(object sender, MouseEventArgs e) {
            if (staticView.Checked) {
                if (!draw)
                    return;

                if (lineTool.Checked) {
                    tactic.getStaticTactic().drawLine(recalculate(mouseFrom), recalculate(e.Location), staticTimeBar.Value * 30);
                } else if (arrowTool.Checked) {
					tactic.getStaticTactic().drawArrow(recalculate(mouseFrom), recalculate(e.Location), staticTimeBar.Value * 30);
				} else if (stampTool.Checked && stampImg != null) {
					tactic.getStaticTactic().drawStamp(recalculate(e.Location), stampImg, (int)stampSize.Value, staticTimeBar.Value * 30);
                } else if (freeTool.Checked) {
					tactic.getStaticTactic().drawPoint(recalculate(e.Location), staticTimeBar.Value * 30);
                } else if (eraseTool.Checked) {
					tactic.getStaticTactic().drawEraserPoint(recalculate(e.Location), staticTimeBar.Value * 30);
                }
                draw = false;
            } else if (dynamicView.Checked) {
                tactic.getDynamicTactic().moveItem(recalculate(e.Location), dynamicTimeBar.Value * 30);
				move = false;
            }

            refreshMap();
        }

        private void arrowTool_Click(object sender, EventArgs e) {
            if (arrowToolChecked) {
                rotateToolTypes();
            } else {
                arrowToolChecked = true;
            }
            lineToolChecked = false;
        }

        private void lineTool_Click(object sender, EventArgs e) {
            if (lineToolChecked) {
                rotateToolTypes();
            } else {
                lineToolChecked = true;
            }
            arrowToolChecked = false;
        }

        private void rotateToolTypes() {
            switch (lineType) {
                case DashStyle.Solid:
                    lineType = DashStyle.Dot;
                    arrowTool.Text = "Arrow tool: Dotted";
                    lineTool.Text = "Line tool: Dotted";
                    break;
                case DashStyle.Dot:
                    lineType = DashStyle.Dash;
                    arrowTool.Text = "Arrow tool: Dash";
                    lineTool.Text = "Line tool: Dash";
                    break;
                case DashStyle.Dash:
                    lineType = DashStyle.Solid;
                    arrowTool.Text = "Arrow tool: Solid";
                    lineTool.Text = "Line tool: Solid";
                    break;
            }
			tactic.getStaticTactic().setDashStyle(lineType);
        }

        private void cursorTool_Click(object sender, EventArgs e) {
            lineToolChecked = arrowToolChecked = false;
        }

        private void freeTool_Click(object sender, EventArgs e) {
            lineToolChecked = arrowToolChecked = false;
        }

        private void stampTool_Click(object sender, EventArgs e) {
            lineToolChecked = arrowToolChecked = false;
        }

        private void eraseTool_Click(object sender, EventArgs e) {
            lineToolChecked = arrowToolChecked = false;
        }

        private void notime_CheckedChanged(object sender, EventArgs e) {
            tactic.getStaticTactic().timer = !notime.Checked;
            staticTimeBar.Enabled = !notime.Checked;
            if (notime.Checked) {
                staticTimeBar.Value = 30;
                time = 900;
                refreshTime();
                refreshMap();
            }
        }

        private void panelChange(object sender, EventArgs e) {
            staticPanel.Visible = dynamicPanel.Visible = playPanel.Visible = false;
            time = (time / 30) * 30;
            if (staticView.Checked) {
                staticPanel.Visible = true;
                if (notime.Checked) {
                    staticTimeBar.Value = 30;
                    time = 900;
                } else {
                    staticTimeBar.Value = time / 30;
                }
            } else if (dynamicView.Checked) {
                dynamicPanel.Visible = true;
                if (dynamicnotimer.Checked) {
                    dynamicTimeBar.Value = 30;
                    time = 900;
                } else {
                    dynamicTimeBar.Value = time / 30;
                }
            } else {
                playPanel.Visible = true;
                playTimeBar.Value = time;
                refreshPlayControllers();
            }
            refreshTime();
            refreshMap();
        }

        private void dynamicnotimer_CheckedChanged(object sender, EventArgs e) {
            tactic.getDynamicTactic().timer = !dynamicnotimer.Checked;
            dynamicTimeBar.Enabled = !dynamicnotimer.Checked;
            if (dynamicnotimer.Checked) {
                dynamicTimeBar.Value = 30;
                time = 900;
                refreshTime();
                refreshMap();
            }
        }

        private void timeBar_Scroll(object sender, EventArgs e) {
            if ((TrackBar)sender == playTimeBar) {
                time = playTimeBar.Value;
            } else {
                time = ((TrackBar)sender).Value * 30;
            }
            refreshTime();
            refreshMap();
        }

        private void refreshPlayControllers() {
            if (tactic == null)
                return;

            play.Enabled = pause.Enabled = stop.Enabled = true;
            if (playStatic.Checked) {
                if (!tactic.getStaticTactic().timer) {
                    play.Enabled = pause.Enabled = stop.Enabled = false;
                    playTimeBar.Value = 900;
                    time = 900;
                }
            } else {
                if (!tactic.getDynamicTactic().timer) {
                    play.Enabled = pause.Enabled = stop.Enabled = false;
                    playTimeBar.Value = 900;
                    time = 900;
                }
            }
            refreshMap();
        }

        private void playMode_CheckedChanged(object sender, EventArgs e) {
            refreshPlayControllers();
        }

        private void stop_Click(object sender, EventArgs e) {
            playTimer.Enabled = false;
            playTimeBar.Value = 900;
            time = 900;
            refreshTime();
            refreshMap();
        }

        private void pause_Click(object sender, EventArgs e) {
            playTimer.Enabled = false;
        }

        private void play_Click(object sender, EventArgs e) {
            if (playTimeBar.Value == 0) {
                return;
            }

            playTimer.Enabled = true;
        }

        private void playSpeed_ValueChanged(object sender, EventArgs e) {
            playTimer.Interval = 300 / (int)playSpeed.Value;
        }

        private void playTimer_Tick(object sender, EventArgs e) {
            if (time == 0) {
                playTimer.Enabled = false;
                return;
            }

            playTimeBar.Value -= 1;
            time -= 1;
            refreshTime();
            refreshMap();
        }

        private void addStatic_Click(object sender, EventArgs e) {
            if (!tactic.getDynamicTactic().addStaticElement((StaticIcon)(cmbDynamicStaticList.SelectedItem))) {
                MessageBox.Show("You've aready added this element.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            refreshMap();
        }

        private void removeDynamicElement_Click(object sender, EventArgs e) {
			if (!tactic.getDynamicTactic().removeStaticElement(((StaticIcon)(cmbDynamicStaticList.SelectedItem)).id)) {
                MessageBox.Show("This item doesn't exist.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            refreshMap();
        }

        private void addTank_Click(object sender, EventArgs e) {
            AddTank addTankWindow = new AddTank(tactic.getTanks());
            addTankWindow.ShowDialog();

            if (addTankWindow.dialogResult != System.Windows.Forms.DialogResult.OK) {
                return;
            }
            
            if (!tactic.getDynamicTactic().addDynamicTank(addTankWindow.tank)) {
                MessageBox.Show("The name must be unique.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            dynamicTankList.Items.Add(addTankWindow.tank);

            refreshMap();
        }

        private void editDynamicTank_Click(object sender, EventArgs e) {
            if (dynamicTankList.SelectedItem == null) {
                return;
            }

			AddTank addTankWindow = new AddTank(tactic.getTanks(), (DynamicTank)((DynamicTank)dynamicTankList.SelectedItem).Clone());
            addTankWindow.ShowDialog();

            if (addTankWindow.dialogResult != System.Windows.Forms.DialogResult.OK) {
                return;
            }

            if (!tactic.getDynamicTactic().editDynamicTank((DynamicTank)dynamicTankList.SelectedItem, addTankWindow.tank)) {
                MessageBox.Show("The name must be unique.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            dynamicTankList.Items[dynamicTankList.SelectedIndex] = addTankWindow.tank;

            refreshMap();
        }

        private void removeDynamicTank_Click(object sender, EventArgs e) {
            if (dynamicTankList.SelectedItem == null) {
                return;
            }

            tactic.getDynamicTactic().removeDynamicTank((DynamicTank)dynamicTankList.SelectedItem);
            dynamicTankList.Items.Remove(dynamicTankList.SelectedItem);

            refreshMap();
        }

        private void dynamicTankList_SelectedIndexChanged(object sender, EventArgs e) {
            refreshDynamicAction();
        }

        private void refreshDynamicAction() {
            if (dynamicTankList.SelectedItem == null) {
                return;
            }

            string actionId = tactic.getDynamicTactic().getDynamicTankActionId(((DynamicTank)dynamicTankList.SelectedItem).name, dynamicTimeBar.Value * 30);
            if (actionId == "") {
                cmbDynamicEvents.SelectedIndex = 0;
            } else {
                cmbDynamicEvents.SelectedItem = tactic.getIcons().getDynamicIcon(actionId);
            }

            bool alive = tactic.getDynamicTactic().isAlive(((DynamicTank)dynamicTankList.SelectedItem).name, dynamicTimeBar.Value * 30);
            if (alive) {
                aliveStatus.Text = "Alive";
            } else {
                aliveStatus.Text = "Dead";
            }
        }

        private void removeDynamicCurrentPosition_Click(object sender, EventArgs e) {
            if (dynamicTankList.SelectedItem == null) {
                return;
            }

            tactic.getDynamicTactic().removeDynamicPosition(((DynamicTank)dynamicTankList.SelectedItem).name, dynamicTimeBar.Value * 30);

            refreshMap();
        }

        private void aliveStatus_Click(object sender, EventArgs e) {
            if (dynamicTankList.SelectedItem == null) {
                return;
            }

            if (aliveStatus.Text == "Alive") {
				tactic.getDynamicTactic().setKill(((DynamicTank)dynamicTankList.SelectedItem).name, dynamicTimeBar.Value * 30);
            } else {
				tactic.getDynamicTactic().setKill(((DynamicTank)dynamicTankList.SelectedItem).name, -1);
            }

            refreshDynamicAction();
            refreshMap();
        }

        private void cmbDynamicEvents_SelectedIndexChanged(object sender, EventArgs e) {
            if (dynamicTankList.SelectedItem == null) {
                return;
            }

			tactic.getDynamicTactic().setDynamicTankAction(((DynamicTank)dynamicTankList.SelectedItem).name, dynamicTimeBar.Value * 30, ((DynamicIcon)cmbDynamicEvents.SelectedItem).id);

            refreshMap();
        }

        private void dynamicIconsSize_ValueChanged(object sender, EventArgs e) {
            refreshMap();
        }

        private void playIconSize_ValueChanged(object sender, EventArgs e) {
            refreshMap();
        }

        private void lblPlayTextColor_Click(object sender, EventArgs e) {
            ColorDialog cd = new ColorDialog();
            DialogResult result = cd.ShowDialog();
            if (result == DialogResult.OK) {
                if (cd.Color.Equals(((Bitmap)TacticPlanner.Properties.Resources.clearTactics).GetPixel(1, 1))) {
                    cd.Color = Color.FromArgb(cd.Color.R + 1, cd.Color.G + 1, cd.Color.B + 1);
                }
				lblDynamicTextColor.BackColor = lblPlayTextColor.BackColor = cd.Color;
				tactic.getDynamicTactic().setPenColor(cd.Color);
            }
            refreshMap();
        }

        private void lblDynamicTextColor_Click(object sender, EventArgs e) {
            ColorDialog cd = new ColorDialog();
            DialogResult result = cd.ShowDialog();
            if (result == DialogResult.OK) {
                if (cd.Color.Equals(((Bitmap)TacticPlanner.Properties.Resources.clearTactics).GetPixel(1, 1))) {
                    cd.Color = Color.FromArgb(cd.Color.R + 1, cd.Color.G + 1, cd.Color.B + 1);
				}
				lblDynamicTextColor.BackColor = lblPlayTextColor.BackColor = cd.Color;
				tactic.getDynamicTactic().setPenColor(cd.Color);
            }
            refreshMap();
        }

        private void showTextToolStripMenuItem_Click(object sender, EventArgs e) {
            showPlayernameToolStripMenuItem.Checked = !showPlayernameToolStripMenuItem.Checked;
			tactic.getDynamicTactic().ShowPlayerName = showPlayernameToolStripMenuItem.Checked;
            refreshMap();
        }

        private void showTankTypeToolStripMenuItem_Click(object sender, EventArgs e) {
            showTankTypeToolStripMenuItem.Checked = !showTankTypeToolStripMenuItem.Checked;
			tactic.getDynamicTactic().ShowTankName = showTankTypeToolStripMenuItem.Checked;
            refreshMap();
        }

        private void loadMapToolStripMenuItem_Click(object sender, EventArgs e) {
            staticPanel.Enabled = dynamicPanel.Enabled = playPanel.Enabled = false;

            OpenFileDialog od = new OpenFileDialog();
            od.Filter = "Tactic planner file (*.tactic)|*.tactic";
            DialogResult result = od.ShowDialog();
            if (result == DialogResult.OK) {
                try {
                    tactic.load(od.FileName);
                } catch (Exception ex) {
                    MessageBox.Show("Error: unabe to load tactic file. Reason: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    drawBox.Image = null;
                    mapBox.Image = null;
                    return;
                }
            }

            initFromTactic();
        }

        private void saveMapToolStripMenuItem_Click(object sender, EventArgs e) {
            if (tactic == null)
                return;

            SaveFileDialog sd = new SaveFileDialog();
            sd.Filter = "Tactic planner file (*.tactic)|*.tactic";
            DialogResult result = sd.ShowDialog();
            if (result == DialogResult.OK) {
                try {
                    tactic.save(sd.FileName);
                } catch (Exception ex) {
                    MessageBox.Show("Error: unabe to save tactic file. Reason: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
        }

		private void thickness_ValueChanged(object sender, EventArgs e) {
			tactic.getStaticTactic().setThickness((int)thickness.Value);
		}

		private void originalToolStripMenuItem_Click(object sender, EventArgs e) {
			if (!originalToolStripMenuItem.Checked) {
				originalToolStripMenuItem.Checked = true;
				hDToolStripMenuItem.Checked = false;
				refreshMapPack();
			}
		}

		private void hDToolStripMenuItem_Click(object sender, EventArgs e) {
			if (!hDToolStripMenuItem.Checked) {
				hDToolStripMenuItem.Checked = true;
				originalToolStripMenuItem.Checked = false;
				refreshMapPack();
			}
		}

		private void refreshMapPack() {
			if (originalToolStripMenuItem.Checked) {
				tactic.setMapPack(types.MapPack.Original);
			} else {
				tactic.setMapPack(types.MapPack.HD);
			}

			if (tactic.isLoaded()) {
				mapBox.Image = tactic.getMap().getMapImage();
				tactic.getDynamicTactic().redrawStaticItems();
				refreshMap();
			}
		}

    }
}
