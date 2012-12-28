using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TacticPlanner.models {
    public class Tank : ICloneable {
        public string id { get; set; }
        public string nation { get; set; }
        public string name { get; set; }
        public TankTypes type { get; set; }
        public string filename { get; set; }

        public Tank(string _id, string _nation = "", string _name = "", TankTypes _type = TankTypes.Heavy, string _filename = "") {
            this.id = _id;
            this.nation = _nation;
            this.name = _name;
            this.type = _type;
            this.filename = _filename;
        }

        public object Clone() {
            return new Tank(
                this.id,
                this.nation,
                this.name,
                this.type,
                this.filename
            );
        }
    }
}
