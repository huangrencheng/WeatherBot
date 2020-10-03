// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
//
// Generated with Bot Builder V4 SDK Template for Visual Studio CoreBot v4.3.0

using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace WeatherQuery
{
    public static class LuisHelper
    {
        public static async Task<QueryDetails> ExecuteLuisQuery(IConfiguration configuration, ILogger logger, ITurnContext turnContext, CancellationToken cancellationToken)
        {
            var QueryDetails = new QueryDetails();

            try
            {
                // Create the LUIS settings from configuration.
                var luisApplication = new LuisApplication(
                    configuration["LuisAppId"],
                    configuration["LuisAPIKey"],
                    $"https://{configuration["LuisAPIHostName"]}.api.cognitive.microsoft.com"
                );
                
                var recognizer = new LuisRecognizer(luisApplication, new LuisPredictionOptions { IncludeAllIntents = true, IncludeInstanceData = true}, includeApiResults:true);
                

                // The actual call to LUIS
                var recognizerResult = await recognizer.RecognizeAsync(turnContext, cancellationToken);

                var (intent, score) = recognizerResult.GetTopScoringIntent();

                Console.WriteLine("LUIS result **************************************************");
                Console.WriteLine(recognizerResult.Entities);
                Console.WriteLine(intent);
                Console.WriteLine(score);

                if (intent == "WeatherQuery" || intent == "NowQuery" || intent == "AirQuery" || intent == "AirNow" || intent == "RainQuery")
                {
                    Console.WriteLine("into");
                    QueryDetails.key = intent;
                    if (recognizerResult.Entities.ContainsKey("location"))
                    {
                        Console.WriteLine("location");
                        QueryDetails.Location = (string)recognizerResult.Entities["location"][0];
                    }
                    if (recognizerResult.Entities.ContainsKey("datetime"))
                    {
                        Console.WriteLine("datetime");
                        QueryDetails.Date = recognizerResult.Entities["datetime"][0]["timex"][0].ToString();
                        if (recognizerResult.Entities["datetime"][0]["type"].ToString() == "daterange")
                        {
                            QueryDetails.DateType = "daterange";

                            var results = DateTimeRecognizer.RecognizeDateTime(recognizerResult.Entities["datetime"][0]["timex"][0].ToString(), Culture.English);
                            int i = 0;
                            foreach (var result in results)
                            {
                                var values = (List<Dictionary<string, string>>)result.Resolution["values"];
                                foreach (var value in values)
                                {
                                    if (value.TryGetValue("timex", out var timex))
                                        if (i++ == 0)
                                            QueryDetails.StartDate = timex.ToString();
                                        else
                                            QueryDetails.EndDate = timex.ToString();
                                }
                            }
                            Console.WriteLine(QueryDetails.StartDate + " " + QueryDetails.EndDate);
                        }
                        else if (recognizerResult.Entities["datetime"][0]["type"].ToString() == "date")
                        {
                            QueryDetails.DateType = "date";
                        }
                    }
                }
                else
                {
                    QueryDetails.Location = "null";
                    QueryDetails.Date = "null";
                    Console.WriteLine("====================== null");
                }
            }
            catch (Exception e)
            {
                logger.LogWarning($"LUIS Exception: {e.Message} Check your LUIS configuration.");
            }

            return QueryDetails;
        }
    }
}
