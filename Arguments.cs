using CommandLine;

namespace Trello.Main
{
    [Verb("card")]
    public class CardOptions
    {
        [Option('c', "create", HelpText="Are we creating a card here?", SetName="Action")]
        public bool Create {get; set;}
        [Option('u', "Update", HelpText="Are we updating a card here?", SetName="Action")]
        public bool Update {get; set;}

    }
}