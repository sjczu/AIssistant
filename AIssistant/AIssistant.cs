using System;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json;
using Microsoft.CognitiveServices.Speech;

// very basic console app for SST(Speech-To-Text)->TTS(Text-To-Speech) communication with OpenAI ChatGPT AI.
// uses Azure Speech Services for both SST and TTS, therefore Azure account with active subscription is needed
// for new users Azure gives free service access under certain limits and restrictions for 12 months, should be enough for personal use
// another third-party dependency is OpenAI API account
// subscription is not needed, however Free Tier doesn't allow usage of any ChatGPT model above gpt-3.5-turbo which is severely outdated
// to gain access to small, limited quota for gpt-4, 4o, and other new models you need to add 5$ to your OpenAI API account balance


class AIssistant
{
    static async Task Main(string[] args)
    {
        var speechToText = new SpeechToText();
        var requestHandler = new RequestHandler();
        var textToSpeech = new TextToSpeech();

        Console.WriteLine("Press ',' to start speaking");

        bool isProcessing = false;

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
                string assistantResponse = await requestHandler.GetResponseFromOpenAI(recognizedText);
                Console.WriteLine("GPT: " + assistantResponse);

                Console.WriteLine("Converting GPT response to speech...");
                await textToSpeech.SpeakAsync(assistantResponse);

                isProcessing = false;
            }
            else if (Console.ReadKey(true).Key == ConsoleKey.Q) break;
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

    public SpeechToText()
    {
        var config = SpeechConfig.FromSubscription(apiKey, region);
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

public class RequestHandler
{
    // two different variables due to debugging - if one API key doesn't work, just generate another one in OpenAI API portal
    // I recommend setting both of these in environment and just using one or the other depending on which one works first
    // OpenAI API can throw some errors even if theoretically it should work (like no permissions error when you're the owner of the project, or insufficient quota even though you have money in account balance and no usage)
    // if such errors occurs, you can just switch between the keys to see if other one works
    // as stated in WriteLine statement below you can set these in command line with setx OPENAI_API_KEY \"your_api_key_here\"
    private readonly string apiKey = Environment.GetEnvironmentVariable("OPENAI_PROJECT_API_KEY");
    private readonly string apiKeyUser = Environment.GetEnvironmentVariable("OPENAI_USER_API_KEY");

    public async Task<string> GetResponseFromOpenAI(string input)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("API key not set. Please configure it in your environment variables or by using 'setx OPENAI_API_KEY \"your_api_key_here\"' command");
            return "API key missing";
        }
        
        var client = new RestClient("https://api.openai.com/v1/chat/completions");
        var request = new RestRequest("https://api.openai.com/v1/chat/completions", Method.Post);
        
        request.AddHeader("Authorization", "Bearer " + apiKeyUser);
        request.AddHeader("Content-Type", "application/json");

        var body = new
        {
            model = "gpt-4o",
            messages = new[] { new { role = "user", content = input } }
        };
        
        request.AddJsonBody(body);

        var response = await client.ExecuteAsync(request);
        Console.WriteLine("API Response: " + response.Content);

        if (response == null || !response.IsSuccessful)
        {
            Console.WriteLine("Error: " + response.ErrorMessage);
            return "Error getting response.";
        }
        dynamic jsonResponse = JsonConvert.DeserializeObject(response.Content);

        if (jsonResponse.choices == null || jsonResponse.choices.Count == 0)
        {
            Console.WriteLine("No choices found in response.");
            return "No response.";
        }

        return jsonResponse.choices[0].message.content;
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
    public TextToSpeech()
    {
        speechConfig = SpeechConfig.FromSubscription(apiKey, region);
        speechConfig.SpeechSynthesisVoiceName = "en-US-AndrewNeural";
        
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

