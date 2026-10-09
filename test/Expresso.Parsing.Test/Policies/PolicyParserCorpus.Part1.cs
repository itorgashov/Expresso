namespace Expresso.Parsing.Test.Policies;

// Literal query corpus captured from the existing parser tests. Generated from
// their C# string tokens; dynamic function families are covered by the catalog sweep.
internal static partial class PolicyParserCorpus
{
    internal static IEnumerable<object[]> Part1()
    {
        yield return new object[] { @"eq(mod(age, 2), 0)", false, @"" };
        yield return new object[] { @"eq(floor(doubleField), 1.5)", false, @"" };
        yield return new object[] { @"eq(ceiling(doubleField), 2.0)", false, @"" };
        yield return new object[] { @"eq(ceil(doubleField), 2.0)", false, @"" };
        yield return new object[] { @"eq(round(doubleField), 2.0)", false, @"" };
        yield return new object[] { @"eq(round(doubleField, -1), 20.0)", false, @"" };
        yield return new object[] { @"eq(sign(age), 1)", false, @"" };
        yield return new object[] { @"eq(power(age, 2), 25)", false, @"" };
        yield return new object[] { @"eq(pow(age, 2), 25)", false, @"" };
        yield return new object[] { @"eq(sqrt(age), 5)", false, @"" };
        yield return new object[] { @"eq(min(age, 18), 18)", false, @"" };
        yield return new object[] { @"eq(max(age, 65), 65)", false, @"" };
        yield return new object[] { @"eq(round(age, -1), 30)", false, @"" };
        yield return new object[] { @"eq(mod(name, 2), 0)", false, @"" };
        yield return new object[] { @"eq(round(age, 1.5), 30)", false, @"" };
        yield return new object[] { @"eq(mod(age), 0)", false, @"" };
        yield return new object[] { @"Mod() function should have 2 arguments", false, @"" };
        yield return new object[] { @"eq(round(age, 1, 2), 30)", false, @"" };
        yield return new object[] { @"Round() function should have 1 or 2 arguments", false, @"" };
        yield return new object[] { @"eq(dateFrom, ""1899-12-31"")", false, @"dateFrom:DateTime;opens:TimeSpan;starts:TimeOnly" };
        yield return new object[] { @"eq(dateFrom, ""31-12-1899"")", false, @"dateFrom:DateTime;opens:TimeSpan;starts:TimeOnly" };
        yield return new object[] { @"eq(opens, ""9:00"")", false, @"dateFrom:DateTime;opens:TimeSpan;starts:TimeOnly" };
        yield return new object[] { @"eq(starts, ""9:00"")", false, @"dateFrom:DateTime;opens:TimeSpan;starts:TimeOnly" };
        yield return new object[] { @"name,asc", true, @"age:int;createdat:DateTime;name:string;salary:double" };
        yield return new object[] { @"name,asc,age,desc", true, @"age:int;createdat:DateTime;name:string;salary:double" };
        yield return new object[] { @"substring(name, 1, 2),asc", true, @"age:int;createdat:DateTime;name:string;salary:double" };
        yield return new object[] { @"invalidField,asc", true, @"age:int;createdat:DateTime;name:string;salary:double" };
        yield return new object[] { @"substring(name, 1, 2),asc,age,desc", true, @"age:int;createdat:DateTime;name:string;salary:double" };
        yield return new object[] { @"lower(name),asc", true, @"age:int;createdat:DateTime;name:string;salary:double" };
        yield return new object[] { @"year(createdat),asc", true, @"age:int;createdat:DateTime;name:string;salary:double" };
        yield return new object[] { @"floor(age),asc", true, @"age:int;createdat:DateTime;name:string;salary:double" };
        yield return new object[] { @"mod(age,2),desc", true, @"age:int;createdat:DateTime;name:string;salary:double" };
        yield return new object[] { @"sortfor(authors, lastname),asc", true, @"dateofbirth:DateTime;displayname:string;firstname:string;lastname:string;price:double;rating:double;title:string;year:int" };
        yield return new object[] { @"year,desc,sortfor(authors, lastname),asc", true, @"dateofbirth:DateTime;displayname:string;firstname:string;lastname:string;price:double;rating:double;title:string;year:int" };
        yield return new object[] { @"sortfor(authors/awards, title),desc", true, @"dateofbirth:DateTime;displayname:string;firstname:string;lastname:string;price:double;rating:double;title:string;year:int" };
        yield return new object[] { @"sortfor(authors, gt(len(lastname),10)),desc", true, @"dateofbirth:DateTime;displayname:string;firstname:string;lastname:string;price:double;rating:double;title:string;year:int" };
        yield return new object[] { @"sortfor(/authors, lastname),asc", true, @"dateofbirth:DateTime;displayname:string;firstname:string;lastname:string;price:double;rating:double;title:string;year:int" };
        yield return new object[] { @"sortfor(tags, name),asc", true, @"dateofbirth:DateTime;displayname:string;firstname:string;lastname:string;price:double;rating:double;title:string;year:int" };
        yield return new object[] { @"count(authors),desc", true, @"dateofbirth:DateTime;displayname:string;firstname:string;lastname:string;price:double;rating:double;title:string;year:int" };
        yield return new object[] { @"sortfor(authors, lastname),asc,sortfor(authors, firstname),desc", true, @"dateofbirth:DateTime;displayname:string;firstname:string;lastname:string;price:double;rating:double;title:string;year:int" };
        yield return new object[] { @"sortfor(authors, lastname)", false, @"dateofbirth:DateTime;displayname:string;firstname:string;lastname:string;price:double;rating:double;title:string;year:int" };
        yield return new object[] { @"contains(name,""war"")", false, @"age:int;foo:string;name:string" };
        yield return new object[] { @"endswith(name,""hn"")", false, @"age:int;foo:string;name:string" };
        yield return new object[] { @"eq(substr(name,1,2),""Jo"")", false, @"age:int;foo:string;name:string" };
        yield return new object[] { @"eq(left(name,1),""J"")", false, @"age:int;foo:string;name:string" };
        yield return new object[] { @"eq(right(name,1),""n"")", false, @"age:int;foo:string;name:string" };
        yield return new object[] { @"eq(concat(name,"" "",foo),""John Doe"")", false, @"age:int;foo:string;name:string" };
        yield return new object[] { @"eq(lower(name),""john"")", false, @"age:int;foo:string;name:string" };
        yield return new object[] { @"eq(upper(name),""JOHN"")", false, @"age:int;foo:string;name:string" };
        yield return new object[] { @"eq(trim(name),""John"")", false, @"age:int;foo:string;name:string" };
        yield return new object[] { @"eq(ltrim(name),""John"")", false, @"age:int;foo:string;name:string" };
        yield return new object[] { @"eq(rtrim(name),""John"")", false, @"age:int;foo:string;name:string" };
        yield return new object[] { @"eq(len(name),4)", false, @"age:int;foo:string;name:string" };
        yield return new object[] { @"eq(replace(name,""J"",""K""),""Kohn"")", false, @"age:int;foo:string;name:string" };
        yield return new object[] { @"gt(indexof(name,""o""),0)", false, @"age:int;foo:string;name:string" };
        yield return new object[] { @"eq(concat(name),""x"")", false, @"age:int;foo:string;name:string" };
        yield return new object[] { @"contains(name)", false, @"age:int;foo:string;name:string" };
    }
}
