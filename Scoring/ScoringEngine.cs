using Citolab.QTI.ScoringEngine.Model;
using Citolab.QTI.ScoringEngine.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Citolab.QTI.ScoringEngine.Helpers;
using System.Threading.Tasks;
using Citolab.QTI.ScoringEngine.OutcomeProcessing;
using Microsoft.Extensions.Logging;
using Citolab.QTI.ScoringEngine.ResponseProcessing;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace Citolab.QTI.ScoringEngine
{
    public class ConsoleLogger<T> : ILogger<T>
    {
        public static readonly ConsoleLogger<T> Instance = new ConsoleLogger<T>();

        /// <inheritdoc />
        public IDisposable BeginScope<TState>(TState state)
        {
            return ConsoleDisposable.Instance;
        }

        /// <inheritdoc />
        /// <remarks>
        /// This method ignores the parameters and does nothing.
        /// </remarks>
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            Console.WriteLine(formatter(state,exception));
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        private class ConsoleDisposable : IDisposable
        {
            public static readonly ConsoleDisposable Instance = new ConsoleDisposable();

            public void Dispose()
            {
                // intentionally does nothing
            }
        }
    }


    public class ScoringEngine : IScoringEngine
    {
        private IExpressionFactory _expressionFactory;
        public List<XDocument> ProcessOutcomes(IOutcomeProcessingContext ctx)
        {
            if (ctx == null)
            {
                throw new ScoringEngineException("context cannot be null");
            }
            if (ctx.AssessmentTest == null)
            {
                throw new ScoringEngineException("AssessmentTest cannot be null when calling outcomeProcessing");
            }
            if (ctx.Logger == null)
            {
                ctx.Logger = ctx.Logger = new NullLogger<ScoringEngine>();
            }
            if (_expressionFactory == null)
            {
                _expressionFactory = new ExpressionFactory(ctx.CustomOperators, ctx.Logger);
            }
            var assessmentTest = new AssessmentTest(ctx.Logger, ctx.AssessmentTest, _expressionFactory);

            if (ctx.ProcessParallel == true)
            {
                var concurrentAssessmentResultList = new ConcurrentBag<XDocument>();
                Parallel.For(0, ctx.AssessmentResults.Count,
                  index =>
                  {
                      var assessmentResultDoc = ctx.AssessmentResults[index];
                      var processedAssessmentResult = AssessmentResultOutcomeProcessing(assessmentResultDoc, assessmentTest, ctx.Logger);
                      concurrentAssessmentResultList.Add(processedAssessmentResult);
                  });
                ctx.AssessmentResults = concurrentAssessmentResultList.ToList();
            }
            else
            {
                ctx.AssessmentResults = ctx.AssessmentResults.Select(assessmentResultDoc =>
                {
                    var processedAssessmentResult = AssessmentResultOutcomeProcessing(assessmentResultDoc, assessmentTest, ctx.Logger);
                    return processedAssessmentResult;
                })
              .OfType<XDocument>()
              .ToList();
            }


            //}
            return ctx.AssessmentResults;
        }

        public List<XDocument> ProcessResponses(IResponseProcessingContext ctx, ResponseProcessingScoringsOptions options = null)
        {
            if (ctx == null)
            {
                throw new ScoringEngineException("context cannot be null");
            }
            if (ctx.AssessmentItems == null)
            {
                throw new ScoringEngineException("AssessmentItems cannot be null when calling responseProcessing");
            }
            if (ctx.Logger == null)
            {
                ctx.Logger = ctx.Logger = new NullLogger<ScoringEngine>();
            }
            if (_expressionFactory == null)
            {
                _expressionFactory = new ExpressionFactory(ctx.CustomOperators, ctx.Logger);
            }
            var assessmentItems = ctx.AssessmentItems
                .Select(assessmentItemDoc => new AssessmentItem(ctx.Logger, assessmentItemDoc, _expressionFactory))
                .ToList();
            if (ctx.ProcessParallel == true)
            {
                var concurrentAssessmentResultList = new ConcurrentBag<XDocument>();
                Parallel.For(0, ctx.AssessmentResults.Count,
                  index =>
                  {
                      var assessmentResultDoc = ctx.AssessmentResults[index];
                      var assessmentResult = (XDocument)AssessmentResultResponseProcessing(assessmentResultDoc, assessmentItems, ctx.Logger, options);
                      concurrentAssessmentResultList.Add(assessmentResult);
                  });
                ctx.AssessmentResults = concurrentAssessmentResultList.ToList();
            }
            else
            {
                ctx.AssessmentResults = ctx.AssessmentResults
              .Select(assessmentResultDoc => (XDocument)AssessmentResultResponseProcessing(assessmentResultDoc, assessmentItems, ctx.Logger, options))
              .ToList();
            }

            //}
            return ctx.AssessmentResults;
        }

        public List<XDocument> ProcessResponsesAndOutcomes(IScoringContext ctx, ResponseProcessingScoringsOptions options = null)
        {
            ProcessResponses(ctx, options);
            ProcessOutcomes(ctx);
            return ctx.AssessmentResults;
        }


        private AssessmentResult AssessmentResultOutcomeProcessing(XDocument assessmentResultDocument, AssessmentTest assessmentTest, ILogger logger)
        {
            var assessmentResult = new AssessmentResult(logger, assessmentResultDocument);
            assessmentResult = OutcomeProcessor.Process(assessmentTest, assessmentResult, logger);
            return assessmentResult;
        }

        private AssessmentResult AssessmentResultResponseProcessing(XDocument assessmentResultDocument, List<AssessmentItem> assessmentItems, ILogger logger, ResponseProcessingScoringsOptions options = null)
        {
            var assessmentResult = new AssessmentResult(logger, assessmentResultDocument);
            foreach (var assessmentItem in assessmentItems)
            {
                assessmentResult = ResponseProcessor.Process(assessmentItem, assessmentResult, logger, options);
            }
            return assessmentResult;
        }

        [UnmanagedCallersOnly]
        public static unsafe double Score(char* assessmentItemStr, char* assessmentResultsStr)
        {
            try {
                var assessmentItem = XDocument.Parse(Marshal.PtrToStringUTF8(new IntPtr(assessmentItemStr)));
                var assessmentResults = XDocument.Parse(Marshal.PtrToStringUTF8(new IntPtr(assessmentResultsStr)));

                var logger = new NullLogger<ScoringEngine>();
                var expressionFactory = new ExpressionFactory(null, logger);
                double maxScore = 1;
                if (new AssessmentItem(logger, assessmentItem, expressionFactory).OutcomeDeclarations.TryGetValue("MAXSCORE", out OutcomeDeclaration maxScoreOutcome)) {
                  if (!double.TryParse(maxScoreOutcome.DefaultValue.ToString(), out maxScore)) {
                    maxScore = 1;
                  }
                }

                var qtiScoringEngine = new ScoringEngine();
                var scoredAssessmentResult = qtiScoringEngine.ProcessResponses(new ScoringContext
                {
                    AssessmentItems = [assessmentItem],
                    AssessmentResults = [assessmentResults],
                }).FirstOrDefault();
                var itemResult = scoredAssessmentResult.FindElementsByElementAndAttributeValue("itemResult", "identifier", "item").FirstOrDefault();
                var outcomeVariable = itemResult?.FindElementsByElementAndAttributeValue("outcomeVariable", "identifier", "SCORE").FirstOrDefault();

                return double.TryParse(outcomeVariable?.Value, out double score) ? Math.Max(0, score)/maxScore : 0;
            }
            catch (Exception e) {
                Console.WriteLine(e);
                return 0;
            }
        }
    }
}
