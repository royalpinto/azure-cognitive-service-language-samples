﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Microsoft.AspNetCore.Http;
using CoreBotCLU.Utils;

namespace Microsoft.BotBuilderSamples.Dialogs
{

    public class TransferCommand: CommandValue<String>
    {

    }

    public class ClientValue
    {
        public string Action { get; set; }
        public string CallID { get; set; }
    }

    public class MainDialog : ComponentDialog
    {
        private readonly FlightBookingRecognizer _cluRecognizer;
        protected readonly ILogger Logger;

        // Dependency injection uses this constructor to instantiate MainDialog
        public MainDialog(FlightBookingRecognizer cluRecognizer, BookingDialog bookingDialog, ILogger<MainDialog> logger)
            : base(nameof(MainDialog))
        {
            _cluRecognizer = cluRecognizer;
            Logger = logger;

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(bookingDialog);
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                IntroStepAsync,
                ActStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            Console.Out.WriteLine(" ==>> IntroStepAsync {0} {1} {2}", stepContext.Context.Activity.Locale, stepContext.Context.Activity.Text, stepContext.Context.Activity.Value);
            if (!_cluRecognizer.IsConfigured)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("NOTE: CLU is not configured. To enable all capabilities, add 'CluProjectName', 'CluDeploymentName', 'CluAPIKey' and 'CluAPIHostName' to the appsettings.json file.", inputHint: InputHints.IgnoringInput), cancellationToken);
                return await stepContext.NextAsync(null, cancellationToken);
            }

