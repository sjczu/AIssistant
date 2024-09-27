using System;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json;
using Microsoft.CognitiveServices.Speech;
using System.Collections.Generic;

// very basic console app for SST(Speech-To-Text)->TTS(Text-To-Speech) communication with OpenAI ChatGPT AI.
// uses Azure Speech Services for both SST and TTS, therefore Azure account with active subscription is needed
// for new users Azure gives free service access under certain limits and restrictions for 12 months, should be enough for personal use
// another third-party dependency is OpenAI API account
// subscription is not needed, however Free Tier doesn't allow usage of any ChatGPT model above gpt-3.5-turbo which is severely outdated
// to gain access to small, limited quota for gpt-4, 4o, and other new models you need to add 5$ to your OpenAI API account balance
// keep in mind that first request will contain only one message, however each subsequent request will contain up to 5 messages (in default config)
// this can be changed in RequestHandler class under maxMessages int to whatever value you want - this will change the number of messages sent in one request


class AIssistant
{
    static async Task Main(string[] args)
    {
        string languageKey = "";

        bool isLanguageChosen = false;

        while (!isLanguageChosen)
        {
            Console.WriteLine("Select language: \n 1. English (EN) \n 2. Polish (PL)");
            string userInput = Console.ReadLine();

            if (userInput == "1" || userInput == "EN" || userInput == "English")
            {
                languageKey = "EN";
                isLanguageChosen = true;
            }
            else if (userInput == "2" || userInput == "PL" || userInput == "Polish")
            {
                languageKey = "PL";
                isLanguageChosen = true;
            }
            else Console.WriteLine("Wrong input");
        }
        var speechToText = new SpeechToText(languageKey);
        var requestHandler = new RequestHandler();
        var textToSpeech = new TextToSpeech(languageKey);

        Console.WriteLine("Press ',' to start speaking");

        bool isProcessing = false;
        bool isFirst = true;
        while (true)
        {
            // this is only for a case where app somehow throws you out of the if statement below, without completing the request
            // it won't fix it, but it'll let you know that the request bugged out - essentially it's a leftover from older implementation based on ConsoleRead() instead of Console.ReadKey()
            if (isProcessing)
            {
                Console.WriteLine("Please wait, processing previous request...");
                await Task.Delay(100);
                continue;
            }

            if (Console.ReadKey(true).Key == ConsoleKey.OemComma)
            {
                isProcessing = true;
                Console.WriteLine("Processing your request...");

                string recognizedText = await speechToText.RecognizeSpeechAsync();
                Console.WriteLine("You said: " + recognizedText);

                Console.WriteLine("Awaiting GPT response...");
                string assistantResponse = await requestHandler.GetResponseFromOpenAI(recognizedText,isFirst);
                Console.WriteLine("GPT: " + assistantResponse);

                Console.WriteLine("Converting GPT response to speech...");
                await textToSpeech.SpeakAsync(assistantResponse);

                isProcessing = false;
            }
            else if (Console.ReadKey(true).Key == ConsoleKey.Q) break;

            isFirst = false;
            Console.WriteLine("Press ',' to speak again or 'q' to quit");

        }
    }
}

public class SpeechToText
{
    // just like OPENAI_API_KEY this one has to be set in environmental variables
    // region is set to germanywestcentral since this is where my Azure Speech service is hosted
    // change it to the region you've chosen for service hosting
    // you only need one instance of AZURE_API_KEY variable in environment - both STT and TTS use same variable

    private readonly SpeechRecognizer recognizer;
    private readonly string apiKey = Environment.GetEnvironmentVariable("AZURE_API_KEY");
    private readonly string region = "germanywestcentral";

    public SpeechToText(string languageKey)
    {
        var config = SpeechConfig.FromSubscription(apiKey, region);
        if (languageKey == "EN")
        {
            config.SpeechRecognitionLanguage = "en-US";
        }
        else config.SpeechRecognitionLanguage = "pl-PL";
        recognizer = new SpeechRecognizer(config);
    }   
	
	public async Task<string> RecognizeSpeechAsync()
    {
        var result = await recognizer.RecognizeOnceAsync();

        if (result.Reason == ResultReason.RecognizedSpeech)
        {
            return result.Text;
        }
        else if (result.Reason == ResultReason.NoMatch)
        {
            return "No speech was recognized";
        }
        else return "Error recognizing speech";
    }
}

public class Message
{
    public string role { get; set; }
    public string content { get; set; }
}

public class RequestHandler
{
    // two different variables due to debugging - if one API key doesn't work, just generate another one in OpenAI API portal
    // I recommend setting both of these in environment and just using one or the other depending on which one works first
    // OpenAI API can throw some errors even if theoretically it should work (like no permissions error when you're the owner of the project, or insufficient quota even though you have money in account balance and no usage)
    // if such errors occurs, you can just switch between the keys to see if other one works
    // as stated in WriteLine statement below you can set these in command line with setx OPENAI_API_KEY \"your_api_key_here\"
    private readonly string apiKey = Environment.GetEnvironmentVariable("OPENAI_PROJECT_API_KEY");
    private readonly string apiKeyUser = Environment.GetEnvironmentVariable("OPENAI_USER_API_KEY");

    // limit of messages in the history
    // you don't have to use it, but then you'll send enormous amount of tokens in longer conversation
    private const int maxMessages = 5;

    private List<Message> messages = new List<Message>();

