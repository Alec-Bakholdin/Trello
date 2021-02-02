using System;
using System.Collections.Generic;
using RestSharp;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;


namespace Trello.Main
{
    using static TrelloUtility;

    public class TrelloCard
    {
        // * * * * * * * * Environment Variables * * * * * * * 
        private static string CreateCardEndpoint              = "{{BaseURL}}/1/cards?key={{key}}&token={{token}}";
        private static string DeleteCardEndpoint              = "{{BaseURL}}/1/cards/{{cardID}}?key={{key}}&token={{token}}";
        private static string UpdateCustomFieldEndpoint       = "{{BaseURL}}/1/card/{{cardID}}/customField/{{customFieldID}}/item?key={{key}}&token={{token}}";
        private static string GetCustomFieldsOnBoardEndpoint  = "{{BaseURL}}/1/boards/{{boardID}}/customFields?key={{key}}&token={{token}}";
        private static string UpdateCardEndpoint              = "{{BaseURL}}/1/cards/{{cardID}}?key={{key}}&token={{token}}";
        private static string GetCustomFieldsForACardEndpoint = "{{BaseURL}}/1/cards/{{cardID}}/customFieldItems?key={{key}}&token={{token}}";



        // * * * * * * * * Private and Utility Variables * * * * * * * 
        private static Dictionary<string, string> CustomFieldLookupDict = new Dictionary<string, string>();
        private Dictionary<string, string> CustomFieldValues = null;
        private string cardID     = null;
        private string identifier = null;



        // * * * * * * * Query Parameters * * * * * * * *
        public string    name              = null;
        public string    desc              = null;
        public string[]  idMembers         = null;
        public string    idAttachmentCover = null;
        public string    idList            = null;
        public string    idBoard           = null;
        public string    pos               = null;
        public DateTime? due               = null;
        public bool?     dueComplete       = null;
        public bool?     subscribed        = null;
        public string    address           = null;
        public string    coordinates       = null;
        public JObject   cover             = null;







        // * * * * * * * * Constructors * * * * * * * *


        /**
         * Base constructor
         */
        public TrelloCard()
        {
        }

        /**
         * Base constructor but sets idList
         */
        public TrelloCard(string idList)
        {
            this.idList = idList;
        }

        /**
         * Map the data from the api JObject to the card,
         * ensuring proper data conversion
         */
        public TrelloCard(JObject cardObject)
        {
            PopulateTrelloCard(cardObject);
        }

















        // * * * * * * * * * * Card Manipulation Functions * * * * * * * * * *


        /**
         * Creates the card through the Api,
         * populates the card with info it received
         */
        public async Task<IRestResponse> Create()
        {
            if(idList == null)
                return new RestResponse(){Content="idList has not been initialized"};

            // get the  url
            var url = BasicURLSubstitution(CreateCardEndpoint) + EnumerateQueryParmeters(this);
            

            // make api request
            var restResponse = await BasicRequest(url, RestSharp.Method.POST);

            // if successful, populate the card with the information received back
            if(restResponse.IsSuccessful)
            {
                var cardObject = (JObject)JsonConvert.DeserializeObject(restResponse.Content);
                PopulateTrelloCard(cardObject);
            }


            return restResponse;
        }


        /**
         * Remove the card from trello, if it exists
         */
        public async Task<IRestResponse> Delete()
        {
            // make sure we have a cardID
            if(cardID == null)
                return new RestResponse(){Content="TrelloCard has no cardID"};

            // get url from function
            var url = BasicURLSubstitution(DeleteCardEndpoint).Replace("{{cardID}}", cardID);

            // call API
            var restResponse = await BasicRequest(url, RestSharp.Method.DELETE);

            return restResponse;
        }


        /**
         * Update the card with the parameters that are set
         */
        public async Task<IRestResponse> Update()
        {
            if(this.cardID == null)
                return new RestResponse(){Content="cardID was not set"};

            var excludeParameters = new string[]{"dueComplete", "cover", "subscribed", "idAttachmentCover"};
            var url = BasicURLSubstitution(UpdateCardEndpoint).Replace("{{cardID}}", cardID) + EnumerateQueryParmeters(this, excludeParameters);

            // send request
            var restResponse = await BasicRequest(url, RestSharp.Method.PUT);

            return restResponse;
        }













        // * * * * * * * * * * Custom Field Functions * * * * * * * * * *


        /**
         * First fetches the ID of the customField on the
         * same board as this card, then updates the custom
         * field using the UpdateCustomFieldByID function
         * stores already-found ids in the customfieldlookup 
         * dictionary to speed things up later
         */
        public async Task<IRestResponse> SetCustomFieldByName(string customFieldName, string value, string type)
        {
            string customFieldID = await LookupCustomFieldID(customFieldName);
            if(customFieldID == null)
                return new RestResponse(){Content = $"Cannot fetch custom field ID from name, idBoard has not been set for card {this.name}"};

            return await SetCustomFieldByID(customFieldID, value, type);
        }


