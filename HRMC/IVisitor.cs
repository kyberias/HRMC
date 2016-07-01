namespace HRMC
{
    public interface IVisitor
    {
        void VisitProgram(Program program);
        void VisitVariableDeclaration(VariableDeclaration vardec);
        void VisitStatement(Statement stmt);
        void VisitExpression(ExpressionBase stmt);
        void VisitVariableExpression(VariableExpression expr);
        void VisitFunctionExpression(FunctionExpression expr);
        void VisitIfStatement(IfStatement stmt);
        void VisitWhileStatement(WhileStatement stmt);
        void VisitBlockStatement(BlockStatement stmt);
        void VisitAssignment(Assignment stmt);
        //void VisitBinaryExpression(BinaryExpression expr);
        void VisitExpressionStatement(ExpressionStatement stmt);
        void VisitLogicalExpression(LogicalExpression expr);
        void VisitEqualityExpression(EqualityExpression expr);
        void VisitOperationExpression(OperationExpression expr);
        void Visit(ConstantLiteralExpression expr);
    }
}