    public async Task<string> GetResponseFromOpenAI(string input, bool isFirst)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("API key not set. Please configure it in your environment variables or by using 'setx OPENAI_API_KEY \"your_api_key_here\"' command");
            return "API key missing";
        }
        
        
        var client = new RestClient("https://api.openai.com/v1/chat/completions");
        var request = new RestRequest("https://api.openai.com/v1/chat/completions", Method.Post);
        
        // I chose isFirst boolean implementation when I was playing around with idea of using Threads instead of Completions API, however
        // it didn't proof fruitful nor profitable, as it still uses up tokens for history, however with much bigger limit.
        // I decided to leave it here in case Threads API changes and actually holds an advantage over Completions API for this app
        if (isFirst&&messages.Count==0)
        {

            request.AddHeader("Authorization", "Bearer " + apiKeyUser);
            request.AddHeader("Content-Type", "application/json");

            messages.Add(new Message { role = "user", content = input });
            var body = new
            {
                model = "gpt-4o",
                //messages = new[] { new { role = "user", content = input } }
                messages = messages
            };

            request.AddJsonBody(body);
            // data for debugging
            Console.WriteLine("User request: \n");
            foreach (var header in request.Parameters)
            {
                if (header.Type == ParameterType.HttpHeader)
                {
                    Console.WriteLine($"{header.Name}: {header.Value}");
                }
            }

            string jsonBody = JsonConvert.SerializeObject(body);
            Console.WriteLine("Request Body: " + jsonBody);

            // waiting for GPT response
            var response = await client.ExecuteAsync(request);
            Console.WriteLine("API Response: " + response.Content);

            if (response == null || !response.IsSuccessful)
            {
                Console.WriteLine("Error: " + response.ErrorMessage);
                return "Error getting response.";
            }
            dynamic jsonResponse = JsonConvert.DeserializeObject(response.Content);

            // more debugging stuff
            Console.WriteLine("Parsed JSON Response: \n" + JsonConvert.SerializeObject(jsonResponse, Formatting.Indented));

            // error check in case there's no GPT response in received json
            if (jsonResponse.choices == null || jsonResponse.choices.Count == 0)
            {
                Console.WriteLine("No choices found in response.");
                return "No response.";
            }

            return jsonResponse.choices[0].message.content;
        }
        else if (!isFirst)
        {

            client = new RestClient("https://api.openai.com/v1/chat/completions");
            request = new RestRequest("https://api.openai.com/v1/chat/completions", Method.Post);


            request.AddHeader("Authorization", "Bearer " + apiKeyUser);
            request.AddHeader("Content-Type", "application/json");

            // removal of messages for limiting token usage
            if (messages.Count >= maxMessages)
            {
                messages.RemoveAt(0);
            }

            messages.Add(new Message { role = "user", content = input });

            var body = new
            {
                model = "gpt-4o",
                //messages = new[] { new { role = "user", content = input } }
                messages = messages
            };
            var jsonBody = JsonConvert.SerializeObject(body);

            //request.AddJsonBody(body);

            request.AddParameter("application/json", jsonBody, ParameterType.RequestBody);

            // debugging stuff
            Console.WriteLine("User request: \n");
            foreach (var header in request.Parameters)
            {
                if (header.Type == ParameterType.HttpHeader)
                {
                    Console.WriteLine($"{header.Name}: {header.Value}");
                }
            }

            //string requestBodyJson = JsonConvert.SerializeObject(body);
            
            Console.WriteLine("Request Body: " + jsonBody);
            
            // waiting for GPT response
            var response = await client.ExecuteAsync(request);
            Console.WriteLine("API Response: " + response.Content);

            if (response == null || !response.IsSuccessful)
            {
                Console.WriteLine("Error: " + response.ErrorMessage);
                return "Error getting response.";
            }
            dynamic jsonResponse = JsonConvert.DeserializeObject(response.Content);

            // more debugging stuff
            Console.WriteLine("Parsed JSON Response: \n" + JsonConvert.SerializeObject(jsonResponse, Formatting.Indented));

            // error check in case there's no GPT response in received json
            if (jsonResponse.choices == null || jsonResponse.choices.Count == 0)
            {
                Console.WriteLine("No choices found in response.");
                return "No response.";
            }

            return jsonResponse.choices[0].message.content;

        }
        return "Method not called";
        //return jsonResponse.choices[0].message.content;
    }
}


public class TextToSpeech
{

    private SpeechConfig speechConfig;

    // just like OPENAI_API_KEY this one has to be set in environmental variables
    // region is set to germanywestcentral since this is where my Azure Speech service is hosted
    // change it to the region you've chosen for service hosting
    private readonly string apiKey = Environment.GetEnvironmentVariable("AZURE_API_KEY");
    private readonly string region = "germanywestcentral";

    // voice can be changed by going to https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-support?tabs=tts#text-to-speech
    // and finding voice name you want to use
    public TextToSpeech(string languageKey)
    {
        speechConfig = SpeechConfig.FromSubscription(apiKey, region);
        if (languageKey == "EN")
        {
            speechConfig.SpeechSynthesisVoiceName = "en-US-AndrewNeural";
        } 
        else speechConfig.SpeechSynthesisVoiceName = "pl-PL-MarekNeural";
    }

    public async Task SpeakAsync(string text)
    {
        using (var synthesizer = new SpeechSynthesizer(speechConfig))
        {
            var result = await synthesizer.SpeakTextAsync(text);
            
            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                Console.WriteLine("Speech synthesized successfully");
            } 
            else Console.WriteLine($"Speech synthesis failed: {result.Reason}");
        }
    }
}

