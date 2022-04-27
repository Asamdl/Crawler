

using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;


namespace Crawler
{
    internal class Program
    {
        /*static public ISet<string> GetNewLinks(string content)
        {
            Regex regexLink = new Regex("(?<=<a\\s*?href=(?:'|\"))[^'\"]*?(?=(?:'|\"))");

            ISet<string> newLinks = new HashSet<string>();
            foreach (var match in regexLink.Matches(content))
            {
                if (!newLinks.Contains(match.ToString()))
                    newLinks.Add(match.ToString());
            }

            return newLinks;
        }
        static public ISet<string> T(string url)
        {
            Regex regexLink = new Regex("(?<=<a\\s*?href=(?:'|\"))[^'\"]*?(?=(?:'|\"))");
            Regex regexImage = new Regex("<img.+?" +
                "  src=[\"'](.+?)[\"'].*?>");

            ISet<string> newLinks = new HashSet<string>();
            foreach (var match in regexLink.Matches(url))
            {
                if (!newLinks.Contains(match.ToString()))
                    newLinks.Add(match.ToString());
            }
            ISet<string> allImage = new HashSet<string>();
            foreach (var match in regexImage.Matches(url))
            {
                if (!allImage.Contains(match.ToString()))
                  
                    allImage.Add(match.ToString());
            }
            return allImage;

        }*/


        const string URL = "https://scienceforum.ru";
        static string LoadPage(string url)
        {
            var result = "";
            if (url[0] == '/' && url[1] != '/' && url[0] != 'h')
            {
                url = URL + url;
            }
            if (url[0] == '/' && url[1] == '/')
            {
                url = "https:" + url;
            }
            var request = (HttpWebRequest)WebRequest.Create(url);
            try
            {
                var response = (HttpWebResponse)request.GetResponse();
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var receiveStream = response.GetResponseStream();
                    if (receiveStream != null)
                    {
                        try
                        {
                            StreamReader readStream;
                            if (response.CharacterSet == null)
                                readStream = new StreamReader(receiveStream);
                            else
                                readStream = new StreamReader(receiveStream, Encoding.GetEncoding(response.CharacterSet));
                            result = readStream.ReadToEnd();
                            readStream.Close();
                        }
                        catch
                        {
                            Console.WriteLine(url);
                        }

                    }
                    response.Close();
                }
                return result;
            }
            catch
            {
                Console.WriteLine("Error");
                return null;
            }
            

            
            
        }

        static void extractingInfoFromSinglePage(string url, ref List<string> pageLinks, ref List<string> imageLinks, ref List<string> pdfLinks)
        {
            var pageContent = LoadPage(url);
            var document = new HtmlDocument();
            if (pageContent != null)
            {
                document.LoadHtml(pageContent);
            }
            else
            {
                return;
            }
           

            HtmlNodeCollection links = document.DocumentNode.SelectNodes(".//a");
            HtmlNodeCollection images = document.DocumentNode.SelectNodes(".//img");
            if (!(images is null))
            {
                foreach (HtmlNode image in images)
                {
                    var value = image.GetAttributeValue("src", "");
                    imageLinks.Add(value);
                }
            }
            if (!(links is null))
            {
                foreach (HtmlNode link in links)
                {
                    var value = link.GetAttributeValue("href", "");
                    if (value.Length > 1 && value.Contains("/"))
                    {
                        if (value.Contains(".pdf"))
                        {
                            pdfLinks.Add(value);
                        }
                        else if ((value.Contains("://") || !value.Contains("index") && !value.Contains("%")) && !imageLinks.Contains(value)&&value[0]!='.')
                        {
                            pageLinks.Add(value);
                        }
                        //if (value.Contains("://"))
                        //{
                        //    absoluteLinks.Add(value);
                        //}
                        //else if (value.Contains(".pdf"))
                        //{
                        //    pdfLinks.Add(value);
                        //}
                        //else if (!value.Contains("//") && !value.Contains("index") && !value.Contains("%"))
                        //{
                        //    relativeLinks.Add(value);
                        //}
                    }
                }
            }
        }

        static void extractingInfoFromMultiplePages(List<string> links, List<string> pageLinks, List<string> imageLinks, List<string> pdfLinks)
        {
            foreach (string url in links)
            {
                extractingInfoFromSinglePage(url, ref pageLinks, ref imageLinks, ref pdfLinks);
            }
        }

        static void queries(int numTask, int depth)
        {

            Task[] tasks = new Task[numTask];

            List<string> pageLinks = new List<string>();
            List<string> imageLinks = new List<string>();
            List<string> pdfLinks = new List<string>();
            extractingInfoFromSinglePage(URL, ref pageLinks, ref imageLinks, ref pdfLinks);
            List<List<string>> pageTaskLinks = new List<List<string>>();
            List<List<string>> imageTaskLinks = new List<List<string>>();
            List<List<string>> pdfTaskLinks = new List<List<string>>();
            int count = 0;
            for (int d = 0; d < depth; d++)
            {
                int size_kit = pageLinks.Count / numTask;
                for (int i = 0; i < numTask; i++)
                {
                    int _count = count;
                    int _i = i;
                    pageTaskLinks.Add(new List<string>());
                    imageTaskLinks.Add(new List<string>());
                    pdfTaskLinks.Add(new List<string>());
                    List<string> kit = new List<string>();
                    for (int k = i * size_kit; k < (i + 1) * size_kit; k++)
                    {
                        kit.Add(pageLinks[k]);
                    }
                    if (i == (numTask - 1))
                    {
                        for(int j = (i + 1) * size_kit; j < pageLinks.Count; j++)
                        {
                            kit.Add(pageLinks[j]);
                        }
                    }
                    tasks[_i] = Task.Factory.StartNew(() => extractingInfoFromMultiplePages(kit, pageTaskLinks[_count], imageTaskLinks[_count], pdfTaskLinks[_count]));
                    count += 1;
                }
                Task.WaitAll(tasks);
                
                pageLinks = new List<string>();
                foreach (var st in pageTaskLinks)
                {
                    foreach(var stm in st)
                    {
                        pageLinks.Add(stm.ToString());
                    }
                }
                Console.WriteLine(2);
            }


            Console.WriteLine(2);



        }

        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var enc1251 = Encoding.GetEncoding(1251);
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = enc1251;
            List<string> pageLinks = new List<string>();
            List<string> imageLinks = new List<string>();
            List<string> pdfLinks = new List<string>();
            //extractingInfoFromSinglePage(URL, ref pageLinks, ref imageLinks, ref pdfLinks);
            queries(20, 2);



            Console.WriteLine("Page Links:");
            foreach (string link in pageLinks)
            {
                Console.WriteLine(link);
            }

            Console.WriteLine("Image Links:");
            foreach (string image in imageLinks)
            {
                Console.WriteLine(image);
            }

            Console.WriteLine("Pdf Links:");
            foreach (string pdf in pdfLinks)
            {
                Console.WriteLine(pdf);
            }


        }

    }
}
