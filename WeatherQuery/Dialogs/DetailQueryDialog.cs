// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.3.0

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace WeatherQuery.Dialogs
{
    public class DetailQueryDialog : CancelAndHelpDialog
    {
        public DetailQueryDialog()
            : base(nameof(DetailQueryDialog))
        {
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new DateResolverDialog());
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[]
            {
                LocationStepAsync,
                DateStepAsync,
                FinalStepAsync,
            }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> LocationStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var QueryDetails = (QueryDetails)stepContext.Options;
            if (QueryDetails.Location == null)
            {
                Console.WriteLine("detail location");
                return await stepContext.PromptAsync(nameof(TextPrompt), new PromptOptions { Prompt = MessageFactory.Text("您想查询哪里的天气？") }, cancellationToken);
            }
            else if (QueryDetails.Location.CompareTo("null")==0 && QueryDetails.Date.CompareTo("null")==0)
            {
                return await stepContext.NextAsync("return", cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(QueryDetails.Location, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> DateStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var QueryDetails = (QueryDetails)stepContext.Options;
            var Location = (string)stepContext.Result;
            QueryDetails.Location = Location;
            var Date = QueryDetails.Date;
            //Console.WriteLine("========================" + QueryDetails.Date);
            if (Location == "return")
            {
                return await stepContext.NextAsync("return", cancellationToken);
            }
            else if (Date == "PRESENT_REF")
            {
                return await stepContext.NextAsync(QueryDetails.Date, cancellationToken);
            }
            else if (Date == null ||IsAmbiguous(Date))
            {
                Console.WriteLine("detail date" + Date + "****");
                return await stepContext.BeginDialogAsync(nameof(DateResolverDialog), Date, cancellationToken);
            }
            else
            {
                return await stepContext.NextAsync(QueryDetails.Date, cancellationToken);
            }
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var Date = (string)stepContext.Result;
            var QueryDetails = (QueryDetails)stepContext.Options;
            QueryDetails.Date = Date;
            if (Date.ToString() != "return")
            {
                return await stepContext.EndDialogAsync(QueryDetails, cancellationToken);
            }
            else
            {
                return await stepContext.EndDialogAsync(null, cancellationToken);
            }
        }

        private static bool IsAmbiguous(string timex)
        {
            var timexProperty = new TimexProperty(timex);
            return !timexProperty.Types.Contains(Constants.TimexTypes.Definite);
        }
    }
}
