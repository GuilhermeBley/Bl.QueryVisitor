namespace Bl.QueryVisitor.MySql;

public interface IItemTranslator
{
    /// <summary>
    /// Determines whether the item should be translated or not.
    /// </summary>
    /// <returns></returns>
    bool ShouldTranslate();
    object? TransformItem(object? input);
}
