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
        Expression _expression;

        public Outputter Convert(string filename)
        {
            string source = File.ReadAllText(filename);

            CSharpParseOptions parseOptions = new CSharpParseOptions(LanguageVersion.CSharp7, DocumentationMode.Parse);

            var tree = CSharpSyntaxTree.ParseText(source, parseOptions);

            _output = new Outputter();
            _expression = new Expression(_output);
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

                    ParseTypeAndVariables(fieldDeclarationSyntax.Declaration.Type, fieldDeclarationSyntax.Declaration.Variables);

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

        private void ParseTypeAndVariables(TypeSyntax type, SeparatedSyntaxList<VariableDeclaratorSyntax> variables)
        {
            string typeString = type.ToString();
            string variableSuffix = String.Empty;

            int index = typeString.IndexOf("[");
            if (index != -1)
            {
                variableSuffix = typeString.Substring(index);
                typeString = typeString.Substring(0, index);
            }

            _output.HandleLeadingTrivia(type);
            _output.Add(typeString);
            _output.HandleTrailingTrivia(type);

            foreach (var variable in variables)
            {
                _output.HandleLeadingTrivia(variable.Identifier);
                _output.Add(variable.Identifier.ToString() + variableSuffix);
                _output.HandleTrailingTrivia(variable.Identifier);

                if (variable.Initializer != null)
                {
                    HandleTokenAndTrivia(variable.Initializer.EqualsToken);
                    _expression.ParseExpressionSyntax(variable.Initializer.Value);
                }
            }
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

        public void HandleTokenAndTrivia(SyntaxToken syntaxToken)
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
                _expression.ParseExpressionSyntax(ifStatementSyntax.Condition);
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
                        _expression.ParseExpressionSyntax(variable.Initializer.Value);
                    }
                }

                ParseOperatorToken(localDeclarationStatementSyntax.SemicolonToken);

                _output.HandleTrailingTrivia(localDeclarationStatementSyntax);
                return;
            }

            ForStatementSyntax forStatementSyntax = statementSyntax as ForStatementSyntax;
            if (forStatementSyntax != null)
            {
                HandleTokenAndTrivia(forStatementSyntax.ForKeyword);
                HandleTokenAndTrivia(forStatementSyntax.OpenParenToken);
                ParseTypeAndVariables(forStatementSyntax.Declaration.Type, forStatementSyntax.Declaration.Variables);

                HandleTokenAndTrivia(forStatementSyntax.FirstSemicolonToken);
                _expression.ParseExpressionSyntax(forStatementSyntax.Condition);
                HandleTokenAndTrivia(forStatementSyntax.SecondSemicolonToken);
                foreach (var incrementer in forStatementSyntax.Incrementors)
                {
                    _expression.ParseExpressionSyntax(incrementer);
                }
                HandleTokenAndTrivia(forStatementSyntax.CloseParenToken);
                HandleStatementSyntax(forStatementSyntax.Statement);
                return;
            }

            SwitchStatementSyntax switchStatementSyntax = statementSyntax as SwitchStatementSyntax;
            if (switchStatementSyntax != null)
            {
                HandleTokenAndTrivia(switchStatementSyntax.SwitchKeyword);

                HandleTokenAndTrivia(switchStatementSyntax.OpenParenToken);
                _expression.ParseExpressionSyntax(switchStatementSyntax.Expression);
                HandleTokenAndTrivia(switchStatementSyntax.CloseParenToken);

                HandleTokenAndTrivia(switchStatementSyntax.OpenBraceToken);

                foreach (SwitchSectionSyntax switchSectionSyntax in switchStatementSyntax.Sections)
                {
                    foreach (SwitchLabelSyntax switchLabelSyntax in switchSectionSyntax.Labels)
                    {
                        HandleTokenAndTrivia(switchLabelSyntax.Keyword);

                        CaseSwitchLabelSyntax caseSwitchLabelSyntax = switchLabelSyntax as CaseSwitchLabelSyntax;
                        if (caseSwitchLabelSyntax != null)
                        {
                            _expression.ParseExpressionSyntax(caseSwitchLabelSyntax.Value);
                        }

                        HandleTokenAndTrivia(switchLabelSyntax.ColonToken);
                    }

                    foreach (StatementSyntax childStatementSyntax in switchSectionSyntax.Statements)
                    {
                        HandleStatementSyntax(childStatementSyntax);
                    }
                }

                HandleTokenAndTrivia(switchStatementSyntax.CloseBraceToken);

                return;
            }

            BreakStatementSyntax breakStatementSyntax = statementSyntax as BreakStatementSyntax;
            if (breakStatementSyntax != null)
            {
                HandleTokenAndTrivia(breakStatementSyntax.BreakKeyword);
                HandleTokenAndTrivia(breakStatementSyntax.SemicolonToken);
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
                    _expression.ParseExpressionSyntax(alignmentExpressionSyntax.Left);
                    ParseOperatorToken(alignmentExpressionSyntax.OperatorToken);
                    _expression.ParseExpressionSyntax(alignmentExpressionSyntax.Right);
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
            ParseInvocationExpressionDeclaration(_output, _expression, invocationExpressionSyntax);
        }

        public static void ParseInvocationExpressionDeclaration(Outputter output, Expression expression, InvocationExpressionSyntax invocationExpressionSyntax)
        {
            output.HandleLeadingTrivia(invocationExpressionSyntax);

            bool handled = false;
            IdentifierNameSyntax identifierNameSyntax = invocationExpressionSyntax.Expression as IdentifierNameSyntax;

            if (identifierNameSyntax != null)
            {
                output.HandleLeadingTrivia(identifierNameSyntax);

                output.Add(identifierNameSyntax.Identifier.Text);

                output.HandleTrailingTrivia(identifierNameSyntax);
                handled = true;
            }

            MemberAccessExpressionSyntax memberAccessExpressionSyntax = invocationExpressionSyntax.Expression as MemberAccessExpressionSyntax;

            if (memberAccessExpressionSyntax != null)
            {
                output.HandleLeadingTrivia(memberAccessExpressionSyntax);

                string expressionString = memberAccessExpressionSyntax.Expression.ToString() + memberAccessExpressionSyntax.OperatorToken + memberAccessExpressionSyntax.Name;
                output.Add(expressionString);
                output.HandleTrailingTrivia(memberAccessExpressionSyntax);
                handled = true;
            }

            if (!handled)
            {
                int k = 15;
            }

            ParseOperatorToken(output, invocationExpressionSyntax.ArgumentList.OpenParenToken);

            bool first = true;
            foreach (ArgumentSyntax argument in invocationExpressionSyntax.ArgumentList.Arguments)
            {
                if (!first)
                {
                    output.Add(", ");
                }
                first = false;

                expression.ParseExpressionSyntax(argument.Expression);
            }

            ParseOperatorToken(output, invocationExpressionSyntax.ArgumentList.CloseParenToken);

            output.HandleTrailingTrivia(invocationExpressionSyntax);
        }

        private static void ParseOperatorToken(Outputter output, SyntaxToken operatorToken)
        {
            output.HandleLeadingTrivia(operatorToken);
            output.Add(operatorToken.ValueText);
            output.HandleTrailingTrivia(operatorToken);
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
