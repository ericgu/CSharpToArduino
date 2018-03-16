using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CSharpToArduino
{
    class CodeConverter
    {
        Outputter _output;

        public Outputter Convert(string filename)
        {
            string source = File.ReadAllText(filename);

            CSharpParseOptions parseOptions = new CSharpParseOptions(LanguageVersion.CSharp7, DocumentationMode.Parse);

            var tree = CSharpSyntaxTree.ParseText(source, parseOptions);

            _output = new Outputter();
            ParseCompilationUnit((CompilationUnitSyntax)tree.GetRoot());

            return _output;
        }

        private void ParseCompilationUnit(CompilationUnitSyntax compilationUnitSyntax)
        {
            foreach (var aUsing in compilationUnitSyntax.Usings)
            {

            }

            foreach (var member in compilationUnitSyntax.Members)
            {
                NamespaceDeclarationSyntax namespaceDeclarationSyntax = member as NamespaceDeclarationSyntax;

                if (namespaceDeclarationSyntax != null)
                {
                    ParseNamespaceDeclaration(namespaceDeclarationSyntax);
                }
            }
        }

        private void ParseNamespaceDeclaration(NamespaceDeclarationSyntax namespaceDeclarationSyntax)
        {
            _output.HandleLeadingTrivia(namespaceDeclarationSyntax);

            foreach (var member in namespaceDeclarationSyntax.Members)
            {
                ClassDeclarationSyntax classDeclarationSyntax = member as ClassDeclarationSyntax;

                if (namespaceDeclarationSyntax != null)
                {
                    ParseClassDeclaration(classDeclarationSyntax);
                }
            }

            _output.HandleTrailingTrivia(namespaceDeclarationSyntax);
        }

        private void ParseClassDeclaration(ClassDeclarationSyntax classDeclarationSyntax)
        {
            _output.HandleLeadingTrivia(classDeclarationSyntax);

            var childNodes = classDeclarationSyntax.ChildNodes();

            foreach (var member in classDeclarationSyntax.Members)
            {
                _output.HandleLeadingTrivia(member);
                bool handled = false;

                MethodDeclarationSyntax methodDeclarationSyntax = member as MethodDeclarationSyntax;

                if (methodDeclarationSyntax != null)
                {
                    ParseMethodDeclaration(methodDeclarationSyntax);
                    handled = true;
                }

                FieldDeclarationSyntax fieldDeclarationSyntax = member as FieldDeclarationSyntax;
                if (fieldDeclarationSyntax != null)
                {
                    foreach (var modifier in fieldDeclarationSyntax.Modifiers)
                    {
                        HandleTokenAndTrivia(modifier);
                    }

                    _output.HandleLeadingTrivia(fieldDeclarationSyntax.Declaration.Type);
                    _output.Add(fieldDeclarationSyntax.Declaration.Type.ToString());
                    _output.HandleTrailingTrivia(fieldDeclarationSyntax.Declaration.Type);

                    foreach (var variable in fieldDeclarationSyntax.Declaration.Variables)
                    {
                        HandleTokenAndTrivia(variable.Identifier);
                        if (variable.Initializer != null)
                        {
                            HandleTokenAndTrivia(variable.Initializer.EqualsToken);
                            ParseExpressionSyntax(variable.Initializer.Value);
                        }
                    }

                    ParseOperatorToken(fieldDeclarationSyntax.SemicolonToken);

                    handled = true;
                }

                if (!handled)
                {
                    int k = 12;
                }

                _output.HandleTrailingTrivia(member);
            }

            _output.HandleTrailingTrivia(classDeclarationSyntax);
        }

        private void ParseMethodDeclaration(MethodDeclarationSyntax methodDeclarationSyntax)
        {
            _output.HandleLeadingTrivia(methodDeclarationSyntax);

            string methodDeclaration = methodDeclarationSyntax.ReturnType.ToString() + " " + methodDeclarationSyntax.Identifier;
            _output.Add(methodDeclaration);

            ParseParameterListSyntax(methodDeclarationSyntax.ParameterList);

            BlockSyntax blockSyntax = methodDeclarationSyntax.ChildNodes().Skip(2).First() as BlockSyntax;
            HandleStatementSyntax(blockSyntax);

            _output.HandleTrailingTrivia(methodDeclarationSyntax);
        }

        private void HandleTokenAndTrivia(SyntaxToken syntaxToken)
        {
            _output.HandleLeadingTrivia(syntaxToken);
            _output.Add(syntaxToken.ToString());
            _output.HandleTrailingTrivia(syntaxToken);
        }

        private void ParseParameterListSyntax(ParameterListSyntax parameterListSyntax)
        {
            HandleTokenAndTrivia(parameterListSyntax.OpenParenToken);

            List<string> parameters = new List<string>();
            foreach (var parameter in parameterListSyntax.Parameters)
            {
                parameters.Add(parameter.Type + " " + parameter.Identifier);
            }
            _output.Add(String.Join(", ", parameters));

            HandleTokenAndTrivia(parameterListSyntax.CloseParenToken);
        }

        private void HandleStatementSyntax(StatementSyntax statementSyntax)
        {
            BlockSyntax blockSyntax = statementSyntax as BlockSyntax;
            if (blockSyntax != null)
            {
                _output.HandleLeadingTrivia(blockSyntax);

                HandleTokenAndTrivia(blockSyntax.OpenBraceToken);

                foreach (StatementSyntax childStatementSyntax in blockSyntax.ChildNodes())
                {
                    HandleStatementSyntax(childStatementSyntax);
                }

                HandleTokenAndTrivia(blockSyntax.CloseBraceToken);

                _output.HandleTrailingTrivia(blockSyntax);
                return;
            }

            ExpressionStatementSyntax expressionStatementSyntax = statementSyntax as ExpressionStatementSyntax;
            if (expressionStatementSyntax != null)
            {
                ParseExpressionStatementDeclaration(expressionStatementSyntax);
                return;
            }

            IfStatementSyntax ifStatementSyntax = statementSyntax as IfStatementSyntax;
            if (ifStatementSyntax != null)
            {
                _output.HandleLeadingTrivia(statementSyntax);

                ParseOperatorToken(ifStatementSyntax.IfKeyword);
                ParseOperatorToken(ifStatementSyntax.OpenParenToken);
                ParseExpressionSyntax(ifStatementSyntax.Condition);
                ParseOperatorToken(ifStatementSyntax.CloseParenToken);

                HandleStatementSyntax(ifStatementSyntax.Statement);

                if (ifStatementSyntax.Else != null)
                {
                    _output.HandleLeadingTrivia(ifStatementSyntax.Else);

                    _output.Add(ifStatementSyntax.Else.ElseKeyword.ValueText);
                    HandleStatementSyntax(ifStatementSyntax.Else.Statement);

                    _output.HandleTrailingTrivia(ifStatementSyntax.Else);
                }

                _output.HandleTrailingTrivia(statementSyntax);
                return;
            }

            LocalDeclarationStatementSyntax localDeclarationStatementSyntax = statementSyntax as LocalDeclarationStatementSyntax;
            if (localDeclarationStatementSyntax != null)
            {
                _output.HandleLeadingTrivia(localDeclarationStatementSyntax);

                _output.HandleLeadingTrivia(localDeclarationStatementSyntax.Declaration.Type);
                _output.Add(localDeclarationStatementSyntax.Declaration.Type.ToString());
                _output.HandleTrailingTrivia(localDeclarationStatementSyntax.Declaration.Type);

                foreach (var variable in localDeclarationStatementSyntax.Declaration.Variables)
                {
                    HandleTokenAndTrivia(variable.Identifier);
                    if (variable.Initializer != null)
                    {
                        HandleTokenAndTrivia(variable.Initializer.EqualsToken);
                        ParseExpressionSyntax(variable.Initializer.Value);
                    }
                }

                ParseOperatorToken(localDeclarationStatementSyntax.SemicolonToken);

                _output.HandleTrailingTrivia(localDeclarationStatementSyntax);
                return;
            }

            int k = 12;
        }

        private void ParseExpressionStatementDeclaration(ExpressionStatementSyntax expressionStatementSyntax)
        {
            _output.HandleLeadingTrivia(expressionStatementSyntax);

            foreach (var childNode in expressionStatementSyntax.ChildNodes())
            {
                _output.HandleLeadingTrivia(childNode);

                InvocationExpressionSyntax invocationExpressionSyntax = childNode as InvocationExpressionSyntax;
                if (invocationExpressionSyntax != null)
                {
                    ParseInvocationExpressionDeclaration(invocationExpressionSyntax);
                }

                AssignmentExpressionSyntax alignmentExpressionSyntax = childNode as AssignmentExpressionSyntax;
                if (alignmentExpressionSyntax != null)
                {
                    ParseExpressionSyntax(alignmentExpressionSyntax.Left);
                    ParseOperatorToken(alignmentExpressionSyntax.OperatorToken);
                    ParseExpressionSyntax(alignmentExpressionSyntax.Right);
                }

                _output.HandleTrailingTrivia(childNode);
            }

            ParseOperatorToken(expressionStatementSyntax.SemicolonToken);
            _output.HandleTrailingTrivia(expressionStatementSyntax);
        }

        private void ParseOperatorToken(SyntaxToken operatorToken )
        {
            _output.HandleLeadingTrivia(operatorToken);
            _output.Add(operatorToken.ValueText);
            _output.HandleTrailingTrivia(operatorToken);
        }

        private void ParseInvocationExpressionDeclaration(InvocationExpressionSyntax invocationExpressionSyntax)
        {
            _output.HandleLeadingTrivia(invocationExpressionSyntax);

            bool handled = false;
            IdentifierNameSyntax identifierNameSyntax = invocationExpressionSyntax.Expression as IdentifierNameSyntax;

            if (identifierNameSyntax != null)
            {
                _output.HandleLeadingTrivia(identifierNameSyntax);

                _output.Add(identifierNameSyntax.Identifier.Text);

                _output.HandleTrailingTrivia(identifierNameSyntax);
                handled = true;
            }

            MemberAccessExpressionSyntax memberAccessExpressionSyntax = invocationExpressionSyntax.Expression as MemberAccessExpressionSyntax;

            if (memberAccessExpressionSyntax != null)
            {
                _output.HandleLeadingTrivia(memberAccessExpressionSyntax);

                string expression = memberAccessExpressionSyntax.Expression.ToString() + memberAccessExpressionSyntax.OperatorToken + memberAccessExpressionSyntax.Name;
                _output.Add(expression);
                _output.HandleTrailingTrivia(memberAccessExpressionSyntax);
                handled = true;
            }

            if (!handled)
            {
                int k = 15;
            }

            ParseOperatorToken(invocationExpressionSyntax.ArgumentList.OpenParenToken);

            bool first = true;
            foreach (ArgumentSyntax argument in invocationExpressionSyntax.ArgumentList.Arguments)
            {
                if (!first)
                {
                    _output.Add(", ");
                }
                first = false;

                ParseExpressionSyntax(argument.Expression);
            }

            ParseOperatorToken(invocationExpressionSyntax.ArgumentList.CloseParenToken);

            _output.HandleTrailingTrivia(invocationExpressionSyntax);
        }

        private void ParseExpressionSyntax(ExpressionSyntax expression)
        {
            _output.HandleLeadingTrivia(expression);
            bool handled = false;

            IdentifierNameSyntax identifierNameSyntax = expression as IdentifierNameSyntax;
            if (identifierNameSyntax != null)
            {
                _output.HandleLeadingTrivia(identifierNameSyntax.Identifier);
                _output.Add(identifierNameSyntax.Identifier.Text);
                _output.HandleTrailingTrivia(identifierNameSyntax.Identifier);
                handled = true;
            }

            LiteralExpressionSyntax literalExpressionSyntax = expression as LiteralExpressionSyntax;
            if (literalExpressionSyntax != null)
            {
                _output.Add(literalExpressionSyntax.Token.ValueText);
                handled = true;
            }

            BinaryExpressionSyntax binaryExpressionSyntax = expression as BinaryExpressionSyntax;
            if (binaryExpressionSyntax != null)
            {
                ParseExpressionSyntax(binaryExpressionSyntax.Left);
                ParseOperatorToken(binaryExpressionSyntax.OperatorToken);
                ParseExpressionSyntax(binaryExpressionSyntax.Right);
                handled = true;
            }

            PrefixUnaryExpressionSyntax prefixUnaryExpressionSyntax = expression as PrefixUnaryExpressionSyntax;
            if (prefixUnaryExpressionSyntax != null)
            {
                ParseOperatorToken(prefixUnaryExpressionSyntax.OperatorToken);
                ParseExpressionSyntax(prefixUnaryExpressionSyntax.Operand);
                handled = true;
            }

            InvocationExpressionSyntax invocationExpressionSyntax = expression as InvocationExpressionSyntax;
            if (invocationExpressionSyntax != null)
            {
                ParseInvocationExpressionDeclaration(invocationExpressionSyntax);
                handled = true;
            }

            if (!handled)
            {
                int k = 12;
            }

            _output.HandleTrailingTrivia(expression);
        }

#if fred
        private void ParseMethodDeclaration(List<string>  node)
        {
            if (node.GetType() == typeof(MethodDeclarationSyntax))
            {
                MethodDeclarationSyntax methodDeclarationSyntax = (MethodDeclarationSyntax)node;

                string methodDeclaration = methodDeclarationSyntax.ReturnType.ToString() + " " + methodDeclarationSyntax.Identifier;

                _output.Add(methodDeclaration);

            }
        }

        public void HandleMethodDeclarationSyntax(SyntaxNode node)
        {
            MethodDeclarationSyntax methodDeclarationSyntax = node as MethodDeclarationSyntax;

            if (methodDeclarationSyntax != null)
            {
                int k = 12;
            }

        }
#endif
    }
}
