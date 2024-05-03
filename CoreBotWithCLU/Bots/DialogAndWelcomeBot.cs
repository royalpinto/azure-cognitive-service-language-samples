// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using CoreBotCLU.Utils;
using LocalizationCultureCore.StringLocalizer;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotBuilderSamples.Bots
{
    public class DialogAndWelcomeBot<T> : DialogBot<T>
        where T : Dialog
    {
        private UserState _userState;
        private DialogAndWelcomeBot<T> _this;

        public DialogAndWelcomeBot(ConversationState conversationState, UserState userState, T dialog, ILogger<DialogBot<T>> logger)
            : base(conversationState, userState, dialog, logger)
        {
            _userState = userState;
            _this = this;
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            Console.Out.WriteLine(" ==>> OnMembersAddedAsync {0}", turnContext.Activity.GetLocale());

            var localizer = LocaleUtil.getLocalizer(turnContext.Activity.GetLocale(), Logger);

            foreach (var member in membersAdded)
            {
                // Greet anyone that was not the target (recipient) of this message.
                // To learn more about Adaptive Cards, see https://aka.ms/msbot-adaptivecards for more details.
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    var messageText = localizer.GetString("WelcomeMessage");
                    var response = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput);
                    //await turnContext.SendActivityAsync(response, cancellationToken);
                    await Dialog.RunAsync(turnContext, ConversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
                }
            }
        }
    }
}
