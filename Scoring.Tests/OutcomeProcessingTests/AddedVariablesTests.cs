﻿using Microsoft.Extensions.Logging;
using Moq;
using Citolab.QTI.Scoring.Helper;
using Citolab.QTI.Scoring.Model;
using Citolab.QTI.Scoring.OutcomeProcessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;

namespace Citolab.QTI.Scoring.Tests.OutcomeProcessingTests
{
    public class AddedVariablesTests
    {

        /// <summary>
        /// Verify that SCORE_TOTAL, SCORE_TOTAL_WEIGHTED and all category scores are correct
        /// </summary>
        [Fact]
        public void OutcomeProcessing_Category_Scoring_Multiple_Categories()
        {
            var assessmentResult = TestHelper.AddVariablesAndStartOutcomeProcessing("Test_Toets", "AssessmentResult_Add_OutcomeVariable");

            Assert.Equal("2", assessmentResult.GetScoreForTest("TST-Test_toets", "SCORE_TOTAL"));
            Assert.Equal("6", assessmentResult.GetScoreForTest("TST-Test_toets", "SCORE_TOTAL_WEIGHTED"));
            Assert.Equal("2", assessmentResult.GetScoreForTest("TST-Test_toets", "SCORE_TOTAL__1F"));
            Assert.Equal("6", assessmentResult.GetScoreForTest("TST-Test_toets", "SCORE_TOTAL_WEIGHTED__1F"));
            Assert.Equal("2", assessmentResult.GetScoreForTest("TST-Test_toets", "SCORE_TOTAL_ACET"));
            Assert.Equal("6", assessmentResult.GetScoreForTest("TST-Test_toets", "SCORE_TOTAL_WEIGHTED_ACET"));
        }

        /// <summary>
        /// Verify that the SCORE_TOTAL_WEIGHTED  and SCORE_TOTAL nodes are correct when WEIGHT hasn't been set
        /// </summary>
        [Fact]
        public void OutcomeProcessing_Without_Weights()
        {
            var mockLogger = new Mock<ILogger>();
            var assessmentResult = TestHelper.AddVariablesAndStartOutcomeProcessing("Test_Toets_Without_Weights", "AssessmentResult_Add_OutcomeVariable", mockLogger);

            // missing weight identifier must be logged as a warning
            mockLogger.VerifyLog((state, t) => state.ContainsValue("Cannot find weight with identifier: WEIGHT from item: ITM-SN02945.SCORE"), LogLevel.Warning, 3);

            Assert.Equal("2", assessmentResult.GetScoreForTest("TST-Test_toets", "SCORE_TOTAL"));
            Assert.Equal("2", assessmentResult.GetScoreForTest("TST-Test_toets", "SCORE_TOTAL_WEIGHTED"));
        }


        /// <summary>
        /// Verify that the nodes SCORE_TOTAL and SCORE_TOTAL_WEIGHTED are updated in the assessmentresult
        /// </summary>
        [Fact]
        public void OutcomeProcessing_Update_OutcomeVariables()
        {
            var assessmentResult = TestHelper.AddVariablesAndStartOutcomeProcessing("Test_Toets", "AssessmentResult_Update_OutcomeVariable");

            Assert.Equal("2", assessmentResult.GetScoreForTest("TST-Test_toets", "SCORE_TOTAL"));
            Assert.Equal("6", assessmentResult.GetScoreForTest("TST-Test_toets", "SCORE_TOTAL_WEIGHTED"));
        }

        /// <summary>
        /// Verify that when a category isn't used, it will be added with a score of 0
        /// </summary>
        [Fact]
        public void OutcomeProcessing_Should_Apply_Category_Scoring_0_For_Unused_Categories()
        {
            //arrange
            var nameOfUnusedCategory = "_8072";
            var assessmentResult = TestHelper.AddVariablesAndStartOutcomeProcessing("Test_Toets_With_Unused_Category", "AssessmentResult_Add_OutcomeVariable");

            var categoryScoreValue = assessmentResult.GetScoreForTest("TST-Test_toets", $"SCORE_TOTAL_{nameOfUnusedCategory}");
            var categoryWeightedScoreValue = assessmentResult.GetScoreForTest("TST-Test_toets", $"SCORE_TOTAL_WEIGHTED_{nameOfUnusedCategory}");

            //assert
            Assert.Equal("0", categoryScoreValue);
            Assert.Equal("0", categoryWeightedScoreValue);
        }

