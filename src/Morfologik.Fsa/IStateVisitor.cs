namespace Morfologik.Fsa
{
    /// <summary>
    /// State visitor.
    /// </summary>
    /// <seealso cref="FSA.VisitInPostOrder{T}(T)"/>
    /// <seealso cref="FSA.VisitInPreOrder{T}(T)"/>
    public interface IStateVisitor
    {
        /// <summary>
        /// 
        /// </summary>
        bool Accept(int state);
    }
}
