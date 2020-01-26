using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace travel_app.src.Dijkstra {
    public class Node {
        public string Name { get; private set; }
        public double? MinCostToStart { get; set; }
        public Node NearestToStart { get; set; }
        public bool Visited { get; set; }
        public List<Line> Connections { get; set; }

        public Node(string name) {
            Name = name;
        }
    }
}
