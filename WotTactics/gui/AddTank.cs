using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using TacticPlanner.models;

namespace TacticPlanner.gui {
    public partial class AddTank : Form {
        private Tanks tanks;
        public DialogResult dialogResult;

        public DynamicTank tank;

        public AddTank(Tanks _tanks, DynamicTank _tank = null) {
            InitializeComponent();

            tanks = _tanks;

            if (_tank == null) {
                tank = new DynamicTank();
                tank.positions.Add(900, new Point(500, 500));
            } else {
                tank = _tank;
                add.Text = "Modify";
            }

            dialogResult = System.Windows.Forms.DialogResult.Cancel;
        }

        private void AddTank_Load(object sender, EventArgs e) {
            string[] nations = tanks.getNations();

            foreach (string nation in nations) {
                cmbnation.Items.Add(nation);
            }
            cmbnation.SelectedIndex = 1;

            cmbTank.ValueMember = "id";
            cmbTank.DisplayMember = "name";

            fillTankCombo();

            if (tank.tank != null) {
                playername.Text = tank.name;
                chkisAlly.Checked = tank.isAlly;
                chkisEnemy.Checked = !tank.isAlly;
                cmbnation.SelectedItem = tank.tank.nation;
                fillTankCombo();
                cmbTank.SelectedItem = tank.tank;
            }
        }

        private void cancel_Click(object sender, EventArgs e) {
            Close();
        }

        private void add_Click(object sender, EventArgs e) {
            if (playername.Text == "") {
                MessageBox.Show("You must fill the Player name box.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            tank.name = playername.Text;
            tank.isAlly = chkisAlly.Checked;
            tank.tank = tanks.getTank(((Tank)(cmbTank.SelectedItem)).id);
            dialogResult = System.Windows.Forms.DialogResult.OK;

            Close();
        }

        private void cmbnation_SelectedIndexChanged(object sender, EventArgs e) {
            fillTankCombo();
        }

        private void fillTankCombo() {
            Tank[] tankList = tanks.getSortedTanks((string)cmbnation.SelectedItem);
            cmbTank.Items.Clear();
            foreach (Tank tank in tankList) {
                cmbTank.Items.Add(tank);
            }
            cmbTank.SelectedIndex = 0;
        }
    }
}
