using Citolab.QTI.ScoringEngine.Model;
using Citolab.QTI.ScoringEngine.Interfaces;
using System;
using System.IO;
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
using Jint;
using Jint.Native;
using Citolab.QTI.ScoringEngine.ResponseProcessing.CustomOperators;
using Citolab.QTI.ScoringEngine.Expressions.ConditionExpressions;

namespace Citolab.QTI.ScoringEngine
{
    public struct ScoreResult
    {
        public double score;
        public int partiallyCorrect;
    }

    class JsConsole
    {
        public void assert(params JsValue[] args)
        {
        }

        public void error(params JsValue[] args)
        {
            Console.WriteLine("[Error] " + args[0]);
        }

        public void warn(params JsValue[] args)
        {
            Console.WriteLine("[Warn] " + args[0]);
        }

        public void info(params JsValue[] args)
        {
            Console.WriteLine("[Info] " + args[0]);
        }

        public void log(params JsValue[] args)
        {
            Console.WriteLine(args[0]);
        }
    }

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
        private static Engine Engine;
        private static Dictionary<string, ICustomOperator> CustomOperators = new()
            {
                { "abu:MathEqual", new MathEqual() }
            };
        private List<AssessmentItem> assessmentItems;
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
            assessmentItems = ctx.AssessmentItems
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

        public static Engine GetEngine()
        {
            return Engine;
        }

        [UnmanagedCallersOnly(EntryPoint = "Score")]
        [DNNE.C99DeclCode("struct ScoreResult{double score; int partiallyCorrect;};")]
        public static unsafe int Score(char* assessmentItemStr, char* assessmentResultsStr, [DNNE.C99Type("struct ScoreResult*")] ScoreResult* result)
        {
            try {
                if (Engine == null) {
                    var userName = Environment.UserName;
                    var moduleDir = (userName == "root") ? "/usr/lib" : $"/home/{userName}/.local/lib";
                    Engine = new Engine(options =>
                        {
                            options.EnableModules(moduleDir);
                        })
                        .SetValue("console", new JsConsole());
                    Engine.Modules.Import("./compute-engine.min.js");
                    StreamReader streamReader = new($"{moduleDir}/memoize.js");
                    string memoize = streamReader.ReadToEnd();
                    streamReader.Close();
                    Engine.Execute(memoize);
                    Engine.Execute("const ce = new ComputeEngine.ComputeEngine()");
                    Engine.Execute("const isEqual_ = (a, b) => ce.parse(a).isEqual(ce.parse(b))");
                    Engine.Execute("const isEqual = memoize(isEqual_, { cacheKey: arguments_ => arguments_.join(',') })");
                }

                var assessmentItem = XDocument.Parse(Marshal.PtrToStringUTF8(new IntPtr(assessmentItemStr)));
                var assessmentResults = XDocument.Parse(Marshal.PtrToStringUTF8(new IntPtr(assessmentResultsStr)));

                var logger = new NullLogger<ScoringEngine>();
                var qtiScoringEngine = new ScoringEngine();
                var scoredAssessmentResult = qtiScoringEngine.ProcessResponses(new ScoringContext
                {
                    AssessmentItems = [assessmentItem],
                    AssessmentResults = [assessmentResults],
                    CustomOperators = CustomOperators,
                    Logger = logger,
                }).FirstOrDefault();
                double maxScore = 1;
                if (qtiScoringEngine.assessmentItems.FirstOrDefault().OutcomeDeclarations.TryGetValue("MAXSCORE", out OutcomeDeclaration maxScoreOutcome))
                {
                    if (!double.TryParse(maxScoreOutcome.DefaultValue.ToString(), out maxScore))
                    {
                        maxScore = 1;
                    }
                }
                var itemResult = scoredAssessmentResult.FindElementsByElementAndAttributeValue("itemResult", "identifier", "item").FirstOrDefault();
                var outcomeVariable = itemResult?.FindElementsByElementAndAttributeValue("outcomeVariable", "identifier", "SCORE").FirstOrDefault();
                var partiallyCorrectVariable = itemResult?.FindElementsByElementAndAttributeValue("outcomeVariable", "identifier", "PARTIALLY_CORRECT").FirstOrDefault();

                if (double.TryParse(outcomeVariable?.Value, out double score))
                {
                    result->score = Math.Max(0, score)/maxScore;
                }
                else
                {
                    result->score = 0;
                }
                if (int.TryParse(partiallyCorrectVariable?.Value, out int partiallyCorrect))
                {
                    result->partiallyCorrect = partiallyCorrect;
                }
                else
                {
                    result->partiallyCorrect = 0;
                }
                return 1;
            }
            catch (Exception e) {
                Console.WriteLine(e);
                result->score = 0;
                result->partiallyCorrect = 0;
                return 0;
            }
        }
    }
}
