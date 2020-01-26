using amadeus;
using amadeus.exceptions;
using amadeus.resources;
using amadeus.util;
using ApiKey;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using travel_app.src.Dijkstra;

namespace travel_app.src {
    public class Connect {
        private const int limit = 1000;
        private char[] alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

        private Amadeus amadeus;
        private List<Location> locFrom = new List<Location>();
        private List<Location> locTo = new List<Location>();

        public Connect() {
            var apiKey = AmadeusKey.Key;
            var apiSecret = AmadeusKey.Secret;

            amadeus = Amadeus
                .builder(apiKey, apiSecret)
                .build();
        }

        public ObservableCollection<string> GetLocations(string str, Direction direction) {
            var locList = new ObservableCollection<string>();
            var locations = GetLocList(direction);

            if ((str != "")&&(locations != null)) {
                locations.Clear();
                try {
                    locations.AddRange(amadeus.referenceData.locations.get(Params
                        .with("keyword", str)
                        .and("subType", resources.referenceData.Locations.ANY)).ToList());

                    foreach (Location loc in locations) {
                        locList.Add($"{loc.subType}: {loc.detailedName}");
                    }
                } catch (ResponseException e) {
                    locList.Add(e.Message);
                }

            } else {
                locList = null;
            }
            return locList;
        }

        private List<Location> GetLocList(Direction direction) {
            List<Location> list = null;
            switch (direction) {
                case Direction.From:
                    list = locFrom;
                    break;
                case Direction.To:
                    list = locTo;
                    break;
            }
            return list;
        }

        public ObservableCollection<string> GetFlights(int ixFrom, int ixTo, DateTime flightDate) {
            ObservableCollection<string> flightsList = new ObservableCollection<string>();
            FlightOffer[] flightOffers = null;

            try {
                flightOffers = amadeus.shopping.flightOffers.get(Params
                    .with("origin", locFrom[ixFrom].iataCode)
                    .and("destination", locTo[ixTo].iataCode)
                    .and("departureDate", flightDate.ToString("yyyy-MM-dd")));
            } catch (ResponseException e) {
                flightsList.Add(e.Message);
                return flightsList;
            }

            foreach (FlightOffer offer in flightOffers) {
                foreach (FlightOffer.OfferItem item in offer.offerItems) {
                    string flight = $"Price {item.price.total}| Taxes {item.price.totalTaxes}| ";

                    foreach (FlightOffer.Service service in item.services) {
                        foreach (FlightOffer.Segment seg in service.segments) {
                            flight += $"{seg.flightSegment.departure.iataCode}({seg.flightSegment.departure.at}) " +
                                $"-> { seg.flightSegment.arrival.iataCode}({seg.flightSegment.arrival.at})| ";
                        }
                    }

                    flightsList.Add(flight);
                }
            }
            return flightsList;
        }

        public ObservableCollection<string> GetDijkstraFlights(int ixFrom, int ixTo, DateTime depDate) {
            ObservableCollection<string> flightsList = new ObservableCollection<string>();
            DijkstraSrch dijkstra = null;

            try {
                dijkstra = new DijkstraSrch(amadeus, locFrom[ixFrom].iataCode, locTo[ixTo].iataCode,
                depDate.ToString("yyyy-MM-dd"));
            } catch (ResponseException e) {
                flightsList.Add(e.Message);
                return flightsList;
            }

            flightsList.Add($"Price {dijkstra.ShortestPathCost}| Checked locations {dijkstra.NodeVisits}| ");
            List<Node> path = dijkstra.ShortestPath;
            for (int i = 0; i < (path.Count-1); i++) {
                flightsList.Add($"{path[i].Name} -> {path[i+1].Name}");
            }
            return flightsList;
        }

        public ObservableCollection<string> GetNoFlightInfo(string testCode) {
            if (testCode == "") {
                return null;
            }

            ObservableCollection<string> infoList = new ObservableCollection<string>();
            SortedDictionary<string, string> locInfo = new SortedDictionary<string, string>();

            try {
                FindCities(locInfo);
            } catch (ResponseException e) {
                infoList.Add(e.Message);
                return infoList;
            }

            infoList.Add("Cities total " + locInfo.Count());

            try {
                FindNoFlights(infoList, locInfo, testCode);
            } catch (ResponseException e) {
                infoList.Clear();
                infoList.Add(e.Message);
                return infoList;
            }

            return infoList;
        }

        private void FindCities(SortedDictionary<string, string> locInfo) {
            foreach (var letter in alpha) {
                int offset = 0;
                bool doMore = true;

                while (doMore) {
                    Location[] locTest = amadeus.referenceData.locations.get(Params
                        .with("keyword", letter.ToString())
                        .and("subType", resources.referenceData.Locations.CITY)
                        .and("view", "LIGHT")
                        .and("page[limit]", limit.ToString())
                        .and("page[offset]", offset.ToString()));

                    foreach (Location loc in locTest) {
                        if (!locInfo.ContainsKey(loc.iataCode)) {
                            locInfo.Add(loc.iataCode, loc.detailedName);
                        }
                    }

                    if (locTest.Length < limit) {
                        doMore = false;
                    } else {
                        offset += limit;
                    }
                }
            }
        }

        private void FindNoFlights(ObservableCollection<string> infoList, SortedDictionary<string, string> locInfo, string testCode) {
            ObservableCollection<string> noDirectList = new ObservableCollection<string>();

            foreach (string code in locInfo.Keys) {
                FlightOffer[] flightOffers = null;

                try {
                    flightOffers = amadeus.shopping.flightOffers.get(Params
                    .with("origin", testCode)
                    .and("destination", code)
                    .and("departureDate", DateTime.Today.ToString("yyyy-MM-dd")));
                } catch (ResponseException e) {
                    if (e.Message.Equals("[400]\n[origin/destination/date(s) combination]No journey found for requested itinerary")) {
                        infoList.Add($"No flights for {locInfo[code]}|{testCode} - {code}");
                        continue;
                    } else throw;
                }

                if (flightOffers != null) {
                    bool noDirect = GetNoDirect(flightOffers);
                    if (noDirect) {
                        noDirectList.Add($"No direct flights for {locInfo[code]}|{testCode} - {code}");
                    }
                }
            }

            infoList.Concat(noDirectList);
        }

        private bool GetNoDirect(FlightOffer[] flightOffers) {
            bool noDirect = true;

            foreach (FlightOffer offer in flightOffers) {
                if (noDirect) {
                    foreach (FlightOffer.OfferItem item in offer.offerItems) {
                        foreach (FlightOffer.Service service in item.services) {
                            if (service.segments.Length < 2) {
                                noDirect = false;
                            }
                        }
                    }
                } else {
                    break;
                }
            }

            return noDirect;
        }
    }
}
