﻿using Citolab.QTI.ScoringEngine.Model;
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
                Parallel.For(0, ctx.AssessmentmentResults.Count,
                  index =>
                  {
                      var assessmentResultDoc = ctx.AssessmentmentResults[index];
                      var processedAssessmentResult = AssessmentResultOutcomeProcessing(assessmentResultDoc, assessmentTest, ctx.Logger);
                      concurrentAssessmentResultList.Add(processedAssessmentResult);
                  });
                ctx.AssessmentmentResults = concurrentAssessmentResultList.ToList();
            }
            else
            {
                ctx.AssessmentmentResults = ctx.AssessmentmentResults.Select(assessmentResultDoc =>
                {
                    var processedAssessmentResult = AssessmentResultOutcomeProcessing(assessmentResultDoc, assessmentTest, ctx.Logger);
                    return processedAssessmentResult;
                })
              .OfType<XDocument>()
              .ToList();
            }


            //}
            return ctx.AssessmentmentResults;
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
                Parallel.For(0, ctx.AssessmentmentResults.Count,
                  index =>
                  {
                      var assessmentResultDoc = ctx.AssessmentmentResults[index];
                      var assessmentResult = (XDocument)AssessmentResultResponseProcessing(assessmentResultDoc, assessmentItems, ctx.Logger, options);
                      concurrentAssessmentResultList.Add(assessmentResult);
                  });
                ctx.AssessmentmentResults = concurrentAssessmentResultList.ToList();
            }
            else
            {
                ctx.AssessmentmentResults = ctx.AssessmentmentResults
              .Select(assessmentResultDoc => (XDocument)AssessmentResultResponseProcessing(assessmentResultDoc, assessmentItems, ctx.Logger, options))
              .ToList();
            }

            //}
            return ctx.AssessmentmentResults;
        }

        public List<XDocument> ProcessResponsesAndOutcomes(IScoringContext ctx, ResponseProcessingScoringsOptions options = null)
        {
            ProcessResponses(ctx, options);
            ProcessOutcomes(ctx);
            return ctx.AssessmentmentResults;
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
        public static unsafe double Score(char* assessmentItem, char* assessmentResults)
        {
            var qtiScoringEngine = new ScoringEngine();
            var scoredAssessmentResult = qtiScoringEngine.ProcessResponses(new ScoringContext
            {
                AssessmentItems = [XDocument.Parse(Marshal.PtrToStringUTF8(new IntPtr(assessmentItem)))],
                AssessmentmentResults = [XDocument.Parse(Marshal.PtrToStringUTF8(new IntPtr(assessmentResults)))],
            }).FirstOrDefault();
            var itemResult = scoredAssessmentResult.FindElementsByElementAndAttributeValue("itemResult", "identifier", "item").FirstOrDefault();
            var outcomeVariable = itemResult?.FindElementsByElementAndAttributeValue("outcomeVariable", "identifier", "SCORE").FirstOrDefault();
            double score = 0;
            return Double.TryParse(outcomeVariable?.Value, out score) ? score : -1;
        }
    }
}