        /**
         * Updates custom field with value in 'value'
         * type is either text, checked (checkbox), date, or number
         */
        public async Task<IRestResponse> SetCustomFieldByID(string customFieldID, string value, string type)
        {
            // make sure card id is set
            if(cardID == null)
                return new RestResponse(){Content = "cardID was not set"};

            // check type is a valid type
            var validTypes = new string[]{"text", "checked", "date", "number"};
            if(!validTypes.Contains(type))
                return new RestResponse(){Content = $"{type} is not a supported custom field type"};

            // create url
            var url = BasicURLSubstitution(UpdateCustomFieldEndpoint).Replace("{{customFieldID}}", customFieldID).Replace("{{cardID}}", cardID);

            // create object to update custom field value
            var jObject = new JObject();
            jObject.Add("value", new JObject());
            ((JObject)jObject["value"]).Add(type, value);

            // create request with a json body
            var restRequest = new RestRequest();
            restRequest.AddJsonBody(jObject.ToString());

            // make API request
            var restClient = new RestClient(url);
            var restResponse = await restClient.ExecuteAsync(restRequest, RestSharp.Method.PUT);
            
            return restResponse;
        }

        /**
         * Get custom field value for this card by name
         */
        public async Task<string> GetCustomFieldValueByName(string customFieldName)
        {
            var customFieldID = await LookupCustomFieldID(customFieldName);

            return await GetCustomFieldValueByID(customFieldID);
        }

        /**
         * Gets the value of a particular custom field for this card by ID
         * referencing the previous
         */
        public async Task<string> GetCustomFieldValueByID(string customFieldID)
        {
            if(this.CustomFieldValues == null)
                await GetCustomFieldValues();

            if(!CustomFieldValues.ContainsKey(customFieldID))
                return null;
            
            return this.CustomFieldValues[customFieldID];
        }


        /**
         * Gets the values for the custom field and puts the values into
         * the dictionary CustomFieldValues with the id as the key
         * and the value as the value. Also sets the private 
         * static dictionary for future use.
         */
        public async Task<Dictionary<string, string>> GetCustomFieldValues()
        {
            if(this.cardID == null)
                throw new Exception($"cardID was not set when attempting to get custom field values");

            // make request
            var url = BasicURLSubstitution(GetCustomFieldsForACardEndpoint).Replace("{{cardID}}", this.cardID);
            var restResponse = await BasicRequest(url, Method.GET);

            if(!restResponse.IsSuccessful)
                throw new Exception($"Erorr when retrieving custom field values for card {this.name}: {restResponse.Content}");

            // convert the values to the json, then the dictionary
            var responseJson = (JArray)JsonConvert.DeserializeObject(restResponse.Content);
            
            this.CustomFieldValues = new Dictionary<string, string>();
            foreach(JObject customFieldObject in responseJson)
                this.CustomFieldValues.Add(customFieldObject["idCustomField"].ToString(), (customFieldObject["value"].First as JProperty).Value.ToString());

            return this.CustomFieldValues;
        }


        /**
         * Looks up a custom field ID from the API, first checking the local
         * dictionary CustomFieldLookup
         */
        public async Task<string> LookupCustomFieldID(string customFieldName)
        {
            if(this.idBoard == null)
                throw new Exception($"idBoard has not been set for card {this.name}");

            string customFieldID = null;

            if(CustomFieldLookupDict.ContainsKey(this.idBoard + customFieldName))
                customFieldID = CustomFieldLookupDict[this.idBoard + customFieldName];
            else
            {
                // set up url and get ID          
                var url = BasicURLSubstitution(GetCustomFieldsOnBoardEndpoint).Replace("{{boardID}}", this.idBoard);
                customFieldID = await GetID(url, customFieldName);

                // store the ID for later
                CustomFieldLookupDict[this.idBoard + customFieldName] = customFieldID;
            }

            return customFieldID;
        }
        











        //  * * * * * * * * Helper functions * * * * * * * * *


        /**
         * Populates the trelloCard with the information
         * stored in the cardObject, which should be a
         * json object received from the API, either
         * through card enumeration or after card
         * creation/updating
         */
        private void PopulateTrelloCard(JObject cardObject)
        {
            this.name               = cardObject["name"].ToString();
            this.desc               = cardObject["desc"].ToString();
            this.idMembers          = ((JArray)cardObject["idMembers"]).ToObject<string[]>();
            this.idAttachmentCover  = cardObject["idAttachmentCover"].ToString();
            this.idList             = cardObject["idList"].ToString();
            this.idBoard            = cardObject["idBoard"].ToString();
            this.pos                = cardObject["pos"].ToString();
            this.dueComplete        = (bool)cardObject["dueComplete"];
            this.subscribed         = (bool)cardObject["subscribed"];
            this.cover              = (JObject)cardObject["cover"];

            if(cardObject["due"].ToString() != "")
                this.due = DateTime.Parse(cardObject["due"].ToString());

            this.cardID             = cardObject["id"].ToString();
        }

        








        // * * * * * * * * * Private Field Getters/Setters * * * * * * * * * * *
        public string GetCardID()
        {
            return this.cardID;
        }
        public void SetIdentifier(string identifier)
        {
            this.identifier = identifier;
        }
        public string GetIdentifier()
        {
            return this.identifier;
        }
    }
}