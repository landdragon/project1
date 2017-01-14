using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Xml.Linq;
using ImportRSS.Annotations;
using ImportRSS.Helpers;

namespace ImportRSS.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged

    {
        public MainWindowViewModel()
        {
            RunCommand = new RelayCommand(Run);
            _timer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(1)};
            _timer.Tick += CollectData;
        }

        private void CollectData(object sender, EventArgs e)
        {
            //var feedXML = XDocument.Load("http://lbc2rss.superfetatoire.com/rss/60358-test1-ventes-immobilieres-ile-de-france-occasions.rss");
            var feedXML = XDocument.Load("http://lbc2rss.superfetatoire.com/rss/60365-plessis.rss");

            var channel = feedXML.Descendants("channel").First();
            AddLineLog("Channel title : " + channel.Element("title").Value);
            AddLineLog("Refresh date : " + channel.Element("pubDate").Value);
            foreach (var descendant in feedXML.Descendants("item"))
            {
                AddLineLog(" title : " + descendant.Element("title").Value);
                AnalyseUrl(descendant.Element("link").Value);
                XElement description = descendant.Element("description");
                AnayseDescription(description.Value);
                AddLineLog(" Date de creation : " + descendant.Element("pubDate").Value);
            }
        }

        private async void AnalyseUrl(string url)
        {
            AddLineLog(" URL : " + url);
            HttpClient http = new HttpClient();
            var response = await http.GetByteArrayAsync(url);
            String source = Encoding.GetEncoding("iso-8859-1").GetString(response, 0, response.Length - 1);
            source = WebUtility.HtmlDecode(source);
            var descritionSplited = Regex.Match(source, @"itemprop=""description"">(.*)<\/p>");

            AddLineLog($"  Vrai description : {descritionSplited.Groups[1].Value.Replace("<br>","\n")}");


        }

        private void AnayseDescription(string descrition)
        {
            AddLineLog(" description : ");
            var descritionSplited = Regex.Match(descrition, @"<h2>(.*)<\/h2>.*<strong>Prix : <\/strong>(.*)<\/h3><p><strong>");

            AddLineLog($"  Title : {descritionSplited.Groups[1].Value}");
            AddLineLog($"  Prix : {descritionSplited.Groups[2].Value}");

        }

        private readonly DispatcherTimer _timer;

        #region action property
        public RelayCommand RunCommand { get; set; }
        #endregion //action property
        #region action Method

        private void Run(object param)
        {
            if (_timer.IsEnabled)
            {
                _timer.Stop();
                AddLineLog("Timer stopped");
                OnPropertyChanged(nameof(ButtonRunName));
            }
            else
            {
                _timer.Start();
                AddLineLog("Timer start");
                OnPropertyChanged(nameof(ButtonRunName));
            }
        }


        private void AddLineLog(string log)
        {
            var logBuilder = new StringBuilder(Log);
            logBuilder.AppendLine(log);
            Log = logBuilder.ToString();
        }

        #endregion //action Method

        private string _log;

        public string Log
        {
            get { return _log; }
            set
            {
                _log = value;
                OnPropertyChanged(nameof(Log));
            }
        }

        public string ButtonRunName => _timer.IsEnabled ? "Stop" : "Run";

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}