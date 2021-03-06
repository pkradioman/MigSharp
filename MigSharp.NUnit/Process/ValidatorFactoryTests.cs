﻿using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FakeItEasy;
using JetBrains.Annotations;
using MigSharp.Process;
using MigSharp.Providers;
using NUnit.Framework;

namespace MigSharp.NUnit.Process
{
    [TestFixture, Category("smoke")]
    public class ValidatorFactoryTests
    {
        [TestCaseSource("GetTestCases")]
        public void CheckProviderValidation(DbPlatform platformUnderExecution, DbAltererOptions options, int expectedTotalNumberOfSupportedProviders, int expectedValidationRuns)
        {
            // arrange
            var providerLocator = new ProviderLocator(new ProviderRegistry());
            int totalNumberOfSupportedProviders = options.SupportedPlatforms.Sum(n => providerLocator.GetAllForMinimumRequirement(n).Count());
            var validatorFactory = new ValidatorFactory(providerLocator.GetExactly(platformUnderExecution), options, providerLocator);
            Validator validator = validatorFactory.Create();

            var reporter = A.Fake<IMigrationReporter>();
            string errors;
            string warnings;

            // act
            validator.Validate(new[] { reporter }, out errors, out warnings);

            // assert
            Assert.AreEqual(expectedTotalNumberOfSupportedProviders, totalNumberOfSupportedProviders, "Wrong total number of providers.");
            A.CallTo(() => reporter.Report(A<IMigrationContext>._)).MustHaveHappened(Repeated.Exactly.Times(expectedValidationRuns));
        }

        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        [UsedImplicitly]
        private static IEnumerable GetTestCases()
        {
            yield return new TestCase
                {
                    DbPlatformUnderExecution = DbPlatform.MySql5,
                    Options = new DbAltererOptions
                        {
                            SupportedPlatforms =
                                {
                                    DbPlatform.SqlServer2012,
                                    DbPlatform.Oracle12c
                                }
                        },
                    ExpectedTotalNumberOfSupportedProviders = 2,
                    ExpectedValidationRuns = 3
                }.SetName("ProviderUnderExecutionIsValidatedAndSupportedProvidersToo");

            yield return new TestCase
                {
                    DbPlatformUnderExecution = DbPlatform.MySql5,
                    Options = new DbAltererOptions
                        {
                            SupportedPlatforms =
                                {
                                    DbPlatform.SqlServer2012,
                                    DbPlatform.Oracle12c
                                }
                        },
                    ExpectedTotalNumberOfSupportedProviders = 2,
                    ExpectedValidationRuns = 3
                }.SetName("ProviderUnderExecutionIsValidatedAndSupportedProvidersToo (with a reachable minimum requirement)");

            yield return new TestCase
                {
                    DbPlatformUnderExecution = DbPlatform.SqlServer2012,
                    Options = new DbAltererOptions
                        {
                            SupportedPlatforms =
                                {
                                    DbPlatform.SqlServer2012
                                }
                        },
                    ExpectedTotalNumberOfSupportedProviders = 1,
                    ExpectedValidationRuns = 1
                }.SetName("ProviderUnderExecutionIsNotValidatedTwice");

            yield return new TestCase
                {
                    DbPlatformUnderExecution = DbPlatform.SqlServer2012,
                    Options = new DbAltererOptions
                        {
                            Validate = false,
                            SupportedPlatforms =
                                {
                                    DbPlatform.SqlServer2012
                                }
                        },
                    ExpectedTotalNumberOfSupportedProviders = 1,
                    ExpectedValidationRuns = 1
                }.SetName("CheckValidateIsRepected");
        }

        private class TestCase : TestCaseData
        {
            public DbPlatform DbPlatformUnderExecution { set { Arguments[0] = value; } }
            public DbAltererOptions Options { set { Arguments[1] = value; } }
            public int ExpectedTotalNumberOfSupportedProviders { set { Arguments[2] = value; } }
            public int ExpectedValidationRuns { set { Arguments[3] = value; } }

            public TestCase()
                : base(null, null, null, null)
            {
            }
        }
    }
}