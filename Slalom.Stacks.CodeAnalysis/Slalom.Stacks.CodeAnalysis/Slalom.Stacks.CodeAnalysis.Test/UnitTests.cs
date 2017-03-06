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
        public void UseCaseShouldHaveRules()
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
        public override void Execute(AddCommand command)
        {
        }
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
        public void UseCaseShouldHaveRulesWithRules()
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
        public override void Execute(AddCommand command)
        {
        }
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

        [TestMethod]
        public void MessagesShouldBeImmutable()
        {
            var test = @"using System;
using System.Linq;
using Slalom.Stacks.Messaging;
using Slalom.Stacks.Messaging.Validation;

namespace Test
{
    public class AddCommand
    {
        public string Property { get; set; }
    }

    public class Add : UseCase<AddCommand>
    {
        public override void Execute(AddCommand command)
        {
        }
    }

    public class add_rule : BusinessRule<AddCommand>
    {
    }
}";

            var expected = new DiagnosticResult
            {
                Id = StacksAnalyzer.MessagePropertiesMustBeImmutable.Id,
                Message = String.Format(StacksAnalyzer.MessagePropertiesMustBeImmutable.MessageFormat.ToString(), "Property"),
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test.cs", 10, 23)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void EventsEndWithEvents()
        {
            var test = @"using System;
using System.Linq;
using Slalom.Stacks.Messaging;

namespace Test
{
    public class Added : Event
    {
    }
}";

            var expected = new DiagnosticResult
            {
                Id = StacksAnalyzer.EventsEndWithEvent.Id,
                Message = String.Format(StacksAnalyzer.EventsEndWithEvent.MessageFormat.ToString(), "Added"),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test.cs", 7, 18)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void MessagesCannotHaveFields()
        {
            var test = @"using System;
using System.Linq;
using Slalom.Stacks.Messaging;

namespace Test
{
    public class AddedEvent : Event
    {
        private string _field;
    }
}";

            var expected = new DiagnosticResult
            {
                Id = StacksAnalyzer.MessagesCannotHaveFields.Id,
                Message = String.Format(StacksAnalyzer.MessagesCannotHaveFields.MessageFormat.ToString(), "AddedEvent"),
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test.cs", 9, 24)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void MessagesShouldBeImmutableWithEvent()
        {
            var test = @"using System;
using System.Linq;
using Slalom.Stacks.Messaging;

namespace Test
{
    public class AddedEvent : Event
    {
        public string Name { get; set; }
    }
}";

            var expected = new DiagnosticResult
            {
                Id = StacksAnalyzer.MessagePropertiesMustBeImmutable.Id,
                Message = String.Format(StacksAnalyzer.MessagePropertiesMustBeImmutable.MessageFormat.ToString(), "Name"),
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test.cs", 9, 23)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [TestMethod]
        public void UseCasesMustHaveImplementation()
        {
            var test = @"using System;
using System.Linq;
using Slalom.Stacks.Messaging;
using Slalom.Stacks.Messaging.Validation;

namespace Test
{
    public class AddProductCommand
    {
    }

    public class AddProduct : UseCase<AddProductCommand>
    {
    }

    public class add_rule : BusinessRule<AddProductCommand>
    {
    }
}";

            var expected = new DiagnosticResult
            {
                Id = StacksAnalyzer.UseCasesMustHaveImplementation.Id,
                Message = String.Format(StacksAnalyzer.UseCasesMustHaveImplementation.MessageFormat.ToString(), "AddProduct"),
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test.cs", 12, 18)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);
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