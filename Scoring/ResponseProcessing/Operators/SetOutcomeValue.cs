﻿using Citolab.QTI.ScoringEngine.Helper;
using Citolab.QTI.ScoringEngine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Citolab.QTI.ScoringEngine.ResponseProcessing.Interfaces;

namespace Citolab.QTI.ScoringEngine.ResponseProcessing.Operators
{
    internal class SetOutcomeValue : IResponseProcessingOperator
    {
        public string Name => "setOutcomeValue";

        public bool Execute(XElement qtiElement, ResponseProcessorContext context)
        {
            var outcomeIdentifier = qtiElement.Identifier();
            if (string.IsNullOrEmpty(outcomeIdentifier))
            {
                context.LogError($"Cannot find attribute identifier");
                return false;
            };
            if (!context.AssessmentItem.OutcomeDeclarations.ContainsKey(outcomeIdentifier))
            {
                context.LogError($"Cannot find outcomeDeclaration: {outcomeIdentifier}");
                return false;
            }
            else
            {
                var outcomeDeclaration = Helper.GetOutcomeDeclaration(outcomeIdentifier, context);
                var outcomeVariable = Helper.GetOutcomeVariable(outcomeIdentifier, outcomeDeclaration, context);
                // check if there is an operator. check value from baseValue
                var childElement = qtiElement.Elements().FirstOrDefault();
                var calculator = context.GetExpression(childElement, context);
                if (calculator != null)
                {
                    outcomeVariable.Value = calculator.Calculate(childElement, context);
                }
                else
                {
                    var values = qtiElement.GetValues(context);
                    if (values.Count > 1)
                    {
                        context.LogError($"Unexpected number of values in SetOutcomeValue: {values.Count }");
                    }
                    else
                    {
                        outcomeVariable.Value = values.Count == 0 ? "" : values.FirstOrDefault().Value;
                    }
                }
            }
            return true;
        }
    }
}
