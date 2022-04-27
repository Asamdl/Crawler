

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


        const string URL = "https://www.utmn.ru";
        static string LoadPage(string url)
        {
            var result = "";
            if (url[0] == '/' && url[1] != '/' && url[0] != 'h')
            {
                url = URL + url;
            }
            if (url[0] == ' ')
            {
                string new_url = "";
                for (int i = 0; i < url.Length; i++)
                {
                    if (url[i] != ' ')
                    {
                        new_url+=url[i];
                    }
                }
                url = URL + new_url;
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
        static string search(string value)
        {
            string result = "";
            for (int i = value.Length - 1; i >= 0; i--)
            {
                if (value[i] != '/')
                {
                    result+=value[i];
                }
            }
            return result;
        }
        static List<List<string>> extractingInfoFromSinglePage(string url)
        {
            List<List<string>> result = new List<List<string>>()
            {
                new List<string>(),
                new List<string>(),
                new List<string>()
            };
            var pageContent = LoadPage(url);
            var document = new HtmlDocument();
            if (pageContent != null)
            {
                document.LoadHtml(pageContent);
            }
            else
            {
                return null;
            }
           

            HtmlNodeCollection links = document.DocumentNode.SelectNodes(".//a");
            HtmlNodeCollection images = document.DocumentNode.SelectNodes(".//img");
            if (!(images is null))
            {
                foreach (HtmlNode image in images)
                {
                    var value = image.GetAttributeValue("src", "");
                    if (value.Length!= 0)
                    {
                        result[1].Add(value);
                    }
                    
                }
            }
            if (!(links is null))
            {
                foreach (HtmlNode link in links)
                {
                    var value = link.GetAttributeValue("href", "");
                    if (value.Length > 1 && value.Contains("/"))
                    {
                        if (value.Contains(".pdf") || value.Contains(".PDF"))
                        {
                            result[2].Add(value);
                        }
                        //else if (!(value.Contains("://") || !value.Contains("index") && !value.Contains("%")) && !result[1].Contains(value) && value[0] != '.')
                        //{
                        //    result[0].Add(value);
                        //}
                        else if (value[0] == '/' && !value.Contains("://")&& !result[1].Contains(value)&& !value.Contains("//") && !search(value).Contains('.'))
                        {
                            result[0].Add(value);
                        }
                    }
                }
            }
            return result;
                

        }

        static List<List<string>> extractingInfoFromMultiplePages(List<string> links)
        {
            List<List<string>> result = new List<List<string>>()
            {
                new List<string>(),
                new List<string>(),
                new List<string>()
            };
            //foreach (string url in links)
            //{
            //    extractingInfoFromSinglePage(url, ref result.Last(), ref imageLinks, ref pdfLinks);
            //}
            return result;
        }

        static void queries(int depth)
        {
            List<List<string>> data = extractingInfoFromSinglePage(URL);
            List<string> kit = data[0];
            for (int d = 0; d < depth; d++)
            {
                int countLinks = kit.Count;
                if (countLinks > 0)
                {
                    Task<List<List<string>>>[] tasks = new Task<List<List<string>>>[countLinks];
                    for (int i = 0; i < countLinks; i++)
                    {
                        int _i = i;
                        tasks[_i] = Task<List<List<string>>>.Factory.StartNew(() => extractingInfoFromSinglePage(kit[_i]));
                    }
                    Task.WaitAll(tasks);
                    kit = new List<string>();
                    foreach (var task in tasks)
                    {
                        List<List<string>> resTask = task.Result;
                        if (!(resTask is null))
                        {
                            if (!(resTask[0] is null))
                            {
                                foreach (var link in resTask[0])
                                {
                                    if (!data[0].Contains(link))
                                    {
                                        data[0].Add(link);
                                        kit.Add(link);
                                    }
                                }
                            }
                        }
                        if (!(resTask is null))
                        {
                            if (!(resTask[1] is null))
                            {
                                foreach (var image in resTask[1])
                                {
                                    if (!data[1].Contains(image))
                                    {
                                        data[1].Add(image);
                                    }
                                }
                            }
                        }
                        if (!(resTask is null))
                        {
                            if (!(resTask[2] is null))
                            {
                                foreach (var pdf in resTask[2])
                                {
                                    if (!data[2].Contains(pdf))
                                    {
                                        data[2].Add(pdf);
                                    }
                                }
                            }
                        }
                    }
                }
                
                
               
                Console.WriteLine(2);
            }


            Console.WriteLine(1);



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
            queries(2);



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
