﻿using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Citolab.QTI.ScoringEngine.ResponseProcessing.Interfaces;

namespace Citolab.QTI.ScoringEngine.ResponseProcessing.Operators
{
    internal class StringMatch : IResponseProcessingOperator
    {
        public string Name { get => "stringMatch"; }

        public bool Execute(XElement qtiElement, ResponseProcessorContext context)
        {
            return Helper.CompareTwoValues(qtiElement, context, Model.BaseType.String);
        }

    }
}