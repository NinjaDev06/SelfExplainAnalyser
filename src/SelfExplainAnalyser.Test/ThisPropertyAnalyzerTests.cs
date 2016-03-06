using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestHelper;

namespace SelfExplainAnalyser.Test
{
    [TestClass]
    public class ThisPropertyAnalyzerTests : CodeFixVerifier
    {
        [TestMethod]
        public void PropertySetterMissingThis()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class SimpleClass
        {   
            private string Bob { get; set; }

            private void SimpleMethod()
            {
                Bob = ""bla"";
            }
        }
    }";

            this.VerifyCSharpDiagnostic(test, this.GetExpectedDiagnosticResult("Bob", 10, 17));

            var fixtest = @"
    namespace ConsoleApplication1
    {
        class SimpleClass
        {   
            private string Bob { get; set; }

            private void SimpleMethod()
            {
                this.Bob = ""bla"";
            }
        }
    }";

            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void PropertySetterMissingThis_PropertyAfterMethod()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class SimpleClass
        {   
            private void SimpleMethod()
            {
                Bob = ""bla"";
            }

            private string Bob { get; set; }
        }
    }";

            this.VerifyCSharpDiagnostic(test, this.GetExpectedDiagnosticResult("Bob", 8, 17));
        }

        [TestMethod]
        public void PropertyGetterMissingThis()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class SimpleClass
        {   
            private string Bob { get; set; }

            private void SimpleMethod()
            {
                var tmp = Bob;
            }
        }
    }";

            this.VerifyCSharpDiagnostic(test, this.GetExpectedDiagnosticResult("Bob", 10, 27));

            var fixtest = @"
    namespace ConsoleApplication1
    {
        class SimpleClass
        {   
            private string Bob { get; set; }

            private void SimpleMethod()
            {
                var tmp = this.Bob;
            }
        }
    }";

            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void PropertyGetterMissingThisOnSamePropertyManyTime()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class SimpleClass
        {   
            private string Bob { get; set; }

            private void SimpleMethod()
            {
                var tmp = Bob;

                var tmp2 = Bob;
            }

            private void SimpleMethod()
            {
                var tmp3 = Bob;
            }
        }
    }";

            this.VerifyCSharpDiagnostic(
                test, 
                this.GetExpectedDiagnosticResult("Bob", 10, 27),
                this.GetExpectedDiagnosticResult("Bob", 12, 28),
                this.GetExpectedDiagnosticResult("Bob", 17, 28));

            var fixtest = @"
    namespace ConsoleApplication1
    {
        class SimpleClass
        {   
            private string Bob { get; set; }

            private void SimpleMethod()
            {
                var tmp = this.Bob;

                var tmp2 = this.Bob;
            }

            private void SimpleMethod()
            {
                var tmp3 = this.Bob;
            }
        }
    }";

            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void PropertyGetterMissingThisOnDifferentPropertiesManyTime()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class SimpleClass
        {   
            private string Bob { get; set; }
            private string Test { get; set; }
            private string Allo { get; set; }

            private void SimpleMethod()
            {
                var tmp = Bob;

                var tmp2 = Test;
            }

            private void SimpleMethod()
            {
                var tmp3 = Allo;
            }
        }
    }";

            this.VerifyCSharpDiagnostic(
                test,
                this.GetExpectedDiagnosticResult("Bob", 12, 27),
                this.GetExpectedDiagnosticResult("Test", 14, 28),
                this.GetExpectedDiagnosticResult("Allo", 19, 28));

            var fixtest = @"
    namespace ConsoleApplication1
    {
        class SimpleClass
        {   
            private string Bob { get; set; }
            private string Test { get; set; }
            private string Allo { get; set; }

            private void SimpleMethod()
            {
                var tmp = this.Bob;

                var tmp2 = this.Test;
            }

            private void SimpleMethod()
            {
                var tmp3 = this.Allo;
            }
        }
    }";

            VerifyCSharpFix(test, fixtest);
        }

        [TestMethod]
        public void PropertyGetterMissingThis_UseInMethodParameter()
        {
            var test = @"
    namespace ConsoleApplication1
    {
        class SimpleClass
        {   
            private string Bob { get; set; }

            private void SimpleMethod()
            {
                this.OtherMethod(Bob);
            }

            private void OtherMethod(string value) { }
        }
    }";

            this.VerifyCSharpDiagnostic(test, this.GetExpectedDiagnosticResult("Bob", 10, 34));
        }
        
        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CodeFixThis();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ThisPropertyAnalyzer();
        }

        private DiagnosticResult GetExpectedDiagnosticResult(string name, int line, int column)
        {
            return new DiagnosticResult
            {
                Id = "SE0001",
                Message = String.Format("The keyword this is missing for a call on the property '{0}'", name),
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", line, column)
                        }
            };
        }
    }
}
