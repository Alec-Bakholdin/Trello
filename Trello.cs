using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using Nito.AsyncEx;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Trello.Main
{

    using static TrelloUtility;

    class Program
    {
        public static CardOptions cardOptions = null;

        static void Main(string[] args)
        {
            try{
                CommandLine.Parser.Default.ParseArguments<CardOptions>(args)
                    .MapResult(
                        (CardOptions o) => 
                        {
                            cardOptions = o;
                            switch(cardOptions.Mode)
                            {
                                case "create": 
                                    return AsyncContext.Run(Create);
                                case "update":
                                    LogError("Update not supported yet.");
                                    return 1;
                                default:
                                    LogError($"{cardOptions.Mode} is not a valid mode. See --help for list of valid modes.");
                                    return 1;

                            }
                        },
                            e => 1
                        );
            } catch (Exception e) {
                LogError($"Unexpected error occurred: {e.StackTrace}");
                return;
            }
                
            return;
        }

        public static async Task<int> Run()
        {
            Console.WriteLine(cardOptions.ToString());

            return 0;
        }

        /**
         * Creates a card using the cardOptions static variable above
         */
        public static async Task<int> Create()
        {
            var trelloCard = new TrelloCard();
            if(cardOptions.ListName == null || cardOptions.BoardName == null)
            {
                // if we haven't stored the data before
                if(!File.Exists("profile.json"))
                {
                    LogError("list and board names are both required when creating a card.");
                    return 1;
                }
                // if we have the data stored in profile.json
                else
                {
                    // read file and convert to json
                    var fileContents = File.ReadAllText("profile.json");
                    var fileContentsJson = (JObject)JsonConvert.DeserializeObject(fileContents);

                    // parse the data into the card options
                    cardOptions.ListName = (cardOptions.ListName == null ? fileContentsJson["list-name"].ToString() : cardOptions.ListName);
                    cardOptions.BoardName = (cardOptions.BoardName == null ? fileContentsJson["board-name"].ToString() : cardOptions.BoardName);
                }
            }

            // fetch list ID, catching errors
            try{
                trelloCard.idList = await GetListID(cardOptions.BoardName, cardOptions.ListName);
            } catch (Exception e) {
                LogError($"Error occurred fetching the id of the specified list: {e.Message}");
                return 1;
            }
            

            // store list-name and board-name in the profile file
            var json = new JObject();
            json["list-name"] = cardOptions.ListName;
            json["board-name"] = cardOptions.BoardName;
            var jsonStr = JsonConvert.SerializeObject(json);
            File.WriteAllText("profile.json", jsonStr);

            // set basic options
            trelloCard.name         = cardOptions.CardName;
            trelloCard.due          = cardOptions.dueDate == null ? null : DateTime.Parse(cardOptions.dueDate);
            trelloCard.dueComplete  = cardOptions.dueComplete;
            trelloCard.pos          = cardOptions.position;
            trelloCard.desc         = cardOptions.description;
            

            // create the card
            try{
                var restResponse = await trelloCard.Create();
                if(!restResponse.IsSuccessful)
                {
                    LogError($"Error creating card: {restResponse.Content}");
                    return 1;
                }

            } catch (Exception e){
                LogError($"Exception occurred while creating card: {e.Message}");
                return 1;
            }

            LogSuccess($"Successfully created card! :)");


            return 0;
        }
    }
}
