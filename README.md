# AIssistant
## Speak with GPT models!

This tool is only a small project that I've decided to take upon myself for learning purposes of json requests and API connections in C#... And also for fun, I mean who doesn't want to talk with their own "AI" assistant? :D

## How does it work?
The way it works is very, very simple:
  -Azure Speech Serivces are used for STT (Speech-To-Text) and TTS (Text-To-Speech) features
  -OpenAI Completions API is used for actual talking with GPT models

## Important variables in the code:
### OPENAI_USER_API_KEY // OPENAI_PROJECT_API_KEY
Your openAI API user/project key.
As mentioned above and in the first comment in the code OpenAI API account and subscription are needed for this app to work.
After creating OpenAI API account (or if you already had one) you need to set OPENAI_USER_API_KEY or OPENAI_PROJECT_API_KEY values in the environment variables on your machine. It's done this way so that you don't have to input your personal API key into the code.
To get your API user key go to: https://platform.openai.com/ -> Settings (cog icon in top right corner) -> Your profile -> User API keys
To get you API project key go to: https://platform.openai.com/ -> Dashboard -> API keys

##### project/user API keys - which one should I use?
Generally speaking project API keys have been implemented by OpenAI to allow assigning users to projects within organization. That means that if you want multiple users to use one subscription, it's highly recommended that you use project API keys instead of user API key. 
For more detailed description on project API keys go to https://help.openai.com/en/articles/9186755-managing-your-work-in-the-api-platform-with-projects
From app perspective you can use whichever key you want. As long as it's assigned to variable in your environment it'll work.

### AZURE_API_KEY
API key for accessing Azure Speech service.
Before accessing the app you need to create Azure account and then Speech service.
After setting it up you can access API key under Resource Management -> Keys and Endpoint in the service overview.

### region
This variable is used to define in which region your service is located in.
By default it's set to 'germanywestcentral', but you should change it to your own value reflecting proper region in which you've created the Speech service.

## Less important, but still important variables in the code:
### languageKey 
Used for choosing language for speech/text recognition.
By default app has 2 possibilities implemented: EN (English US) and PL (Polish).
You can add more languages or remove the option to choose entirely and hardcode the value for your desired language.

### speechConfig.SpeechSynthesisVoiceName
It affects synthesizer by changing the voice in which it reads out GPT's response.
By default there are 2 values implemented: AndrewNeural for English and MarekNeural for Polish.
It's value is defined by languageKey variable.
You can change these values or add more choices if you want to.
For full list of available voices in Azure Speech services go to: https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-support?tabs=tts

### recognitionConfig.SpeechRecognitionLanguage
It affect synthezier, which recognizes your speech and converts it into text.
By defauly there are 2 possibilities: 'en-US' and 'pl-PL'.
For full list of language codes go to: https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-support?tabs=language-identification

### maxMessages - number of messages that'll be stored in the list and sent with each subsequent request. It's there for a reason - reason being token usage limitation so that you don't use up all your precious tokens within 50 requests.
Simple explanation - in order for GPT models to actually remember what conversation was about and give thoughtout responses that make sense it needs to have a context. This context is created by sending multiple messages with request, instead of just newest one.
This means that each subsequent request after maxMessages value will contain that many messages inside, and will use up tokens for each message it sends.
For default maxMessages = 5 we send:
  - 1 message with first request
  - 2 messages with second request
  - 3 messages with third request
  - ...
  - 5 messages with fifth request
  - 5 messages with sixth request
  - and so on...

Tokens are these thingies that OpenAI uses to estimate how 'valuable' each word is in context of computing power for models to process. That means that words like 'no', 'cat', 'yes', 'ok' will usually be worth ~1 token, since they are easy and short, but words like 'approximately' will be worth way more tokens. It works the same for both incoming requests as well as outgoing requests, so model's response will be converted into token value as well, and these tokens will be used up from your limits.

More variables will be added when I think of any other crucial ones.