        /// <summary>
        /// Verify that an exception is thrown when an assessmentItemRef can't be found
        /// </summary>
        [Fact]
        public void OutcomeProcessing_Invalid_ItemRef()
        {
            var mockLogger = new Mock<ILogger>();
            var assessmentResult = TestHelper.AddVariablesAndStartOutcomeProcessing("Test_Toets", "AssessmentResult_Invalid_ItemRef", mockLogger);

            mockLogger.VerifyLog((state, t) => state.ContainsValue("TST-Test_toets - 900001: Cannot find assessmentItemRef outcomeVariable: ITM-SN02945.SCORE"), LogLevel.Error, 6);
        }


        /// <summary>
        /// Verify that all assessmentItemRef's and Categories can be found, even in a nested structure
        /// Verify that all scores are correct including the categorie scores
        /// </summary>
        [Fact]
        public void OutcomeProcessing_Multiple_SubSections()
        {
            var mockLogger = new Mock<ILogger>();
            var assessmentResult = TestHelper.AddVariablesAndStartOutcomeProcessing("Test_Toets_Multiple_SubSections", "AssessmentResult_Add_OutcomeVariable", mockLogger);

            Assert.Equal("2", assessmentResult.GetScoreForTest("TST-P-Reken-MiniToets-001", "SCORE_TOTAL"));
            Assert.Equal("9", assessmentResult.GetScoreForTest("TST-P-Reken-MiniToets-001", "SCORE_TOTAL_WEIGHTED"));
            Assert.Equal("2", assessmentResult.GetScoreForTest("TST-P-Reken-MiniToets-001", "SCORE_TOTAL_ACET"));
            Assert.Equal("9", assessmentResult.GetScoreForTest("TST-P-Reken-MiniToets-001", "SCORE_TOTAL_WEIGHTED_ACET"));
            Assert.Equal("2", assessmentResult.GetScoreForTest("TST-P-Reken-MiniToets-001", "SCORE_TOTAL__8001"));
            Assert.Equal("9", assessmentResult.GetScoreForTest("TST-P-Reken-MiniToets-001", "SCORE_TOTAL_WEIGHTED__8001"));
            Assert.Equal("2", assessmentResult.GetScoreForTest("TST-P-Reken-MiniToets-001", "SCORE_TOTAL__8060"));
            Assert.Equal("9", assessmentResult.GetScoreForTest("TST-P-Reken-MiniToets-001", "SCORE_TOTAL_WEIGHTED__8060"));
            Assert.Equal("1", assessmentResult.GetScoreForTest("TST-P-Reken-MiniToets-001", "SCORE_TOTAL__8063"));
            Assert.Equal("8", assessmentResult.GetScoreForTest("TST-P-Reken-MiniToets-001", "SCORE_TOTAL_WEIGHTED__8063"));
        }

        /// <summary>
        /// Verify that the testresult scores are zero when all answers are incorrect
        /// </summary>
        [Fact]
        public void OutcomeProcessing_InCorrect_Answers()
        {
            var assessmentResult = TestHelper.AddVariablesAndStartOutcomeProcessing("Test_Toets", "AssessmentResult_InCorrect");

            Assert.Equal("0", assessmentResult.GetScoreForTest("TST-Test_toets", "SCORE_TOTAL"));
            Assert.Equal("0", assessmentResult.GetScoreForTest("TST-Test_toets", "SCORE_TOTAL_WEIGHTED"));
        }


        /// <summary>
        /// Verify that items with empty category are added to the total score of the session
        /// </summary>
        [Fact]
        public void ApplyOutcomeProcessing_Should_Include_Items_With_Empty_Categories_In_Total()

        {
            //arrange - act
            var assessmentResult = TestHelper.AddVariablesAndStartOutcomeProcessing("Test_Toets", "AssessmentResult_Add_OutcomeVariable");

            //assert
            Assert.Equal("2", assessmentResult.GetScoreForTest("TST-Test_toets", "SCORE_TOTAL"));
            Assert.Equal("6", assessmentResult.GetScoreForTest("TST-Test_toets", "SCORE_TOTAL_WEIGHTED"));
        }


    }
}
