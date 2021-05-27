﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Citolab.QTI.Scoring.ResponseProcessing.Interfaces;

namespace Citolab.QTI.Scoring.ResponseProcessing.Executors
{
    internal class ResponseElse : IExecuteReponseProcessing
    {
        public string Name { get => "responseElse"; }

        public bool Execute(XElement qtiElement, ResponseProcessorContext context)
        {
            var elements = qtiElement.Elements();
            var maxLoops = elements.Count() >= 100 ? 100 : elements.Count();
            foreach (var child in qtiElement.Elements().Take(maxLoops)) // get a max value to
            {
                var childExecutor = context.GetExecutor(child, context);
                childExecutor.Execute(child, context);
            }
            return true;
        }
    }
}
