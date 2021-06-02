﻿using System;
using Citolab.QTI.ScoringEngine.Helper;
using Citolab.QTI.ScoringEngine.ResponseProcessing.Operators;
using Citolab.QTI.ScoringEngine.ResponseProcessing.Interfaces;
using Citolab.QTI.ScoringEngine.Tests;
using System.Collections.Generic;
using System.Xml.Linq;
using Xunit;

namespace ScoringEngine.Tests.ResponseProcessingTests.Operators
{
    public class GtTests
    {
        [Fact]
        public void OneGreaterThanZero()
        {
            // arrange
            var context = TestHelper.GetDefaultResponseProcessingContext(null);
            context.Operators = new Dictionary<string, IResponseProcessingOperator>();
            context.Expressions = new Dictionary<string, IResponseProcessingExpression>();
   
            var gt = new Gt();

            context.Operators.Add(gt.Name, gt);

            var gtElement = XElement.Parse("<gt></gt>");
            gtElement.Add(1.0F.ToBaseValue().ToXElement());
            gtElement.Add(0.0F.ToBaseValue().ToXElement());
            // act
            var result = gt.Execute(gtElement, context);

            //assert
            Assert.True(result);
        }
        [Fact]
        public void ZeroSmallerThanOne()
        {
            // arrange
            var context = TestHelper.GetDefaultResponseProcessingContext(null);
            context.Operators = new Dictionary<string, IResponseProcessingOperator>();
            context.Expressions = new Dictionary<string, IResponseProcessingExpression>();

            var gt = new Gt();

            context.Operators.Add(gt.Name, gt);

            var gtElement = XElement.Parse("<gt></gt>");
            gtElement.Add(0.0F.ToBaseValue().ToXElement());
            gtElement.Add(1.0F.ToBaseValue().ToXElement());
            // act
            var result = gt.Execute(gtElement, context);

            //assert
            Assert.False (result);
        }
        [Fact]
        public void EqualReturnsFalse()
        {
            // arrange
            var context = TestHelper.GetDefaultResponseProcessingContext(null);
            context.Operators = new Dictionary<string, IResponseProcessingOperator>();
            context.Expressions = new Dictionary<string, IResponseProcessingExpression>();

            var gt = new Gt();

            context.Operators.Add(gt.Name, gt);

            var gtElement = XElement.Parse("<gt></gt>");
            gtElement.Add(1.0F.ToBaseValue().ToXElement());
            gtElement.Add(1.0F.ToBaseValue().ToXElement());
            // act
            var result = gt.Execute(gtElement, context);

            //assert
            Assert.False(result);
        }
    }
}
