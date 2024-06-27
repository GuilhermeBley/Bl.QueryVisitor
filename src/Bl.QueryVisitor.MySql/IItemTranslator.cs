namespace Bl.QueryVisitor.MySql;

public interface IItemTranslator
{
    object? TransformItem(object? input);
}
