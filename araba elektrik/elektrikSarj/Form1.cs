using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;

namespace elektrikSarj
{
    public partial class Form1 : Form
    {
        // Şehirler ve koordinat + ülke kodları
        private Dictionary<string, (double lat, double lon, string countryCode)> cities = new Dictionary<string, (double, double, string)>
        {
            { "Istanbul", (41.0082, 28.9784, "TR") },
            { "Paris", (48.8566, 2.3522, "FR") },
            { "Berlin", (52.5200, 13.4050, "DE") },
            { "Hamburg", (53.5511, 9.9937, "DE") },
            { "Munich", (48.1351, 11.5820, "DE") },
            { "Cologne", (50.9375, 6.9603, "DE") },
            { "Frankfurt", (50.1109, 8.6821, "DE") }
        };

        private string apiKey = "";//API yaz
        private JArray stations = new JArray(); // API’den dönen tüm istasyonlar
        private List<JToken> visibleStations = new List<JToken>(); // Filtrelenmiş istasyonlar
        private HttpClient client = new HttpClient();

        public Form1()
        {
            InitializeComponent();
            this.Load += Form1_Load;
            this.btnSearch.Click += async (s, e) => await LoadStations();
            this.listBox1.SelectedIndexChanged += ShowStationDetails;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();
            foreach (var city in cities.Keys)
                comboBox1.Items.Add(city);
            comboBox1.SelectedIndex = 0;
        }

        private async Task LoadStations()
        {
            listBox1.Items.Clear();
            listBox2.Items.Clear();
            visibleStations.Clear();

            string selectedCity = comboBox1.Text.Trim();
            if (!cities.ContainsKey(selectedCity))
            {
                MessageBox.Show("Geçerli bir şehir seçin.");
                return;
            }

            var (lat, lon, countryCode) = cities[selectedCity];

            string url = $"https://api.openchargemap.io/v3/poi/?output=json&countrycode={countryCode}&latitude={lat}&longitude={lon}&distance=15&maxresults=100&key={apiKey}";

            try
            {
                string response = await client.GetStringAsync(url);
                stations = JArray.Parse(response);

                foreach (var station in stations)
                {
                    var info = station["AddressInfo"];
                    if (info == null) continue;

                    double stationLat = (double)info["Latitude"];
                    double stationLon = (double)info["Longitude"];
                    string country = info["Country"]?["ISOCode"]?.ToString() ?? "";

                    // Şehir merkezine 15 km’den fazla veya ülke farklı ise atla
                    if (country != countryCode || GetDistance(lat, lon, stationLat, stationLon) > 15) continue;

                    visibleStations.Add(station);

                    string title = info["Title"]?.ToString() ?? "Bilinmiyor";
                    string town = info["Town"]?.ToString() ?? "";
                    listBox1.Items.Add($"{title} - {town}");
                }

                if (listBox1.Items.Count == 0)
                    listBox1.Items.Add("Bu şehirde şarj istasyonu bulunamadı.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("API hatası: " + ex.Message);
            }
        }

        private void ShowStationDetails(object sender, EventArgs e)
        {
            int index = listBox1.SelectedIndex;
            listBox2.Items.Clear();

            if (index < 0 || index >= visibleStations.Count) return;

            var info = visibleStations[index]["AddressInfo"];
            if (info == null) return;

            listBox2.Items.Add($"Title: {info["Title"] ?? "Bilinmiyor"}");
            listBox2.Items.Add($"AddressLine1: {info["AddressLine1"] ?? "Bilinmiyor"}");
            listBox2.Items.Add($"AddressLine2: {info["AddressLine2"] ?? "Bilinmiyor"}");
            listBox2.Items.Add($"Town: {info["Town"] ?? "Bilinmiyor"}");
            listBox2.Items.Add($"StateOrProvince: {info["StateOrProvince"] ?? "Bilinmiyor"}");
            listBox2.Items.Add($"Postcode: {info["Postcode"] ?? "Bilinmiyor"}");
            listBox2.Items.Add($"Country: {info["Country"]?["Title"] ?? "Bilinmiyor"}");
        }

        // Haversine formülü ile iki koordinat arasındaki mesafe (km)
        private double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371; // km
            double dLat = (lat2 - lat1) * Math.PI / 180;
            double dLon = (lon2 - lon1) * Math.PI / 180;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return 2 * R * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }

       
    }
}
