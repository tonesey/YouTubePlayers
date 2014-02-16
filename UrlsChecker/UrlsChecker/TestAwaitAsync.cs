// Add the following using directives, and add a reference for System.Net.Http. 
using System.Net.Http;
using System.IO;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AsyncExampleWPF_HttpClient_WhenAll
{
    public class MainWindow
    {
        public MainWindow()
        {
        }

        private async void startButton_Click()
        {
            await SumPageSizesAsync();
        }

        private async Task SumPageSizesAsync()
        {
            // Make a list of web addresses.
            List<string> urlList = SetUpURLList();


            // Declare an HttpClient object and increase the buffer size. The 
            // default buffer size is 65,536.
            HttpClient client = new HttpClient() { MaxResponseContentBufferSize = 1000000 };

            // Create a query.
            IEnumerable<Task<string>> downloadTasksQuery =
                from url in urlList select ProcessURL(url, client);

            // Use ToArray to execute the query and start the download tasks.
            Task<string>[] downloadTasks = downloadTasksQuery.ToArray();

            // You can do other work here before awaiting. 

            // Await the completion of all the running tasks. 
            string[] lengths = await TaskEx.WhenAll(downloadTasks);

        }


        private List<string> SetUpURLList()
        {
            List<string> urls = new List<string> 
            { 
                "http://msdn.microsoft.com",
                "http://msdn.microsoft.com/en-us/library/hh290136.aspx",
                "http://msdn.microsoft.com/en-us/library/ee256749.aspx",
                "http://msdn.microsoft.com/en-us/library/hh290138.aspx",
                "http://msdn.microsoft.com/en-us/library/hh290140.aspx",
                "http://msdn.microsoft.com/en-us/library/dd470362.aspx",
                "http://msdn.microsoft.com/en-us/library/aa578028.aspx",
                "http://msdn.microsoft.com/en-us/library/ms404677.aspx",
                "http://msdn.microsoft.com/en-us/library/ff730837.aspx"
            };
            return urls;
        }


        // The actions from the foreach loop are moved to this async method.
        async Task<string> ProcessURL(string url, HttpClient client)
        {
            byte[] byteArray = await client.GetByteArrayAsync(url);
            DisplayResults(url, byteArray);
            return byteArray.Length.ToString();
        }


        private void DisplayResults(string url, byte[] content)
        {
            // Display the length of each web site. The string format  
            // is designed to be used with a monospaced font, such as 
            // Lucida Console or Global Monospace. 
            //var bytes = content.Length;
            //// Strip off the "http://".
            //var displayURL = url.Replace("http://", "");
            //resultsTextBox.Text += string.Format("\n{0,-58} {1,8}", displayURL, bytes);
        }
    }
}