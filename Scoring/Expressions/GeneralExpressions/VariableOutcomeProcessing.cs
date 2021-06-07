﻿using Citolab.QTI.ScoringEngine.Helpers;
using Citolab.QTI.ScoringEngine.Interfaces;
using Citolab.QTI.ScoringEngine.Model;
using Citolab.QTI.ScoringEngine.OutcomeProcessing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Citolab.QTI.ScoringEngine.Expressions.GeneralExpressions
{
    internal class VariableOutcomeProcessing : IOutcomeProcessingExpression
    {
        public string Name => "qti-variable";

        public BaseValue Apply(XElement qtiElement, IProcessingContext ctx)
        {

            var identifier = qtiElement.Identifier();
            if (ctx.OutcomeVariables != null && ctx.OutcomeVariables.ContainsKey(identifier))
            {
                return ctx.OutcomeVariables[identifier].ToBaseValue();
            }
            else                // it is probably itemResult outcome.
            {
                // because IOutcomeProcessingExpression we can assume ctx is of type  OutcomeProcessingContext
                // this is not prefered way but makes the code cleaner since variable are 
                // the only expression that differ that much between outcome and responseProcessing
                var outcomeProcessorContext = (OutcomeProcessorContext)ctx;

                var splittedIdentifier = identifier.Split('.');
                if (splittedIdentifier.Length > 1)
                {
                    var itemIdentifier = string.Join(".", splittedIdentifier.Take(splittedIdentifier.Length - 1));
                    var outcomeIdentifier = splittedIdentifier[splittedIdentifier.Length - 1];
                    var weightIdentifier = qtiElement.GetAttributeValue("weight-identifier");
                    return outcomeProcessorContext.GetItemResultBaseValue(itemIdentifier, outcomeIdentifier, weightIdentifier);
                }
                else
                {
                    ctx.LogError($"Cannot find variable {identifier}");
                    return 0.0F.ToBaseValue();
                }
            }
        }
    }
}
