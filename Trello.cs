using System;
using System.Threading.Tasks;
using CommandLine;
using Nito.AsyncEx;

namespace Trello.Main
{

    using static TrelloUtility;

    class Program
    {


        static void Main(string[] args)
        {
            CommandLine.Parser.Default.ParseArguments<CardOptions>();
                

            AsyncContext.Run(Run);

            return;
        }

        public static async Task<bool> Run()
        {
            var trelloCard = new TrelloCard();
            trelloCard.name = "testing something";
            trelloCard.idList = await GetListID("Projects", "Homework");
            var response = await trelloCard.Create();
            if(!response.IsSuccessful)
                LogError(response.Content);

            return true;
        }
    }
}
