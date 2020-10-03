// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.3.0

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace WeatherQuery.Dialogs
{
    public class DateResolverDialog : CancelAndHelpDialog
    {
        public DateResolverDialog(string id = null)
            : base(id ?? nameof(DateResolverDialog))
        {
            AddDialog(new DateTimePrompt(nameof(DateTimePrompt), DateTimePromptValidator));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                InitialStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> InitialStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var timex = (string)stepContext.Options;

            var promptMsg = "您想查询的日期？";
            var repromptMsg = $"输入的日期需要包含完整的年月日，如2000-1-1";
            if (timex == null)
            {
                // We were not given any date at all so prompt the user.
                return await stepContext.PromptAsync(nameof(DateTimePrompt),
                    new PromptOptions
                    {
                        Prompt = MessageFactory.Text(promptMsg),
                        RetryPrompt = MessageFactory.Text(repromptMsg)
                    }, cancellationToken);
            }
            else
            {
                // We have a Date we just need to check it is unambiguous.
                var timexProperty = new TimexProperty(timex);
                if (!timexProperty.Types.Contains(Constants.TimexTypes.Definite))
                {
                    // This is essentially a "reprompt" of the data we were given up front.
                    return await stepContext.PromptAsync(nameof(DateTimePrompt),
                        new PromptOptions
                        {
                            Prompt = MessageFactory.Text(repromptMsg)
                        }, cancellationToken);
                }
                else
                {
                    return await stepContext.NextAsync(new DateTimeResolution { Timex = timex }, cancellationToken);
                }
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var timex = ((List<DateTimeResolution>)stepContext.Result)[0].Timex;
            return await stepContext.EndDialogAsync(timex, cancellationToken);
        }

        private static Task<bool> DateTimePromptValidator(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
            {
                var timex = promptContext.Recognized.Value[0].Timex.Split('T')[0];

                var isDefinite = new TimexProperty(timex).Types.Contains(Constants.TimexTypes.Definite);
                return Task.FromResult(isDefinite);
            }
            else
            {
                Console.WriteLine("^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^");
                Console.WriteLine(false);
                return Task.FromResult(false);
            }
        }
    }
}
