namespace W3CValidator.Css;

public sealed class Issues : IIssues
{
    public IList<ErrorsGroup> ErrorsGroupsList { get; set; } = [];
    public IList<WarningsGroup> WarningsGroupsList { get; set; } = [];

    public IEnumerable<IErrorsGroup> ErrorsGroups => ErrorsGroupsList;
    public IEnumerable<IWarningsGroup> WarningsGroups => WarningsGroupsList;
}