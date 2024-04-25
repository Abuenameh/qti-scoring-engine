using Citolab.QTI.ScoringEngine.Interfaces;
using Citolab.QTI.ScoringEngine.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Citolab.QTI.ScoringEngine;
using Jint;

namespace Citolab.QTI.ScoringEngine.ResponseProcessing.CustomOperators
{
    internal class MathEqual : ICustomOperator
    {
        public BaseValue Apply(List<BaseValue> values)
        {
            var numValues = values.Count;
            if (numValues < 3) {
                return new BaseValue
                {
                    BaseType = BaseType.Boolean,
                    Value = "False"
                };
            }

            var setup = values[0].Value;
            var engine = ScoringEngine.GetEngine();
            engine.Execute("var ce = new ComputeEngine.ComputeEngine()");
            engine.Execute(setup);

            var lhs = values[1].Value.Replace("\\", "\\\\");
            var equal = values.ToList().Skip(2).Select(rhs => engine.Evaluate($"ce.parse('{lhs}').isEqual(ce.parse('{rhs.Value.Replace("\\", "\\\\")}'))")).Any(x => x.AsBoolean());

            return new BaseValue
            {
                BaseType = BaseType.Boolean,
                Value = equal.ToString()
            };
        }
    }
}