            var localizer = LocaleUtil.getLocalizer(stepContext.Context.Activity.Locale, Logger);
            // Use the text provided in FinalStepAsync or the default if it is the first time.
            var messageText = stepContext.Options?.ToString() ?? localizer.GetString("Init");
            var promptMessage = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
            return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = promptMessage }, cancellationToken);
        }

        private async Task<DialogTurnResult> ActStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            Console.Out.WriteLine(" ==>> ActStepAsync {0} {1} {2}", stepContext.Context.Activity.Locale, stepContext.Context.Activity.Text, stepContext.Context.Activity.Value);


            var localizer = LocaleUtil.getLocalizer(stepContext.Context.Activity.Locale, Logger);

            if (stepContext.Context.Activity.Value != null)
            {
                string svlaue = stepContext.Context.Activity.Value.ToString();
                ClientValue value = JsonSerializer.Deserialize<ClientValue>(svlaue);
                Console.Out.WriteLine(" ==>> ClientPayload {0}", svlaue);
                if (value.Action.Equals("init")) {
                    string messageText = localizer.GetString("HowCanIHelpYouToday");
                    Activity activity = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                    await stepContext.Context.SendActivityAsync(activity, cancellationToken);
                    return await stepContext.NextAsync(null, cancellationToken);
                }
            }

            if (!_cluRecognizer.IsConfigured)
            {
                // CLU is not configured, we just run the BookingDialog path with an empty BookingDetailsInstance.
                return await stepContext.BeginDialogAsync(nameof(BookingDialog), new BookingDetails(), cancellationToken);
            }

            // Call CLU and gather any potential booking details. (Note the TurnContext has the response to the prompt.)
            var cluResult = await _cluRecognizer.RecognizeAsync<FlightBooking>(stepContext.Context, cancellationToken);
            var intent = cluResult.GetTopIntent().intent;

            switch (intent)
            {
                case FlightBooking.Intent.OrderPizza:
                {
                    // Initialize BookingDetails with any entities we may have found in the response.
                    var details = new PizzaOrderingDetails()
                    {
                        Name = cluResult.Entities.GetPizzaName(),
                        Size = cluResult.Entities.GetPizzaSize(),
                        Extras = cluResult.Entities.GetPizzaExtra(),
                        //Destination = cluResult.Entities.GetToCity(),
                        //Origin = cluResult.Entities.GetFromCity(),
                        //TravelDate = cluResult.Entities.GetFlightDate(),
                    };
                    // Run the BookingDialog giving it whatever details we have from the CLU call, it will fill out the remainder.
                    return await stepContext.BeginDialogAsync(nameof(OrderPizzaDialog), details, cancellationToken);
                }
                //case FlightBooking.Intent.TrackPizza:
                //{
                //    // Initialize BookingDetails with any entities we may have found in the response.
                //    var details = new PizzaOrderingDetails()
                //    {
                //        Destination = cluResult.Entities.GetToCity(),
                //        Origin = cluResult.Entities.GetFromCity(),
                //        TravelDate = cluResult.Entities.GetFlightDate(),
                //        CallID = "Shit"
                //    };

                //    // Run the BookingDialog giving it whatever details we have from the CLU call, it will fill out the remainder.
                //    return await stepContext.BeginDialogAsync(nameof(BookingDialog), details, cancellationToken);
                //}
                //case FlightBooking.Intent.CancelPizza:
                //{
                //    // Initialize BookingDetails with any entities we may have found in the response.
                //    var details = new PizzaOrderingDetails()
                //    {
                //        Destination = cluResult.Entities.GetToCity(),
                //        Origin = cluResult.Entities.GetFromCity(),
                //        TravelDate = cluResult.Entities.GetFlightDate(),
                //        CallID = "Shit"
                //    };

                //    // Run the BookingDialog giving it whatever details we have from the CLU call, it will fill out the remainder.
                //    return await stepContext.BeginDialogAsync(nameof(BookingDialog), details, cancellationToken);
                //}
                case FlightBooking.Intent.BookFlight:
                    // Initialize BookingDetails with any entities we may have found in the response.
                    var bookingDetails = new BookingDetails()
                    {
                        Destination = cluResult.Entities.GetToCity(),
                        Origin = cluResult.Entities.GetFromCity(),
                        TravelDate = cluResult.Entities.GetFlightDate(),
                        CallID = "Shit"
                    };

                    // Run the BookingDialog giving it whatever details we have from the CLU call, it will fill out the remainder.
                    return await stepContext.BeginDialogAsync(nameof(BookingDialog), bookingDetails, cancellationToken);

                case FlightBooking.Intent.GetWeather:
                    // We haven't implemented the GetWeatherDialog so we just display a TODO message.
                    var getWeatherMessageText = "TODO: get weather flow here";
                    var getWeatherMessage = MessageFactory.Text(getWeatherMessageText, getWeatherMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(getWeatherMessage, cancellationToken);
                    break;

                case FlightBooking.Intent.Transfer:
                    Console.Out.WriteLine("Transferring...");
                    String transferMessageText = localizer.GetString("TransferredToQueue");
                    // Invoke ZIWO API to transfer ? Where is the CallID ???
                    // Workaround as we don't have a way to get the callID to do the transfer from the bot server.

                    // await stepContext.Context.SendActivityAsync(new Activity { Type = ActivityTypes.Command, Name = "Transfer", Value="1234" }, cancellationToken);
                    var transferMessage = MessageFactory.Text("action=transfer", transferMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(transferMessage, cancellationToken);

                    await stepContext.Context.SendActivityAsync(transferMessage, cancellationToken);


                    break;
                default:
                    // Catch all for unhandled intents
                    var didntUnderstandMessageText = $"Sorry, I didn't get that. Please try asking in a different way (intent was {cluResult.GetTopIntent().intent})";
                    var didntUnderstandMessage = MessageFactory.Text(didntUnderstandMessageText, didntUnderstandMessageText, InputHints.IgnoringInput);
                    await stepContext.Context.SendActivityAsync(didntUnderstandMessage, cancellationToken);
                    break;
            }

            return await stepContext.NextAsync(null, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            Console.Out.WriteLine(" ==>> FinalStepAsync {0}", stepContext.Context.Activity.Locale);

            // If the child dialog ("BookingDialog") was cancelled, the user failed to confirm or if the intent wasn't BookFlight
            // the Result here will be null.
            if (stepContext.Result is BookingDetails result)
            {
                // Now we have all the booking details call the booking service.

                // If the call to the booking service was successful tell the user.

                var timeProperty = new TimexProperty(result.TravelDate);
                var travelDateMsg = timeProperty.ToNaturalLanguage(DateTime.Now);
                var messageText = $"I have you booked to {result.Destination} from {result.Origin} on {travelDateMsg}";
                var message = MessageFactory.Text(messageText, messageText, InputHints.IgnoringInput);
                // await stepContext.Context.SendActivityAsync(new Activity { Type = ActivityTypes.Command, Name = "Transfer", Value = "1234" }, cancellationToken);
                await stepContext.Context.SendActivityAsync(message, cancellationToken);
            }

            // Restart the main dialog with a different message the second time around
            var promptMessage = "What else can I do for you?";
            return await stepContext.ReplaceDialogAsync(InitialDialogId, promptMessage, cancellationToken);
        }
    }
}
