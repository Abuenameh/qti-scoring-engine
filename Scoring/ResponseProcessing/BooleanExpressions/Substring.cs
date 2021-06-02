﻿using Citolab.QTI.ScoringEngine.ResponseProcessing;
using Citolab.QTI.ScoringEngine.Helper;
using Citolab.QTI.ScoringEngine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Citolab.QTI.ScoringEngine.ResponseProcessing.Interfaces;

namespace Citolab.QTI.ScoringEngine.ResponseProcessing.BooleanExpressions
{
    internal class Substring : IBooleanExpression
    {
        public string Name => "substring";

        public bool Execute(XElement qtiElement, ResponseProcessorContext context)
        {
            var caseSensitiveAttribute = qtiElement.GetAttributeValue("caseSensitive");
            if (!(!string.IsNullOrEmpty(caseSensitiveAttribute) && bool.TryParse(caseSensitiveAttribute, out var caseSensitive))) {
                caseSensitive = true;
            }
            var values = qtiElement.GetValues(context);// Helper.GetStringValueOfChildren(qtiElement, context).ToList();
            context.LogInformation($"substring check. Values: {string.Join(", ", values.Select(v => v.Value).ToArray())}");
            if (values.Count != 2)
            {
                context.LogError($"unexpected values to compare: expected: 2, retrieved: {values.Count}");
                return false;
            }
            if (values[0].BaseType != BaseType.String ||  values[1].BaseType != BaseType.String)
            {
                context.LogError($"substring only supports baseType string: {values[0]?.BaseType.GetString()} and {values[1]?.BaseType.GetString()}. Proceeding with type: {values[1].BaseType.GetString()}");
                return false;
            }
            if (string.IsNullOrEmpty(values[0].Value) || string.IsNullOrEmpty(values[1].Value))
            {
                context.LogWarning($"substring called with empty value: {values[0]?.BaseType.GetString()} and {values[1]?.BaseType.GetString()}. Proceeding with type: {values[1].BaseType.GetString()}");
                return false;
            }
            var value1 = caseSensitive ? values[0].Value : values[0].Value.ToLower();
            var value2 = caseSensitive ? values[1].Value : values[1].Value.ToLower();
            return value2.Contains(value1);
        }

    }
}