using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace CraigslistStudy
{
    class Program
    {
        static string rootUrl = @"http://seattle.craigslist.org";

        static string outputFile = @"C:\craigslist.txt";

        static List<string> phrases = new List<string>();

        static List<string> terms = new List<string>();

        static Dictionary<string, int> wordCounts = new Dictionary<string, int>();

        static void Main(string[] args)
        {
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }

            LoadTerms();

            ProcessSubSite("/acc/");
            ProcessSubSite("/bus/");
            ProcessSubSite("/csr/");
            ProcessSubSite("/edu/");
            ProcessSubSite("/egr/");
            ProcessSubSite("/eng/");
            ProcessSubSite("/etc/");
            ProcessSubSite("/fbh/");
            ProcessSubSite("/gov/");
            ProcessSubSite("/hea/");
            ProcessSubSite("/hum/");
            ProcessSubSite("/lab/");
            ProcessSubSite("/lgl/");
            ProcessSubSite("/mar/");
            ProcessSubSite("/med/");
            ProcessSubSite("/mnu/");
            ProcessSubSite("/npo/");
            ProcessSubSite("/ofc/");
            ProcessSubSite("/rej/");
            ProcessSubSite("/ret/");
            ProcessSubSite("/sad/");
            ProcessSubSite("/sci/");
            ProcessSubSite("/sec/");
            ProcessSubSite("/sls/");
            ProcessSubSite("/sof/");
            ProcessSubSite("/spa/");
            ProcessSubSite("/tch/");
            ProcessSubSite("/tfr/");
            ProcessSubSite("/trd/");
            ProcessSubSite("/trp/");
            ProcessSubSite("/web/");
            ProcessSubSite("/wri/");

            Console.WriteLine("Done.");
            Console.ReadLine();
        }

        private static void LoadTerms()
        {
            Console.WriteLine("Loading terms...");
            using (TextReader file = new StreamReader("Terms.txt"))
            {
                string term = file.ReadLine();
                while (!string.IsNullOrEmpty(term))
                {
                    terms.Add(term);
                    term = file.ReadLine();
                }
            }

            Console.WriteLine("Loading phrases...");
            using (TextReader file = new StreamReader("Phrases.txt"))
            {
                string phrase = file.ReadLine();
                while (!string.IsNullOrEmpty(phrase))
                {
                    phrases.Add(phrase);
                    phrase = file.ReadLine();
                }
            }
        }

        private static void ProcessSubSite(string subSite)
        {
            int pageCount = 10;

            string[] allWords = new string[] { };
            IEnumerable<string> allUniqueWords = new string[] { };
            List<KeyValuePair<string, int>> wordList = null;

            WebClient client = new WebClient();

            // First page for a category has different url format
            Console.WriteLine();
            Console.WriteLine("Subsite: " + subSite);
            Console.Write("Processing page 1...");
            ProcessPage(client, rootUrl + subSite, subSite);

            for (int i = 1; i <= pageCount; i++)
            {
                Console.Write(Environment.NewLine + "Processing page " + (i + 1).ToString() + "...");
                ProcessPage(client, rootUrl + subSite + "index" + i + "00.html", subSite);
            }

            Console.WriteLine(Environment.NewLine + "Cleanup and save...");
            wordList = wordCounts.ToList();

            var items = from pair in wordCounts
                        orderby pair.Value descending
                        select pair;

            using (TextWriter file = new StreamWriter(outputFile, true))
            {
                int index = 1;
                foreach (var item in items)
                {
                    if (item.Value > 2)
                    {
                        file.WriteLine(subSite.Replace("/", string.Empty) + "\t" + item.Key + "\t" + index + "\t" + item.Value);
                        index++;
                    }
                }
            }
        }

        private static void ProcessPage(WebClient client, string url, string subSite)
        {
            try
            {
                // Some are of form http://seattle.craigslist.org/search/eng?query=+
                string page = client.DownloadString(url);
                Extract(page, subSite);
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(url + ": " + ex.Message);
            }
        }

        private static void Extract(string home, string subSite)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(home);
            List<string> hrefTags = new List<string>();
            hrefTags = ExtractAllAHrefTags(doc);

            string lastItem = string.Empty;
            foreach (string item in hrefTags)
            {
                if (item != lastItem)
                {
                    lastItem = item;
                    try
                    {
                        if (item.Contains(subSite) && item.Length > 12)
                        {
                            Console.Write(".");
                            WebClient adClient = new WebClient();
                            string ad = string.Empty;
                            try
                            {
                                if (item.Contains("http://"))
                                {
                                    ad = adClient.DownloadString(item);
                                }
                                else
                                {
                                    ad = adClient.DownloadString(rootUrl + item);
                                }

                                HtmlDocument adDoc = new HtmlDocument();
                                doc.LoadHtml(ad);
                                string text = GetPostingBody(doc);
                                if (!string.IsNullOrEmpty(text))
                                {
                                    text = text.Replace("#", "sharp");
                                    text = text.Replace("++", "plusplus");

                                    FindPhrases(text);
                                    FindWords(text);
                                }
                            }
                            catch (Exception xx)
                            {
                                Console.WriteLine("adClient.DownloadString, rootUrl = " + rootUrl + ", item = " + item + " - " + xx.Message);
                                continue;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Extract: " + ex.Message);
                        break;
                    }
                }
            }
        }

        private static void FindPhrases(string text)
        {
            foreach (string phrase in phrases)
            {
                int appearances = Regex.Matches(text, phrase).Count;
                if (appearances > 0)
                {
                    if (!wordCounts.ContainsKey(phrase))
                    {
                        wordCounts.Add(phrase, appearances);
                    }
                    else
                    {
                        wordCounts[phrase] = wordCounts[phrase] + appearances;
                    }
                }
            }
        }

        private static void FindWords(string text)
        {
            string[] words = Regex.Split(text, @"\W+");
            foreach (var word in words)
            {
                string lowerWord = word.ToLower();
                if (!string.IsNullOrEmpty(lowerWord) && terms.Contains(lowerWord))
                {
                    if (!wordCounts.ContainsKey(lowerWord))
                    {
                        wordCounts.Add(lowerWord, 1);
                    }
                    else
                    {
                        wordCounts[lowerWord] = wordCounts[lowerWord] + 1;
                    }
                }
            }
        }

        private static void Save()
        {
            SkillTrendsEntities context = new SkillTrendsEntities();
            Term term = new Term();

        }

        private static List<string> ExtractAllAHrefTags(HtmlDocument htmlSnippet)
        {
            List<string> hrefTags = new List<string>();
            foreach (HtmlNode link in htmlSnippet.DocumentNode.SelectNodes("//a[@href]"))
            {
                HtmlAttribute att = link.Attributes["href"];
                hrefTags.Add(att.Value);
            }

            return hrefTags;
        }

        private static string GetPostingBody(HtmlDocument htmlSnippet)
        {
            string result = string.Empty;
            HtmlNodeCollection nodes = htmlSnippet.DocumentNode.SelectNodes("//section[@id='postingbody']");
            if (nodes != null)
            {
                result = nodes[0].InnerHtml;
            }

            return result;
        }
    }
}
