using System;
using System.Threading.Tasks;
using CommandLine;
using Nito.AsyncEx;

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
                LogError("list and board names are both required when creating a card.");
                return 1;
            }

            // fetch list ID, catching errors
            try{
                trelloCard.idList = await GetListID(cardOptions.BoardName, cardOptions.ListName);
            } catch (Exception e) {
                LogError($"Error occurred fetching the id of the specified list: {e.Message}");
                return 1;
            }
            

            // set basic options
            trelloCard.name         = cardOptions.CardName;
            trelloCard.due          = cardOptions.dueDate == null ? null : DateTime.Parse(cardOptions.dueDate);
            trelloCard.dueComplete  = cardOptions.dueComplete;
            trelloCard.pos          = cardOptions.position;
            trelloCard.desc         = cardOptions.description;
            

            // creete the card
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
