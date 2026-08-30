using Expresso.Core.CriteriaExpressions;
using Expresso.Core.CriteriaExpressions.Abstract;
using Expresso.Core.Filtering;
using Expresso.Core.Sorting;
using Expresso.SqlServer;

namespace Expresso.Tests.SqlServer
{
    namespace Expresso.Tests.SqlServer
    {
        public class ExpressionToSqlServerQueryClauseTransformerTests
        {
            private readonly ExpressionToSqlServerQueryClauseTransformer _transformer;

            private Dictionary<string, string> _fieldMap = new()
            {
                { "name", "name_col"},
                { "age", "age_col"},
                { "salary", "salary_col"},
            };

            private string _paramPrefix = "param";

            public ExpressionToSqlServerQueryClauseTransformerTests()
            {
                _transformer = new ExpressionToSqlServerQueryClauseTransformer();
            }

            #region GenerateWhereClause tests

            [Fact]
            public void GenerateWhereClause_AndFunc_ReturnsCorrectSql()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new AndFunc(new List<AbstractExpression>
                    {
                        new EqFunc(new Field("name", typeof(string)), new Literal("John")),
                        new GtFunc(new Field("age", typeof(int)), new Literal(25))
                    })
                };

                var result = _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix);

                Assert.Equal($"(([{_fieldMap["name"]}] = @{_paramPrefix}_0) AND ([{_fieldMap["age"]}] > @{_paramPrefix}_1))", result.whereClause);
                Assert.Equal(2, result.parameters.Count);
                Assert.Equal("John", result.parameters[$"@{_paramPrefix}_0"]);
                Assert.Equal(25, result.parameters[$"@{_paramPrefix}_1"]);
            }

            [Fact]
            public void GenerateWhereClause_OrFunc_ReturnsCorrectSql()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new OrFunc(new List<AbstractExpression>
                    {
                        new EqFunc(new Field("name", typeof(string)), new Literal("John")),
                        new GtFunc(new Field("age", typeof(int)), new Literal(25))
                    })
                };

                var result = _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix);

                Assert.Equal($"(([{_fieldMap["name"]}] = @{_paramPrefix}_0) OR ([{_fieldMap["age"]}] > @{_paramPrefix}_1))", result.whereClause);
                Assert.Equal(2, result.parameters.Count);
                Assert.Equal("John", result.parameters[$"@{_paramPrefix}_0"]);
                Assert.Equal(25, result.parameters[$"@{_paramPrefix}_1"]);
            }

            [Fact]
            public void GenerateWhereClause_NotFunc_ReturnsCorrectSql()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new NotFunc(new EqFunc(new Field("name", typeof(string)), new Literal("John")))
                };

                var result = _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix);

                Assert.Equal($"NOT (([{_fieldMap["name"]}] = @{_paramPrefix}_0))", result.whereClause);
                Assert.Single(result.parameters);
                Assert.Equal("John", result.parameters[$"@{_paramPrefix}_0"]);
            }

            [Fact]
            public void GenerateWhereClause_EqFunc_ReturnsCorrectSql()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new EqFunc(new Field("name", typeof(string)), new Literal("John"))
                };

                var result = _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix);

                Assert.Equal($"([{_fieldMap["name"]}] = @{_paramPrefix}_0)", result.whereClause);
                Assert.Single(result.parameters);
                Assert.Equal("John", result.parameters[$"@{_paramPrefix}_0"]);
            }

            [Fact]
            public void GenerateWhereClause_NeqFunc_ReturnsCorrectSql()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new NeqFunc(new Field("name", typeof(string)), new Literal("John"))
                };

                var result = _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix);

                Assert.Equal($"([{_fieldMap["name"]}] != @{_paramPrefix}_0)", result.whereClause);
                Assert.Single(result.parameters);
                Assert.Equal("John", result.parameters[$"@{_paramPrefix}_0"]);
            }

            [Fact]
            public void GenerateWhereClause_GtFunc_ReturnsCorrectSql()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new GtFunc(new Field("age", typeof(int)), new Literal(25))
                };

                var result = _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix);

                Assert.Equal($"([{_fieldMap["age"]}] > @{_paramPrefix}_0)", result.whereClause);
                Assert.Single(result.parameters);
                Assert.Equal(25, result.parameters[$"@{_paramPrefix}_0"]);
            }

            [Fact]
            public void GenerateWhereClause_GteFunc_ReturnsCorrectSql()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new GteFunc(new Field("age", typeof(int)), new Literal(25))
                };

                var result = _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix);

                Assert.Equal($"([{_fieldMap["age"]}] >= @{_paramPrefix}_0)", result.whereClause);
                Assert.Single(result.parameters);
                Assert.Equal(25, result.parameters[$"@{_paramPrefix}_0"]);
            }

            [Fact]
            public void GenerateWhereClause_LtFunc_ReturnsCorrectSql()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new LtFunc(new Field("age", typeof(int)), new Literal(30))
                };

                var result = _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix);

                Assert.Equal($"([{_fieldMap["age"]}] < @{_paramPrefix}_0)", result.whereClause);
                Assert.Single(result.parameters);
                Assert.Equal(30, result.parameters[$"@{_paramPrefix}_0"]);
            }

            [Fact]
            public void GenerateWhereClause_LteFunc_ReturnsCorrectSql()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new LteFunc(new Field("age", typeof(int)), new Literal(30))
                };

                var result = _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix);

                Assert.Equal($"([{_fieldMap["age"]}] <= @{_paramPrefix}_0)", result.whereClause);
                Assert.Single(result.parameters);
                Assert.Equal(30, result.parameters[$"@{_paramPrefix}_0"]);
            }

            [Fact]
            public void GenerateWhereClause_AddFunc_ReturnsCorrectSql()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new EqFunc(new AddFunc(new Field("age", typeof(int)), new Literal(1)), new Literal(30))
                };

                var result = _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix);

                Assert.Equal($"(([{_fieldMap["age"]}] + @{_paramPrefix}_0) = @{_paramPrefix}_1)", result.whereClause);
                Assert.Equal(result.parameters?.Count, 2);
                Assert.Equal(1, result.parameters?[$"@{_paramPrefix}_0"]);
                Assert.Equal(30, result.parameters?[$"@{_paramPrefix}_1"]);
            }

            [Fact]
            public void GenerateWhereClause_SubFunc_ReturnsCorrectSql()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new EqFunc(new SubFunc(new Field("age", typeof(int)), new Literal(1)), new Literal(30))
                };

                var result = _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix);

                Assert.Equal($"(([{_fieldMap["age"]}] - @{_paramPrefix}_0) = @{_paramPrefix}_1)", result.whereClause);
                Assert.Equal(result.parameters?.Count, 2);
                Assert.Equal(1, result.parameters?[$"@{_paramPrefix}_0"]);
                Assert.Equal(30, result.parameters?[$"@{_paramPrefix}_1"]);
            }

            [Fact]
            public void GenerateWhereClause_MultFunc_ReturnsCorrectSql()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new EqFunc(new MultFunc(new Field("age", typeof(int)), new Literal(1)), new Literal(30))
                };

                var result = _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix);

                Assert.Equal($"(([{_fieldMap["age"]}] * @{_paramPrefix}_0) = @{_paramPrefix}_1)", result.whereClause);
                Assert.Equal(result.parameters?.Count, 2);
                Assert.Equal(1, result.parameters?[$"@{_paramPrefix}_0"]);
                Assert.Equal(30, result.parameters?[$"@{_paramPrefix}_1"]);
            }

            [Fact]
            public void GenerateWhereClause_DivFunc_ReturnsCorrectSql()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new EqFunc(new DivFunc(new Field("age", typeof(int)), new Literal(1)), new Literal(30))
                };

                var result = _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix);

                Assert.Equal($"(([{_fieldMap["age"]}] / @{_paramPrefix}_0) = @{_paramPrefix}_1)", result.whereClause);
                Assert.Equal(result.parameters?.Count, 2);
                Assert.Equal(1, result.parameters?[$"@{_paramPrefix}_0"]);
                Assert.Equal(30, result.parameters?[$"@{_paramPrefix}_1"]);
            }

            [Fact]
            public void GenerateWhereClause_AbsFunc_ReturnsCorrectSql()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new EqFunc(new AbsFunc(new Field("age", typeof(int))), new Literal(30))
                };

                var result = _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix);

                Assert.Equal($"(ABS([{_fieldMap["age"]}]) = @{_paramPrefix}_0)", result.whereClause);
                Assert.Single(result.parameters);
                Assert.Equal(30, result.parameters?[$"@{_paramPrefix}_0"]);

            }

            [Fact]
            public void GenerateWhereClause_InFunc_ReturnsCorrectSql()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new InFunc(new List<AbstractExpression>
                    {
                        new Field("name", typeof(string)),
                        new Literal("John"),
                        new Literal("Jane")
                    })
                };

                var result = _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix);

                Assert.Equal($"([{_fieldMap["name"]}] IN (@{_paramPrefix}_0, @{_paramPrefix}_1))", result.whereClause);
                Assert.Equal(2, result.parameters.Count);
                Assert.Equal("John", result.parameters[$"@{_paramPrefix}_0"]);
                Assert.Equal("Jane", result.parameters[$"@{_paramPrefix}_1"]);
            }

            [Fact]
            public void GenerateWhereClause_StrStartswithFunc_ReturnsCorrectSql()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new StrStartswithFunc(new Field("name", typeof(string)), new Literal("Jo"))
                };

                var result = _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix);

                Assert.Equal($"([{_fieldMap["name"]}] LIKE @{_paramPrefix}_0 ESCAPE '\\')", result.whereClause);
                Assert.Single(result.parameters);
                Assert.Equal("Jo%", result.parameters[$"@{_paramPrefix}_0"]);
            }

            [Fact]
            public void GenerateWhereClause_SubStringFunc_ReturnsCorrectSql()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new EqFunc(new SubStringFunc(new Field("name", typeof(string)), new Literal(1), new Literal(3)), new Literal("John"))
                };

                var result = _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix);

                Assert.Equal($"(SUBSTRING([{_fieldMap["name"]}], @{_paramPrefix}_0, @{_paramPrefix}_1) = @{_paramPrefix}_2)", result.whereClause);
                Assert.Equal(3, result.parameters.Count);
                Assert.Equal(1, result.parameters[$"@{_paramPrefix}_0"]);
                Assert.Equal(3, result.parameters[$"@{_paramPrefix}_1"]);
                Assert.Equal("John", result.parameters[$"@{_paramPrefix}_2"]);
            }

            [Fact]
            public void GenerateWhereClause_ComplexExpression_ReturnsCorrectSql()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new AndFunc(new List<AbstractExpression>
                    {
                        new OrFunc(new List<AbstractExpression>
                        {
                            new StrStartswithFunc(new Field("name", typeof(string)), new Literal("Jo")),
                            new InFunc(new List<AbstractExpression>
                            {
                                new Field("age", typeof(int)),
                                new Literal(25),
                                new Literal(30)
                            })
                        }),
                        new GtFunc(new Field("salary", typeof(double)), new Literal(50000.0))
                    })
                };

                var result = _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix);

                Assert.Equal($"((([{_fieldMap["name"]}] LIKE @{_paramPrefix}_0 ESCAPE '\\') OR ([{_fieldMap["age"]}] IN (@{_paramPrefix}_1, @{_paramPrefix}_2))) AND ([{_fieldMap["salary"]}] > @{_paramPrefix}_3))", result.whereClause);
                Assert.Equal(4, result.parameters.Count);
                Assert.Equal("Jo%", result.parameters[$"@{_paramPrefix}_0"]);
                Assert.Equal(25, result.parameters[$"@{_paramPrefix}_1"]);
                Assert.Equal(30, result.parameters[$"@{_paramPrefix}_2"]);
                Assert.Equal(50000.0, result.parameters[$"@{_paramPrefix}_3"]);
            }

            [Fact]
            public void GenerateWhereClause_NullFilterCriteria_ThrowsArgumentNullException()
            {
                var exception = Assert.Throws<ArgumentNullException>(() => _transformer.RenderWhereClause(null!, _fieldMap, _paramPrefix));
                Assert.Equal("filterCriteria", exception.ParamName);
            }

            [Fact]
            public void GenerateWhereClause_NullExpression_ThrowsArgumentNullException()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = null!
                };

                var exception = Assert.Throws<ArgumentException>(() => _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix));
                Assert.Equal("filterCriteria", exception.ParamName);
            }

            [Fact]
            public void GenerateWhereClause_NullMapping_ThrowsArgumentNullException()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new DummyFunction()
                };

                var exception = Assert.Throws<ArgumentNullException>(() => _transformer.RenderWhereClause(filterCriteria, (Dictionary<string, string>)null!, _paramPrefix));
                Assert.Equal("fieldToColumnMap", exception.ParamName);
            }


            [Fact]
            public void GenerateWhereClause_NullOrIncorrectParamPrefix_ThrowsArgumentNullException()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new DummyFunction()
                };

                var exception1 = Assert.Throws<ArgumentNullException>(() => _transformer.RenderWhereClause(filterCriteria, _fieldMap, null!));
                Assert.Equal("paramNamePrefix", exception1.ParamName);
                var exception2 = Assert.Throws<ArgumentException>(() => _transformer.RenderWhereClause(filterCriteria, _fieldMap, string.Empty));
                Assert.Equal("paramNamePrefix", exception2.ParamName);
                var exception3 = Assert.Throws<ArgumentException>(() => _transformer.RenderWhereClause(filterCriteria, _fieldMap, "2a"));
                Assert.Equal("paramNamePrefix", exception3.ParamName);
            }

            [Fact]
            public void GenerateWhereClause_UnsupportedExpression_ThrowsNotSupportedException()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new DummyFunction()
                };

                var exception = Assert.Throws<NotSupportedException>(() => _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix));
            }

            [Fact]
            public void GenerateWhereClause_IsNullFunc_ReturnsCorrectSql()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new IsNullFunc(new Field("name", typeof(string)))
                };

                var result = _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix);

                Assert.Equal($"([{_fieldMap["name"]}] IS NULL)", result.whereClause);
                Assert.Empty(result.parameters);
            }

            [Fact]
            public void GenerateWhereClause_IsNullWithNot_ReturnsCorrectSql()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new NotFunc(new IsNullFunc(new Field("name", typeof(string))))
                };

                var result = _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix);

                Assert.Equal($"NOT (([{_fieldMap["name"]}] IS NULL))", result.whereClause);
                Assert.Empty(result.parameters);
            }

            [Fact]
            public void GenerateWhereClause_IsNullInComplexExpression_ReturnsCorrectSql()
            {
                FilterCriteria filterCriteria = new()
                {
                    Expression = new AndFunc(new List<AbstractExpression>
                    {
                        new IsNullFunc(new Field("name", typeof(string))),
                        new GtFunc(new Field("age", typeof(int)), new Literal(25))
                    })
                };

                var result = _transformer.RenderWhereClause(filterCriteria, _fieldMap, _paramPrefix);

                Assert.Equal($"(([{_fieldMap["name"]}] IS NULL) AND ([{_fieldMap["age"]}] > @{_paramPrefix}_0))", result.whereClause);
                Assert.Single(result.parameters);
                Assert.Equal(25, result.parameters[$"@{_paramPrefix}_0"]);
            }

            #endregion

            #region GenerateOrderBy tests

            [Fact]
            public void GenerateOrderBy_SingleFieldAscending_ReturnsCorrectOrderByClause()
            {
                ExpressionToSqlServerQueryClauseTransformer transformer = new();
                SortDirective sortDirective =
                new(
                    new List<SortDirectiveItem>
                    {
                        new SortDirectiveItem
                        {
                            Expression = new Field("name", typeof(string)),
                            Direction = SortDirection.Ascending
                        }
                    }
                );

                var result = transformer.RenderOrderByClause(sortDirective, _fieldMap, _paramPrefix);

                Assert.Equal($"[{_fieldMap["name"]}] ASC", result.orderByClause);
                Assert.Empty(result.parameters);
            }

            [Fact]
            public void GenerateOrderBy_SingleFieldDescending_ReturnsCorrectOrderByClause()
            {
                ExpressionToSqlServerQueryClauseTransformer transformer = new();
                SortDirective sortDirective =
                new(
                    new List<SortDirectiveItem>
                    {
                        new SortDirectiveItem
                        {
                            Expression = new Field("name", typeof(string)),
                            Direction = SortDirection.Descending
                        }
                    }
                );

                var result = transformer.RenderOrderByClause(sortDirective, _fieldMap, _paramPrefix);

                Assert.Equal($"[{_fieldMap["name"]}] DESC", result.orderByClause);
                Assert.Empty(result.parameters);
            }

            [Fact]
            public void GenerateOrderBy_MultipleFields_ReturnsCorrectOrderByClause()
            {
                ExpressionToSqlServerQueryClauseTransformer transformer = new();
                SortDirective sortDirective =
                new(
                    new List<SortDirectiveItem>
                    {
                        new SortDirectiveItem { Expression = new Field("name", typeof(string)), Direction = SortDirection.Ascending },
                        new SortDirectiveItem { Expression = new Field("age", typeof(int)), Direction = SortDirection.Descending }
                    }
                );

                var result = transformer.RenderOrderByClause(sortDirective, _fieldMap, _paramPrefix);

                Assert.Equal($"[{_fieldMap["name"]}] ASC, [{_fieldMap["age"]}] DESC", result.orderByClause);
                Assert.Empty(result.parameters);
            }

            [Fact]
            public void GenerateOrderBy_BooleanFunction_ReturnsCorrectOrderByClause()
            {
                ExpressionToSqlServerQueryClauseTransformer transformer = new();
                BooleanFunction boolFn = new EqFunc(new Field("age", typeof(int)), new Literal(1));
                SortDirective sortDirective =
                new(
                    new List<SortDirectiveItem>
                    {
                        new SortDirectiveItem { Expression = boolFn, Direction = SortDirection.Ascending }
                    }
                );

                var result = transformer.RenderOrderByClause(sortDirective, _fieldMap, _paramPrefix);

                Assert.Equal($"(CASE WHEN ([{_fieldMap["age"]}] = @{_paramPrefix}_0) THEN 1 ELSE 0 END) ASC", result.orderByClause);
                Assert.Single(result.parameters);
                Assert.Equal(1, result.parameters[$"@{_paramPrefix}_0"]);
            }

            [Fact]
            public void GenerateOrderBy_NullSortDirective_ThrowsArgumentNullException()
            {
                ExpressionToSqlServerQueryClauseTransformer transformer = new();

                var exception = Assert.Throws<ArgumentNullException>(() => transformer.RenderOrderByClause(null!, _fieldMap, _paramPrefix));
                Assert.Equal("sortDirective", exception.ParamName);
            }

            [Fact]
            public void GenerateOrderBy_NullFieldToColumnMap_ThrowsArgumentNullException()
            {
                ExpressionToSqlServerQueryClauseTransformer transformer = new();
                SortDirective sortDirective =
                new(
                    new List<SortDirectiveItem>
                    {
                        new SortDirectiveItem
                        {
                            Expression = new DummyFunction(),
                            Direction = SortDirection.Ascending
                        }
                    }
                );

                var exception = Assert.Throws<ArgumentNullException>(() => transformer.RenderOrderByClause(sortDirective, (Dictionary<string, string>)null!, _paramPrefix));
                Assert.Equal("fieldToColumnMap", exception.ParamName);
            }

            [Fact]
            public void GenerateOrderBy_NullOrIncorrectParamPrefix_ThrowsArgumentNullException()
            {
                ExpressionToSqlServerQueryClauseTransformer transformer = new();
                SortDirective sortDirective =
                new(
                    new List<SortDirectiveItem>
                    {
                        new SortDirectiveItem
                        {
                            Expression = new DummyFunction(),
                            Direction = SortDirection.Ascending
                        }
                    }
                );

                var exception1 = Assert.Throws<ArgumentNullException>(() => transformer.RenderOrderByClause(sortDirective, _fieldMap, null!));
                Assert.Equal("paramNamePrefix", exception1.ParamName);
                var exception2 = Assert.Throws<ArgumentException>(() => transformer.RenderOrderByClause(sortDirective, _fieldMap, string.Empty));
                Assert.Equal("paramNamePrefix", exception2.ParamName);
                var exception3 = Assert.Throws<ArgumentException>(() => transformer.RenderOrderByClause(sortDirective, _fieldMap, "2a"));
                Assert.Equal("paramNamePrefix", exception3.ParamName);
            }


            [Fact]
            public void GenerateOrderBy_EmptySortDirectiveItems_ThrowsArgumentException()
            {
                ExpressionToSqlServerQueryClauseTransformer transformer = new();
                SortDirective sortDirective = new(new List<SortDirectiveItem>());

                var exception = Assert.Throws<ArgumentException>(() => transformer.RenderOrderByClause(sortDirective, _fieldMap, _paramPrefix));
                Assert.Equal("sortDirective", exception.ParamName);
            }

            #endregion

            private class DummyFunction : BooleanFunction
            {
                public DummyFunction() { }
            }
        }
    }

}