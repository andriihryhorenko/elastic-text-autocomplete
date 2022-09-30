// See https://aka.ms/new-console-template for more information
using elastic_autocomplete_test_app.Model;
using Elasticsearch.Net;
using Nest;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;

static string readLineWithCancel()
{
    string result = null;

    StringBuilder buffer = new StringBuilder();

    //The key is read passing true for the intercept argument to prevent
    //any characters from displaying when the Escape key is pressed.
    ConsoleKeyInfo info = Console.ReadKey(true);
    while (info.Key != ConsoleKey.Enter && info.Key != ConsoleKey.Escape)
    {
        Console.Write(info.KeyChar);
        buffer.Append(info.KeyChar);
        info = Console.ReadKey(true);
    }

    if (info.Key == ConsoleKey.Enter)
    {
        result = buffer.ToString();
    }

    return result;
}



Console.WriteLine("Hello!");


var pool = new SingleNodeConnectionPool(new Uri("http://127.0.0.1:9200"));

var settings = new ConnectionSettings(pool);


var elasticClient = new ElasticClient(settings);
var indexExists = elasticClient.Indices.Exists("autocomplete3").Exists;

if (!indexExists)
{
    Console.WriteLine("Initializing!");
    var path = Path.Combine(Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName, @"dictionary\dictionary.json");
    if (!File.Exists(path))
    {
        Console.WriteLine($"No dictionary.json file [{path}]");
        return;

    }
    var file = File.Open(path, FileMode.Open);
    List<Word> items;
    using (StreamReader r = new StreamReader(file))
    {
        string json = r.ReadToEnd();
        items = JsonConvert.DeserializeObject<List<Word>>(json);
    }

    if (items == null || items.Count == 0)
    {
        Console.WriteLine($"No workds in dictionary.json file [{path}]");
        return;
    }


    Console.WriteLine("Putting index...");
    var response = elasticClient.LowLevel.Indices.Create<CreateResponse>("autocomplete3", @"{
          ""settings"": {
            ""analysis"": {
              ""analyzer"": {
                ""autocomplete"": {
                  ""tokenizer"": ""autocomplete"",
                  ""filter"": [
                    ""lowercase""
                  ]
                },
                ""autocomplete_search"": {
                  ""tokenizer"": ""lowercase""
                }
              },
              ""tokenizer"": {
                ""autocomplete"": {
                  ""type"": ""edge_ngram"",
                  ""min_gram"": 1,
                  ""max_gram"": 10,
                  ""token_chars"": [
                    ""letter""
                  ]
                }
              }
            }
          },
          ""mappings"": {
            ""properties"": {
              ""word"": {
                ""type"": ""text"",
                ""analyzer"": ""autocomplete"",
                ""search_analyzer"": ""autocomplete_search""
              }
            }
          }
        }");


    Console.WriteLine($"{response}");


    Console.WriteLine("Adding words...");

    var response2 = elasticClient.Bulk(b => b
    .Index("autocomplete3")
    .IndexMany(items)
    );


    Console.WriteLine($"{response2}");
    Console.WriteLine("Initializing - done");

}


Console.WriteLine("Please enter a word. (ESC to cancel)");

string word = string.Empty;
do
{
    word = readLineWithCancel();

    if (word == null)
        return;

    var query = @"{
        ""query"": {
            ""match"": {
                ""word"": {
                    ""query"": ""{term}"",
                    ""fuzziness"": ""AUTO""
                }
            }
    }}";
    query= query.Replace("{term}", word);

    var hits = elasticClient.LowLevel.Search<SearchResponse<Word>>("autocomplete3", query);

    var documents = hits.Documents.ToList();

    var otions = documents.Select(document => document.word).ToList();

    Console.WriteLine();

    Console.WriteLine("OPTION: " +String.Join(",", otions));

    Console.WriteLine();

}
while (word != "Cancelled");








