﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Citolab.QTI.Scoring.ResponseProcessing.Interfaces;

namespace Citolab.QTI.Scoring.ResponseProcessing.Executors
{
    internal class ResponseCondition : IExecuteReponseProcessing
    {
        public string Name { get => "responseCondition"; }

        public bool Execute(XElement qtiElement, ResponseProcessorContext context)
        {
            foreach (var child in qtiElement.Elements())
            {
                var executor = context.GetExecutor(child, context);
                var result = executor?.Execute(child, context);
                if (result == true) // if handled then exit for;
                {
                    break;
                }
            }
            return true;
        }
    }
}
