

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


        //const string URL = "https://www.utmn.ru";
        const string URL = "http://tsh1.ucoz.ru";
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
            catch(Exception ex)
            {
                Console.WriteLine(string.Format("{0}{1}",ex.Message.ToString(), url));
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
        //getting data from the home page
        static List<List<string>> extractingInfoFromSinglePage(string url)
        {
            List<List<string>> result = new List<List<string>>()
            {
                new List<string>(),//pageLinks
                new List<string>(),//imageLinks
                new List<string>()//pdfLinks
            };
            var pageContent = LoadPage(url);
            var document = new HtmlDocument();
            if (pageContent != null)
            {
                document.LoadHtml(pageContent);
            }
            else
            {
                //Console.WriteLine(string.Format("html is null {0}", url));
                return null;
            }

            //search for links and images
            HtmlNodeCollection links = document.DocumentNode.SelectNodes(".//a");
            HtmlNodeCollection images = document.DocumentNode.SelectNodes(".//img");
            if (!(images is null))
            {
                foreach (HtmlNode image in images)
                {
                    var value = image.GetAttributeValue("src", "");
                    if (value.Contains("/")&&value.Length!= 0 && !value.Contains("//") && !value.Contains(".."))
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
                        if ((value.Contains(".pdf") || value.Contains(".PDF"))&& !value.Contains("span"))
                        {
                            result[2].Add(value);
                        }
                        else if (!value.Contains(' ')&&value[0] == '/' && !value.Contains("://")&& !result[1].Contains(value)&& !value.Contains("//") && !search(value).Contains('.'))
                        {
                            result[0].Add(value);
                        }
                    }
                }
            }
            return result;
        }
        //main function 
        static List<List<string>> queries(int depth)
        {
            //getting data from home page
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
            }

            return data;
        }
        static void save_res(List<string> data,string path)
        {
            using (FileStream fstream = new FileStream(path, FileMode.OpenOrCreate))
            {
                foreach (string link in data)
                {
                    byte[] buffer = Encoding.Default.GetBytes(URL + link+"\n");
                    fstream.Write(buffer, 0, buffer.Length);
                }
            }
        }
        static void Main(string[] args)
        {
            #region [ rus_in_console ]
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var enc1251 = Encoding.GetEncoding(1251);
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = enc1251;
            #endregion

            string path = @"C:\Users\Aoki\source\repos\Crawler\Crawler\data";
            string subpath = string.Format(@"{0}",DateTime.Now.Millisecond*DateTime.Now.Second);
            DirectoryInfo dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }
            dirInfo.CreateSubdirectory(subpath);
            string path_to_file_links = path+"\\"+subpath+"\\links.txt";
            string path_to_file_images = path + "\\" + subpath +"\\images.txt";
            string path_to_file_pdfFiles = path + "\\" + subpath +"\\pdfFiles.txt";

            List<string> pageLinks = new List<string>();
            List<string> imageLinks = new List<string>();
            List<string> pdfLinks = new List<string>();
            //extractingInfoFromSinglePage(URL, ref pageLinks, ref imageLinks, ref pdfLinks);
            List<List<string>> data = new List<List<string>>()
            {
                new List<string>(),//pageLinks
                new List<string>(),//imageLinks
                new List<string>()//pdfLinks
            };
            int depth = 3;
            data = queries(depth);



            Console.WriteLine("Page Links:");
            save_res(data[0], path_to_file_links);



            Console.WriteLine("Image Links:");
            save_res(data[1], path_to_file_images);

            Console.WriteLine("Pdf Links:");
            save_res(data[2], path_to_file_pdfFiles);

            Console.WriteLine("name folder:" + subpath);

            Console.WriteLine(String.Format("Глубина : {0}", depth));
            Console.WriteLine("Найдено:");
            Console.WriteLine(String.Format("Ссылок: {0}", data[0].Count));
            Console.WriteLine(String.Format("Картинок: {0}", data[1].Count));
            Console.WriteLine(String.Format("DPF-файлов: {0}",data[2].Count));


        }

    }
}
