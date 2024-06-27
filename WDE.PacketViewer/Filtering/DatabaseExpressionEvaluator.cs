using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using WDE.PacketViewer.Filtering.Antlr;
using WDE.PacketViewer.ViewModels;
using WowPacketParser.Proto;

namespace WDE.PacketViewer.Filtering
{
    public class DatabaseExpressionEvaluator
    {
        private readonly IPacketViewModelStore store;
        private SyntaxLexer lexer;
        private CommonTokenStream tokens;
        private SyntaxParser parser;
        private ExpressionVisitor visitor;
        private ExpressionTree.ITreeNode root;
        
        public DatabaseExpressionEvaluator(string expression, UniversalGuid playerGuid, IPacketViewModelStore store)
        {
            this.store = store;
            lexer = new SyntaxLexer(new AntlrInputStream(expression));
            tokens = new CommonTokenStream(lexer);
            parser = new SyntaxParser(tokens);
            parser.BuildParseTree = true;
            parser.RemoveErrorListeners();

            visitor = new ExpressionVisitor(new IsPacketSpecificPlayerProcessor(playerGuid), store);
            root = new ExpressionTree.TreeBuilder().Visit(parser.expr());
        }

        public FilterOutput Evaluate(PacketViewModel entity)
        {
            visitor.SetContext(entity);
            return visitor.Visit(root);
        }
    }
}