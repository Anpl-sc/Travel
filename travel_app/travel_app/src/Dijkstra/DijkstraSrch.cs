using amadeus;
using amadeus.exceptions;
using amadeus.resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace travel_app.src.Dijkstra {

    class DijkstraSrch {
        private Amadeus AmadeusObj { get; set; }
        private string Date { get; set; }
        private Node Start { get; set; }
        private Node End { get; set; }
        public int NodeVisits { get; private set; }
        public double ShortestPathCost { get; private set; }
        public List<Node> ShortestPath { get; set; }

        public DijkstraSrch(Amadeus amadeus, string start, string end, string date) {
            AmadeusObj = amadeus;
            Start = new Node(start);
            End = new Node(end);
            Date = date;
            GetShortestPath();
        }

        private void GetShortestPath() {
            DijkstraBuildMap();
            BuilShortestPath();
        }

        private void BuilShortestPath() {
            var shortestPath = new List<Node>();
            shortestPath.Add(End);
            FillPath(shortestPath, End);
            shortestPath.Reverse();
            ShortestPathCost = End.MinCostToStart.Value;
            ShortestPath = shortestPath;
        }

        private void FillPath(List<Node> list, Node node) {
            if (node.NearestToStart == null)
                return;
            list.Add(node.NearestToStart);
            FillPath(list, node.NearestToStart);
        }

        private void DijkstraBuildMap() {
            NodeVisits = 0;
            Start.MinCostToStart = 0;
            var prioQueue = new List<Node>();
            prioQueue.Add(Start);
            do {
                NodeVisits++;
                prioQueue = prioQueue.OrderBy(q => q.MinCostToStart.Value).ToList();
                var node = prioQueue.First();
                prioQueue.Remove(node);

                node.Connections = FindConnections(node.Name);
                foreach (var cnn in node.Connections) {
                    var childNode = cnn.ConnectedNode;
                    if (childNode.Visited)
                        continue;
                    if (childNode.MinCostToStart == null ||
                        node.MinCostToStart + cnn.Cost < childNode.MinCostToStart) {
                        childNode.MinCostToStart = node.MinCostToStart + cnn.Cost;
                        childNode.NearestToStart = node;
                        if (!prioQueue.Contains(childNode))
                            prioQueue.Add(childNode);
                    }
                }
                node.Visited = true;
                if (node.Name == End.Name) {
                    End = node;
                    return;
                }

            } while (prioQueue.Any());
        }

        private List<Line> FindConnections(string name) {
            var connections = new List<Line>();
            FlightDestination[] flightDestinations = null;

            flightDestinations = AmadeusObj.shopping.flightDestinations.get(Params
                .with("origin", name)
                .and("departureDate", Date)
                .and("oneWay", "true"));

            if (flightDestinations != null) {
                foreach (var dest in flightDestinations) {
                    var connection = new Line {
                        ConnectedNode = new Node(dest.destination),
                        Cost = dest.price.total
                    };
                    connections.Add(connection);
                }
            }
            return connections.OrderBy(c => c.Cost).ToList();
        }
    }
}
