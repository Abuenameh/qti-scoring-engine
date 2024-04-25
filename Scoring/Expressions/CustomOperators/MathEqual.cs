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
            if (values.Count == 0) {
                return new BaseValue
                {
                    BaseType = BaseType.String,
                    Value = "False"
                };
            }

            var engine = ScoringEngine.GetEngine();
            engine.Execute("var ce = new ComputeEngine.ComputeEngine()");
            if (values[0].Identifier?.Equals("setup") ?? false) {
                engine.Execute(values[0].Value);
                values.RemoveAt(0);
            }

            if (values.Count < 2) {
                return new BaseValue
                {
                    BaseType = BaseType.String,
                    Value = "False"
                };
            }

            var lhs = values[0].Value.Replace("\\", "\\\\");
            var equal = values.ToList().Skip(1).Select(rhs => engine.Evaluate($"ce.parse('{lhs}').isEqual(ce.parse('{rhs.Value.Replace("\\", "\\\\")}'))")).Any(x => x.AsBoolean());

            return new BaseValue
            {
                BaseType = BaseType.String,
                Value = equal.ToString()
            };
        }
    }
}
