using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using travel_app.src;

namespace travel_app {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private Connect connect;

        public MainWindow() {
            InitializeComponent();
            connect = new Connect();
        }

        private void LocFrom_TextChanged(object sender, TextChangedEventArgs e) {
            LocFromView.ItemsSource = connect.GetLocations(LocFrom.Text, Direction.From);
        }

        private void LocTo_TextChanged(object sender, TextChangedEventArgs e) {
            LocToView.ItemsSource = connect.GetLocations(LocTo.Text, Direction.To);
        }

        private void FindBtn_Click(object sender, RoutedEventArgs e) {
            if (IsFind()) {
                FlightView.ItemsSource = connect.GetFlights(
                    LocFromView.SelectedIndex, LocToView.SelectedIndex, (DateTime)FlightDate.SelectedDate);
            }
        }

        private void FindBtnCompl_Click(object sender, RoutedEventArgs e) {
            if (IsFind()) {
                FlightView.ItemsSource = connect.GetDijkstraFlights(
                    LocFromView.SelectedIndex, LocToView.SelectedIndex, (DateTime)FlightDate.SelectedDate);
            }
        }

        private bool IsFind() {
            return (LocFromView.SelectedIndex != -1) && (LocToView.SelectedIndex != -1) && (FlightDate.SelectedDate != null);
        }

        private void CopyBtn_Click(object sender, RoutedEventArgs e) {
            var items = FlightView.Items;
            StringBuilder buffer = new StringBuilder();

            foreach (var item in items) {
                buffer.Append(item.ToString());
                buffer.Append("\n");
            }

            Clipboard.SetText(buffer.ToString());
        }

        private void NoFlightBtn_Click(object sender, RoutedEventArgs e) {
            FlightView.ItemsSource = connect.GetNoFlightInfo(LocFrom.Text);
        }
    }
}
