﻿using Citolab.QTI.ScoringEngine.Interfaces;
using Citolab.QTI.ScoringEngine.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Citolab.QTI.ScoringEngine.ResponseProcessing.CustomOperators
{
    internal class Trim : ICustomOperator
    {
        public string Definition => "depcp:Trim";

        public BaseValue Apply(BaseValue value)
        {
            if (value?.Value != null)
            {
                value.Value = value.Value.Trim();
            }
            return value;
        }
    }
}
