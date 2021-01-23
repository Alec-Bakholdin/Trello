using RestSharp;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CredentialManagement;

namespace Trello.Main
{
    public static class TrelloUtility
    {
        // * * * * * * * * * * Generic Stuff * * * * * * * * * *
        private static string TrelloBaseURL                   = "https://api.trello.com";
        private static Dictionary<string, string> Credentials = FetchCredentials("credentials.json");
        private static string TrelloKey                       = Credentials["Key"];
        private static string TrelloToken                     = Credentials["Token"];
        
        // * * * * * * * * * * Relevant Endpoints * * * * * * * * * *
        private static string GetAllOpenBoardsEndpoint        = "{{BaseURL}}/1/members/me/boards?key={{key}}&token={{token}}&filter=open";
        private static string GetAllListsOnABoardEndpoint     = "{{BaseURL}}/1/boards/{{boardID}}/lists?key={{key}}&token={{token}}";














        //  * * * * * * * * * * API Url Manipulation * * * * * * * * * *

        
        /**
         * Substitutes the base url, key, and token
         * for any endpoint, since these are present
         * in each endpoint string
         */
        public static string BasicURLSubstitution(string url)
        {
            return url
                    .Replace("{{BaseURL}}", TrelloBaseURL)
                    .Replace("{{key}}", TrelloKey)
                    .Replace("{{token}}", TrelloToken);
        }


        /**
         * Creates a string of all public fields
         * of this class that are not null and puts
         * them in query parameter format:
         *      key=value&key=value ...
         */
        public static string EnumerateQueryParmeters(object obj, string[] excludeParameters = null)
        {
            if(excludeParameters == null)
                excludeParameters = new string[0];

            var queryParameterValues = obj  .GetType()
                                            .GetFields()
                                            .Select(field => (Name: FirstLetterLowercase(field.Name), Value: GetFieldValue(field.GetValue(obj)))) // create (Name, Value) tuple
                                            .ToList()
                                            .FindAll(fieldNameValue => fieldNameValue.Value != null && !excludeParameters.Contains(fieldNameValue.Name)); // filter out those that are null or excluded

            var queryParameterStr = queryParameterValues
                                        .Select(input => $"{input.Name}={input.Value}")
                                        .Aggregate((x, y) => x += $"&{y}");

            return $"&{queryParameterStr}";
        }

        public static string FirstLetterLowercase(string target)
        {
            return target.First().ToString().ToLower() + target.Substring(1);
        }

        public static string GetFieldValue(object fieldValue)
        {
            if(fieldValue is string)
                return (string)fieldValue == "" ? null : (string)fieldValue;
            else if(fieldValue is string[])
                return ((string[])fieldValue).Length == 0 ? null : String.Join(",", (string[])fieldValue);
            else if(fieldValue is bool)
                return (bool)fieldValue ? "true" : "false";
            else if(fieldValue is DateTime)
                return ((DateTime)fieldValue).ToString("yyyy-MM-ddTHH:mm:ssZ");
            else return null;
        }

        /**
         * Calls basic RestSharp request with
         * empty RestRequest
         */ 
        public static async Task<IRestResponse> BasicRequest(string url, RestSharp.Method method)
        {
            var restRequest = new RestRequest();
            var restClient = new RestClient(url);
            var restResponse = await restClient.ExecuteAsync(restRequest, method);

            return restResponse;
        }



















        // * * * * * * * * * * Querying the API for information * * * * * * * * * *

