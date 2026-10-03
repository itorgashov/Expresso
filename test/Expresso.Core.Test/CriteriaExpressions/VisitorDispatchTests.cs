using System.Reflection;
using System.Runtime.Serialization;
using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;

namespace Expresso.Tests.Core.CriteriaExpressions
{
    public class VisitorDispatchTests
    {
        public static IEnumerable<object[]> ConcreteNodeTypes() =>
            typeof(AbstractExpression).Assembly.GetTypes()
                .Where(t => typeof(AbstractExpression).IsAssignableFrom(t) && !t.IsAbstract && t.IsPublic)
                .OrderBy(t => t.Name)
                .Select(t => new object[] { t });

        [Theory]
        [MemberData(nameof(ConcreteNodeTypes))]
        public void Accept_DispatchesToVisitMethodNamedAfterNode(Type nodeType)
        {
            // Nodes validate arguments in their constructors; dispatch does not read any state.
#pragma warning disable SYSLIB0050
            var node = (AbstractExpression)FormatterServices.GetUninitializedObject(nodeType);
#pragma warning restore SYSLIB0050

            var dispatched = node.Accept(new MethodNameVisitor(), null);

            var expected = "Visit" + (nodeType.Name.EndsWith("Func", StringComparison.Ordinal)
                ? nodeType.Name.Substring(0, nodeType.Name.Length - 4)
                : nodeType.Name);
            Assert.Equal(expected, dispatched);
        }

        [Fact]
        public void Visitor_HasOneMethodPerConcreteNode()
        {
            var visitMethods = typeof(IExpressoVisitor<,>).GetMethods(BindingFlags.Public | BindingFlags.Instance);
            var nodeCount = ConcreteNodeTypes().Count();

            Assert.Equal(nodeCount, visitMethods.Length);
            Assert.Equal(67, nodeCount);
        }
    }
}
