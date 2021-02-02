using CommandLine;
using System;

namespace Trello.Main
{
    [Verb("card")]
    public class CardOptions
    {
        // modes of operation
        [Option('m', "mode", HelpText="Mode of operation: create, update", Default="create")]
        public string Mode {get; set;}
        
        // identify the card
        [Option('c', "card-name", HelpText="Name of the card", Default=null, Group="Name")]
        public string CardName {get; set;}
        [Option('l', "list-name", HelpText="Name of the list the card is located under", Default=null, Group="Name")]
        public string ListName {get; set;}
        [Option('b', "board-name", HelpText="Name of the board the card is located in. Only necessary if using list-name", Default=null, Group="Name")]
        public string BoardName {get; set;}
        
        // card properties
        [Option('d', "desc", HelpText="The description of the card", Default=null)]
        public string description {get; set;} 
        [Option('p', "pos", HelpText="Position of card: top, bottom, or positive float", Default=null)]
        public string position {get; set;}
        [Option('u', "due-date", HelpText="The date and time the card is due", Default=null)]
        public string dueDate {get; set;}
        [Option("due-complete", HelpText="Set whether the task is complete or not", Default=null)]
        public bool? dueComplete {get; set;}
        
    }
}