using Domain.Rules;
using Domain.Rules.Parameters;
using Domain.Sessions;
using Domain.ValueObjects;

namespace Tests.Data;

public static class RulesData
{
    public const string SeparateGroupsName = "Групи окремо";

    public static Rule SeparateGroups(ExamSessionId sessionId)
        => Rule.New(RuleId.New(), sessionId, new SeparateByAttributeParameters(AttributeKeys.Group, RuleScope.Room, 1), true, RuleConstraints.DefaultWeight, true, SeparateGroupsName);

    public static Rule FillFromFront(ExamSessionId sessionId)
        => Rule.New(RuleId.New(), sessionId, new FillFromFrontParameters(), false, RuleConstraints.DefaultWeight, true, null);
}