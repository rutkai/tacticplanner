using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace TacticPlanner.models {
    public class DynamicTank : ICloneable {
        public string name { get; set; }
        public bool isAlly { get; set; }
        public Tank tank { get; set; }
        public SortedList<int, Point> positions { get; set; }
        public SortedList<int, string> actions { get; set; }
        public int killTime { get; set; }

        public string listName {
            get {
                return name + " (" + tank.name + ")";
            }
        }

        public DynamicTank() {
            positions = new SortedList<int, Point>();
            actions = new SortedList<int, string>();

            killTime = -30;
        }

        #region ICloneable Members

        public object Clone() {
            DynamicTank clone = new DynamicTank();
            clone.name = this.name;
            clone.isAlly = this.isAlly;
            clone.tank = (Tank)this.tank.Clone();
            foreach (KeyValuePair<int, Point> item in positions) {
                clone.positions.Add(item.Key, new Point(item.Value.X, item.Value.Y));
            }
            foreach (KeyValuePair<int, string> item in actions) {
                clone.actions.Add(item.Key, item.Value);
            }
            clone.killTime = this.killTime;
            return clone;
        }

        #endregion
    }
}