        /**
         * Calls the endpoint, then
         * finds the object whose 'name' field matches and
         * returns its ID
         */
        public static async Task<string> GetID(string endpoint, string targetName)
        {
            // get jobject
            var url = BasicURLSubstitution(endpoint);
            var targetObject = await GetJObjectByName(url,"name", targetName);

            // get id
            var targetID = targetObject["id"].ToString();
            return targetID;
        }

        
        /**
         * Queries the API for a JObject with the target property
         * set to the value specified. Applies Newtonsoft's
         * Linq.Select format for selecting fields (e.g. value[0].name)
         */
        public static async Task<JObject> GetJObjectByName(string url, string targetField, string valueToCompare)
        {
            var restResponse = await BasicRequest(url, RestSharp.Method.GET);

            // check success
            if(!restResponse.IsSuccessful)
                throw new Exception($"Error finding match for {targetField}:{valueToCompare} - {restResponse.Content}");

            // find object whose name matches targetName
            var responseJson = (JArray)JsonConvert.DeserializeObject(restResponse.Content);
            var responseObjectList = responseJson.Where(jToken => ((JObject)jToken)["name"].ToString() == valueToCompare).ToArray();
            
            // check there is exactly one
            if(responseObjectList.Length > 1)
                throw new Exception($"There are {responseObjectList.Length} objects with the name {valueToCompare}");
            else if(responseObjectList.Length == 0)
                throw new Exception($"There are no objects with the {targetField} {valueToCompare}");

            
            var targetObject = (JObject)responseObjectList[0];

            return targetObject;
        }

        
        /**
         * Get the id of a list on the board and with the name given
         */
        public static async Task<string> GetListID(string boardName, string listName)
        {
            var boardURL = BasicURLSubstitution(GetAllOpenBoardsEndpoint);
            var boardID  = await GetID(boardURL, boardName);

            var listURL  = BasicURLSubstitution(GetAllListsOnABoardEndpoint).Replace("{{boardID}}", boardID);
            var listID   = await GetID(listURL, listName);

            return listID;
        }





















        // * * * * * * * * * * Json Manipulation * * * * * * * * * *


        /**
         * Takes a JArray and turns it into a dictionary where the key
         * for each JObject of the JArray is the value stored at targetField
         * 
         * The JObjects are references, not copies, so be wary of this
         * when modifying values in the Dictionary
         */
        public static Dictionary<string, JObject> AssociateJArray(JArray jArray, string CardIdentifierField)
        {
            // initialize associated array (origin: PHP, screw that language)
            var associatedArray = new Dictionary<string, JObject>();
            foreach(JObject jObject in jArray)
            {
                // check to make sure jObject contains the target field so we have verbose error
                if(!jObject.ContainsKey(CardIdentifierField))
                    throw new Exception($"Error associating JArray: JObject does not contain a field {CardIdentifierField}");

                // get the value at targetField and set the jobject as the value to that key
                var jObjectValue = jObject[CardIdentifierField].ToString();
                if(associatedArray.ContainsKey(jObjectValue))
                    throw new Exception($"JArray contains multiple cards with {CardIdentifierField} {jObjectValue}");
                associatedArray[jObjectValue] = jObject;
            }
            return associatedArray;
        }










        
        // * * * * * * * * * * Console Functions * * * * * * * * * *
        private static object ConsoleLock = new object();

        // Prints red
        public static void LogError(string payload)
        {
            LogColor(payload, ConsoleColor.Red);
        }

        // Prints yellow
        public static void LogWarning(string payload)
        {
            LogColor(payload, ConsoleColor.Yellow);
        }

        // Prints green
        public static void LogSuccess(string payload)
        {
            LogColor(payload, ConsoleColor.Green);
        }

        // prints without color
        public static void LogNormal(string payload)
        {
            Console.WriteLine(payload);
        }

        public static void LogColor(string payload, ConsoleColor color)
        {
            lock(ConsoleLock)
            {
                // change color and store old color
                var storedColor = Console.ForegroundColor;
                Console.ForegroundColor = color;

                // print payload
                Console.WriteLine(payload);

                // changes color back to what it was
                Console.ForegroundColor = storedColor;
            }
        }














        // * * * * * * * * * * Fetch Credentials * * * * * * * * * *
        private static Dictionary<string, string> FetchCredentials(string filename)
        {
            // retrieve credentials from windows
            Credential cred = new Credential(){Target="TrelloAPI"};
            if(!cred.Load())
            {
                cred = PromptUserForCredentials();
                cred.Save();
            }

            // store the credentials in our dictionary
            var credDict = new Dictionary<string, string>();
            credDict["Key"]   = cred.Username;
            credDict["Token"] = cred.Password;

            return credDict;
        }

        private static Credential PromptUserForCredentials()
        {

            // prompt user for api key and token
            Console.Write("API Key: ");
            string key = Console.ReadLine();
            Console.Write("API Token: ");
            string token = Console.ReadLine();

            // create credential
            var cred = new Credential(){
                Target = "TrelloAPI",
                Username = key,
                Password = token,
                PersistanceType = PersistanceType.LocalComputer
            };

            return cred;
        }









    }
}