using System.Globalization;

namespace Expresso.Parsing.Policies.Syntax;

internal sealed partial class PolicySyntaxParser
{
    private void Arguments(PatternAst call)
    {
        if (Current.Is(")")) Fail("P07", "Empty argument lists are not allowed.");
        if (Take("..."))
        {
            if (Take(","))
            {
                call.Shape = ArgumentShape.Exists;
                call.Children.Add(Pattern()); Expect(","); Expect("...");
                if (Current.Is("[")) Fail("P04", "Exists patterns cannot have bounds.");
            }
            else { call.Shape = ArgumentShape.Tail; Bounds(call); }
            return;
        }
        while (true)
        {
            var argument = Pattern();
            if (Take("..."))
            {
                call.Shape = ArgumentShape.Tail; call.Tail = argument; Bounds(call);
                if (Current.Is(",")) Fail("P05", "A repeated tail must be last.");
                return;
            }
            call.Children.Add(argument);
            if (!Take(",")) return;
            if (Take("..."))
            {
                call.Shape = ArgumentShape.Tail; Bounds(call);
                if (Current.Is(",")) Fail("P05", "A bare tail must be last.");
                return;
            }
        }
    }

    private void Bounds(PatternAst call)
    {
        if (!Take("[")) return;
        call.ExplicitBounds = true;
        if (Take(",")) { call.Minimum = 0; call.Maximum = Count(); }
        else
        {
            call.Minimum = Count();
            if (Take(",")) { if (!Current.Is("]")) call.Maximum = Count(); }
            else call.Maximum = call.Minimum;
        }
        if (!Take("]")) Fail("P04", "Malformed repetition bounds.");
        if (call.Maximum.HasValue && call.Minimum > call.Maximum)
            throw Error(call.Token, "S10", "The minimum repetition exceeds the maximum.");
    }
    private int Count()
    {
        var token = Current;
        if (token.Kind != TokenKind.Number || token.Text.Any(c => c < '0' || c > '9'))
            Fail("P04", "A repetition count must contain digits only.");
        if (!int.TryParse(token.Text, NumberStyles.None, CultureInfo.InvariantCulture, out int count))
            Fail("P09", "A repetition count must fit in Int32.");
        _index++; return count;
    }
}
