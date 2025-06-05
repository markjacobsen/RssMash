using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;

public class RssMash
{
    public static void Main(string[] args)
    {
        Console.WriteLine("RSS Feed Aggregator started.");

        // Check if the correct number of arguments are provided
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: RssAggregator <inputFilePath> <outputFilePath>");
            Console.WriteLine("  <inputFilePath>: Path to the text file containing RSS feed URLs (one URL per line).");
            Console.WriteLine("  <outputFilePath>: Path where the aggregated RSS feed will be saved.");
            Console.WriteLine("Example: RssAggregator C:\\Feeds\\MyFeeds.txt C:\\Output\\AggregatedFeed.xml");
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
            return;
        }

        string inputFilePath = args[0];
        string outputFilePath = args[1];

        try
        {
            // 1. Read RSS feed URLs from the input file
            List<string> feedUrls = ReadFeedUrls(inputFilePath);

            if (feedUrls.Count == 0)
            {
                Console.WriteLine($"No feed URLs found in '{inputFilePath}'. Please add URLs, one per line.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey();
                return;
            }

            // 2. Aggregate all feed items
            SyndicationFeed outputFeed = AggregateFeeds(feedUrls);

            // 3. Write the aggregated feed to an output file
            WriteFeedToFile(outputFeed, outputFilePath);

            Console.WriteLine($"Successfully aggregated {outputFeed.Items.Count()} items into '{outputFilePath}'.");
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"Error: Input file not found. {ex.Message}");
            Console.WriteLine($"Please ensure '{inputFilePath}' exists.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An unexpected error occurred: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        Console.WriteLine("RSS Feed Aggregator finished.");
    }

    /// <summary>
    /// Reads RSS feed URLs from the specified file.
    /// </summary>
    /// <param name="fileName">The path to the file containing feed URLs.</param>
    /// <returns>A list of feed URLs.</returns>
    private static List<string> ReadFeedUrls(string fileName)
    {
        Console.WriteLine($"Reading feed URLs from '{fileName}'...");
        List<string> urls = new List<string>();
        if (File.Exists(fileName))
        {
            urls = File.ReadAllLines(fileName)
                       .Where(line => !string.IsNullOrWhiteSpace(line))
                       .ToList();
            Console.WriteLine($"Found {urls.Count} URLs.");
        }
        else
        {
            // We no longer create the file automatically if it doesn't exist,
            // as the expectation is the user provides an existing input file.
            // Throwing FileNotFoundException here to be caught in Main.
            throw new FileNotFoundException($"The input file '{fileName}' was not found.");
        }
        return urls;
    }

    /// <summary>
    /// Aggregates items from multiple RSS feeds into a single feed, ordered by publish date.
    /// </summary>
    /// <param name="feedUrls">A list of RSS feed URLs.</param>
    /// <returns>A SyndicationFeed containing all aggregated items.</returns>
    private static SyndicationFeed AggregateFeeds(List<string> feedUrls)
    {
        Console.WriteLine("Aggregating feed items...");
        List<SyndicationItem> allItems = new List<SyndicationItem>();

        foreach (string url in feedUrls)
        {
            try
            {
                Console.WriteLine($"  Processing feed: {url}");
                using (XmlReader reader = XmlReader.Create(url))
                {
                    SyndicationFeed feed = SyndicationFeed.Load(reader);
                    if (feed != null && feed.Items != null)
                    {
                        allItems.AddRange(feed.Items);
                        Console.WriteLine($"    Added {feed.Items.Count()} items from {url}");
                    }
                }
            }
            catch (UriFormatException)
            {
                Console.WriteLine($"    Error: Invalid URL format for '{url}'. Skipping.");
            }
            catch (XmlException ex)
            {
                Console.WriteLine($"    Error parsing XML from '{url}': {ex.Message}. Skipping.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"    An error occurred while processing '{url}': {ex.Message}. Skipping.");
            }
        }

        // Order all items by publish date (most recent first)
        List<SyndicationItem> sortedItems = allItems.OrderByDescending(item => item.PublishDate).ToList();

        SyndicationFeed outputFeed = new SyndicationFeed("Aggregated RSS Feed", "A combined RSS feed from multiple sources.", new Uri("http://example.com/aggregated-feed"));
        outputFeed.LastUpdatedTime = DateTimeOffset.Now;
        outputFeed.Items = sortedItems;

        Console.WriteLine($"Total aggregated items: {sortedItems.Count}");
        return outputFeed;
    }

    /// <summary>
    /// Writes the contents of a SyndicationFeed to an XML file.
    /// </summary>
    /// <param name="feed">The SyndicationFeed to write.</param>
    /// <param name="fileName">The path to the output file.</param>
    private static void WriteFeedToFile(SyndicationFeed feed, string fileName)
    {
        Console.WriteLine($"Writing aggregated feed to '{fileName}'...");
        using (XmlWriter writer = XmlWriter.Create(fileName, new XmlWriterSettings { Indent = true }))
        {
            // Use Rss20FeedFormatter to ensure it's written as RSS 2.0
            Rss20FeedFormatter formatter = new Rss20FeedFormatter(feed);
            formatter.WriteTo(writer);
        }
        Console.WriteLine("Feed written successfully.");
    }
}