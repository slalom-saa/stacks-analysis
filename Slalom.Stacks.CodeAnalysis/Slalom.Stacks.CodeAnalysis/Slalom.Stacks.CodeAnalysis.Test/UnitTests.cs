using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using Slalom.Stacks.CodeAnalysis;

namespace Slalom.Stacks.CodeAnalysis.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {
        [TestMethod]
        public void UseCasesShouldHaveRules()
        {
            var test = @"
using Slalom.Stacks.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Test
{
    public class AddCommand
    {
    }

    public class Add : UseCase<AddCommand>
    {
    }
}";
            var expected = new DiagnosticResult
            {
                Id = StacksAnalyzer.UseCaseShouldHaveRules.Id,
                Message = String.Format(StacksAnalyzer.UseCaseShouldHaveRules.MessageFormat.ToString(), "Add"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test.cs", 14, 18)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            //        var fixtest = @"
            //using System;
            //using System.Collections.Generic;
            //using System.Linq;
            //using System.Text;
            //using System.Threading.Tasks;
            //using System.Diagnostics;

            //namespace ConsoleApplication1
            //{
            //    class TYPENAME
            //    {   
            //    }
            //}";
            //        VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void Another()
        {
            var test = @"using System;
using System.Linq;
using Slalom.Stacks.Messaging;
using Slalom.Stacks.Messaging.Validation;

namespace Test2
{
    public class AddCommand
    {
    }

    public class Add : UseCase<AddCommand>
    {
    }

    public class add_rule : BusinessRule<AddCommand>
    {
    }
}";

            var expected = new DiagnosticResult
            {
                Id = StacksAnalyzer.UseCaseShouldHaveRules.Id,
                Message = String.Format(StacksAnalyzer.UseCaseShouldHaveRules.MessageFormat.ToString(), "Add"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test.cs", 12, 18)
                        }
            };

            VerifyCSharpDiagnostic(test);

            //        var fixtest = @"
            //using System;
            //using System.Collections.Generic;
            //using System.Linq;
            //using System.Text;
            //using System.Threading.Tasks;
            //using System.Diagnostics;

            //namespace ConsoleApplication1
            //{
            //    class TYPENAME
            //    {   
            //    }
            //}";
            //        VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new StacksCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new StacksAnalyzer();
        }
    }
}