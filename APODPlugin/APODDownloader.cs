using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using System.Drawing;

namespace APODPlugin
{
    class APODDownloader
    {
        String APODURL = "https://science.nasa.gov/apod/";
        internal event downloadErrorEvent onDownloadError;
        internal event downloadCompleteEvent onDownloadComplete;
        internal event downloadNoImageFound onDownloadNoImageFound;
        public delegate void downloadErrorEvent(object sender, downloadErrorEventArgs e);
        public delegate void downloadCompleteEvent(object sender, downloadCompleteEventArgs e);
        public delegate void downloadNoImageFound(object sender, downloadNoImageEventArgs e);
        WebClient client;
        WebClient imageClient;
        List<String> apodURLs;
        int currentStep = 0;
        directions direction;
        string information;
        string title;

        internal APODDownloader()
        {
            client = new WebClient();
            apodURLs = new List<string>();
            apodURLs.Add(APODURL);
        }

        internal void Dispose()
        {
            client.OpenReadCompleted -= new OpenReadCompletedEventHandler(readComplete);
            imageClient.OpenReadCompleted -= new OpenReadCompletedEventHandler(imageReadComplete);
            client.Dispose();
        }

        internal bool GetImage(int step, directions d)
        {
            try
            {
                direction = d;
                currentStep = step;
                client.OpenReadCompleted += new OpenReadCompletedEventHandler(readComplete);
                client.OpenReadAsync(new Uri(apodURLs[step]));
                return true;
            }
            catch (IndexOutOfRangeException)
            {
                return false; //no image to get!
            }
        }

        private void readComplete(object sender, OpenReadCompletedEventArgs args)
        {
            if (args.Error != null)
            {
                onDownloadError(this, new downloadErrorEventArgs(args.Error));
            }
            else
            {
                try
                {
                    String imageURL = "";
                    Stream resultStream = args.Result;
                    if (!resultStream.CanRead)
                        return;

                    StreamReader sr = new StreamReader(resultStream);
                    String pagesource = sr.ReadToEnd();
                    sr.Close();
                    resultStream.Close();
                    sr.Dispose();
                    resultStream.Dispose();
                    client.OpenReadCompleted -= readComplete;
                    client.Dispose();

                    //Find the previous page URL as well... i.e. <a href="ap130319.html">&lt;</a>
                    Match prevMatcher = Regex.Match(pagesource, "(?<=<a href=\")(.+?)(?=\">&lt;</a>)");
                    String prevURL = "http://apod.nasa.gov/apod/" + prevMatcher.Value;
                    if (!apodURLs.Contains(prevURL))
                        apodURLs.Add(prevURL);

                    //Look for information
                    Match infoMatcher = Regex.Match(pagesource, "(?<=<b> Explanation: </b>)(.+?)(?=<p> <center>)", RegexOptions.Singleline);
                    if (infoMatcher.Success)
                        information = StripTagsCharArray(infoMatcher.Value).Replace("\n"," ");
                    else
                        information = "No information found!";

                    //Look for the title.
                    //e.g. <center><b> Sunspot at Sunset </b> <br>
                    Match titleMatcher = Regex.Match(pagesource, "(?<=<b>)(.+?)(?=</b>\\s<br>)", RegexOptions.Singleline | RegexOptions.IgnorePatternWhitespace);
                    if (titleMatcher.Success)
                        title = titleMatcher.Value;
                    else
                        title = "Untitled";

                    //Now look for image
                    Match sourceMatcher = Regex.Match(pagesource, "(?<=<IMG SRC=\")(.+?)(?=\")", RegexOptions.Singleline);
                    if (sourceMatcher.Success)
                    {
                        imageURL = "http://apod.nasa.gov/apod/" + sourceMatcher.Value;
                        downloadImage(imageURL);
                    }
                    else
                    {
                        onDownloadNoImageFound(this, new downloadNoImageEventArgs(direction));
                    }
                }
                catch (Exception ex)
                {
                    onDownloadError(this, new downloadErrorEventArgs(ex));
                }
            }
        }

        private void downloadImage(String imageURL)
        {
            imageClient = new WebClient();
            imageClient.OpenReadCompleted += new OpenReadCompletedEventHandler(imageReadComplete);
            imageClient.OpenReadAsync(new Uri(imageURL));
        }

        private void imageReadComplete(object sender, OpenReadCompletedEventArgs args)
        {
            if (args.Error != null)
            {
                onDownloadError(this, new downloadErrorEventArgs(args.Error));
            }
            else
            {
                Stream resultStream = args.Result;
                Image bitmap = Bitmap.FromStream(resultStream);
                resultStream.Close();
                resultStream.Dispose();
                imageClient.Dispose();

                onDownloadComplete(this, new downloadCompleteEventArgs(bitmap, information, title));
            }
        }

        public static string StripTagsCharArray(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }
    }

    internal class downloadErrorEventArgs : EventArgs
    {
        internal Exception downloadException;
        internal downloadErrorEventArgs(Exception ex)
        {
            downloadException = ex;
        }
    }

    internal class downloadCompleteEventArgs : EventArgs
    {
        internal Image bitmap;
        internal string infostring;
        internal string titlestring;
        internal downloadCompleteEventArgs(Image b, string info, string title)
        {
            bitmap = b;
            infostring = info;
            titlestring = title;
        }
    }

    internal class downloadNoImageEventArgs : EventArgs
    {
        internal directions scrollDirection;
        internal downloadNoImageEventArgs(directions d)
        {
            scrollDirection = d;
        }
    }
}