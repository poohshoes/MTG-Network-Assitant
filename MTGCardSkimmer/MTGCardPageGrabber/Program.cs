using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MTGCardPageGrabber
{
    class Program
    {
        static void Main(string[] args)
        {
            // Scrape the actual cards.
            List<string> dataListing;
            if (File.Exists("Data\\listing.txt"))
                dataListing = File.ReadAllLines("Data\\listing.txt").ToList();
            else
                dataListing = new List<string>();
            List<string> cardImageLinks = File.ReadAllLines("cardImageLinks.txt").ToList();
            WebClient client = new WebClient();
            for (int i = 0; i < cardImageLinks.Count; i += 2)
            {
                string cardFileName = cardImageLinks[i];
                cardFileName.Replace("'", "");
                cardFileName.Replace(",", "");
                cardFileName.Replace("-", "");
                cardFileName.Replace(" ", "");
                cardFileName += ".jpg";

                if (!dataListing.Contains(cardImageLinks[0]))
                    File.AppendAllText("Data\\listing.txt", cardImageLinks[i] + "\n" + cardFileName + "\n");

                if (!File.Exists("Data\\" + cardFileName))
                    client.DownloadFile(cardImageLinks[i + 1], "Data\\" + cardFileName);

                Console.WriteLine(cardImageLinks[i]);
            }

            //client.DownloadFile(new Uri(url), @"MTGCore2015\");

            // Clean up the names
            //List<string> cardImageLinks = File.ReadAllLines("cardImageLinks.txt").ToList();
            //for (int i = 0; i < cardImageLinks.Count; i+=2)
            //{
            //    cardImageLinks[i] = cardImageLinks[i].Replace("%20", " ");
            //    cardImageLinks[i] = cardImageLinks[i].Replace("%27", "'");
            //    cardImageLinks[i] = cardImageLinks[i].Replace("%2C", ",");
            //}
            //File.WriteAllLines("cardImageLinks.txt", cardImageLinks);

            // Scrape the image links
            //List<string> cardPageLinks = File.ReadAllLines("cardPageLinks.txt").ToList();
            ////<img height='310px' id='card_image' src='/system/images/mtg/cards/383175.jpg'>
            ////https://deckbox.org/system/images/mtg/cards/383175.jpg

            //WebClient client = new WebClient();
            //List<string> cardImageLinks = new List<string>();
            //foreach (string cardPageLink in cardPageLinks)
            //{
            //    int lastSlashIndex = cardPageLink.LastIndexOf("/");
            //    string cleanName = cardPageLink.Substring(lastSlashIndex + 1);
            //    cardImageLinks.Add(cleanName);
            //    string match = "id='card_image' src='";
            //    string source = client.DownloadString(cardPageLink);
            //    int cardID = source.IndexOf(match) + match.Length;
            //    int cardIDEnd = source.IndexOf("'", cardID);
            //    cardImageLinks.Add("https://deckbox.org" + source.Substring(cardID, cardIDEnd - cardID));
            //    Console.WriteLine(cardImageLinks.Count / 2 + " : " + cleanName);
            //}
            //File.WriteAllLines("cardImageLinks.txt", cardImageLinks);


            // Extracing the card page links
            //string listings = File.ReadAllText("listingSource.txt");
            //string output = "";

            //string sample = "https://deckbox.org/mtg/";

            //int matchIndex = listings.IndexOf(sample);
            //while (matchIndex != -1)
            //{
            //    int index = listings.IndexOf("'", matchIndex + sample.Length);
            //    output += listings.Substring(matchIndex, index - matchIndex) + "\n";

            //    matchIndex = listings.IndexOf(sample, matchIndex + 1);
            //}

            //File.AppendAllText("output.txt", output);
        }
    }
}
