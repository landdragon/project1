using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using System.Xml.Linq;
using CsvHelper;
using ImportRSS.Annotations;
using ImportRSS.Helpers;
using ImportRSS.Model;

namespace ImportRSS.ViewModel
{
    public class MainWindowViewModel : INotifyPropertyChanged

    {
        readonly List<string> Rss = new List<string>
        {
            "http://lbc2rss.superfetatoire.com/rss/60365-plessis.rss",
            "http://lbc2rss.superfetatoire.com/rss/60402-vincennes.rss",
            "http://lbc2rss.superfetatoire.com/rss/60403-villiers-sur-marne.rss",
            "http://lbc2rss.superfetatoire.com/rss/60404-champigny-sur-marne.rss","http://lbc2rss.superfetatoire.com/rss/60405-le-perreux-sur-marne.rss",
            "http://lbc2rss.superfetatoire.com/rss/60406-noisy-le-grand.rss",
            "http://lbc2rss.superfetatoire.com/rss/60407-fontenay-sous-bois.rss",

        };
        public MainWindowViewModel()
        {
            RunCommand = new RelayCommand(Run);
            ExportCommand = new RelayCommand(Export);
            _timer = new DispatcherTimer {Interval = TimeSpan.FromSeconds(1)};
            _timer.Tick += CollectData;
        }

        private void CollectData(object sender, EventArgs e)
        {
            AddLineLog("start CollectData");
            foreach (var rss in Rss)
            {
                var feedXML = XDocument.Load(rss);
                AnalyzeRss(feedXML);
            }
            AddLineLog("End CollectData");


            Run(null);
        }

        private void AnalyzeRss(XDocument feedXML)
        {
            var channel = feedXML.Descendants("channel").First();
            var title = channel.Element("title").Value;

            AddLineLog($"start analyze {title}");
            var listElement = new List<Element>();
            foreach (var descendant in feedXML.Descendants("item"))
            {
                var element = AnalyzeElement(descendant);
                listElement.Add(element);
                OnPropertyChanged(nameof(Elements));
            }
            Elements.AddRange(listElement);
            var date = DateTime.Parse(channel.Element("pubDate").Value);
            ExportRss($"{title}.{date.ToString("yyyy-MM-dd hh.mm.ss")}.csv", listElement);

            AddLineLog($"End analyze {title}");
        }

        private Element AnalyzeElement(XElement descendant)
        {
            var element = new Element {Title = descendant.Element("title").Value};
            AnalyseUrl(descendant.Element("link").Value, element);
            XElement description = descendant.Element("description");
            AnayseDescription(description.Value, element);
            element.DateCreation = DateTime.Parse(descendant.Element("pubDate").Value);
            return element;
        }

        private void AnalyseUrl(string url, Element element)
        {
            element.Url = url;
            var http = new HttpClient();
            var response = http.GetByteArrayAsync(url).Result;
            var source = Encoding.GetEncoding("iso-8859-1").GetString(response, 0, response.Length - 1);
            source = WebUtility.HtmlDecode(source);
            var descritionSplited = Regex.Match(source, @"itemprop=""description"">(.*)<\/p>");
            element.Description = descritionSplited.Groups[1].Value.Replace("<br>", "\n");
        }

        private void AnayseDescription(string descrition, Element element)
        {
            var descritionSplited = Regex.Match(descrition, @"<h2>.*<\/h2>.*<strong>Prix : <\/strong>(.*)<\/h3><p><strong>");
            element.Price = double.Parse(descritionSplited.Groups[1].Value);
        }

        private readonly DispatcherTimer _timer;

        #region action property

        public RelayCommand RunCommand { get; set; }
        public RelayCommand ExportCommand { get; set; }

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

        private void Export(object param)
        {
            AddLineLog("start Export");
            ExportRss("file.csv", Elements);

            AddLineLog("End Export");
        }

        private static void ExportRss(string fileName, IEnumerable<Element> listElement)
        {
            using (var csv = new CsvWriter(new StreamWriter(fileName, false, Encoding.UTF8)))
            {
                csv.Configuration.Delimiter = ";";
                csv.Configuration.Encoding = Encoding.UTF8;
                csv.WriteRecords(listElement);
            }
        }

        #endregion //action Method


        private void AddLineLog(string log)
        {
            var logBuilder = new StringBuilder(Log);
            logBuilder.AppendLine(log);
            Log = logBuilder.ToString();
        }


        private string _log;
        private List<Element> _elements = new List<Element>();

        public string Log
        {
            get { return _log; }
            set
            {
                _log = value;
                OnPropertyChanged(nameof(Log));
            }
        }

        public List<Element> Elements
        {
            get { return _elements; }
            set
            {
                _elements = value;
                OnPropertyChanged(nameof(Elements));
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