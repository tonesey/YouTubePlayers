using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Threading;
using System.Resources;
using System.Reflection;

namespace Centapp.CartoonCommon.Utility
{
    public class MyResourceManager
    {
        private Dictionary<string, Dictionary<CultureInfo, string>> _dict = new Dictionary<string, Dictionary<CultureInfo, string>>();

        private CultureInfo _defaultCulture = new CultureInfo("en-US");
        private CultureInfo _currentCulture = null;

        //public MyResourceManager(XDocument doc)
        //    : this(doc, Thread.CurrentThread.CurrentCulture, new CultureInfo("en-US"))
        //{
        //}

        //public MyResourceManager(XDocument doc, CultureInfo currentCulture)
        //    : this(doc, currentCulture, new CultureInfo("en-US"))
        //{
        //}

        public MyResourceManager(XDocument doc, CultureInfo currentCulture, CultureInfo defaultCulture)
        {
            _currentCulture = currentCulture;
            _defaultCulture = defaultCulture;
            Parse(doc);
        }

        private void Parse(XDocument doc)
        {
            var items = doc.Element("root").Descendants("item");
            int index = 0;
            foreach (var item in items)
            {
                //int idValue = int.Parse(item.Attribute("id").Value);
                index++;
                string dictKey = string.Format("ep_{0}", index < 10 ? index.ToString().PadLeft(2, '0') : index.ToString());

                Dictionary<CultureInfo, string> dictKeyTranslations = new Dictionary<CultureInfo, string>();

                var translatedEls = item.Element("desc").Elements("descItem");

                if (translatedEls.Count() > 0)
                {
                    //è un'app con titoli localizzati su più lingua (es. Pingu)
                    foreach (var trItem in translatedEls)
                    {
                        dictKeyTranslations.Add(new CultureInfo(trItem.Attribute("lang").Value), 
                                                trItem.Attribute("value").Value);
                    }
                }
                else
                {
                    //è un'app con titoli localizzati in una lingua specifica (es. Peppa Pig)
                    dictKeyTranslations.Add(_currentCulture, item.Element("desc").Value);
                }
                _dict.Add(dictKey, dictKeyTranslations);
            }
        }

        public string GetString(string key)
        {
            return GetString(key, _currentCulture);
        }

        public string GetString(string key, CultureInfo culture)
        {
            string translatedValue = key;

            if (!(_dict.ContainsKey(key)))
            {
                return translatedValue; //chiave non trovata
            }

            if (!_dict[key].ContainsKey(culture))
            {

                //tentativo con invariant culture della culture specificata
                var invCultureLang = culture.TwoLetterISOLanguageName;
                var invCultureKey = _dict[key].Keys.FirstOrDefault(l => l.TwoLetterISOLanguageName == invCultureLang);
                if (invCultureKey != null)
                {
                    var invCultureStringVal = _dict[key][invCultureKey];
                    return invCultureStringVal;
                }

                return _dict[key][_defaultCulture]; //return def culture string
            }

            translatedValue = _dict[key][culture];

            return translatedValue;
        }

    }
}
