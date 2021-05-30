﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Citolab.QTI.Scoring.ResponseProcessing.Interfaces;

namespace Citolab.QTI.Scoring.ResponseProcessing.Operators
{
    internal class ResponseIf : IResponseProcessingOperator
    {
        public string Name { get => "responseIf"; }

        public bool Execute(XElement qtiElement, ResponseProcessorContext context)
        {
            var firstChild = qtiElement.Elements().FirstOrDefault();
            var conditionExecutor = context.GetOperator(firstChild, context);
            var result = conditionExecutor != null && conditionExecutor.Execute(firstChild, context);
            if (result == true)
            {
                var otherChilds = qtiElement.Elements().Skip(1);
                var maxLoops = otherChilds.Count() >= 100 ? 100 : otherChilds.Count();
                foreach (var otherChild in otherChilds.Take(maxLoops))
                {
                    var childExecutor = context.GetOperator(otherChild, context);
                    childExecutor.Execute(otherChild, context);
                }
                return true;
            }
            return false;
        }
    }
}