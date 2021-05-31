﻿using Citolab.QTI.ScoringEngine.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Citolab.QTI.ScoringEngine.ResponseProcessing.Interfaces
{
    internal interface IResponseProcessingExpression
    {
        string Name { get; }

        // OperationResult Calculate(XElement qtiElement, OutcomeDeclaration outcomeDeclaration, ProcessingContext context);
        float Calculate(XElement qtiElement, ResponseProcessorContext context);
    }
}
