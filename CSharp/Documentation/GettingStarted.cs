namespace Microsoft.Bot.Builder.Connector
{
    /**
\page gettingstarted Getting started with the %Connector

The %Microsoft %Bot %Connector is a communication service that helps you connect your %Bot with many different 
communication channels (Skype, SMS, email, and others). If you write a conversational %Bot or agent and expose a 
%Microsoft %Bot Framework-compatible API on the Internet, the %Bot Framework %Connector service will forward messages 
from your %Bot to a user, and will send user messages back to your %Bot.

To use the %Microsoft %Bot Framework %Connector, you must have:

1. A %Microsoft Account (Hotmail, Live, Outlook.com) to log into the %Bot Framework developer portal, which you will use to register your %Bot.
2. An Azure-accessible REST endpoint exposing a callback for the %Connector service.
3. Developer accounts on one or more communication services(such as Skype) where your %Bot will communicate.

In addition you may wish to have an Azure App Insights account so you can capture telemetry from your %Bot. There are different ways to go 
about building a %Bot; from scratch, coded directly to the %Bot %Connector API, the %Bot %Builder SDK's for Node.JS & .NET, and the 
%Bot %Connector .NET template which is what this QuickStart guide demonstrates.

\section started Getting started in .NET    
This is a step-by-step guide to writing an %Bot in C\# using the %Bot Framework %Connector SDK .NET template.
1. Install prerequisite software
        - Visual Studio 2015 (latest update) - you can download the community version here for free: <a href="https://www.visualstudio.com/" target="_blank">www.visualstudio.com</a>
        - Important: Please update all VS extensions to their latest versions Tools->Extensions and Updates->Updates
2. Download and install the %Bot Application template
        - Download the file from the direct download link <a href="http://aka.ms/bf-bc-vstemplate" target="_blank">here</a>:
        - Save the zip file to your Visual Studio 2015 project templates directory which is traditionally in "%USERPROFILE%\Documents\Visual Studio 2015\Templates\ProjectTemplates\Visual C#\"
        - Additionally, you can download <a href="https://aka.ms/bf-bc-vsdialogtemplate" target="_blank">Dialog</a> and <a href="https://aka.ms/bf-bc-vscontrollertemplate" target="_blank">Controller</a> Item templates. 
        - Save these to your Visual Studio 2015 item templates directory which is traditionally in "%USERPROFILE%\Documents\Visual Studio 2015\Templates\ItemTemplates\Visual C#\"
3. Open Visual Studio
4. Create a new C\# project using the new %Bot Application template.
        ![Create a new C\# project using the new %Bot Application template.](/en-us/images/connector/connector-getstarted-create-project.png)
5. The template is a fully functional Echo %Bot that takes the user's text utterance as input and returns it as output. In order to run however, 
        - The %bot has to be registered with %Bot %Connector
        - The AppId and AppPassword from the %Bot Framework registration page have to be recorded in the project's web.config
        - The project needs to be published to the web

\section building Building your Bot
The core functionality of the %Bot Template is all in the Post function within Controllers\MessagesController.cs. In this case the code 
takes the message text for the user, then creates a reply message using the CreateReplyMessage function. The BotAuthentication decoration 
on the method is used to validate your %Bot %Connector credentials over HTTPS.

\code{.cs}
[BotAuthentication]
public class MessagesController : ApiController
{
        <summary>
        POST: api/Messages
        Receive a message from a user and reply to it
        </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            ConnectorClient connector = new ConnectorClient(new Uri(activity.ServiceUrl));
            if (activity.Type == ActivityTypes.Message)
            {
                // calculate something for us to return
                int length = (activity.Text ?? string.Empty).Length;

                // return our reply to the user
                Activity reply = activity.CreateReply($"You sent {activity.Text} which was {length} characters");
                await connector.Conversations.ReplyToActivityAsync(reply);
            }
            else
            {
                HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }
}

\endcode

    \section emulator Emulator
Use the %Bot Framework Emulator to test your %Bot application

The %Bot Framework provides a a channel emulator that lets you test calls to your %Bot as if it were being called 
by the %Bot Framework cloud service. To install the %Bot Framework Emulator, download it from <strong><a href="https://aka.ms/bf-bc-emulator" target="_blank">here</a></strong>.

Once installed, you're ready to test. First, start your %Bot in Visual Studio using a browser as the application
host. The image below uses %Microsoft Edge.

![Start your Bot in VS2015 targeting the browser](/en-us/images/connector/connector-getstarted-start-bot-locally.png)

When the application is built and deployed the web browser will open and display the application Default.htm 
file (which is part of the %Bot Application project). Feel free to modify the Default.html file to match the 
name and description of your %Bot Application.

Here's the %Bot Application Default.htm file in %Microsoft Edge

![Bot running the browser targeting localhost](/en-us/images/connector/connector-getstarted-bot-running-localhost.png)

When using the emulator to test your %Bot application, make note of the port that the application is running on, 
which in this example is port 3978. You will need this information to run the %Bot Framework Emulator.

Now open the %Bot Framework Emulator. There are a few items that you will need to configure in the tool before 
you can interact with your %Bot Application.

When working with the emulator with a bot **running locally**, you need:
    - The **Url** for your bot set the localhost:<port> pulled from the last step. 
        > Note: will need to add the path "/api/messages" to your  URL when using the %Bot Application template.
    - Empty out the **MicrosoftAppId** field
    - Empty out the **MicrosoftAppPassword** field

This will only work with the emulator running locally; in the cloud you would instead have to specify the appropriate URL and authentication values.
For more about the emulator, read <a href="/en-us/tools/bot-framework-emulator/" target="_blank">here</a>.

![Configure the emulator with your locahost URL, AppId & AppPassword](/en-us/images/connector/connector-getstarted-configure-emulator.png)

Now that everything is configured you can interact with your service. The bottom of the %Bot Framework Emulator application has a 
Text Box that you can use to enter a message, this message will be echoed back to you, like below.

![Testing the interaction with the Bot via the emulator](/en-us/images/connector/connector-getstarted-test-conversation-emulator.png)

If we take a look at the code in the %Bot Application that was generated by the Visual Studio 2015 %Bot Application Template, 
specifically the file called MessageController.cs we can see how the message entered by a user is converted into the reply 
Activity, sending "You sent {activity.Text} which was {length} characters" back to the user.     

\section publishing Publishing your Bot Application to Microsoft Azure

In this tutorial, we use %Microsoft Azure to host the %Bot application. To publish your %Bot Application you will need 
a %Microsoft Azure subscription. You can get a free trial from here: <a href="https://azure.microsoft.com/en-us/" target="_blank">azure.microsoft.com/en-us/</a> 

Make what changes you like to the project, and now you're ready to publish. Right click on the project and choose "Publish", 
and then your appropriate Azure subscription information. By default, the %bot should be published as an 
%Microsoft Azure App Service. When publishing, keep track of the URL you chose because we'll need it to update the 
%Bot Framework registration endpoint. The first time you publish there are a few extra steps; but you only have to 
do them once.


In Visual Studio, right clicking on the project in Solution Explorer and select "Publish" - or alternately 
selecting "Build | Publish" displays the following dialog:

![Right click on the project and choose __Publish__ to start the Azure publish wizard](/en-us/images/connector/connector-getstarted-publish-dialog.png)

The Publish to Azure wizard will start. For this tutorial you will need to select "Microsoft Azure App Service" as 
your project type.

![Select %Microsoft Azure App Service and click Next](/en-us/images/connector/connector-getstarted-publish.png)

The next step in the Azure App Service publishing process is to create your App Service. Click on "New…" on the 
right side of the dialog to create the App Service.

![Click new to create a _New..._ Azure App Service](/en-us/images/connector/connector-getstarted-publish-app-service.png)

The Create App Service dialog will be displayed, fill in the details as appropriate.Make sure to choose "Web App" 
from the Change Type drop down in the top right instead of "API App"(which is the default).

![Give your App Service a name, then click New App Service Plan to define one](/en-us/images/connector/connector-getstarted-publish-app-service-create.png)

One final complexity on this dialog is the App Service Plan. This just lets you give a name to a combination of 
location and system size so you can re - use it on future deployments. Just put in any name, then choose the 
datacenter and size of deployment you want.

![Create your definition for an App Service Plan ](/en-us/images/connector/connector-getstarted-publish-app-service-create-spinner.png)

Once you hit okay on the App Service Plan, you'll have defined your App Service completely. Hit Create, and 
you'll be taken back to the Publish Web Wizard.

![Complete the Create App Service wizard by clicking Create](/en-us/images/connector/connector-getstarted-publish-destination.png)

Now that you've returned to the Publish Web wizard copy the destination URL to the clipboard, you'll need it in a 
few moments. Hit "Validate Connection" to ensure the configuration is good, and if all goes well, click "Next".

![Validate and click next to move on to the last step.](/en-us/images/connector/connector-getstarted-publish-configuration.png)

By default your %Bot will be published in a Release configuration. If you want to debug your %Bot, change Configuration 
to Debug. Regardless, from here you'll hit "Publish" and your %Bot will be published to Azure.

![Last step; click Publish to submit to Azure](/en-us/images/connector/connector-getstarted-publish-preview.png)

You will see a number of messages displayed in the Visual Studio 2015 "Output" window. Once publishing is complete you 
will also see the web page for your %Bot Application displayed in your browser (the browser will launch, and render 
your %Bot Application HTML page), see below.

![Voila, your Bot has been published and is running.](/en-us/images/connector/connector-getstarted-publish-output.png)

\section registering Registering your Bot with the Microsoft Bot Framework 

Registering your %Bot tells the %Connector how to call your %Bot's web service. Note that the **MicrosoftAppId** and 
    **MicrosoftAppPassword** are generated when your %Bot is registered with the %Microsoft %Bot Framework %Connector, 
the MicrosoftAppId and MicrosoftAppPassword are used to authenticate the conversation, and allows the developer to 
configure their %Bot with the Channels they'd like to be visible on. The BotId, which you specify, is used for the 
URL in the directory and developer portal.


1. Go to the %Microsoft %Bot Framework portal at <a href="https://dev.botframework.com" target="_blank">https://dev.botframework.com</a> and 
sign in with your %Microsoft Account.

2. Click the "Register a Bot" button and fill out the form. Many of the fields on this form can be changed later. 
Use a the endpoint generated from your Azure deployment, and don't forget that when using the %Bot Application 
tempalate you'll need to extend the URL you pasted in with the path to the endpoint at / API / Messages. You 
should also prefix your URL with HTTPS instead of HTTP; Azure will take care of providing HTTPS support on your 
bot. Save your changes by hitting "Create" at the bottom of the form.

![Register a bot](/en-us/images/connector/connector-getstarted-register-agent.png)

3. Once your registration is created, %Microsoft %Bot Framework will take you through generating your **MicrosoftAppId** and **MicrosoftAppPassword**. 
These are used to authenticate your %Bot with the %Microsoft %Bot Framework. __NOTE:__ When you generate your MicrosoftAppPassword, be sure to record 
it somewhere as you won't be able to see it again.

![Microsoft Bot Framework will have generated your MicrosoftAppId and MicrosoftAppPassword](/en-us/images/connector/connector-getstarted-subscription-keys.png)

Now that the %Bot is registered, you need to update the keys in the web.config file in your Visual Studio project. 
Change the following keys in the web.config file to match the ones generated when you saved your registration, and 
you're ready to build. Clicking the "show" link will show the value, along with exposing the 
regenerate link if you ever need to change your AppPassword. Update your web.config, and re-publish your 
%bot to Azure.

~~~

<? xml version = "1.0" encoding = "utf-8" ?>
       < !--
       For more information on how to configure your ASP.NET application, please visit
       http://go.microsoft.com/fwlink/?LinkId=301879
-->
< configuration >
< appSettings >
        < !--update these with your appid and one of your appsecret keys-->
        < add key = "MicrosoftAppId" value = "[GUID]" />
        < add key = "MicrosoftAppPassword" value = "[PASSWORD]" />
</ appSettings >
~~~

\section testing Testing the connection to your bot

Back in the developer dashboard for your %Bot there's a test chat window that you can use to interact with 
your %Bot without further configuration, and verify that the %Bot Framework can communicate with your %Bot's 
web service.

Note that the first request after your %Bot starts up can take 10 - 15 s as Azure starts up the web service 
for the first time. Subsequent requests will be quick. This simple viewer will let you see the JSON object 
returned by your %Bot.


![Test communication with your now deployed bot in the test channel.](/en-us/images/connector/connector-getstarted-test-channel-verification.png)

\section channels Configuring Channels

Now that you have a %Bot up and running, you'll want to configure it for one or more channels your users are 
using. Configuring channels is a combination of %Microsoft %Bot Framework workflow and conversation service 
workflow, and is unique for each channel you wish to configure.  

1. To configure a channel, go back to the %Bot Framework portal at https://www.botframework.com. Sign in, select your %Bot, and go to the channels panel.
      ![Sign in, select your %Bot, and go to the Channels panel.](/en-us/images/connector/connector-getstarted-configure-channels.png)


2. Pick the channel you wish to configure, and click add. You'll be taken to a page of instructions for registering a %Bot. 
In the end in most cases you're configuring your credentials as a developer on the target service, registering your app, 
and getting a set of Oauth keys that %Microsoft %Bot Framework can use on your behalf.
      ![Configuring a channel, for example, Facebook Messenger.](/en-us/images/connector/connector_channel_config_facebook.png)


3. Once you've gone through the steps here, return to the channel page on the dev portal, click the checkbox for the channel 
you chose (if you haven't already), and hit "save changes".

That's the end of configuration - your %Bot is ready for your users. They will have their own steps to follow to give the 
%Bot permission to participate in their group/channel or get connection details like the SMS phone number or e-mail. 
They can do this in the %Bot Directory page for your %Bot. The link to this is at the top of the %Bot Details page 
in the dev portal. 


    **/
}
