using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TacticPlanner.models {
    class Map {
        public string id { get; set; }
        public string name { get; set; }
        public string filename { get; set; }

        public Map(string _id, string _name = "", string _filename = "") {
            this.id = _id;
            this.name = _name;
            this.filename = _filename;
        }
    }
}
